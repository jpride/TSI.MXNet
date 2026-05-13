using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using TSI.UtilityClasses;

// NOTE: TSI.MXNet is intentionally NOT referenced here.
// TcpClientAsync is a general-purpose TCP utility. It must not reach into
// application-layer singletons (CBox). Debug state is injected via a delegate.

namespace TcpClientLibrary
{
    public class TcpClientAsync : IDisposable
    {
        // ─── Private fields ───────────────────────────────────────────────────────

        private TcpClient         _client;
        private NetworkStream     _stream;
        private readonly ConcurrentQueue<string> _commandQueue;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly string   _ipAddress;
        private readonly int      _port;

        // Injected debug flag — evaluated at call time so CBox.Debug changes
        // at runtime are reflected without re-constructing the client.
        private readonly Func<bool> _debugEnabled;

        private readonly int _dequeueingDelay         = 200;
        private readonly int _commandCheckDelay        = 50;
        private readonly int _responseCheckInterval    = 100;
        private readonly int _reconnectInterval        = 5000;
        private readonly int _connectionMonitorInterval = 3000;

        // ─── Public surface ───────────────────────────────────────────────────────

        public event EventHandler<string> ResponseReceived;
        public event EventHandler<bool>   ConnectionStatusChanged;

        public bool IsConnected { get; private set; }

        // ─── Construction ─────────────────────────────────────────────────────────

        /// <summary>
        /// </summary>
        /// <param name="ipAddress">Target device IP.</param>
        /// <param name="port">Target device TCP port.</param>
        /// <param name="debugEnabled">
        ///     Delegate evaluated each time a debug print is needed.
        ///     Pass <c>() => false</c> to disable. Typically wired to CBox._debug
        ///     via a lambda so runtime Debug toggle is reflected immediately.
        /// </param>
        public TcpClientAsync(string ipAddress, int port, Func<bool> debugEnabled = null)
        {
            _ipAddress    = ipAddress;
            _port         = port;
            _debugEnabled = debugEnabled ?? (() => false); // safe default: debug off
            _commandQueue = new ConcurrentQueue<string>();
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(ManageConnectionAsync, _cancellationTokenSource.Token);
        }

        private async Task ManageConnectionAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    await Task.Delay(_connectionMonitorInterval);
                    continue;
                }

                try
                {
                    DebugUtility.DebugPrint($"Attempting to connect to {_ipAddress}:{_port}...", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);

                    _client = new TcpClient();
                    await _client.ConnectAsync(_ipAddress, _port);
                    _stream = _client.GetStream();

                    IsConnected = true;
                    OnConnectionStatusChanged(true);

                    DebugUtility.DebugPrint("Connection successful.", "MxnetDecoderClass", DebugUtility.DebugLevels.NOTICE);

                    // All three tasks run concurrently for this connection lifetime.
                    // The first one to exit (disconnect / error) causes WhenAny to return,
                    // which falls through to the finally block for cleanup + reconnect.
                    var sendTask    = StartSendingCommandsAsync();
                    var receiveTask = StartReceivingResponsesAsync();
                    var monitorTask = MonitorConnectionAsync();

                    await Task.WhenAny(sendTask, receiveTask, monitorTask);
                }
                catch (Exception ex)
                {
                    DebugUtility.DebugPrint($"Connection failed: {ex.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                    OnConnectionStatusChanged(false);
                }
                finally
                {
                    await HandleDisconnectionAsync();

                    // Swallow OperationCanceledException so the cancellation path
                    // exits cleanly without throwing up through the task.
                    try
                    {
                        await Task.Delay(_reconnectInterval, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException) { }
                }
            }
        }

        // ─── Command queuing ──────────────────────────────────────────────────────

        public void QueueCommand(string command)
        {
            if (!string.IsNullOrEmpty(command))
                _commandQueue.Enqueue(command);
        }

        // ─── Private async loops ──────────────────────────────────────────────────

        private async Task StartSendingCommandsAsync()
        {
            while (IsConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_commandQueue.TryDequeue(out string command))
                    {
                        // Ensure consistent line termination
                        if (!(command.EndsWith("\r\n") || command.EndsWith("\n") || command.EndsWith("\r")))
                            command += "\r\n";

                        byte[] data = Encoding.UTF8.GetBytes(command);
                        await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
                        await Task.Delay(_dequeueingDelay);
                    }
                    else
                    {
                        await Task.Delay(_commandCheckDelay);
                    }
                }
                catch (IOException ioEx)
                {
                    DebugUtility.DebugPrint($"Send loop error (likely disconnect): {ioEx.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    DebugUtility.DebugPrint("Send loop stopped: client disposed.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
                    break;
                }
                catch (Exception e)
                {
                    DebugUtility.DebugPrint($"Send loop unexpected error: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                    // Non-fatal: log and continue unless it recurs
                }
            }
        }

        private async Task StartReceivingResponsesAsync()
        {
            var buffer = new byte[65535];

            while (IsConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_stream.DataAvailable)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);

                        if (bytesRead > 0)
                        {
                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            OnResponseReceived(response);
                        }
                        else
                        {
                            // Zero-byte read = graceful remote shutdown
                            DebugUtility.DebugPrint("Remote host closed the connection.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
                            break;
                        }
                    }

                    await Task.Delay(_responseCheckInterval);
                }
                catch (IOException ioEx)
                {
                    DebugUtility.DebugPrint($"Receive loop error (likely disconnect): {ioEx.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    DebugUtility.DebugPrint("Receive loop stopped: client disposed.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
                    break;
                }
                catch (Exception e)
                {
                    DebugUtility.DebugPrint($"Receive loop unexpected error: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                }
            }
        }

        private async Task MonitorConnectionAsync()
        {
            while (IsConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Poll(1, SelectRead) returns true when data is available OR the socket
                    // is closed. If Available == 0 alongside that, the socket is dead.
                    if (_client.Client.Poll(1, SelectMode.SelectRead) && _client.Client.Available == 0)
                    {
                        DebugUtility.DebugPrint("Connection monitor: dead socket detected.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
                        break;
                    }

                    await Task.Delay(_connectionMonitorInterval);
                }
                catch (Exception ex)
                {
                    DebugUtility.DebugPrint($"Connection monitor error: {ex.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
                    break;
                }
            }
        }

        // ─── Disconnection ────────────────────────────────────────────────────────

        private Task HandleDisconnectionAsync()
        {
            if (!IsConnected) return Task.CompletedTask;

            IsConnected = false;
            OnConnectionStatusChanged(false);

            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;

            DebugUtility.DebugPrint("Disconnected. Reconnect will be attempted.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
            return Task.CompletedTask;
        }

        // ─── Event raisers ────────────────────────────────────────────────────────

        protected virtual void OnResponseReceived(string response)
            => ResponseReceived?.Invoke(this, response);

        protected virtual void OnConnectionStatusChanged(bool status)
            => ConnectionStatusChanged?.Invoke(this, status);

        // ─── Teardown ─────────────────────────────────────────────────────────────

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            HandleDisconnectionAsync().Wait();
        }

        public void Dispose()
        {
            Disconnect();
            _cancellationTokenSource?.Dispose();
        }
    }
}
