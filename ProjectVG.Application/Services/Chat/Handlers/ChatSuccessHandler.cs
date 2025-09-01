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
            foreach (var segment in context.Segments.OrderBy(s => s.Order)) {
                if (segment.IsEmpty) continue;

                var integratedMessage = new ChatProcessResultMessage {
                    Type = segment.Type == SegmentType.Text ? "text" : "action",
                    Text = segment.Content,
                    Timestamp = DateTime.UtcNow
                };

                if (segment.Type == SegmentType.Text && segment.HasAudio)
                {
                    integratedMessage.AudioFormat = segment.AudioContentType ?? "wav";
                    integratedMessage.AudioLength = segment.AudioLength;
                    integratedMessage.SetAudioData(segment.AudioData);
                }

                var wsMessage = new WebSocketMessage("chat", integratedMessage);
                await _webSocketService.SendAsync(context.UserId.ToString(), wsMessage);
            }

            _logger.LogDebug("채팅 결과 전송 완료: 세션 {UserId}, 세그먼트 {SegmentCount}개",
                context.RequestId, context.Segments.Count(s => !s.IsEmpty));
        }
    }
}
