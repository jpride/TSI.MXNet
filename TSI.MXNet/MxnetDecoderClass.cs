using System;
using System.IO;
using Crestron.SimplSharp;


namespace TSI.MXNet
{
    /// <summary>
    /// Manages a single Decoder.
    /// This class is instantiated by Simpl+ and acts as the "translator"
    /// between the CBox singleton and the S+ wrapper.
    /// </summary>
    public class MxnetDecoderClass
    {
        // --- S+ Feedback Events (S+ Compatible) ---
        public event EventHandler<RouteEventArgs> CurrentRouteChanged;
        public event EventHandler<ResponseErrorEventArgs> ErrorReceived;

        // --- S+ Getter Properties (S+ Compatible) ---
        public ushort CurrentSourceIndex { get; private set; }
        public string CurrentSourceId { get; private set; }
        public ushort IsStreamOn { get; private set; }

        public string LastError { get; private set; }
        public string LastErrorCmd { get; private set; }

        // --- Internal Properties ---
        private string _myDecoderId; // The ID of this specific decoder


        public MxnetDecoderClass()
        {

        }
        /// <summary>
        /// S+ must call this immediately after creation.
        /// </summary>
        public void Initialize(string decoderId)
        {
            _myDecoderId = decoderId;

            // Subscribe to the CBox singleton's C#-to-C# events
            CBox.Instance.RouteEvent += CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;
            CBox.Instance.DeviceListUpdateEvent += CBox_DeviceListUpdateEvent;

            CrestronConsole.PrintLine($"Decoder Initialized");
        }

        private void CBox_DeviceListUpdateEvent(object sender, DeviceListUpdateEventArgs e)
        {
            CrestronConsole.PrintLine($"Decoder has received a CBox event");
            CrestronConsole.PrintLine($"CBox received a device list update.");
            CrestronConsole.PrintLine($"Decoders: {e.decoderCount}");
            CrestronConsole.PrintLine($"Encoders: {e.encoderCount}");
        }

        public void RequestVideoRoute(string switchType, ushort sourceIndex)
        {
            CrestronConsole.PrintLine($"Decoder requested video switch!");


            try
            {
                lock (CBox._listLock)
                {
                    CBox.Instance.PrintLists();

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
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestVideoRoute: {e.Message}");
            }
        }

        // --- C# Event Handlers (Listening to CBox) ---

        private void CBox_RouteEvent(object sender, RouteEventArgs e)
        {
            CrestronConsole.PrintLine($"Decoder recieved route event");
            // ** This is the critical filter **
            // Only process this event if it's meant for THIS decoder instance
            if (e.DecoderId == _myDecoderId)
            {
                // 1. Set the S+ "getter" properties
                CurrentSourceIndex = e.SourceIndex;
                CurrentSourceId = e.SourceId;
                IsStreamOn = e.StreamOn;

                // 2. Fire the S+ compatible event
                CurrentRouteChanged?.Invoke(this, e);
            }
        }

        private void CBox_ResponseErrorEvent(object sender, ResponseErrorEventArgs e)
        {
            // Check if the error command was related to this decoder
            if (e.Cmd.Contains(_myDecoderId))
            {
                // 1. Set the S+ "getter" properties
                LastError = e.Error;
                LastErrorCmd = e.Cmd;

                // 2. Fire the S+ compatible event
                ErrorReceived?.Invoke(this, e);
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