using Newtonsoft.Json;

namespace TSI.MXNet.JsonUtilities
{
 
    public class ReportResponse : BaseResponse
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("mac")]
        public string Mac {  get; set; }
    }

}
