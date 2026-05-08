using System;

namespace TSI.MXNet
{
    // ─── Base classes ────────────────────────────────────────────────────────────

    /// <summary>
    /// Common base for any event that carries a protocol response code.
    /// Code is ushort throughout so SIMPL+ analog outputs need no extra casting.
    /// </summary>
    public class MxnetEventArgsBase : EventArgs
    {
        public ushort Code { get; set; }
    }

    /// <summary>
    /// Base for events that carry a device ID and a raw data payload.
    /// </summary>
    public class DataResponseBaseEventArgs : MxnetEventArgsBase
    {
        public string Id   { get; set; }
        public string Data { get; set; }
    }

    // ─── Concrete event args ─────────────────────────────────────────────────────

    /// <summary>
    /// Fired when the panel returns an error response.
    /// </summary>
    public class ResponseErrorEventArgs : MxnetEventArgsBase
    {
        public string Error { get; set; }
        public string Cmd   { get; set; }
        // Code inherited from MxnetEventArgsBase
    }

    /// <summary>
    /// Marker class for RS-232 pass-through responses.
    /// </summary>
    public class Rs232ResponseEventArgs : DataResponseBaseEventArgs { }

    /// <summary>
    /// General-purpose response event — used for both SimpleInfoResponse
    /// and DetailedInfoReportResponse parse paths.
    /// </summary>
    public class SimpleResponseEventArgs : MxnetEventArgsBase
    {
        public string Info   { get; set; }
        public string Cmd    { get; set; }
        public string Id     { get; set; }
        public string Source { get; set; }
        // Code inherited from MxnetEventArgsBase
    }

    /// <summary>
    /// Fired once when the device list has been fully parsed and both
    /// mxnetEncoders and mxnetDecoders lists are populated and sorted.
    /// This is the correct signal that initialization is complete.
    /// </summary>
    public class DeviceListUpdateEventArgs : EventArgs
    {
        public ushort   DecoderCount { get; set; }
        public ushort   EncoderCount { get; set; }
        public string[] Decoders     { get; set; }
        public string[] Encoders     { get; set; }
    }

    /// <summary>
    /// Fired once per decoder during device-list parsing, carrying the
    /// decoder's initial state (stream source, stream on/off).
    /// </summary>
    public class DecoderInfoUpdateEventArgs : EventArgs
    {
        public MxnetDecoder Decoder { get; set; }
    }

    /// <summary>
    /// Fired whenever a routing change is confirmed by the panel.
    /// All indices are 0-based internally; the SIMPL+ wrapper is responsible
    /// for any 1-based presentation to the control program.
    /// </summary>
    public class RouteEventArgs : EventArgs
    {
        public string DecoderId   { get; set; }
        public ushort DestIndex   { get; set; }   // 0-based
        public ushort SourceIndex { get; set; }   // 0-based
        public string SourceId    { get; set; }
        public ushort StreamOn    { get; set; }
    }

    /// <summary>
    /// Fired when the TCP connection state changes.
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        public ushort IsConnected { get; set; }
    }
}
