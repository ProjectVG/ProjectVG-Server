using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class UserInputAnalysisProcessor
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<UserInputAnalysisProcessor> _logger;

        public UserInputAnalysisProcessor(
            ILLMClient llmClient,
            ILogger<UserInputAnalysisProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task<UserInputAnalysis> ProcessAsync(string userInput, List<ConversationHistory> conversationHistory)
        {
            try
            {
                // LLM 포맷 생성
                var format = LLMFormatFactory.CreateUserInputAnalysisFormat();
                
                // 대화 기록을 컨텍스트로 구성
                var conversationContext = conversationHistory
                    .Select(c => $"{c.Role}: {c.Content}")
                    .ToList();

                // LLM 요청
                var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(userInput),
                    userInput,
                    format.GetInstructions(userInput),
                    conversationContext,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

                // 결과 파싱
                var analysis = format.Parse(llmResponse.Response, userInput);
                
                _logger.LogDebug("사용자 입력 분석 완료: '{Input}' -> 맥락: {Context}, 의도: {Intent}, 액션: {Action}", 
                    userInput, analysis.ConversationContext, analysis.UserIntent, analysis.Action);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 입력 분석 중 오류 발생: '{Input}'", userInput);
                // 오류 발생 시 기본값 반환
                return UserInputAnalysis.CreateValid("일반적인 대화", "대화", UserInputAction.Chat, new List<string>());
            }
        }
    }
}
