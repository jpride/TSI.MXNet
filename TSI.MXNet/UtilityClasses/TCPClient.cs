using System;
using System.Net.Sockets;
using Crestron.SimplSharp;


namespace TSI.Sockets
{
    public class SimpleTcpClient
    {
        private string _ipaddress;
        private ushort _port;
        private int _buffersize = 8048;
        private TcpClient _client;
        

        public bool isInitialized;

        public event EventHandler<EventArgs> ClientConnected;
        public event EventHandler<EventArgs> ClientDisconnected;

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

        public SimpleTcpClient(string ipaddress, ushort port)
        { 
            //assign private fields for this class
            IPAddress = ipaddress;
            Port = port;    

            //instantiate new client
            _client = new TcpClient(ipaddress, port);
            _client.ReceiveTimeout = 3000;

            //set isInitialized var to true
            isInitialized = true;
            CrestronConsole.PrintLine($"Client Initialized");
        }

        public string SendCommand(string command)
        {
            try
            {

                if (_client != null && _client.Client != null)
                {
                    if (!_client.Client.Connected)
                    {
                        _client.Connect(IPAddress, Port);
                    }

                    if (_client.Client.Connected)
                    {
                        ClientConnected?.Invoke(this, EventArgs.Empty);
                    }

                }
                               
                //convert command to send into Bytestream
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(command);


                //create a network stream
                NetworkStream ns = _client.GetStream();


                //write to network stream
                ns.Write(data, 0, data.Length);


                //Buffer to store the response bytes
                data = new Byte[_buffersize]; //the size here is important if your device repsonds with hugely variable chunks of text

                //define response string
                String responseData = String.Empty;

 
                //read from nework stream
                Int32 bytes = ns.Read(data, 0, data.Length);


                //convert response data bytestream to string
                responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

                //print to console
                //CrestronConsole.PrintLine($"Client Repsonse Data: {responseData}");

                //close everything
                //ns.Close();

                return responseData;
            }
            catch (InvalidCastException icEx)
            {
                ErrorLog.Error($"Sendcommand: Invalid Cast Exception. {icEx.Source}\n{icEx.InnerException}");
                return String.Empty;
            }
            catch (Exception ex)
            {
                ClientDisconnected?.Invoke(this, EventArgs.Empty);
                ErrorLog.Error($"Error in SendCommand(): {ex.Message}");
                return String.Empty;
            }
        }

        public void DisconnectClient()
        {
            _client.Close();
            if (!_client.Connected)
            {
                ClientDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

       
    }
}
