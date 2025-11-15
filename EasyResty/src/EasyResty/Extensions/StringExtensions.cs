using Newtonsoft.Json;
using System.Text.Json.Nodes;

namespace Resty.Extensions
{
    internal static class StringExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (serializer, err) => err.ErrorContext.Handled = true
        };


        public static string ToJson(this object item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            try
            {
                return JsonConvert.SerializeObject(item, JsonSerializerSettings);

                //TODO: check this give > overflow exception
                //return SpanJson.JsonSerializer.Generic.Utf16.Serialize(item);
            }
            catch (Exception ex)
            {
                //ignored
                Console.WriteLine(ex.Message);
            }

            return JsonConvert.SerializeObject(item, JsonSerializerSettings);
        }

        public static bool IsValidJson(this string text)
        {
            try
            {
                JsonNode.Parse(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
