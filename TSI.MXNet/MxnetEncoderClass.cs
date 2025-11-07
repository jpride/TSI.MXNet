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