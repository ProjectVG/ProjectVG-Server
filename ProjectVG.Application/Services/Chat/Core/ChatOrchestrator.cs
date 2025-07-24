using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Services.Chat.Core;
using ProjectVG.Application.Services.Chat.Handlers;
using System.Diagnostics;
using System.Globalization;
using System.IO;

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
            var metricsDto = new ChatProcessMetricsDto();
            var requestId = command.RequestId;
            var timestamp = DateTime.UtcNow;

            var totalSw = Stopwatch.StartNew();

            command.UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: 세션 {SessionId}, 메시지: {Message}", command.SessionId, command.Message);

                // Preprocess
                var sw = Stopwatch.StartNew();
                var preContext = await _preprocessor.PreprocessAsync(command);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "[Preprocess]", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });
                metricsDto.PreprocessTimeMs = sw.Elapsed.TotalMilliseconds;

                // LLM
                sw.Restart();
                var processResult = await _llmHandler.ProcessLLMAsync(preContext);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "[LLM]", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = processResult.Cost });
                metricsDto.LLMTimeMs = sw.Elapsed.TotalMilliseconds;
                metricsDto.LLMTokenUsed = processResult.TokensUsed;
                metricsDto.LLMCost = processResult.Cost;
                metricsDto.LLMResponse = processResult.Response;

                // TTS
                sw.Restart();
                await _ttsHandler.ApplyTTSAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "[TTS]", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = processResult.Cost });
                metricsDto.TTSTimeMs = sw.Elapsed.TotalMilliseconds;
                metricsDto.TTSEnabled = !string.IsNullOrWhiteSpace(preContext.VoiceName);
                metricsDto.TTSAudioLength = processResult.AudioLength;
                metricsDto.TTSCost = processResult.Cost - metricsDto.LLMCost;

                // Persist
                sw.Restart();
                await _resultPersister.PersistResultAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "[Persist]", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });
                metricsDto.PersistTimeMs = sw.Elapsed.TotalMilliseconds;

                // Send
                sw.Restart();
                await _resultSender.SendResultAsync(preContext, processResult);
                sw.Stop();
                metrics.Add(new StepMetrics { StepName = "[Send]", TimeMs = sw.Elapsed.TotalMilliseconds, Cost = 0 });
                metricsDto.SendTimeMs = sw.Elapsed.TotalMilliseconds;

                totalSw.Stop();

                // 종합 결과 로그
                var totalTime = metrics.Sum(m => m.TimeMs);
                var totalCost = metrics.Sum(m => m.Cost);
                var totalUsd = totalCost * 0.001;
                metricsDto.TotalTimeMs = totalTime;
                metricsDto.TotalCost = totalCost;
                metricsDto.Timestamp = timestamp;
                metricsDto.RequestId = requestId;
                metricsDto.SessionId = command.SessionId;
                metricsDto.Message = command.Message;
                metricsDto.UserId = command.UserId.ToString();

                // CSV 기록
                var csvFile = "chat_metrics.csv";
                var csvHeader = "RequestId,Timestamp,UserId,SessionId,Message,LLMResponse,TTSEnabled,TTSAudioLength,LLMTokenUsed,LLMCost,TTSCost,TotalCost,PreprocessTimeMs,LLMTimeMs,TTSTimeMs,PersistTimeMs,SendTimeMs,TotalTimeMs";
                if (!File.Exists(csvFile))
                {
                    File.AppendAllText(csvFile, csvHeader + "\n");
                }
                var csvLine = string.Join(",",
                    metricsDto.RequestId,
                    metricsDto.Timestamp.ToString("o", CultureInfo.InvariantCulture),
                    metricsDto.UserId,
                    metricsDto.SessionId,
                    metricsDto.Message.Replace("\n", " ").Replace(",", ";"),
                    metricsDto.LLMResponse.Replace("\n", " ").Replace(",", ";"),
                    metricsDto.TTSEnabled,
                    metricsDto.TTSAudioLength,
                    metricsDto.LLMTokenUsed,
                    metricsDto.LLMCost,
                    metricsDto.TTSCost,
                    metricsDto.TotalCost,
                    metricsDto.PreprocessTimeMs,
                    metricsDto.LLMTimeMs,
                    metricsDto.TTSTimeMs,
                    metricsDto.PersistTimeMs,
                    metricsDto.SendTimeMs,
                    metricsDto.TotalTimeMs
                );
                File.AppendAllText(csvFile, csvLine + "\n");

                // 기존 로그
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