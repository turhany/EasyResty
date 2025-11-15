using Newtonsoft.Json.Linq;

namespace Resty.Extensions
{
    internal static class JsonExtensions
    {
        //https://stackoverflow.com/a/64749470
        public static JToken GetPropertyFromPath(this JToken token, string path)
        {
            if (token == null)
            {
                return null;
            }
            string[] pathParts = path.Split(".");
            JToken current = token;
            foreach (string part in pathParts)
            {
                current = current.GetProperty(part);
                if (current == null)
                {
                    return null;
                }
            }
            return current;
        }

        public static JToken GetProperty(this JToken token, string name)
        {
            if (token == null)
            {
                return null;
            }

            var obj = token as JObject;
            JToken match;
            if (obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out match))
            {
                if (match is JObject)
                {
                    return JObject.Parse(match.ToJson());
                }

                return match;
            }
            return null;
        }
    }
}
