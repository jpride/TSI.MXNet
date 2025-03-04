using Newtonsoft.Json;

namespace TSI.MXNet
{
 
    public class SimpleInfoResponse : BaseResponse
    {
        [JsonProperty("info")]
        public string Info { get; set; }
    }

}
