using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TcpClientLibrary;
using TSI.UtilityClasses;

namespace TSI.MXNet
{
    public sealed class CBox
    {
        // ─── Singleton ────────────────────────────────────────────────────────────

        private static CBox _instance;
        private static readonly object _instanceLock = new object();

        public static CBox Instance { get { return _instance; } }

        // ─── Private fields ───────────────────────────────────────────────────────

        private bool            _debug;
        private TcpClientAsync  _asyncClient;
        private string          _ipaddress;
        private ushort          _port;

        // ─── Public device lists ──────────────────────────────────────────────────

        public List<MxnetDecoder> mxnetDecoders { get; private set; }
        public List<MxnetEncoder> mxnetEncoders { get; private set; }

        // ─── Events ───────────────────────────────────────────────────────────────

        public event EventHandler<ResponseErrorEventArgs>    ResponseErrorEvent;
        public event EventHandler<Rs232ResponseEventArgs>    Rs232ResponseEvent;
        public event EventHandler<DeviceListUpdateEventArgs> DeviceListUpdateEvent;
        public event EventHandler<SimpleResponseEventArgs>   SimpleResponseEvent;
        public event EventHandler<RouteEventArgs>            RouteEvent;
        public event EventHandler<DecoderInfoUpdateEventArgs> DecoderInfoUpdateEvent;
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusEvent;

        /// <summary>
        /// Fired after the device list response has been fully parsed and both
        /// mxnetEncoders and mxnetDecoders are populated and sorted.
        /// This is the correct point for SIMPL+ to call MxnetDecoderClass.Initialize()
        /// and MxnetEncoderClass.Initialize() — device IDs are valid at this point.
        /// Previously this fired at the start of InitializeClient() before any TCP
        /// connection existed, which was semantically wrong (Phase 4 / Option B fix).
        /// </summary>
        public event EventHandler InitializationCompleteEvent;

        // ─── Properties ───────────────────────────────────────────────────────────

        public string IPAddress
        {
            get { return _ipaddress; }
            set { _ipaddress = value; }
        }

        public ushort Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public ushort Debug
        {
            get { return _debug ? (ushort)1 : (ushort)0; }
            set
            {
                _debug = value == 1;
                DebugUtility.SetDebugState(_debug);
            }
        }

        // ─── Constructor ──────────────────────────────────────────────────────────

        public CBox()
        {
            // Thread-safe singleton guard. On a multi-core CP4/RMC3, two near-simultaneous
            // program starts could otherwise race through the null check.
            lock (_instanceLock)
            {
                if (_instance != null)
                    throw new Exception("CBox is a singleton and has already been instantiated.");

                _instance = this;
            }

            mxnetDecoders = new List<MxnetDecoder>();
            mxnetEncoders = new List<MxnetEncoder>();
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        public void InitializeClient()
        {
            try
            {
                // Clean up any previous client instance
                if (_asyncClient != null)
                {
                    _asyncClient.ResponseReceived      -= Client_ResponseReceived;
                    _asyncClient.ConnectionStatusChanged -= Client_ConnectionChange;
                    _asyncClient.Dispose();
                    _asyncClient = null;
                }

                // Inject the debug delegate so TcpClientAsync never needs to touch CBox.
                // The lambda captures _debug by reference to the field, so runtime
                // changes to CBox.Debug are immediately reflected in TCP logging.
                _asyncClient = new TcpClientAsync(IPAddress, Port, () => _debug);
                _asyncClient.ResponseReceived       += Client_ResponseReceived;
                _asyncClient.ConnectionStatusChanged += Client_ConnectionChange;
                _asyncClient.Initialize();

                // Queue the devicelist request. It will be sent once TCP connects.
                // InitializationCompleteEvent is NOT fired here — it fires later in
                // ParseResponse() once the device list response comes back and both
                // encoder/decoder lists are fully populated.
                QueueCommand("config get devicelist\n");
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint($"Error in InitializeClient: {ex.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
                DebugUtility.DebugPrint($"Error in InitializeClient: {ex.StackTrace}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        // ─── Command queuing ──────────────────────────────────────────────────────

        public void QueueCommand(string cmd)
        {
            if (_asyncClient == null)
            {
                DebugUtility.DebugPrint("TcpClientAsync is not initialized. Call InitializeClient() first.", "Cbox", DebugUtility.DebugLevels.ERROR);
                return;
            }

            _asyncClient.QueueCommand(cmd);
        }

        // ─── Response parsing ─────────────────────────────────────────────────────

        public void SplitResponse(string response)
        {
            string[] rspArray = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                foreach (string s in rspArray)
                    ParseResponse(s);
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Exception in SplitResponse: {e.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
                DebugUtility.DebugPrint($"{e.StackTrace}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        public void ParseResponse(string response)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new CustomResponseConverter() }
                };

                BaseResponse baseResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);

                // ── DeviceListResponse ────────────────────────────────────────────
                if (baseResponse is DeviceListResponse detailedResponse)
                {
                    if (detailedResponse.Info != null && detailedResponse.Info.Any())
                    {
                        mxnetDecoders.Clear();
                        mxnetEncoders.Clear();

                        foreach (var kvp in detailedResponse.Info)
                        {
                            Device device = kvp.Value;

                            // Guard against devices that come back with no model name
                            if (string.IsNullOrEmpty(device.Modelname))
                            {
                                DebugUtility.DebugPrint($"Device {kvp.Key} has no Modelname — skipped.", "Cbox", DebugUtility.DebugLevels.WARN);
                                continue;
                            }

                            if (device.Modelname.Contains("1G-R") ||
                                device.Modelname.Contains("1G-D") ||
                                device.Modelname.Contains("DV2"))   
                            {
                                mxnetDecoders.Add(new MxnetDecoder
                                {
                                    id        = device.Id,
                                    ip        = device.Ip,
                                    mac       = device.Mac,
                                    modelname = device.Modelname,
                                    streamOn  = device.Stream == "on" ? (ushort)1 : (ushort)0,
                                    chV      = device.ChV ?? string.Empty //chV denotes the current video channel on decoders, used for matching to encoders in the SIMPL+ wrapper
                                });
                            }
                            else if (device.Modelname.Contains("1G-T")       ||
                                     device.Modelname.Contains("IP-1G-WP-T") ||
                                     device.Modelname.Contains("EV2"))        
                            {
                                mxnetEncoders.Add(new MxnetEncoder
                                {
                                    id        = device.Id,
                                    ip        = device.Ip,
                                    mac       = device.Mac,
                                    modelname = device.Modelname,
                                    ch        = device.Ch ?? string.Empty   //ch denotes the current channel number on encoders, used for matching to decoders in the SIMPL+ wrapper
                                });
                            }
                        }

                        // Sort both lists — devices must be named "01-Decoder", "02-Decoder", or some other sortable format
                        mxnetDecoders = mxnetDecoders.OrderBy(d => d.id).ToList();
                        mxnetEncoders = mxnetEncoders.OrderBy(e => e.id).ToList();

                        // Build the ID string arrays for SIMPL+ event args
                        List<string> encIdStrings = new List<string>();
                        foreach (MxnetEncoder enc in mxnetEncoders)
                            encIdStrings.Add(enc.id);


                        List<string> decIdStrings = new List<string>();
                        foreach (MxnetDecoder dec in mxnetDecoders)
                        {
                            decIdStrings.Add(dec.id);
                            MxnetEncoder matchedEncoder = mxnetEncoders.FirstOrDefault(e => !string.IsNullOrEmpty(e.ch) && e.ch == dec.chV); //figure out the current stream encoder by matching the channel numbers

                            dec.streamSource = matchedEncoder != null ? matchedEncoder.id : string.Empty;

                            // Fire per-decoder info update so MxnetDecoderClass instances
                            // can capture their initial stream state. Both lists are fully
                            // populated and sorted before any of these events fire.
                            DecoderInfoUpdateEvent?.Invoke(this, new DecoderInfoUpdateEventArgs { Decoder = dec });
                        }

                        DeviceListUpdateEventArgs args = new DeviceListUpdateEventArgs
                        {
                            Encoders     = encIdStrings.ToArray(),
                            Decoders     = decIdStrings.ToArray(),
                            EncoderCount = (ushort)encIdStrings.Count,
                            DecoderCount = (ushort)decIdStrings.Count
                        };

                        DeviceListUpdateEvent?.Invoke(this, args);

                        DebugUtility.DebugPrint($"Initialization complete. {mxnetEncoders.Count} encoders, {mxnetDecoders.Count} decoders.", "Cbox", DebugUtility.DebugLevels.NOTICE);
                        InitializationCompleteEvent?.Invoke(this, EventArgs.Empty);
                    }
                }

                // ── SimpleInfoResponse ────────────────────────────────────────────
                else if (baseResponse is SimpleInfoResponse simpleResponse)
                {
                    SimpleResponseEventArgs args = new SimpleResponseEventArgs
                    {
                        Cmd  = simpleResponse.Cmd,
                        Info = simpleResponse.Info,
                        Code = simpleResponse.Code.HasValue ? (ushort)simpleResponse.Code.Value : (ushort)0
                    };

                    SimpleResponseEvent?.Invoke(this, args);

                    // Route and stream-state responses all flow through ParseRouteResponse.
                    // The call-site guard was extended to include stream on/off so those
                    // branches inside ParseRouteResponse are no longer dead code.
                    if (simpleResponse.Cmd.Contains("matrix aset")                      ||
                        simpleResponse.Cmd.Contains("config set device videopathdisable") ||
                        simpleResponse.Cmd.Contains("device stream"))
                    {
                        ParseRouteResponse(simpleResponse.Cmd);
                    }
                }

                // ── ErrorResponse ─────────────────────────────────────────────────
                else if (baseResponse is ErrorResponse errorRsp)
                {
                    ResponseErrorEventArgs args = new ResponseErrorEventArgs
                    {
                        Error = errorRsp.Error,
                        Cmd   = errorRsp.Cmd,
                        Code  = errorRsp.Code.HasValue ? (ushort)errorRsp.Code.Value : (ushort)0
                    };

                    ResponseErrorEvent?.Invoke(this, args);
                }

                // ── DetailedInfoReportResponse ────────────────────────────────────
                else if (baseResponse is DetailedInfoReportResponse reportRsp)
                {
                    SimpleResponseEventArgs args = new SimpleResponseEventArgs
                    {
                        Cmd  = reportRsp.Cmd,
                        Info = reportRsp.Info,
                        Code = reportRsp.Code.HasValue ? (ushort)reportRsp.Code.Value : (ushort)0,
                        Id   = reportRsp.Id
                    };

                    SimpleResponseEvent?.Invoke(this, args);
                }

                else
                {
                    DebugUtility.DebugPrint("Response not matched to any monitored pattern.", "Cbox", DebugUtility.DebugLevels.WARN);
                }
            }
            catch (JsonSerializationException jse)
            {
                DebugUtility.DebugPrint($"Cannot deserialize JSON: {jse.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint($"Error in ParseResponse: {ex.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        public void ParseRouteResponse(string rsp)
        {
            try
            {
                if (rsp.Contains("matrix aset"))
                {
                    // Format: "matrix aset :<type> <encId> <decId>"
                    string[] parts = rsp.Split(' ');
                    string enc = parts[3];
                    string dec = parts[4];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);
                    int encIndex = mxnetEncoders.FindIndex(x => x.id == enc);

                    if (decIndex != -1 && encIndex != -1)
                    {
                        mxnetDecoders[decIndex].streamSource = mxnetEncoders[encIndex].id;

                        RouteEvent?.Invoke(this, new RouteEventArgs
                        {
                            DecoderId   = mxnetDecoders[decIndex].id,
                            DestIndex   = (ushort)decIndex,
                            SourceIndex = (ushort)encIndex,
                            StreamOn    = 1,
                            SourceId    = mxnetEncoders[encIndex].id
                        });
                    }
                    else
                    {
                        DebugUtility.DebugPrint($"ParseRouteResponse: could not match enc '{enc}' or dec '{dec}' in lists.", "Cbox", DebugUtility.DebugLevels.WARN);
                    }
                }

                else if (rsp.Contains("config set device videopathdisable"))
                {
                    // Format: "config set device videopathdisable <decId>"
                    string[] parts = rsp.Split(' ');
                    string dec = parts[4];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        mxnetDecoders[decIndex].streamSource = string.Empty;

                        RouteEvent?.Invoke(this, new RouteEventArgs
                        {
                            DecoderId   = dec,
                            DestIndex   = (ushort)decIndex,
                            SourceIndex = 0,
                            StreamOn    = 1,
                            SourceId    = string.Empty
                        });
                    }
                }

                else if (rsp.Contains("device stream off"))
                {
                    // Format: "config set device stream off <decId>"
                    string[] parts = rsp.Split(' ');
                    string dec = parts[5];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        mxnetDecoders[decIndex].streamOn = 0;

                        RouteEvent?.Invoke(this, new RouteEventArgs
                        {
                            DecoderId = dec,
                            DestIndex = (ushort)decIndex,
                            StreamOn  = 0
                        });
                    }
                }

                else if (rsp.Contains("device stream on"))
                {
                    // Format: "config set device stream on <decId>"
                    string[] parts = rsp.Split(' ');
                    string dec = parts[5];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        mxnetDecoders[decIndex].streamOn = 1;

                        RouteEvent?.Invoke(this, new RouteEventArgs
                        {
                            DecoderId = dec,
                            DestIndex = (ushort)decIndex,
                            StreamOn  = 1
                        });
                    }
                }
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Error in ParseRouteResponse: {e.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        // ─── Routing commands ─────────────────────────────────────────────────────

        /// <summary>
        /// Routes a source encoder to a destination decoder by 1-based index.
        /// Indices are 1-based to match SIMPL+ analog signal conventions.
        /// Valid range: 1 .. Count (inclusive).
        /// </summary>
        public void Switch(string type, ushort sourceIndex, ushort destIndex)
        {
            try
            {
                // sourceIndex and destIndex are 1-based. Valid range: 1..Count.
                if (sourceIndex >= 1 && sourceIndex <= mxnetEncoders.Count &&
                    destIndex   >= 1 && destIndex   <= mxnetDecoders.Count)
                {
                    string cmd = $"matrix aset :{type} {mxnetEncoders[sourceIndex - 1].id} {mxnetDecoders[destIndex - 1].id}\n";
                    QueueCommand(cmd);
                }
                else
                {
                    DebugUtility.DebugPrint($"Switch: index out of range (src={sourceIndex}, dst={destIndex}, encoders={mxnetEncoders.Count}, decoders={mxnetDecoders.Count})", "Cbox", DebugUtility.DebugLevels.WARN);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint($"Error in Switch: {ex.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        /// <summary>
        /// Routes a source encoder to a destination decoder by device ID strings.
        /// </summary>
        public void Switch(string type, string sourceID, string destID)
        {
            string cmd = $"matrix aset :{type} {sourceID} {destID}\n";
            QueueCommand(cmd);
        }

        // ─── Video path disable ───────────────────────────────────────────────────

        /// <summary>
        /// Disables the video path on a decoder by 1-based index.
        /// </summary>
        public void VideoPathDisable(ushort destIndex)
        {
            // destIndex is 1-based
            if (destIndex >= 1 && destIndex <= mxnetDecoders.Count)
            {
                string cmd = $"config set device videopathdisable {mxnetDecoders[destIndex - 1].id}\n";
                QueueCommand(cmd);
            }
            else
            {
                DebugUtility.DebugPrint($"VideoPathDisable: index {destIndex} out of range (decoders={mxnetDecoders.Count})", "Cbox", DebugUtility.DebugLevels.WARN);
            }
        }

        /// <summary>
        /// Disables the video path on a decoder by device ID string.
        /// </summary>
        public void VideoPathDisable(string decoderId)
        {
            string cmd = $"config set device videopathdisable {decoderId}\n";
            QueueCommand(cmd);
        }

        // ─── Stream state / RS-232 commands ──────────────────────────────────────

        public void SetStreamStatus(string decoderID, ushort s)
        {
            string state = s == 1 ? "on" : "off";
            string cmd   = $"config set device stream {state} {decoderID}\n";
            QueueCommand(cmd);
        }

        public void SendRs232Command(string decoderId, string rs232cmd, string hexOrAscii)
        {
            try
            {
                string cmd = $"config set device rs232 {hexOrAscii} {rs232cmd} {decoderId}\n";
                QueueCommand(cmd);
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint($"Error in SendRs232Command: {ex.Message}", "Cbox", DebugUtility.DebugLevels.ERROR);
            }
        }

        // ─── TCP client event handlers ────────────────────────────────────────────

        private void Client_ConnectionChange(object sender, bool e)
        {
            ConnectionStatusEvent?.Invoke(this, new ConnectionStatusEventArgs
            {
                IsConnected = e ? (ushort)1 : (ushort)0
            });
        }

        private void Client_ResponseReceived(object sender, string response)
        {
            DebugUtility.DebugPrint($"Received: {response}", "Cbox", DebugUtility.DebugLevels.OFF);
            SplitResponse(response);
        }
    }
}
