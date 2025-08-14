using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Services.Chat.Core;
using ProjectVG.Application.Services.Chat.Handlers;
using System.Diagnostics;

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
            var totalSw = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", command.SessionId);

                var preContext = await _preprocessor.PreprocessAsync(command);
                var processResult = await _llmHandler.ProcessLLMAsync(preContext);
                await _ttsHandler.ApplyTTSAsync(preContext, processResult);
                await _resultPersister.PersistResultAsync(preContext, processResult);
                await _resultSender.SendResultAsync(preContext, processResult);

                totalSw.Stop();
                _logger.LogInformation("채팅 요청 처리 완료: {SessionId}, 소요시간: {ProcessingTime:F2}ms, 토큰: {TokensUsed}",
                    command.SessionId, totalSw.Elapsed.TotalMilliseconds, processResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", command.SessionId);
                throw;
            }
        }
    }
} 