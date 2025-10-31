using System;
using System.IO;
using Crestron.SimplSharp;


namespace TSI.MXNet
{
    public class MxnetDecoderClass
    {
        public event EventHandler<RouteEventArgs> CurrentRouteChanged;
        public event EventHandler<ResponseErrorEventArgs> ErrorReceived;


        public ushort CurrentSourceIndex { get; private set; }
        public string CurrentSourceId { get; private set; }
        public ushort IsStreamOn { get; private set; }

        public string LastError { get; private set; }
        public string LastErrorCmd { get; private set; }

        
        private string _myDecoderId; 


        public MxnetDecoderClass()
        {

        }

        public void Initialize(string decoderId)
        {
            _myDecoderId = decoderId;

            CBox.Instance.RouteEvent += CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;
            CBox.Instance.DeviceListUpdateEvent += CBox_DeviceListUpdateEvent;

            CrestronConsole.PrintLine($"Decoder {_myDecoderId} Initialized");
        }



        private void CBox_DeviceListUpdateEvent(object sender, DeviceListUpdateEventArgs e)
        {
            //CrestronConsole.PrintLine($"Decoder has received a CBox event");
            //CrestronConsole.PrintLine($"CBox received a device list update.");
            //CrestronConsole.PrintLine($"Decoders: {e.decoderCount}");
            //CrestronConsole.PrintLine($"Encoders: {e.encoderCount}");
        }

        public void RequestVideoRoute(string switchType, ushort sourceIndex)
        {
            try
            {
                // Get the source ID from CBox's public list
                if (sourceIndex > 0 && sourceIndex <= CBox.Instance.mxnetEncoders.Count)
                {
                    string sourceId = CBox.Instance.mxnetEncoders[sourceIndex - 1].id;

                    // Call the CBox singleton to send the command
                    CBox.Instance.Switch(switchType, sourceId, _myDecoderId);
                }
                else
                {
                    CrestronConsole.PrintLine($"MxnetEncoders count: {CBox.Instance.mxnetEncoders.Count}");
                    CrestronConsole.PrintLine($"Invalid source index: {sourceIndex}");
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestVideoRoute: {e.Message}");
            }
        }


        private void CBox_RouteEvent(object sender, RouteEventArgs args)
        {
            //CrestronConsole.PrintLine($"Decoder {_myDecoderId} recieved route event from CBox");
            //CrestronConsole.PrintLine($"Route Event: {args.DecoderId} => {args.SourceId}");

            if (args.DecoderId == _myDecoderId)
            {
                CurrentSourceIndex = args.SourceIndex;
                CurrentSourceId = args.SourceId;
                IsStreamOn = args.StreamOn;

                CurrentRouteChanged?.Invoke(this, args);
            }
        }

        private void CBox_ResponseErrorEvent(object sender, ResponseErrorEventArgs args)
        {
            if (args.Cmd.Contains(_myDecoderId))
            {
                LastError = args.Error;
                LastErrorCmd = args.Cmd;

                ErrorReceived?.Invoke(this, args);
            }
        }

        /// <summary>
        /// S+ must call this when it is disposed
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from the CBox events to prevent memory leaks
            CBox.Instance.RouteEvent -= CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent -= CBox_ResponseErrorEvent;
        }
    }
}