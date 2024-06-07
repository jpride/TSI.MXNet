﻿using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpClientLibrary
{
    public class TcpClientExample : IDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly ConcurrentQueue<string> _commandQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TcpClientExample(string ipAddress, int port)
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, port);
            _stream = _client.GetStream();
            _commandQueue = new ConcurrentQueue<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            StartSendingCommands();
            StartReceivingResponses();
        }

        public void QueueCommand(string command)
        {
            _commandQueue.Enqueue(command);
        }

        private async void StartSendingCommands()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_commandQueue.TryDequeue(out string command))
                {
                    byte[] data = Encoding.UTF8.GetBytes(command + Environment.NewLine);
                    await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
                    await Task.Delay(400); // 200 milliseconds delay between messages
                }
                else
                {
                    await Task.Delay(50); // Check for new commands every 50 milliseconds
                }
            }
        }

        private async void StartReceivingResponses()
        {
            byte[] buffer = new byte[65535];
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_stream.DataAvailable)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        OnResponseReceived(response);
                    }
                }
                await Task.Delay(50); // Check for new responses every 50 milliseconds
            }
        }

        protected virtual void OnResponseReceived(string response)
        {
            ResponseReceived?.Invoke(this, response);
        }

        public event EventHandler<string> ResponseReceived;

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _stream.Close();
            _client.Close();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
