using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Models.External.LLM;

namespace MainAPI_Server.Services.LLM
{
    public class ChatLLMService : ILLMService
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMService> _logger;

        private const string _instructions = "";
        private const int _maxTokens = 1000;
        private const float _temperature = 0.7f;
        private const string _model = "gpt-4.1-mini";

        public ChatLLMService(ILLMClient llmClient, ILogger<ChatLLMService> logger) 
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task<LLMResponse> CreateTextResponseAsync(string systemMessage, string userMessage, List<string> conversationContext, List<string> memoryContext)
        {
            var request = new LLMRequest
            {
                SystemMessage = systemMessage ?? "",
                UserMessage = userMessage,
                Instructions = _instructions,
                MemoryContext = memoryContext ?? new List<string>(),
                ConversationHistory = conversationContext ?? new List<string>(),
                MaxTokens = _maxTokens,
                Temperature = _temperature,
                Model = _model
            };
            return await _llmClient.SendRequestAsync(request);
        }

    }
} 