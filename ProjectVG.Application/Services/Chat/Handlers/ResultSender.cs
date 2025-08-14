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
            foreach (var segment in result.Segments.OrderBy(s => s.Order))
            {
                if (segment.IsEmpty) continue;
                
                var integratedMessage = new IntegratedChatMessage
                {
                    SessionId = context.SessionId,
                    Text = segment.Text,
                    AudioFormat = segment.AudioContentType ?? "wav",
                    AudioLength = segment.AudioLength,
                    Timestamp = DateTime.UtcNow
                };
                
                integratedMessage.SetAudioData(segment.AudioData);
                
                var wsMessage = new WebSocketMessage("chat", integratedMessage);
                await _messageBroker.SendWebSocketMessageAsync(context.SessionId, wsMessage);
            }
        }
    }
} 