namespace ProjectVG.Common.Models
{
    public record ErrorResponse
    {
        public string ErrorCode { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public int StatusCode { get; init; }
        public DateTime Timestamp { get; init; }
        public string? TraceId { get; init; }
        public List<string>? Details { get; init; }
    }
}
