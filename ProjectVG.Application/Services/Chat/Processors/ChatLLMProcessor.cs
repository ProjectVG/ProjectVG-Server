using Mono.TextTemplating;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using System.Diagnostics;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatLLMProcessor
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMProcessor> _logger;

        public ChatLLMProcessor(
            ILLMClient llmClient,
            ILogger<ChatLLMProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task ProcessAsync(ChatProcessContext context)
        {
            var format = LLMFormatFactory.CreateChatFormat();

            var conversationHistory = context.ParseConversationHistory()
                .Select(h => new ProjectVG.Infrastructure.Integrations.LLMClient.Models.History 
                { 
                    Role = h.Role, 
                    Content = h.Content 
                })
                .ToList();


            var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(context),
                    context.UserMessage,
                    format.GetInstructions(context),
                    conversationHistory,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

            var segments = format.Parse(llmResponse.OutputText, context);
            var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
            context.SetResponse(llmResponse.OutputText, segments, cost);

        }
    }
}
