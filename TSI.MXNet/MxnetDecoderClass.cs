using System;
using System.Linq;
using Crestron.SimplSharp;
using TSI.UtilityClasses;

namespace TSI.MXNet
{
    public class MxnetDecoderClass
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a confirmed route change arrives from the panel.</summary>
        public event EventHandler<RouteEventArgs>           CurrentRouteChanged;

        /// <summary>Fired when an error response references this decoder's ID.</summary>
        public event EventHandler<ResponseErrorEventArgs>   ErrorReceived;

        /// <summary>
        /// Fired during device-list initialization with the decoder's initial
        /// stream source and stream-on state.
        /// </summary>
        public event EventHandler<RouteEventArgs>           DeviceInfoUpdate;

        /// <summary>
        /// Fired at the end of Initialize() to confirm subscription is complete.
        /// </summary>
        public event EventHandler Initialized;

        // ─── Properties ───────────────────────────────────────────────────────────

        /// <summary>0-based index into CBox.mxnetEncoders of the current source.</summary>
        public ushort CurrentSourceIndex    { get; private set; }

        public string CurrentSourceId       { get; private set; }
        public ushort IsStreamOn            { get; private set; }
        public string LastError             { get; private set; }
        public string LastErrorCmd          { get; private set; }

        private string _myDecoderId;

        // ─── Constructor ──────────────────────────────────────────────────────────

        public MxnetDecoderClass() { }

        // ─── Initialization ───────────────────────────────────────────────────────

        /// <summary>
        /// Call this from SIMPL+ in response to CBox.InitializationCompleteEvent —
        /// not before, because mxnetDecoders/mxnetEncoders won't be populated yet.
        /// </summary>
        public void Initialize(string decoderId)
        {
            _myDecoderId = decoderId;

            CBox.Instance.RouteEvent += CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent += CBox_ResponseErrorEvent;
            CBox.Instance.DecoderInfoUpdateEvent += CBox_DecoderUpdateEvent;

            // At this point CBox.mxnetDecoders is already populated.
            // Look up our current state and fire DeviceInfoUpdate immediately
            // so the SIMPL+ wrapper gets initial Route_Fb and StreamOn_Fb
            // without needing a second device list request.
            MxnetDecoder myDecoder = CBox.Instance.mxnetDecoders.FirstOrDefault(d => d.id == _myDecoderId);

            if (myDecoder != null)
            {
                int encIndex = CBox.Instance.mxnetEncoders.FindIndex(x => x.id == myDecoder.streamSource);

                DeviceInfoUpdate?.Invoke(this, new RouteEventArgs
                {
                    SourceId = myDecoder.streamSource ?? string.Empty,
                    SourceIndex = encIndex >= 0 ? (ushort)encIndex : (ushort)0,
                    StreamOn = myDecoder.streamOn
                });
            }

            Initialized?.Invoke(this, EventArgs.Empty);
        }

        // ─── Public command methods ───────────────────────────────────────────────

        /// <summary>
        /// Request a video route. sourceIndex is 1-based (matches SIMPL+ analog).
        /// Passing 0 is treated as "no valid source" and logs a warning.
        /// </summary>
        public void RequestVideoRoute(string switchType, ushort sourceIndex)
        {
            try
            {
                if (sourceIndex >= 1 && sourceIndex <= CBox.Instance.mxnetEncoders.Count)
                {
                    string sourceId = CBox.Instance.mxnetEncoders[sourceIndex - 1].id;
                    CBox.Instance.Switch(switchType, sourceId, _myDecoderId);
                }
                else
                {
                    if (CBox.Instance.Debug == 1)
                        DebugUtility.DebugPrint($"RequestVideoRoute: invalid sourceIndex {sourceIndex} (encoders={CBox.Instance.mxnetEncoders.Count})", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
                }
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Error in RequestVideoRoute: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
            }
        }

        public void RequestVideoPathDisable()
        {
            try
            {
                CBox.Instance.VideoPathDisable(_myDecoderId);
                this.CurrentSourceIndex = 0; //added 5-11-26
                this.CurrentSourceId = String.Empty; //added 5-11-26

                CBox_RouteEvent(this, new RouteEventArgs
                {
                    SourceId = String.Empty,
                    SourceIndex = 0,
                });
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Error in RequestVideoPathDisable: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
            }
        }

        public void RequestRs232CommandSend(string command, string hexOrAscii)
        {
            try
            {
                CBox.Instance.SendRs232Command(_myDecoderId, command, hexOrAscii);
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Error in RequestRs232CommandSend: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
            }
        }

        public void RequestStreamStatusChange(ushort onOrOff)
        {
            try
            {
                CBox.Instance.SetStreamStatus(_myDecoderId, onOrOff);
            }
            catch (Exception e)
            {
                DebugUtility.DebugPrint($"Error in RequestStreamStatusChange: {e.Message}", "MxnetDecoderClass", DebugUtility.DebugLevels.ERROR);
            }
        }

        // ─── Teardown ─────────────────────────────────────────────────────────────

        public void Dispose()
        {
            CBox.Instance.RouteEvent             -= CBox_RouteEvent;
            CBox.Instance.ResponseErrorEvent     -= CBox_ResponseErrorEvent;
            CBox.Instance.DecoderInfoUpdateEvent -= CBox_DecoderUpdateEvent;
        }

        // ─── CBox event handlers ──────────────────────────────────────────────────

        private void CBox_RouteEvent(object sender, RouteEventArgs args)
        {
            if (args.DecoderId != _myDecoderId) return;

            CurrentSourceIndex = args.SourceIndex;
            CurrentSourceId    = args.SourceId;
            IsStreamOn         = args.StreamOn;

            CurrentRouteChanged?.Invoke(this, args);
        }

        private void CBox_ResponseErrorEvent(object sender, ResponseErrorEventArgs args)
        {
            if (!args.Cmd.Contains(_myDecoderId)) return;

            LastError    = args.Error;
            LastErrorCmd = args.Cmd;

            ErrorReceived?.Invoke(this, args);
        }

        private void CBox_DecoderUpdateEvent(object sender, DecoderInfoUpdateEventArgs e)
        {
            if (e.Decoder.id != _myDecoderId) return;

            // FindIndex returns -1 when the encoder is not found (e.g. the decoder
            // has no stream source assigned yet). Casting -1 to ushort gives 65535,
            // which would appear as a wild analog value in SIMPL+. Guard it explicitly.
            int encIndex = CBox.Instance.mxnetEncoders.FindIndex(x => x.id == e.Decoder.streamSource);

            RouteEventArgs rArgs = new RouteEventArgs
            {
                SourceId    = e.Decoder.streamSource,
                SourceIndex = encIndex >= 0 ? (ushort)encIndex : (ushort)0,
                StreamOn    = e.Decoder.streamOn
            };

            if (encIndex < 0)
            {
                if (CBox.Instance.Debug == 1)
                    DebugUtility.DebugPrint($"CBox_DecoderUpdateEvent: encoder '{e.Decoder.streamSource}' not found for decoder '{_myDecoderId}'. SourceIndex defaulted to 0.", "MxnetDecoderClass", DebugUtility.DebugLevels.WARN);
            }

            DeviceInfoUpdate?.Invoke(this, rArgs);
        }
    }
}
