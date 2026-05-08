using System;
using Crestron.SimplSharp;
using TSI.UtilityClasses;

namespace TSI.MXNet
{
    public class MxnetEncoderClass
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when an error response references this encoder's ID.</summary>
        public event EventHandler<ResponseErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Reserved for future use — encoders currently have no push-state updates
        /// from the panel, but the event is kept for API symmetry with MxnetDecoderClass.
        /// </summary>
        public event EventHandler<RouteEventArgs> DeviceInfoUpdate;

        /// <summary>
        /// Fired at the end of Initialize() to confirm subscription is complete.
        /// </summary>
        public event EventHandler Initialized;

        // ─── Properties ───────────────────────────────────────────────────────────

        public string LastError    { get; private set; }
        public string LastErrorCmd { get; private set; }

        private string _myEncoderId;

        // ─── Constructor ──────────────────────────────────────────────────────────

        public MxnetEncoderClass() { }

        // ─── Initialization ───────────────────────────────────────────────────────

        /// <summary>
        /// Call this from SIMPL+ in response to CBox.InitializationCompleteEvent —
        /// not before, because mxnetEncoders won't be populated yet.
        /// </summary>
        public void Initialize(string encoderId)
        {
            _myEncoderId = encoderId;

            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;

            Initialized?.Invoke(this, EventArgs.Empty);
        }

        // ─── Teardown ─────────────────────────────────────────────────────────────

        public void Dispose()
        {
            CBox.Instance.ResponseErrorEvent -= CBox_ResponseErrorEvent;
        }

        // ─── CBox event handlers ──────────────────────────────────────────────────

        private void CBox_ResponseErrorEvent(object sender, ResponseErrorEventArgs args)
        {
            if (!args.Cmd.Contains(_myEncoderId)) return;

            LastError    = args.Error;
            LastErrorCmd = args.Cmd;

            ErrorReceived?.Invoke(this, args);
        }
    }
}
