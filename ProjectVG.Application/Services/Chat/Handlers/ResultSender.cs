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
                // 여러 텍스트/오디오 쌍을 순서대로 전송
                for (int i = 0; i < result.Text.Count; i++)
                {
                    await _sessionService.SendMessageAsync(context.SessionId, result.Text[i]);

                    if (result.AudioDataList.Count > i && result.AudioDataList[i] != null && result.AudioDataList[i].Length > 0)
                    {
                        await _sessionService.SendAudioAsync(
                            context.SessionId,
                            result.AudioDataList[i],
                            result.AudioContentTypeList.Count > i ? result.AudioContentTypeList[i] : null,
                            result.AudioLengthList.Count > i ? result.AudioLengthList[i] : null
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 전송 실패: 세션 {SessionId}", context.SessionId);
            }
        }
    }
} 