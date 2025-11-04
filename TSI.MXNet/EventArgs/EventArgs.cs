using System;
using System.Text;


namespace TSI.MXNet
{
    public class DataResponseBaseEventArgs : EventArgs
    {
        public string id { get; set; }
        public string data { get; set; }
    }
    public class ResponseErrorEventArgs : EventArgs
    {
        public string Error { get; set; }
        public string Cmd { get; set; }
        public int Code { get; set; }    
    }

    public class rs232ResponseEventArgs : DataResponseBaseEventArgs
    { 
        
    }

    public class SimpleResponseEventArgs : DataResponseBaseEventArgs
    {
        public string info { get; set; }
        public string cmd { get; set; }
        public int code { get; set; }
        public string source { get; set; }
    }


    public class DeviceListUpdateEventArgs : EventArgs
    {
        public ushort decoderCount { get; set; }
        public ushort encoderCount { get; set; }
        public string[] decoders { get; set; }
        public string[] encoders { get; set; }
    }

    public class DecoderInfoUpdateEventArgs : EventArgs
    {
        public MxnetDecoder Decoder { get; set; }
    }

    public class RouteEventArgs : EventArgs
    {
        public string DecoderId { get; set; }
        public ushort DestIndex { get; set; }
        public ushort SourceIndex { get; set; }
        public string SourceId { get; set; }
        public ushort StreamOn { get; set; }

    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public ushort IsConnected { get; set; }
    }
}
