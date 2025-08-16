namespace ProjectVG.Common.Models
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TraceId { get; set; }
        public List<string>? Details { get; set; }
    }
}
