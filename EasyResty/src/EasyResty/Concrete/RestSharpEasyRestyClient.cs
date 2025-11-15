using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RestSharp;
using Resty.Abstract;
using Resty.Extensions;
using Resty.Models;
using System.Diagnostics;
using System.Net;

namespace Resty.Concrete
{
    public class RestSharpEasyRestyClient : IEasyRestyClient
    {
        private readonly IRestClient _restClient;
        private readonly ILogger<IEasyRestyClient> _logger;

        public RestSharpEasyRestyClient(IRestClient restClient, ILogger<IEasyRestyClient> logger)
        {
            _restClient = restClient;
            _logger = logger;
            //https://romikoderbynew.com/2012/01/17/slow-httpwebrequest-getresponse/
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            ServicePointManager.UseNagleAlgorithm = false;
#pragma warning restore SYSLIB0014 // Type or member is obsolete
        }

        public async Task<EasyRestyClientResponse> RequestAsync(EasyRestyClientRequest request, CancellationToken cancellationToken)
        {
            var restClientResponse = new EasyRestyClientResponse();
            var retryPolicy = Policy
            .HandleResult<RestResponse>(x =>
                    !x.IsSuccessful &&
                    x.StatusCode != 0 /*operation canceled request*/ &&
                    !request.IgnoredHttpReTryStatusCodes.Contains(x.StatusCode))
            .WaitAndRetryAsync(request.MaxRetryAttemptCount, x => request.PauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
            {
                var reTryMesage = new
                {
                    Message = $"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.",
                    RequestLog = iRestResponse.Result.Request.GenerateLog(),
                    ResultContent = iRestResponse.Result.Content,
                    ErrorMessage = iRestResponse.Result.ErrorMessage
                };

                _logger.LogWarning(reTryMesage.ToJson());
            });
            try
            {
                var restRequest = new RestRequest(request.Url);
                restRequest.Timeout = request.RequestTimeout;
                restRequest.Method = Enum.Parse<Method>(request.HttpMethod.ToString(), true);

                if (request.Headers != null && request.Headers.Any())
                {
                    foreach (var header in request.Headers)
                    {
                        restRequest.AddHeader(header.Key, header.Value);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.RequestBody))
                {
                    restRequest.AddBody(request.RequestBody);
                }

                Stopwatch restRequestTimeStopwatch = new Stopwatch();
                restRequestTimeStopwatch.Start();
                RestResponse restResponse;
                if (request.MaxRetryAttemptCount > 0)
                {
                    restResponse = await retryPolicy.ExecuteAsync((cancellationToken) => _restClient.ExecuteAsync(restRequest, cancellationToken), cancellationToken);
                }
                else
                {
                    restResponse = await _restClient.ExecuteAsync(restRequest, cancellationToken);
                }
                restRequestTimeStopwatch.Stop();

                restClientResponse.RestRequestResponseLog = _restClient.GenerateLog(restRequest, restResponse, restRequestTimeStopwatch.ElapsedMilliseconds);
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldJsonPath = request.ResponseDataFieldJsonPath;
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldDataType = request.ResponseDataFieldDataType;

                restClientResponse.IsSuccess = restResponse.IsSuccessful;
                restClientResponse.Content = restResponse.Content;
                if (!restResponse.IsSuccessful)
                {
                    var errors = new List<string>();
                    if (!string.IsNullOrWhiteSpace(restResponse.ErrorMessage))
                    {
                        errors.Add(restResponse.ErrorMessage);
                    }
                    if (restResponse.ErrorException != null)
                    {
                        errors.Add(restResponse.ErrorException.ToJson());
                    }
                    if (restResponse.Content != null)
                    {
                        errors.Add(restResponse.Content);
                    }

                    if (errors.Any())
                    {
                        restClientResponse.Errors.AddRange(errors);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.ResponseDataFieldJsonPath) && request.ResponseDataFieldDataType != null)
                    {
                        if (!string.IsNullOrWhiteSpace(restResponse.Content) && restResponse.Content.IsValidJson())
                        {
                            JObject contentAsJson = JObject.Parse(restResponse.Content);
                            if (contentAsJson != null)
                            {
                                var selectedJsonArea = contentAsJson.GetPropertyFromPath(request.ResponseDataFieldJsonPath);
                                if (selectedJsonArea != null)
                                {
                                    try
                                    {
                                        var responseDataFieldPathData =
                                            Convert.ChangeType
                                            (
                                                selectedJsonArea,
                                                request.ResponseDataFieldDataType
                                            );

                                        restClientResponse.IsSuccess = true;
                                        restClientResponse.ResponseDataFieldPathData = responseDataFieldPathData;
                                    }
                                    catch (Exception ex)
                                    {
                                        restClientResponse.IsSuccess = false;
                                        restClientResponse.Errors.Add($"JsonPath value \"{selectedJsonArea}\" parse error! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                        restClientResponse.Errors.Add(ex.ToJson());
                                    }

                                }
                                else
                                {
                                    restClientResponse.IsSuccess = false;
                                    restClientResponse.Errors.Add($"JsonPath not found in rest response content! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                }
                            }
                            else
                            {
                                //ignored                                
                            }
                        }
                        else
                        {
                            restClientResponse.IsSuccess = false;
                            restClientResponse.Errors.Add($"Rest response content is not valid json to get jsonpath data! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                restClientResponse.IsSuccess = false;
                restClientResponse.Errors.Add(ex.ToJson());
            }
            if (request.EnableReqRespLogging)
            {
                try
                {
                    if (restClientResponse.RestRequestResponseLog is not null)
                    {
                        _logger.LogInformation(JsonConvert.SerializeObject(restClientResponse.RestRequestResponseLog));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging RestRequestResponseLog");
                }

            }
            return restClientResponse;
        }

        public async Task<EasyRestyClientResponse<T>> RequestAsync<T>(EasyRestyClientRequest request, CancellationToken cancellationToken)
        {
            var restClientResponse = new EasyRestyClientResponse<T>();
            var retryPolicy = Policy
            .HandleResult<RestResponse>(x =>
                    !x.IsSuccessful &&
                    x.StatusCode != 0 /*operation canceled request*/ &&
                    !request.IgnoredHttpReTryStatusCodes.Contains(x.StatusCode))
            .WaitAndRetryAsync(request.MaxRetryAttemptCount, x => request.PauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
            {
                var reTryMesage = new
                {
                    Message = $"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.",
                    RequestLog = iRestResponse.Result.Request.GenerateLog(),
                    ResultContent = iRestResponse.Result.Content,
                    ErrorMessage = iRestResponse.Result.ErrorMessage
                };

                _logger.LogWarning(reTryMesage.ToJson());
            });
            try
            {
                var restRequest = new RestRequest(request.Url);
                restRequest.Timeout = request.RequestTimeout;
                restRequest.Method = Enum.Parse<Method>(request.HttpMethod.ToString(), true);

                if (request.Headers != null && request.Headers.Any())
                {
                    foreach (var header in request.Headers)
                    {
                        restRequest.AddHeader(header.Key, header.Value);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.RequestBody))
                {
                    restRequest.AddBody(request.RequestBody);
                }

                if (request.QueryParams != null && request.QueryParams.Any())
                {
                    foreach (var queryParam in request.QueryParams)
                    {
                        restRequest.AddQueryParameter(queryParam.Key, queryParam.Value);
                    }
                }

                Stopwatch restRequestTimeStopwatch = new Stopwatch();
                restRequestTimeStopwatch.Start();
                RestResponse restResponse;
                if (request.MaxRetryAttemptCount > 0)
                {
                    restResponse = await retryPolicy.ExecuteAsync((cancellationToken) => _restClient.ExecuteAsync(restRequest, cancellationToken), cancellationToken);
                }
                else
                {
                    restResponse = await _restClient.ExecuteAsync(restRequest, cancellationToken);
                }
                restRequestTimeStopwatch.Stop();

                restClientResponse.RestRequestResponseLog = _restClient.GenerateLog(restRequest, restResponse, restRequestTimeStopwatch.ElapsedMilliseconds);
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldJsonPath = request.ResponseDataFieldJsonPath;
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldDataType = request.ResponseDataFieldDataType;

                restClientResponse.IsSuccess = restResponse.IsSuccessful;
                if (restClientResponse.IsSuccess)
                {
                    restClientResponse.Data = _restClient.Serializers.DeserializeContent<T>(restResponse);
                }
                if (!restResponse.IsSuccessful)
                {
                    var errors = new List<string>();
                    if (!string.IsNullOrWhiteSpace(restResponse.ErrorMessage))
                    {
                        errors.Add(restResponse.ErrorMessage);
                    }
                    if (restResponse.ErrorException != null)
                    {
                        errors.Add(restResponse.ErrorException.ToJson());
                    }
                    if (restResponse.Content != null)
                    {
                        errors.Add(restResponse.Content);
                    }

                    if (errors.Any())
                    {
                        restClientResponse.Errors.AddRange(errors);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.ResponseDataFieldJsonPath) && request.ResponseDataFieldDataType != null)
                    {
                        if (!string.IsNullOrWhiteSpace(restResponse.Content) && restResponse.Content.IsValidJson())
                        {
                            JObject contentAsJson = JObject.Parse(restResponse.Content);
                            if (contentAsJson != null)
                            {
                                var selectedJsonArea = contentAsJson.GetPropertyFromPath(request.ResponseDataFieldJsonPath);
                                if (selectedJsonArea != null)
                                {
                                    try
                                    {
                                        var responseDataFieldPathData =
                                            Convert.ChangeType
                                            (
                                                selectedJsonArea,
                                                request.ResponseDataFieldDataType
                                            );

                                        restClientResponse.IsSuccess = true;
                                        restClientResponse.ResponseDataFieldPathData = responseDataFieldPathData;
                                    }
                                    catch (Exception ex)
                                    {
                                        restClientResponse.IsSuccess = false;
                                        restClientResponse.Errors.Add($"JsonPath value \"{selectedJsonArea}\" parse error! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                        restClientResponse.Errors.Add(ex.ToJson());
                                    }

                                }
                                else
                                {
                                    restClientResponse.IsSuccess = false;
                                    restClientResponse.Errors.Add($"JsonPath not found in rest response content! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                }
                            }
                            else
                            {
                                //ignored                                
                            }
                        }
                        else
                        {
                            restClientResponse.IsSuccess = false;
                            restClientResponse.Errors.Add($"Rest response content is not valid json to get jsonpath data! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                restClientResponse.IsSuccess = false;
                restClientResponse.Errors.Add(ex.ToJson());

                _logger.LogError(ex, "Error in RestClientHelper RequestAsync");
            }
            if (request.EnableReqRespLogging)
            {
                try
                {
                    if (restClientResponse.RestRequestResponseLog is not null)
                    {
                        _logger.LogInformation(JsonConvert.SerializeObject(restClientResponse.RestRequestResponseLog));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging RestRequestResponseLog");
                }

            }
            return restClientResponse;
        }

        public async Task<EasyRestyClientResponse<TSuccessModel, TFailModel>> RequestAsync<TSuccessModel, TFailModel>(EasyRestyClientRequest request, CancellationToken cancellationToken)
        {
            var restClientResponse = new EasyRestyClientResponse<TSuccessModel, TFailModel>();
            var retryPolicy = Policy
            .HandleResult<RestResponse>(x =>
                !x.IsSuccessful &&
                x.StatusCode != 0 /*operation canceled request*/ &&
                !request.IgnoredHttpReTryStatusCodes.Contains(x.StatusCode))
            .WaitAndRetryAsync(request.MaxRetryAttemptCount, x => request.PauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
            {
                var reTryMesage = new
                {
                    Message = $"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.",
                    RequestLog = iRestResponse.Result.Request.GenerateLog(),
                    ResultContent = iRestResponse.Result.Content,
                    ErrorMessage = iRestResponse.Result.ErrorMessage
                };

                _logger.LogWarning(reTryMesage.ToJson());
            });
            try
            {
                var restRequest = new RestRequest(request.Url);
                restRequest.Timeout = request.RequestTimeout;
                restRequest.Method = Enum.Parse<Method>(request.HttpMethod.ToString(), true);

                if (request.Headers != null && request.Headers.Any())
                {
                    foreach (var header in request.Headers)
                    {
                        restRequest.AddHeader(header.Key, header.Value);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.RequestBody))
                {
                    restRequest.AddBody(request.RequestBody);
                }

                if (request.QueryParams != null && request.QueryParams.Any())
                {
                    foreach (var queryParam in request.QueryParams)
                    {
                        restRequest.AddQueryParameter(queryParam.Key, queryParam.Value);
                    }
                }

                Stopwatch restRequestTimeStopwatch = new Stopwatch();
                restRequestTimeStopwatch.Start();
                RestResponse restResponse;
                if (request.MaxRetryAttemptCount > 0)
                {
                    restResponse = await retryPolicy.ExecuteAsync(() => _restClient.ExecuteAsync(restRequest, cancellationToken));
                }
                else
                {
                    restResponse = await _restClient.ExecuteAsync(restRequest, cancellationToken);
                }

                restRequestTimeStopwatch.Stop();

                restClientResponse.RestRequestResponseLog = _restClient.GenerateLog(restRequest, restResponse, restRequestTimeStopwatch.ElapsedMilliseconds);
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldJsonPath = request.ResponseDataFieldJsonPath;
                restClientResponse.RestRequestResponseLog.RequestLog.ResponseDataFieldDataType = request.ResponseDataFieldDataType;

                restClientResponse.IsSuccess = restResponse.IsSuccessful;
                if (restClientResponse.IsSuccess)
                {
                    restClientResponse.SuccessData = _restClient.Serializers.DeserializeContent<TSuccessModel>(restResponse);
                }
                if (!restResponse.IsSuccessful)
                {
                    var errors = new List<string>();
                    try
                    {
                        restClientResponse.FailData = _restClient.Serializers.DeserializeContent<TFailModel>(restResponse);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.ToJson());
                    }

                    if (!string.IsNullOrWhiteSpace(restResponse.ErrorMessage))
                    {
                        errors.Add(restResponse.ErrorMessage);
                    }
                    if (restResponse.ErrorException != null)
                    {
                        errors.Add(restResponse.ErrorException.ToJson());
                    }
                    if (restResponse.Content != null)
                    {
                        errors.Add(restResponse.Content);
                    }

                    if (errors.Any())
                    {
                        restClientResponse.Errors.AddRange(errors);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.ResponseDataFieldJsonPath) && request.ResponseDataFieldDataType != null)
                    {
                        if (!string.IsNullOrWhiteSpace(restResponse.Content) && restResponse.Content.IsValidJson())
                        {
                            JObject contentAsJson = JObject.Parse(restResponse.Content);
                            if (contentAsJson != null)
                            {
                                var selectedJsonArea = contentAsJson.GetPropertyFromPath(request.ResponseDataFieldJsonPath);
                                if (selectedJsonArea != null)
                                {
                                    try
                                    {
                                        var responseDataFieldPathData =
                                            Convert.ChangeType
                                            (
                                                selectedJsonArea,
                                                request.ResponseDataFieldDataType
                                            );

                                        restClientResponse.IsSuccess = true;
                                        restClientResponse.ResponseDataFieldPathData = responseDataFieldPathData;
                                    }
                                    catch (Exception ex)
                                    {
                                        restClientResponse.IsSuccess = false;
                                        restClientResponse.Errors.Add($"JsonPath value \"{selectedJsonArea}\" parse error! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                        restClientResponse.Errors.Add(ex.ToJson());
                                    }

                                }
                                else
                                {
                                    restClientResponse.IsSuccess = false;
                                    restClientResponse.Errors.Add($"JsonPath not found in rest response content! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                                }
                            }
                            else
                            {
                                //ignored                                
                            }
                        }
                        else
                        {
                            restClientResponse.IsSuccess = false;
                            restClientResponse.Errors.Add($"Rest response content is not valid json to get jsonpath data! (Path: {request.ResponseDataFieldJsonPath}, Type: {request.ResponseDataFieldDataType})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                restClientResponse.IsSuccess = false;
                restClientResponse.Errors.Add(ex.ToJson());

                _logger.LogError(ex, "Error in RestClientHelper RequestAsync");
            }
            if (request.EnableReqRespLogging)
            {
                try
                {
                    if (restClientResponse.RestRequestResponseLog is not null)
                    {
                        _logger.LogInformation(JsonConvert.SerializeObject(restClientResponse.RestRequestResponseLog));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging RestRequestResponseLog");
                }

            }
            return restClientResponse;
        }
    }
}
