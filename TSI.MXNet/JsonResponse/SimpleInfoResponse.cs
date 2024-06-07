using Newtonsoft.Json;

namespace TSI.MXNet.JsonResponses
{
 
    public class SimpleInfoResponse : BaseResponse
    {
        [JsonProperty("info")]
        public string Info { get; set; }
    }

}
