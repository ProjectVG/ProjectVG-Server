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
            var metrics = new List<StepMetrics>();
            const double TOKEN_UNIT_COST = 0.002 / 1000; // 1K 토큰당 $0.002
            const double TTS_UNIT_COST = 0.00001;        // 1자당 $0.00001

            var totalSw = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: 세션 {SessionId}, 메시지: {Message}", command.SessionId, command.Message);

                // Preprocess
                var sw = Stopwatch.StartNew();
                var preContext = await _preprocessor.PreprocessAsync(command);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "Preprocess", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });

                // LLM
                sw.Restart();
                var processResult = await _llmHandler.ProcessLLMAsync(preContext);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "LLM", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = processResult.Cost });

                // TTS
                sw.Restart();
                await _ttsHandler.ApplyTTSAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "TTS", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = processResult.Cost });

                // Persist
                sw.Restart();
                await _resultPersister.PersistResultAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "Persist", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });

                // Send
                sw.Restart();
                await _resultSender.SendResultAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "Send", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });

                totalSw.Stop();

                // 종합 결과 로그
                var totalTime = metrics.Sum(m => m.TimeMs);
                var totalCost = metrics.Sum(m => m.Cost);
                var totalUsd = totalCost * 0.001;

                _logger.LogInformation("=== Chat 처리 메트릭 ===");
                foreach (var m in metrics)
                    _logger.LogInformation("{Step}: {Time:F2}ms, Cost: {Cost:F2}", m.StepName, m.TimeMs, m.Cost);
                _logger.LogInformation("총 소요시간: {TotalTime:F2}ms, 총 Cost: {TotalCost:F2}, (USD: ${TotalUsd:F6})", totalTime, totalCost, totalUsd);

                // 기존 완료 로그
                _logger.LogInformation("채팅 요청 처리 완료: 세션 {SessionId}, 소요시간: {ProcessingTime:F2}ms, 토큰: {TokensUsed}",
                    preContext.SessionId, totalTime, processResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: 세션 {SessionId}", command.SessionId);
            }
        }
    }
} 