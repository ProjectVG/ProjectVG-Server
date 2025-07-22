using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Services.Chat.Core;
using ProjectVG.Application.Services.Chat.Handlers;

namespace ProjectVG.Application.Services.Chat.Core
{
    public class ChatOrchestrator
    {
        private readonly ChatPreprocessor _preprocessor;
        private readonly LLMHandler _llmHandler;
        private readonly TTSHandler _ttsHandler;
        private readonly ResultSender _resultSender;
        private readonly ResultPersister _resultPersister;
        private readonly ILogger<ChatOrchestrator> _logger;

        public ChatOrchestrator(
            ChatPreprocessor preprocessor,
            LLMHandler llmHandler,
            TTSHandler ttsHandler,
            ResultSender resultSender,
            ResultPersister resultPersister,
            ILogger<ChatOrchestrator> logger)
        {
            _preprocessor = preprocessor;
            _llmHandler = llmHandler;
            _ttsHandler = ttsHandler;
            _resultSender = resultSender;
            _resultPersister = resultPersister;
            _logger = logger;
        }

        public async Task ProcessChatRequestAsync(ProcessChatCommand command)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: 세션 {SessionId}, 메시지: {Message}", command.SessionId, command.Message);

                var preContext = await _preprocessor.PreprocessAsync(command);
                var processResult = await _llmHandler.ProcessLLMAsync(preContext);
                await _ttsHandler.ApplyTTSAsync(preContext, processResult);
                await _resultPersister.PersistResultAsync(preContext, processResult);
                await _resultSender.SendResultAsync(preContext, processResult);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;
                _logger.LogInformation("채팅 요청 처리 완료: 세션 {SessionId}, 소요시간: {ProcessingTime:F2}ms, 토큰: {TokensUsed}",
                    preContext.SessionId, processingTime, processResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: 세션 {SessionId}", command.SessionId);
            }
        }
    }
} 