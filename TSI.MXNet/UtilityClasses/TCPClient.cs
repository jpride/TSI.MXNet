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
            this.IPAddress = ipaddress;
            this.Port = port;    

            //instantiate new client
            _client = new TcpClient(ipaddress, port);

            //set isInitialized var to true
            isInitialized = true;
            CrestronConsole.PrintLine($"Client Initialized");
        }


        public string SendCommand(string command)
        {
            try //this code is taken largely from https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=net-6.0 
            {
                /*Pseudo Code
                 * create a client at ipaddress and port
                 * convert command string to byte stream
                 * open netstream on client
                 * write commandbytes to netstream
                 * initilize response byte stream buffer
                 * create empty string for response string
                 * read bytes of response  on netstream
                 * convert response bytestream to string
                 * close netstream and client                 
                 */

                //create new client
                _client = new TcpClient(IPAddress, Port);

                //convert command to send into Bytestream
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(command);

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
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                //print to console
                CrestronConsole.PrintLine($"Client Repsonse Data: {responseData}");
                

                //close everything
                ns.Close();
                _client.Close();

                return responseData;    
            }
            catch (Exception ex)
            {
                ErrorLog.Error($"Error in SendCommand(): {ex.Message}");
                return String.Empty;
            }
        }

       
    }
}
