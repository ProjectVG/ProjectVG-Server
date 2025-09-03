using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Token;


namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatSuccessHandler
    {
        private readonly ILogger<ChatSuccessHandler> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly ITokenManagementService _tokenManagementService;

        public ChatSuccessHandler(
        ILogger<ChatSuccessHandler> logger,
        IWebSocketManager webSocketService,
        ITokenManagementService tokenManagementService)
        {
            _logger = logger;
            _webSocketService = webSocketService;
            _tokenManagementService = tokenManagementService;
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

                // 성공적인 전송 후 토큰 차감 처리
                await DeductTokensForSuccessfulChatAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 결과 전송 중 오류 발생: 요청 {RequestId}", context.RequestId);
                throw;
            }
        }

        /// <summary>
        /// 성공적인 채팅 처리 후 토큰 차감
        /// </summary>
        private async Task DeductTokensForSuccessfulChatAsync(ChatProcessContext context)
        {
            try
            {
                // 실제 사용된 Cost를 토큰으로 차감
                if (context.Cost > 0)
                {
                    var transactionId = $"CHAT_{context.RequestId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                    var result = await _tokenManagementService.DeductTokensAsync(
                        context.UserId,
                        (decimal)context.Cost,
                        transactionId,
                        "CHAT_USAGE",
                        $"채팅 사용료 - 캐릭터: {context.CharacterId}",
                        context.RequestId.ToString(),
                        "ChatSession"
                    );

                    if (result.Success)
                    {
                        _logger.LogInformation("채팅 토큰 차감 완료: {UserId}, 차감 토큰: {Cost}, 잔액: {Balance}",
                            context.UserId, context.Cost, result.BalanceAfter);
                    }
                    else
                    {
                        _logger.LogError("채팅 토큰 차감 실패: {UserId}, 에러: {Error}",
                            context.UserId, result.ErrorMessage);
                        // 토큰 차감 실패는 로그만 남기고 사용자에게는 이미 성공 응답을 보냈으므로 예외를 던지지 않음
                    }
                }
                else
                {
                    _logger.LogWarning("채팅 처리 완료했지만 Cost가 0 또는 음수: {RequestId}, Cost: {Cost}",
                        context.RequestId, context.Cost);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 토큰 차감 처리 중 예외 발생: {RequestId}", context.RequestId);
                // 토큰 차감 실패는 사용자 경험에 영향을 주지 않도록 예외를 삼킴
            }
        }
    }
}
