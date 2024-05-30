using Newtonsoft.Json;

namespace TSI.MXNet.JsonUtilities
{
 
    public class SimpleInfoResponse : BaseResponse
    {
        [JsonProperty("info")]
        public string Info { get; set; }
    }

}
