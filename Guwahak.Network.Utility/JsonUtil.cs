using Newtonsoft.Json;

namespace Guwahak.Network.Utility
{
    public class JsonUtil
    {
        public static string ObjectToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T JsonToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object JsonToObject(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }
    }

}
