using Newtonsoft.Json;

namespace NMP.Portal.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);

            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public static bool Exists(this ISession session, string key)
        {
            return session.TryGetValue(key, out _);
        }
    }
}
