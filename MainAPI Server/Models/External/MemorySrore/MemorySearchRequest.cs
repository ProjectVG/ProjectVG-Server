public class MemorySearchRequest
{
    public string Query { get; set; } = default!;
    public int TopK { get; set; } = 3;
}