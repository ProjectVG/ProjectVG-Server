using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Messaging;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ResultSender
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ILogger<ResultSender> _logger;

        public ResultSender(IMessageBroker messageBroker, ILogger<ResultSender> logger)
        {
            _messageBroker = messageBroker;
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
                    
                    // WebSocket 메시지로 래핑하여 전송
                    var integratedMessage = new IntegratedChatMessage
                    {
                        SessionId = context.SessionId,
                        Text = segment.Text,
                        AudioFormat = segment.AudioContentType ?? "wav",
                        AudioLength = segment.AudioLength,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    // 오디오 데이터를 Base64로 변환
                    integratedMessage.SetAudioData(segment.AudioData);
                    
                    var wsMessage = new WebSocketMessage("chat", integratedMessage);
                    await _messageBroker.SendWebSocketMessageAsync(context.SessionId, wsMessage);
                    
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