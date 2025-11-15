using RestSharp;
using Resty.Models;

namespace Resty.Extensions
{
    internal static class RestExtensions
    {
        public static EasyRestyRequestResponseLog GenerateLog(this IRestClient restClient, RestRequest request, RestResponse response, long durationAsMiliseconds)
        {
            var url = string.Empty;
            try
            {
                url = restClient.BuildUri(request).ToString();
            }
            catch
            {
                url = request.Resource;
            }

            var log = new EasyRestyRequestResponseLog();
            log.DurationAsMiliseconds = durationAsMiliseconds;

            log.RequestLog = new RequestLog
            {
                Resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                Parameters = request.Parameters?.Select(parameter => new Models.Parameter
                {
                    Name = parameter.Name,
                    Value = parameter.Value?.ToJson(),
                    Type = parameter.Type.ToString()
                }).ToList(),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                Method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                Uri = url,
            };

            log.ResponseLog = new ResponseLog
            {
                StatusCode = response.StatusCode.ToString(),
                Content = response.Content,
                Headers = response.Headers?.Select(header => new Models.Header
                {
                    Key = header.Name,
                    Value = header.Value.ToJson()
                }).ToList(),
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                ResponseUri = response.ResponseUri?.ToString(),
                ErrorMessage = response.ErrorMessage,
            };

            return log;
        }

        public static RequestLog GenerateLog(this RestRequest request)
        {
            var log = new RequestLog
            {
                Resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                Parameters = request.Parameters?.Select(parameter => new Models.Parameter
                {
                    Name = parameter.Name,
                    Value = parameter.Value?.ToJson(),
                    Type = parameter.Type.ToString()
                }).ToList(),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                Method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                Uri = request.Resource,
            };

            return log;
        }
    }
}
