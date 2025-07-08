using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Models.External.LLM;
using MainAPI_Server.Config;

namespace MainAPI_Server.Services.LLM
{
    public class ChatLLMService : ILLMService
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMService> _logger;

        public ChatLLMService(ILLMClient llmClient, ILogger<ChatLLMService> logger) 
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task<LLMResponse> CreateTextResponseAsync(string systemMessage, string userMessage, List<string> conversationContext, List<string> memoryContext)
        {
            var request = new LLMRequest
            {
                SystemMessage = LLMSettings.Chat.SystemPrompt,
                UserMessage = userMessage,
                Instructions = LLMSettings.Chat.Instructions,
                MemoryContext = memoryContext ?? new List<string>(),
                ConversationHistory = conversationContext ?? new List<string>(),
                MaxTokens = LLMSettings.Chat.MaxTokens,
                Temperature = LLMSettings.Chat.Temperature,
                Model = LLMSettings.Chat.Model
            };
            return await _llmClient.SendRequestAsync(request);
        }
    }
} 