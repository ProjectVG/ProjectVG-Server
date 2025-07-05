using MainAPI_Server.Models.External.LLM;

namespace MainAPI_Server.Clients.LLM
{
    public interface ILLMClient
    {
        Task<LLMResponse> GenerateResponseAsync(string userMessage, List<string> conversationContext, List<string> memoryContext);
    }
} 