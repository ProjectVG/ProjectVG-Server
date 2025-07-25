namespace ProjectVG.Api.Properties
{
    public sealed class ExternalApisOptions
    {
        public LLMOptions LLM { get; set; } = new();
        public MemoryOptions Memory { get; set; } = new();
        public TTSOptions TTS { get; set; } = new();

        public sealed class LLMOptions
        {
            public string BaseUrl { get; set; } = null!;
        }
        public sealed class MemoryOptions
        {
            public string BaseUrl { get; set; } = null!;
        }
        public sealed class TTSOptions
        {
            public string BaseUrl { get; set; } = null!;
        }
    }

}
