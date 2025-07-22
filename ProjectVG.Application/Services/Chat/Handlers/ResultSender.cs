using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Session;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ResultSender
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<ResultSender> _logger;

        public ResultSender(ISessionService sessionService, ILogger<ResultSender> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task SendResultAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            try
            {
                // 결과 전송 (텍스트)
                await _sessionService.SendMessageAsync(context.SessionId, result.Response);

                // 오디오(wav) 전송
                if (result.AudioData != null && result.AudioData.Length > 0)
                {
                    await _sessionService.SendAudioAsync(context.SessionId, result.AudioData, result.AudioContentType, result.AudioLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 전송 실패: 세션 {SessionId}", context.SessionId);
            }
        }
    }
} 