namespace Resty.Models
{
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
}
