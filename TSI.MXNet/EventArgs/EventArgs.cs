using System;


namespace TSI.MXNet
{
    public class DataResponseBaseEventArgs : EventArgs
    {
        public string id { get; set; }
        public string data { get; set; }
    }
    public class ResponseErrorEventArgs : EventArgs
    {
        public string error { get; set; }
        public string cmd { get; set; }
        public int code { get; set; }    
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

    public class RouteEventArgs : EventArgs
    {
        public ushort destIndex { get; set; }
        public ushort sourceIndex { get; set; }

    }
}
