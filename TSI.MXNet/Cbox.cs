using System;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using TSI.MXNet.JsonResponses;
using System.Linq;
using TcpClientLibrary;


namespace TSI.MXNet
{
    public class CBox
    {
        private TcpClientExample _asyncClient;

        private string _ipaddress;
        private ushort _port;

        private List<Device> _decoders = new List<Device>();
        private List<Device> _encoders = new List<Device>();

        public List<string> _decoderStrings = new List<string>();
        public List<string> _encoderStrings = new List<string>();

        private ushort _useRouting;
        public List<ushort> _routes = new List<ushort>();

        public event EventHandler<ResponseErrorEventArgs> ResponseErrorEvent;
        public event EventHandler<rs232ResponseEventArgs> Rs232ResponseEvent;
        public event EventHandler<DeviceListUpdateEventArgs> DeviceListUpdateEvent;
        public event EventHandler<SimpleResponseEventArgs> SimpleResponseEvent;
        public event EventHandler<RouteEventArgs> RouteEvent;

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

        public ushort UseRouting
        {
            get { return _useRouting; }
            set { _useRouting = value; }
        }

        public CBox()
        {

        }

        public void InitializeClient()
        {
            try
            {  
                _asyncClient = new TcpClientExample(IPAddress, Port);
                _asyncClient.ResponseReceived += Client_ResponseReceived;
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.Message}");
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.StackTrace}");
            }
        }

        private void Client_ResponseReceived(object sender, string response)
        {
            Console.WriteLine("Received: " + response);
            SplitResponse(response);
        }

        public void QueueCommand(string cmd)
        { 
            _asyncClient.QueueCommand(cmd);
        }

        public void SplitResponse(string response)
        {
            string[] rspArray = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            foreach(string s in rspArray)
            {
                //CrestronConsole.PrintLine($"Split Rsp: {i}:{s}");
                i++;

                ParseResponse(s);
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

                BaseResponse deviceListResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);
                BaseResponse simpleInfoResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);
                BaseResponse errorResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);
                BaseResponse detailedInfoReportResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);


                if (deviceListResponse is DeviceListResponse detailedResponse)
                {

                    DeviceListUpdateEventArgs args = new DeviceListUpdateEventArgs();

                    _decoders.Clear();
                    _encoders.Clear();
                    _encoderStrings.Clear();
                    _decoderStrings.Clear();

                    foreach (var kvp in detailedResponse.Info)
                    {
                        string deviceId = kvp.Key;
                        Device device = kvp.Value;

                        if (device.Modelname == "AC-MXNET-1G-R")
                        {
                            _decoders.Add(device);
                            _decoderStrings.Add(device.Id);
                            _routes.Add(0);
                        }
                        else if (device.Modelname == "AC-MXNET-1G-T")
                        {
                            _encoders.Add(device);
                            _encoderStrings.Add(device.Id);
                        }
                    }

                    _encoders = _encoders.OrderBy(d => d.Id).ToList();
                    _decoders = _decoders.OrderBy(d => d.Id).ToList();
                    _encoderStrings.Sort();
                    _decoderStrings.Sort();

                    args.encoders = _encoderStrings.ToArray();
                    args.decoders = _decoderStrings.ToArray();
                    args.decoderCount = (ushort)args.decoders.Length;
                    args.encoderCount = (ushort)args.encoders.Length;

                    DeviceListUpdateEvent?.Invoke(this, args);

                }

                else if (simpleInfoResponse is SimpleInfoResponse simpleResponse)
                {
                    SimpleResponseEventArgs args = new SimpleResponseEventArgs
                    { 
                        cmd = simpleResponse.Cmd,
                        info = simpleResponse.Info,
                        code = (int)simpleResponse.Code
                    };

                    SimpleResponseEvent?.Invoke(this, args);

                    if ((simpleResponse.Cmd.Contains("matrix aset") || simpleResponse.Cmd.Contains("config set device videopathdisable")) && (UseRouting == 1))
                    {
                        ParseRouteResponse(simpleResponse.Cmd);
                    }                  
                }

                else if (errorResponse is ErrorResponse errorRsp)
                {
                    ResponseErrorEventArgs args = new ResponseErrorEventArgs
                    {
                        error = errorRsp.Error,
                        cmd = errorRsp.Cmd,
                        code = (ushort)errorRsp.Code
                    };

                    ResponseErrorEvent?.Invoke(this, args);
                }

                else if (detailedInfoReportResponse is DetailedInfoReportResponse reportRsp)
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
                    CrestronConsole.PrintLine("Response not matched to monitored pattern");
                }

            }
            catch (JsonSerializationException jse)
            {
                CrestronConsole.PrintLine($"Cannot Deserialize JSON Object. Error: {jse.Message}");
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in ParseResponse: {ex.Message}");
            }
            
        }

        public void ParseRouteResponse(string rsp)
        {   
            if (rsp.Contains("matrix aset"))
            {
                string[] rspCmd = rsp.Split(' ');
                string enc = rspCmd[3];
                string dec = rspCmd[4];

                ushort decIndex = (ushort)_decoders.FindIndex(x => x.Id == dec);
                ushort encIndex = (ushort)_encoders.FindIndex(x => x.Id == enc);

                _routes[decIndex] = encIndex;

                RouteEventArgs args = new RouteEventArgs
                {
                    destIndex = (ushort)(decIndex + 1),
                    sourceIndex = (ushort)(encIndex + 1)
                };

                RouteEvent?.Invoke(this, args);
            }

            if (rsp.Contains("config set device videopathdisable"))
            {
                string[] rspCmd = rsp.Split(' ');
                string dec = rspCmd[4];

                ushort decIndex = (ushort)_decoders.FindIndex(x => x.Id == dec);
                ushort encIndex = 99;

                _routes[decIndex] = encIndex;

                RouteEventArgs args = new RouteEventArgs
                {
                    destIndex = (ushort)(decIndex + 1),
                    sourceIndex = encIndex
                };

                RouteEvent?.Invoke(this, args);
            }
        }

        public void Switch(string type,ushort sourceIndex, ushort destIndex)
        {
            try
            {              
                if (sourceIndex == 0 && (destIndex - 1) < _decoders.Count)
                {
                    string cmd = $"c s d videopathdisable {_decoders[destIndex - 1].Id}\n";
                    QueueCommand(cmd);
                }
                else if ((sourceIndex - 1) <= _encoders.Count && (destIndex - 1) <= _decoders.Count)               
                {
                    string cmd = $"matrix aset :{type} {_encoders[sourceIndex - 1].Id} {_decoders[destIndex - 1].Id}\n";
                    QueueCommand(cmd);
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in Switch (Ushort): {ex.Message}");
            }
            
        }

        public void Switch(string type, string sourceID, string destID)
        {
            string cmd = $"matrix aset :{type} {sourceID} {destID}\n";
            QueueCommand(cmd);
        }
    }

   
}
