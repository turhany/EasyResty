using System.Net;

namespace Resty.Models
{
    public class EasyRestyClientRequest
    {
        public string Url { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParams { get; set; }
        public string RequestBody { get; set; }
        public string ResponseDataFieldJsonPath { get; set; }
        public Type ResponseDataFieldDataType { get; set; }
        public bool EnableReqRespLogging { get; set; } = true;

        public int MaxRetryAttemptCount { get; set; } = 2;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan PauseBetweenFailures { get; set; } = TimeSpan.FromSeconds(1);
        public List<HttpStatusCode> IgnoredHttpReTryStatusCodes { get; set; } = new List<HttpStatusCode>
        {
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError
        };
    }
}
