using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Crestron.SimplSharp;
using Independentsoft.Exchange;
using Newtonsoft.Json;
using TcpClientLibrary;
using TSI.UtilityClasses;


namespace TSI.MXNet
{
    public sealed class CBox
    {

        private static CBox _instance;
        public static CBox Instance {get { return _instance; }}



        private bool _debug;
        private TcpClientAsync _asyncClient;
        private string _ipaddress;
        private ushort _port;

        internal static readonly object _listLock = new object();

        public List<MxnetDecoder> mxnetDecoders { get; private set;}
        public List<MxnetEncoder> mxnetEncoders { get; private set; }

        public event EventHandler<ResponseErrorEventArgs> ResponseErrorEvent;
        public event EventHandler<rs232ResponseEventArgs> Rs232ResponseEvent;
        public event EventHandler<DeviceListUpdateEventArgs> DeviceListUpdateEvent;
        public event EventHandler<SimpleResponseEventArgs> SimpleResponseEvent;
        public event EventHandler<RouteEventArgs> RouteEvent;
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusEvent;

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
            set 
            {
                _debug = value == 1;
                DebugUtility.DebugPrint(_debug, $"Debug is {_debug}");
            }
        }



        public CBox()
        {
            if (_instance == null)
            {                 
                _instance = this;
                CrestronConsole.PrintLine($"CBox Singleton Instance has been set");
            }
            else
            {
                throw new Exception("CBox is a singleton class and has already been instantiated.");
            }

            CrestronConsole.PrintLine($"CBox Constructor");
            mxnetDecoders = new List<MxnetDecoder>();
            mxnetEncoders = new List<MxnetEncoder>();
        }

        public void InitializeClient()
        {
            try
            {
                _asyncClient = new TcpClientAsync(IPAddress, Port);
                _asyncClient.ResponseReceived += Client_ResponseReceived;
                _asyncClient.ConnectionStatusChanged += Client_ConnectionChange;
                _asyncClient.Initialize();
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint(_debug, $"Error in InitializeClient - {ex.Message}");
                DebugUtility.DebugPrint(_debug, $"Error in InitializeClient - {ex.StackTrace}");
            }
        }

        private void Client_ConnectionChange(object sender, bool e)
        {
            DebugUtility.DebugPrint(_debug, $"Connection Status: " + e);
            
            ConnectionStatusEventArgs args = new ConnectionStatusEventArgs
            {
                IsConnected = e ? (ushort)1 : (ushort)0
            };

            ConnectionStatusEvent?.Invoke(this, args);
        }

        private void Client_ResponseReceived(object sender, string response)
        {
            DebugUtility.DebugPrint(_debug, $"Received: " + response);
            SplitResponse(response);
        }

        public void QueueCommand(string cmd)
        {
            _asyncClient.QueueCommand(cmd);
        }

        public void SplitResponse(string response)
        {

            string[] rspArray = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                foreach (string s in rspArray)
                {
                    ParseResponse(s);
                }
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint(_debug, $"Exception in SplitResponse: {e.Message}\n");
                DebugUtility.DebugPrint(_debug, $"{e.StackTrace}\n");
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

                if (baseResponse is DeviceListResponse detailedResponse)
                {
                    if (detailedResponse.Info != null && detailedResponse.Info.Any())
                    {
                        CrestronConsole.PrintLine($"CBox: Parsing DeviceListResponse");
                        DeviceListUpdateEventArgs args = new DeviceListUpdateEventArgs();                        

                        lock (_listLock)
                        {
                            mxnetDecoders.Clear();
                            mxnetEncoders.Clear();

                            foreach (var kvp in detailedResponse.Info)
                            {
                                string deviceId = kvp.Key;
                                Device device = kvp.Value;

                                if (device.Modelname == "AC-MXNET-1G-R" | device.Modelname == "AC-MXNET-1G-D")
                                {
                                    MxnetDecoder d = new MxnetDecoder
                                    {
                                        id = device.Id,
                                        ip = device.Ip,
                                        mac = device.Mac,
                                        modelname = device.Modelname,
                                    };
                                    CrestronConsole.PrintLine($"adding Decoder: {d.id} to 'mxnetDecoders' list");
                                    mxnetDecoders.Add(d);
                                }
                                else if (device.Modelname == "AC-MXNET-1G-T")
                                {
                                    MxnetEncoder e = new MxnetEncoder
                                    {
                                        id = device.Id,
                                        ip = device.Ip,
                                        mac = device.Mac,
                                        modelname = device.Modelname
                                    };
                                    CrestronConsole.PrintLine($"adding Encoder: {e.id} to 'mxnetEncoders' list");
                                    mxnetEncoders.Add(e);
                                }
                            }

                            mxnetDecoders = mxnetDecoders.OrderBy(d => d.id).ToList(); //Devices must be named with "01Decoder, 02 Decoder..."
                            mxnetEncoders = mxnetEncoders.OrderBy(d => d.id).ToList();

                        }

                        List<string> _encIdStrings = new List<string>();
                        foreach (MxnetEncoder e in mxnetEncoders)
                        {
                            CrestronConsole.PrintLine($"adding Encoder: {e.id} @ {mxnetEncoders.FindIndex(x => x.id == e.id)}");
                            _encIdStrings.Add(e.id);
                        }

                        List<string> _decIdStrings = new List<string>();
                        foreach (MxnetDecoder d in mxnetDecoders)
                        {
                            CrestronConsole.PrintLine($"adding Decoder: {d.id} @ {mxnetDecoders.FindIndex(x => x.id == d.id)}");
                            _decIdStrings.Add(d.id);
                        }

                        args.encoders = _encIdStrings.ToArray();
                        args.decoders = _decIdStrings.ToArray();

                        args.encoderCount = (ushort)_encIdStrings.Count;
                        args.decoderCount = (ushort)_decIdStrings.Count;

                        DeviceListUpdateEvent?.Invoke(this, args);
                    }
                }

                else if (baseResponse is SimpleInfoResponse simpleResponse)
                {
                    SimpleResponseEventArgs args = new SimpleResponseEventArgs
                    {
                        cmd = simpleResponse.Cmd,
                        info = simpleResponse.Info,
                        code = (ushort)simpleResponse.Code
                    };

                    SimpleResponseEvent?.Invoke(this, args);

                    DebugUtility.DebugPrint(_debug, $"SimpleResponse Cmd: {simpleResponse.Cmd}");

                    if ((simpleResponse.Cmd.Contains("matrix aset") || simpleResponse.Cmd.Contains("config set device videopathdisable")))
                    {
                        ParseRouteResponse(simpleResponse.Cmd);
                    }
                }

                else if (baseResponse is ErrorResponse errorRsp)
                {
                    ResponseErrorEventArgs args = new ResponseErrorEventArgs
                    {
                        Error = errorRsp.Error,
                        Cmd = errorRsp.Cmd,
                        Code = (ushort)errorRsp.Code
                    };

                    ResponseErrorEvent?.Invoke(this, args);
                }

                else if (baseResponse is DetailedInfoReportResponse reportRsp)
                {
                    SimpleResponseEventArgs args = new SimpleResponseEventArgs
                    {
                        cmd = reportRsp.Cmd,
                        info = reportRsp.Info,
                        code = (ushort)reportRsp.Code,
                        id = reportRsp.Id,

                    };

                    SimpleResponseEvent?.Invoke(this, args);
                }

                else
                {
                    DebugUtility.DebugPrint(_debug, "Response not matched to monitored pattern");
                }

            }
            catch (JsonSerializationException jse)
            {
                DebugUtility.DebugPrint(_debug, $"Cannot Deserialize JSON Object. Error: {jse.Message}");
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint(_debug, $"Error in ParseResponse: {ex.Message}");
            }

        }

        public void ParseRouteResponse(string rsp)
        {
            try
            {
                if (rsp.Contains("matrix aset"))
                {
                    string[] rspCmd = rsp.Split(' ');
                    string enc = rspCmd[3];
                    string dec = rspCmd[4];

                    int decIndex;
                    int encIndex;

                    lock (_listLock)
                    {
                        decIndex = mxnetDecoders.FindIndex(x => x.id == dec);
                        encIndex = mxnetEncoders.FindIndex(x => x.id == enc);
                    }

                    DebugUtility.DebugPrint(_debug, $"ParseRouteRepsonse - decIndex: {decIndex} | encIndex: {encIndex}\n");

                    if (decIndex != -1 && encIndex != -1)
                    {
                        mxnetDecoders[decIndex].streamSource = mxnetEncoders[encIndex].id;

                        RouteEventArgs args = new RouteEventArgs
                        {
                            DestIndex = (ushort)(decIndex),
                            SourceIndex = (ushort)(encIndex),
                            StreamOn = 1,
                            SourceId = mxnetEncoders[encIndex].id
                        };

                        DebugUtility.DebugPrint(_debug, $"Sending Route Event to SImpl+\n");
                        DebugUtility.DebugPrint(_debug, $"Dest[{args.DestIndex}] ==> {args.SourceIndex}\n");
                        DebugUtility.DebugPrint(_debug, $"StreamOn: {args.StreamOn == 1} | StreamSource: {args.SourceId}");

                        RouteEvent?.Invoke(this, args);
                    }
                }

                else if (rsp.Contains("config set device videopathdisable"))
                {
                    string[] rspCmd = rsp.Split(' ');
                    string dec = rspCmd[4];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        RouteEventArgs args = new RouteEventArgs
                        {
                            DecoderId = dec,
                            DestIndex = (ushort)(decIndex),
                            SourceIndex = 0,
                            StreamOn = 1,
                            SourceId = ""
                        };

                        RouteEvent?.Invoke(this, args);
                    }
                }
                else if (rsp.Contains("device stream off"))
                {
                    string[] rspCmd = rsp.Split(' ');
                    string dec = rspCmd[5];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        RouteEventArgs args = new RouteEventArgs
                        {
                            DecoderId = dec,
                            DestIndex = (ushort)decIndex,
                            StreamOn = 0
                        };

                        RouteEvent?.Invoke(this, args);
                    }

                }
                else if (rsp.Contains("device stream on"))
                {
                    string[] rspCmd = rsp.Split(' ');
                    string dec = rspCmd[5];

                    int decIndex = mxnetDecoders.FindIndex(x => x.id == dec);

                    if (decIndex != -1)
                    {
                        RouteEventArgs args = new RouteEventArgs
                        {
                            DecoderId = dec,
                            DestIndex = (ushort)decIndex,
                            StreamOn = 1
                        };

                        RouteEvent?.Invoke(this, args);
                    }
                }
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint(_debug, $"Error in ParseRouteResponse: {e.Message}");
            }

        }


        public void PrintLists()
        {
            CrestronConsole.PrintLine($"MxnetDecoders List:");
            foreach(MxnetDecoder d in mxnetDecoders)
            {
                CrestronConsole.PrintLine($"Decoder ID: {d.id} | IP: {d.ip} | MAC: {d.mac} | Model: {d.modelname}");
            }

            CrestronConsole.PrintLine($"MxnetEncoders List:");
            foreach (MxnetEncoder e in mxnetEncoders)
            {
                CrestronConsole.PrintLine($"Encoder ID: {e.id} | IP: {e.ip} | MAC: {e.mac} | Model: {e.modelname}");
            }

        }
        public void Switch(string type, ushort sourceIndex, ushort destIndex) //zero based
        {
            DebugUtility.DebugPrint(_debug, $"SourceIndex: {sourceIndex} | DestIndex: {destIndex}");

            try
            {
                lock (_listLock)
                {

                    if ((sourceIndex <= mxnetEncoders.Count) && (destIndex <= mxnetDecoders.Count))
                    {
                        string cmd = $"matrix aset :{type} {mxnetEncoders[sourceIndex - 1].id} {mxnetDecoders[destIndex - 1].id}\n";
                        QueueCommand(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtility.DebugPrint(_debug, $"Error in Switch (Ushort): {ex.Message}");
            }

        }

        public void Switch(string type, string sourceID, string destID)
        {
            string cmd = $"matrix aset :{type} {sourceID} {destID}\n";
            QueueCommand(cmd);
        }

        public void VideoPathDisable(ushort destIndex)
        {

            if (destIndex <= mxnetDecoders.Count)
            {
                string cmd = $"config set device videopathdisable {mxnetDecoders[destIndex - 1].id}\n";
                QueueCommand(cmd);
            }
        }

        public void SetStreamStatus(ushort destIndex, ushort s)
        {
            if (destIndex <= mxnetDecoders.Count)
            {
                string state = s == 1 ? "on" : "off";
                string cmd = $"config set device stream {state} {mxnetDecoders[destIndex - 1].id}";
                QueueCommand(cmd);
            }

        }
    }


}
