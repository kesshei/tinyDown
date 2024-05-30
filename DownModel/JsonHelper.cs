using Newtonsoft.Json;

namespace DownModel
{
    public static class JsonHelper
    {
        public static string ToJson(this object message)
        {
            return JsonConvert.SerializeObject(message, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public static T ToObj<T>(this string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }
    }
}
