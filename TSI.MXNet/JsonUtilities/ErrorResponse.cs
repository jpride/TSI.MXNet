using Newtonsoft.Json;


namespace TSI.MXNet.JsonUtilities
{
    public class ErrorResponse : BaseResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
