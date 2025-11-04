using System;
using Crestron.SimplSharp;
using TSI.UtilityClasses;


namespace TSI.MXNet
{
    public class MxnetEncoderClass
    {
        //Events
        public event EventHandler<ResponseErrorEventArgs> ErrorReceived;
        public event EventHandler<RouteEventArgs> DeviceInfoUpdate;
        public event EventHandler Initialized;

        //props and vars

        public string LastError { get; private set; }
        public string LastErrorCmd { get; private set; }
        private string _myEncoderId; 

        //methods
        public MxnetEncoderClass()
        {

        }

        public void Initialize(string encoderId)
        {
            _myEncoderId = encoderId;

            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;

            Initialized?.Invoke(this, EventArgs.Empty);

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
                    CBox.Instance.Switch(switchType, sourceId, _myEncoderId);
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
                CBox.Instance.VideoPathDisable(_myEncoderId);
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
                CBox.Instance.SendRs232Command(_myEncoderId, command, HexOrAscii);
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
                CBox.Instance.SetStreamStatus(_myEncoderId, OnOrOff);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in RequestStreamOn: {e.Message}");
            }
        }

        public void Dispose()
        {
            // Unsubscribe from the CBox events to prevent memory leaks
            CBox.Instance.ResponseErrorEvent -= CBox_ResponseErrorEvent;
        }

        //Event handlers
        private void CBox_ResponseErrorEvent(object sender, ResponseErrorEventArgs args)
        {
            if (args.Cmd.Contains(_myEncoderId))
            {
                LastError = args.Error;
                LastErrorCmd = args.Cmd;

                ErrorReceived?.Invoke(this, args);
            }
        }
    }
}