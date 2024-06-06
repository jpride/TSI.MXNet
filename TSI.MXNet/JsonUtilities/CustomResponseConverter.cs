using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace TSI.MXNet.JsonUtilities
{
    public class CustomResponseConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(BaseResponse).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            BaseResponse response;

            if (jsonObject["error"] != null)
            {
                response = new ErrorResponse();
            }

            else if (jsonObject["info"] is JObject)
            {
                response = new DeviceListResponse();
            }

            else if (jsonObject["info"] != null && jsonObject["source"] != null && jsonObject["code"] != null)
            {
                response = new DetailedInfoReportResponse();
            }
            else //simple repsonse
            {
                response = new SimpleInfoResponse();
            }

            serializer.Populate(jsonObject.CreateReader(), response);
            return response;
        }

        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}
