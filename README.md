![alt tag](/img/easyresty.png)  

Simplfy your rest calls with EasyResty. (Base on  [RestSharp](https://github.com/restsharp/RestSharp) )

[![NuGet version](https://badge.fury.io/nu/EasyResty.svg)](https://badge.fury.io/nu/EasyResty)  ![Nuget](https://img.shields.io/nuget/dt/EasyResty)

#### Features:
- Can configure request url
- Can configure request http method
- Can configure request headers
- Can configure request query string params
- Can configure request body
- Can ready only spesific data fron response json model without deserialize all response model
    - ResponseDataFieldJsonPath > give field path with jsonata notation
    - ResponseDataFieldDataType > give field data type for corect parse operation
- Can configure automaticaly request-response logging with ILogger<IRestyClient>
- Can configure request retry count
- Can configure request timeout
- Can configure pause time between request retrys
- Can configure ignore retry http statuts code
- Also every request automaticaly generate detailed request response log (RestyRequestResponseLog model)
- Can deserialize success and error model seperatly


#### EasyResty Models:
```cs

//Request Model
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

//Response Models
public class RestyClientResponse
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; }
    public object ResponseDataFieldPathData { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public EasyRestyRequestResponseLog RestRequestResponseLog { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

public class EasyRestyClientResponse<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public object ResponseDataFieldPathData { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public EasyRestyRequestResponseLog RestRequestResponseLog { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

public class EasyRestyClientResponse<TSuccessModel, TFailModel>
{
    public bool IsSuccess { get; set; }
    public TSuccessModel SuccessData { get; set; }
    public TFailModel FailData { get; set; }
    public object ResponseDataFieldPathData { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public EasyRestyRequestResponseLog RestRequestResponseLog { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

//Log Model
public class EasyRestyRequestResponseLog
{
    public long DurationAsMiliseconds { get; set; }
    public RequestLog RequestLog { get; set; } = new();
    public ResponseLog ResponseLog { get; set; } = new();
}

public class RequestLog
{
    public string Resource { get; set; }
    public List<Parameter> Parameters { get; set; }
    public string Method { get; set; }
    public string Uri { get; set; }
    public string ResponseDataFieldJsonPath { get; set; }
    public Type ResponseDataFieldDataType { get; set; }
}

public class ResponseLog
{
    public string StatusCode { get; set; }
    public string Content { get; set; }
    public List<Header> Headers { get; set; }
    public string ResponseUri { get; set; }
    public string ErrorMessage { get; set; }
}

public class Parameter
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
}

public class Header
{
    public string Key { get; set; }
    public string Value { get; set; }
}

```


#### Usages:

###### Program.cs Configuration:

```cs

//Program.cs configuration
var builder = WebApplication.CreateBuilder(args);

builder.WithEasyResty(); //Register EasyResty service

var application = builder.Build();
await application.RunAsync();

```

###### Sample Models:

```cs

public class SuccessResponseModel ()
{
   public int Id {get; set;}
}

public class ErrorResponseModel ()
{
    public string Mesage {get; set}
}

```

###### Usage Samples:

```cs
//Resolve interface 
IEasyRestyClient

//Return Generic Model
var response = await _easyRestyClient.RequestAsync(new EasyRestyClientRequest
{
    Url = "http://userservice.com/api/v1/users/25",
    HttpMethod = HttpMethod.Get,
    Headers = new Dictionary<string, string>
    {
        {
            "X-Authorization", "token"
        }
    }
}, cancellationToken);

//Return Given Model Type
var response = await _easyRestyClient.RequestAsync<SuccessResponseModel>(new EasyRestyClientRequest
{
    Url = "http://userservice.com/api/v1/users",
    HttpMethod = HttpMethod.Post,
    RequestBody = JsonConvert.SerializeObject(new
    {
        Name = "John",
        Surname = "Doe"
    })
}, cancellationToken);

//Return Seperate Model for Success and Error Case
var response = await _easyRestyClient.RequestAsync<SuccessResponseModel,ErrorResponseModel>(new EasyRestyClientRequest
{
    Url = "http://userservice.com/api/v1/users/search",
    HttpMethod = HttpMethod.Get,
    RequestBody = JsonConvert.SerializeObject(new
    {
        CompanyId = 12
    }),
    QueryParams = new Dictionary<string, string>
    {
        { "PageIndex", request.PageIndex.ToString() },
        { "PageSize", request.PageSize.ToString() }
    }
}, cancellationToken);

//All features sample
var response = await _easyRestyClient.RequestAsync(new EasyRestyClientRequest
{
    Url = "http://userservice.com/api/v1/users/sample", 
    HttpMethod = HttpMethod.Get,
    Headers = new Dictionary<string, string>
    {
        {
            "X-Authorization", "token"
        }
    },
    QueryParams = new Dictionary<string, string>
    {
        { "PageIndex", request.PageIndex.ToString() },
        { "PageSize", request.PageSize.ToString() }
    }
    RequestBody = JsonConvert.SerializeObject(new
    {
        CompanyId = 12
    }),
    EnableReqRespLogging = true, //request log with ILogger<IEasyRestyClient>
    MaxRetryAttemptCount = 3, //Retry policy count
    RequestTimeout =  TimeSpan.FromMinutes(2),
    PauseBetweenFailures = TimeSpan.FromSeconds(1),
    IgnoredHttpReTryStatusCodes = new List<HttpStatusCode>
    {
        HttpStatusCode.InternalServerError
    }

}, cancellationToken);

```

### Release Notes

##### 1.0.3 
* Polly version updated to 8.7.0
* RestSharp version updated to 114.0.0
* RestSharp.Serializers.NewtonsoftJson version updated to 114.0.0

##### 1.0.2
* Rest request original Http response Status added in EasyRestyClientResponse model as "StatusCode" (Default value is  HttpStatusCode.OK)

##### 1.0.1
* Null check added for header and query params
* Polly version updated to 8.6.5
* RestSharp version updated to 113.0.0
* RestSharp.Serializers.NewtonsoftJson version updated to 113.0.0

##### 1.0.0
* Base release