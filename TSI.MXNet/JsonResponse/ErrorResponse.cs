using Newtonsoft.Json;


namespace TSI.MXNet.JsonResponses
{
    public class ErrorResponse : BaseResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
