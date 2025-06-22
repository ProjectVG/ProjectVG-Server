public class MemoryAddRequest
{
    public string Text { get; set; } = default!;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
