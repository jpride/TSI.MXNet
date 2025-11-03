using System;
using System.Diagnostics;
using Crestron.SimplSharp;
using TSI.UtilityClasses;


namespace TSI.MXNet
{
    public class MxnetDecoderClass
    {
        //Events
        public event EventHandler<RouteEventArgs> CurrentRouteChanged;
        public event EventHandler<ResponseErrorEventArgs> ErrorReceived;
        public event EventHandler<RouteEventArgs> DeviceInfoUpdate;
        public event EventHandler Initialized;

        //props and vars
        public ushort CurrentSourceIndex { get; private set; }
        public string CurrentSourceId { get; private set; }
        public ushort IsStreamOn { get; private set; }
        public string LastError { get; private set; }
        public string LastErrorCmd { get; private set; }
        private string _myDecoderId; 

        //methods
        public MxnetDecoderClass()
        {

        }

        public void Initialize(string decoderId)
        {
            _myDecoderId = decoderId;

            CBox.Instance.RouteEvent += CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;
            CBox.Instance.DecoderInfoUpdateEvent += CBox_DecoderUpdateEvent;

            Initialized?.Invoke(this, EventArgs.Empty);

        }

        private void CBox_DecoderUpdateEvent(object sender, DecoderInfoUpdateEventArgs e)
        {
            if (e.Decoder.id == _myDecoderId)
            {
                RouteEventArgs rArgs = new RouteEventArgs
                {
                    SourceId = e.Decoder.streamSource,
                    SourceIndex = (ushort)CBox.Instance.mxnetEncoders.FindIndex(x => x.id == e.Decoder.streamSource),
                    StreamOn = e.Decoder.streamOn
                };

                DeviceInfoUpdate?.Invoke(this, rArgs);
            }
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
                else //if sourceindex is 0 perhaps send videopathdisable?)
                {
                    DebugUtility.DebugPrint(CBox.Instance.Debug == 1, $"MxnetEncoders count: {CBox.Instance.mxnetEncoders.Count}");
                    DebugUtility.DebugPrint(CBox.Instance.Debug == 1, $"Invalid source index: {sourceIndex}");
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestVideoRoute: {e.Message}");
            }
        }

        public void RequestVideoPathDisable()
        {
            try
            {
                CBox.Instance.VideoPathDisable(_myDecoderId);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestVideoPathDisable: {e.Message}");
            }
        }

        public void RequestRs232CommandSend(string command, string HexOrAscii)
        {
            try
            {
                CBox.Instance.SendRs232Command(_myDecoderId, command, HexOrAscii);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestRs232CommandSend: {e.Message}");
            }
        }

        public void RequestStreamStatusChange(ushort OnOrOff)
        {
            try
            {
                CBox.Instance.SetStreamStatus(_myDecoderId, OnOrOff);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestStreamOn: {e.Message}");
            }
        }

        public void Dispose()
        {
            // Unsubscribe from the CBox events to prevent memory leaks
            CBox.Instance.RouteEvent -= CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent -= CBox_ResponseErrorEvent;
            //CBox.Instance.DeviceListUpdateEvent -= CBox_DeviceListUpdateEvent;
            CBox.Instance.DecoderInfoUpdateEvent -= CBox_DecoderUpdateEvent;
        }

        //Event handlers

        private void CBox_RouteEvent(object sender, RouteEventArgs args)
        {
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
    }
}