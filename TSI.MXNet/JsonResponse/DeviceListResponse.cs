using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TSI.MXNet.JsonResponses
{


    public class DeviceListResponse : BaseResponse
    {
        [JsonProperty("info")]
        public Dictionary<string, Device> Info { get; set; }
    }

    public class Device
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("mac")]
        public string Mac { get; set; }

        [JsonProperty("ch_p")]
        public string ChP { get; set; }

        [JsonProperty("dtype")]
        public string Dtype { get; set; }

        [JsonProperty("ch_u")]
        public string ChU { get; set; }

        [JsonProperty("ch_v")]
        public string ChV { get; set; }

        [JsonProperty("vmode")]
        public string Vmode { get; set; }

        [JsonProperty("ch_s")]
        public string ChS { get; set; }

        [JsonProperty("rs232baudrate")]
        public string Rs232baudrate { get; set; }

        [JsonProperty("rs232mode")]
        public string Rs232mode { get; set; }

        [JsonProperty("ch_r")]
        public string ChR { get; set; }

        [JsonProperty("light")]
        public int? Light { get; set; }

        [JsonProperty("usbmode")]
        public int? Usbmode { get; set; }

        [JsonProperty("rotate")]
        public string Rotate { get; set; }

        [JsonProperty("gateway")]
        public string Gateway { get; set; }

        [JsonProperty("stream")]
        public string Stream { get; set; }

        [JsonProperty("chipset")]
        public string Chipset { get; set; }

        [JsonProperty("sn")]
        public string Sn { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kmoip")]
        public int? Kmoip { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("stretch")]
        public string Stretch { get; set; }

        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        [JsonProperty("switchport")]
        public string Switchport { get; set; }

        [JsonProperty("osdsize")]
        public int? Osdsize { get; set; }

        [JsonProperty("blackout")]
        public string Blackout { get; set; }

        [JsonProperty("cec")]
        public int? Cec { get; set; }

        [JsonProperty("ch_a")]
        public string ChA { get; set; }

        [JsonProperty("osd")]
        public int? Osd { get; set; }

        [JsonProperty("chipsetvtime")]
        public string Chipsetvtime { get; set; }

        [JsonProperty("ipmode")]
        public string Ipmode { get; set; }

        [JsonProperty("modelname")]
        public string Modelname { get; set; }

        [JsonProperty("chipsetver")]
        public string Chipsetver { get; set; }

        [JsonProperty("switchip")]
        public string Switchip { get; set; }

        [JsonProperty("ch_c")]
        public string ChC { get; set; }

        [JsonProperty("subnet")]
        public string Subnet { get; set; }

        [JsonProperty("hdcp")]
        public string Hdcp { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("rs232responsetype")]
        public int? Rs232responsetype { get; set; }

        // Additional properties for Encoders
        [JsonProperty("audioinputtype")]
        public string Audioinputtype { get; set; }

        [JsonProperty("is_host")]
        public int? IsHost { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("ch")]
        public string Ch { get; set; }

        [JsonProperty("exaudiovolume")]
        public string Exaudiovolume { get; set; }

        [JsonProperty("edid")]
        public string Edid { get; set; }
    }

    public class Video
    {
        [JsonProperty("frames_per_second")]
        public string FramesPerSecond { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }
    }

}
