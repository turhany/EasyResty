using System.Net;

namespace Resty.Models
{
    public class EasyRestyClientResponse
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
}
