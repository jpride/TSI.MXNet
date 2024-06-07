using Newtonsoft.Json;

namespace TSI.MXNet.JsonResponses
{
    

    public class BaseResponse
    {
        [JsonProperty("cmd")]
        public string Cmd { get; set; }

        [JsonProperty("code")]
        public int? Code { get; set; }
    }

}
