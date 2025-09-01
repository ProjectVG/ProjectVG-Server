using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.WebSocket;


namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatSuccessHandler
    {
        private readonly ILogger<ChatSuccessHandler> _logger;
        private readonly IWebSocketManager _webSocketService;

        public ChatSuccessHandler(
        ILogger<ChatSuccessHandler> logger,
        IWebSocketManager webSocketService)
        {
            _logger = logger;
            _webSocketService = webSocketService;
        }

        public async Task HandleAsync(ChatProcessContext context)
        {
            try
            {
                var validSegments = context.Segments
                    .Where(s => !s.IsEmpty)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (!validSegments.Any())
                {
                    _logger.LogWarning("채팅 처리 결과에 유효한 세그먼트가 없습니다: 요청 {RequestId}", context.RequestId);
                    return;
                }

                var requestId = context.RequestId.ToString();
                var userId = context.UserId.ToString();

                foreach (var segment in validSegments)
                {
                    try
                    {
                        var message = ChatProcessResultMessage.FromSegment(segment, requestId);
                        var wsMessage = new WebSocketMessage("chat", message);
                        
                        await _webSocketService.SendAsync(userId, wsMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "세그먼트 전송 실패: 사용자 {UserId}, 세그먼트 순서 {Order}", 
                            userId, segment.Order);
                        throw;
                    }
                }

                _logger.LogDebug("채팅 결과 전송 완료: 요청 {RequestId}, 세그먼트 {SegmentCount}개",
                    context.RequestId, validSegments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 결과 전송 중 오류 발생: 요청 {RequestId}", context.RequestId);
                throw;
            }
        }
    }
}
