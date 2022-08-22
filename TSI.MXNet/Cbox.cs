using System;
using Crestron.SimplSharp;
using System.Net.Sockets;
using Newtonsoft.Json;
using TSI.Sockets;
using TSI.FourSeries.CommandQueue;

namespace TSI.MXNet
{
    public class CBox
    {
        
        private SimpleTcpClient _tcpClient;
        private CommandQueue _queue;

        private string _ipaddress;
        private ushort _port;

        public event EventHandler<ResponseErrorEventArgs> ResponseErrorEvent;

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
            {
                _queue = new CommandQueue();
                _tcpClient = new SimpleTcpClient(IPAddress, Port);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.Message}");
                CrestronConsole.PrintLine($"Error in InitializeClient - {ex.StackTrace}");
            }
        }


        public void QueueCommand(string cmd)
        { 
            _queue.AddCommand(cmd);
            TCPSendCommand(_queue.ProcessQueue());
        }
        public void TCPSendCommand(string command)
        {
            if (!_tcpClient.isInitialized)
            { 
                InitializeClient(); 
            }

            string response = _tcpClient.SendCommand(command);
            ParseResponse(response);
        }

        public void ParseResponse(string response)
        {
            try
            {
                if (!String.IsNullOrEmpty(response))
                {
                    var rspJson = JsonConvert.DeserializeObject<JsonDataObject>(response);
                    
                    CrestronConsole.PrintLine($"Cmd: {rspJson.cmd}");
                    CrestronConsole.PrintLine($"Info: {rspJson.info}");
                    CrestronConsole.PrintLine($"Code: {rspJson.code}");

                    if (rspJson.code < 0)
                    {
                        ResponseErrorEventArgs args = new ResponseErrorEventArgs
                        {
                            ErrorMsg = rspJson.error,
                        };

                        ResponseErrorEvent?.Invoke(this, args);

                        CrestronConsole.PrintLine($"Error running command: \"{rspJson.cmd}\"");
                        CrestronConsole.PrintLine($"Error: {rspJson.error}");
                    }
                }
            }
            catch (JsonSerializationException)
            {
                CrestronConsole.PrintLine($"Cannot Deserialize JSON Object. Probably an issue with Info field.");
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error in ParseResponse: {ex.Message}");
            }
        }
    }
}
