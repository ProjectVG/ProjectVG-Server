namespace ProjectVG.Common.Models
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TraceId { get; set; }
        public List<string>? Details { get; set; }
    }
}
