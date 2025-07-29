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
                // 세그먼트들을 순서대로 전송
                foreach (var segment in result.Segments.OrderBy(s => s.Order))
                {
                    if (segment.IsEmpty)
                    {
                        _logger.LogDebug("빈 세그먼트 건너뜀: 세션 {SessionId}, 순서 {Order}", 
                            context.SessionId, segment.Order);
                        continue;
                    }
                    
                    // 통합 메시지로 전송
                    await _sessionService.SendIntegratedMessageAsync(
                        context.SessionId,
                        segment.Text,
                        segment.AudioData,
                        segment.AudioContentType ?? "wav",
                        segment.AudioLength
                    );
                    
                    _logger.LogDebug("세그먼트 전송 완료: 세션 {SessionId}, 순서 {Order}, 텍스트: {HasText}, 오디오: {HasAudio}", 
                        context.SessionId, segment.Order, segment.HasText, segment.HasAudio);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 전송 실패: 세션 {SessionId}", context.SessionId);
            }
        }
    }
} 