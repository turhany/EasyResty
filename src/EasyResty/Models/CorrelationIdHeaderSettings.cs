namespace Resty.Models
{
    public class CorrelationIdHeaderSettings
    {
        public bool Include { get; set; }
        public string HeaderKey { get; set; }
        public string HeaderValue { get; set; }
    }
}
