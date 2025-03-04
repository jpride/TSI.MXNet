using Newtonsoft.Json;


namespace TSI.MXNet
{
    public class ErrorResponse : BaseResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
