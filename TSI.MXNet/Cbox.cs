using System;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using TSI.Sockets;
using TSI.FourSeries.CommandQueue;
using System.Collections.Generic;
using TSI.MXNet.JsonUtilities;
using System.Text;
using System.Linq;


namespace TSI.MXNet
{
    public class CBox
    {
        
        private SimpleTcpClient _tcpClient;
        private CommandQueue _queue;

        private string _ipaddress;
        private ushort _port;

        private List<Device> _decoders = new List<Device>();
        private List<Device> _encoders = new List<Device>();

        public List<string> _decoderStrings = new List<string>();
        public List<string> _encoderStrings = new List<string>();

        public List<ushort> _routes = new List<ushort>();

        public event EventHandler<ResponseErrorEventArgs> ResponseErrorEvent;
        public event EventHandler<rs232ResponseEventArgs> Rs232ResponseEvent;
        public event EventHandler<DeviceListUpdateEventArgs> DeviceListUpdateEvent;
        public event EventHandler<GeneralInfoEventArgs> GeneralResponseEvent;
        public event EventHandler<EventArgs> ClientConnectedEvent;
        public event EventHandler<EventArgs> ClientDisconnectedEvent;
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

        public CBox()
        {

        }


        public void InitializeClient()
        {
            try
            {   //create a queue and add the processqueueventcall eventhandler
                _queue = new CommandQueue();
                _queue.ProcessQueueEventCall += q_ProcessQueueEventCall;

                _tcpClient = new SimpleTcpClient(IPAddress, Port);
                _tcpClient.ClientConnected += OnClientConnected;
                _tcpClient.ClientDisconnected += OnClientDisconnected;
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.Message}");
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.StackTrace}");
            }
        }

        private void OnClientDisconnected(object sender, EventArgs e)
        {
            CrestronConsole.PrintLine($"Client Disconnected");
            ClientDisconnectedEvent?.Invoke(this, e);
        }

        public void OnClientConnected(object sender, EventArgs e)
        {
            CrestronConsole.PrintLine($"Client Connected");
            ClientConnectedEvent?.Invoke(this, e);
        }

        private void q_ProcessQueueEventCall(object sender, ProcessQueueEventArgs args)
        {
            TCPSendCommand(args.cmd);   
        }

        public void QueueCommand(string cmd)
        { 
            _queue.AddCommand(cmd);
            _queue.ProcessQueueEvent();
        }

        public void TCPSendCommand(string command)
        {
            if (!_tcpClient.isInitialized)
            { 
                InitializeClient(); 
            }

            string response = _tcpClient.SendCommand(command);
            ParseResponse(response ?? String.Empty);
        }

        //can be used from Simpl+ without utilizing the client

        public string[] SplitResponse(string response)
        {
            string[] rspArray = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            foreach(string s in rspArray)
            {
                CrestronConsole.PrintLine($"Split Rsp: {i}:{s}");
                i++;
            }

            return rspArray;
        }
        public void ParseResponse(string response)
        {
            //CrestronConsole.PrintLine($"response from cbox: {response}");

            string[] rsp = SplitResponse(response);
            CrestronConsole.PrintLine($"rspArray Size: {rsp.Length}");

            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                { 
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new CustomResponseConverter() }
                };

                BaseResponse deviceListResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);
                BaseResponse simpleInfoResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);
                BaseResponse errorResponse = JsonConvert.DeserializeObject<BaseResponse>(response,settings);
                BaseResponse reportResponse = JsonConvert.DeserializeObject<BaseResponse>(response, settings);

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
                    CrestronConsole.PrintLine($"*******Simple Response*******\n");
                    CrestronConsole.PrintLine($"Cmd: {simpleResponse.Cmd}");
                    CrestronConsole.PrintLine($"Info: {simpleResponse.Info}");
                    CrestronConsole.PrintLine($"Code: {simpleResponse.Code}");

                    ParseRouteResponse(simpleResponse.Cmd);
                    
                }

                else if (errorResponse is ErrorResponse errorRsp)
                {
                    CrestronConsole.PrintLine($"*******Error Response*******\n");
                    CrestronConsole.PrintLine($"Error: {errorRsp.Error}");
                    CrestronConsole.PrintLine($"Commmand: {errorRsp.Cmd}");
                    CrestronConsole.PrintLine($"Code: {errorRsp.Code}");

                    ResponseErrorEventArgs args = new ResponseErrorEventArgs
                    {
                        error = errorRsp.Error,
                        cmd = errorRsp.Cmd,
                        code = (ushort)errorRsp.Code
                    };

                    ResponseErrorEvent?.Invoke(this, args);
                }

                else if (reportResponse is ReportResponse reportRsp)
                {
                    CrestronConsole.PrintLine($"*******Report Response*******\n");
                    CrestronConsole.PrintLine($"Info: {reportRsp.Info}");
                    CrestronConsole.PrintLine($"Code: {reportRsp.Id}");
                    CrestronConsole.PrintLine($"Code: {reportRsp.Source}");
                    CrestronConsole.PrintLine($"Code: {reportRsp.Code}");
                    CrestronConsole.PrintLine($"Code: {reportRsp.Mac}");
                }

                else
                {
                    CrestronConsole.PrintLine(response);
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

                CrestronConsole.PrintLine($"{enc} routed to {dec}");

                ushort decIndex = (ushort)_decoders.FindIndex(x => x.Id == dec);
                ushort encIndex = (ushort)_encoders.FindIndex(x => x.Id == enc);
          
                _routes[decIndex] = encIndex;
                CrestronConsole.PrintLine($"route[{decIndex}]: {encIndex}");

                RouteEventArgs args = new RouteEventArgs
                {
                    destIndex = decIndex,
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
                    string cmd = $"config set device videopathdisable {_decoders[destIndex - 1].Id}\n";
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
