using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.MessageBroker;


namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatSuccessHandler
    {
        private readonly ILogger<ChatSuccessHandler> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly ICreditManagementService _tokenManagementService;

        public ChatSuccessHandler(
        ILogger<ChatSuccessHandler> logger,
        IMessageBroker messageBroker,
        ICreditManagementService tokenManagementService)
        {
            _logger = logger;
            _messageBroker = messageBroker;
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

                // 토큰 차감 및 잔액 정보 수집
                decimal? tokensUsed = null;
                decimal? tokensRemaining = null;
                
                if (context.Cost > 0)
                {
                    var tokenDeductionResult = await DeductTokensForChatAsync(context);
                    if (tokenDeductionResult.Success)
                    {
                        tokensUsed = (decimal)context.Cost;
                        tokensRemaining = tokenDeductionResult.BalanceAfter;
                    }
                }

                foreach (var segment in validSegments)
                {
                    try
                    {
                        var message = ChatProcessResultMessage.FromSegment(segment, requestId)
                            .WithCreditInfo(tokensUsed, tokensRemaining);
                        var wsMessage = new WebSocketMessage("chat", message);

                        _logger.LogInformation("[메시지브로커] 사용자에게 메시지 전송 시작: UserId={UserId}, MessageType={MessageType}, SegmentOrder={Order}, BrokerType={BrokerType}",
                            userId, wsMessage.Type, segment.Order, _messageBroker.IsDistributed ? "Distributed" : "Local");

                        await _messageBroker.SendToUserAsync(userId, wsMessage);

                        _logger.LogInformation("[메시지브로커] 사용자에게 메시지 전송 완료: UserId={UserId}, MessageType={MessageType}, SegmentOrder={Order}",
                            userId, wsMessage.Type, segment.Order);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "세그먼트 전송 실패: 사용자 {UserId}, 세그먼트 순서 {Order}", 
                            userId, segment.Order);
                        throw;
                    }
                }

                _logger.LogDebug("채팅 결과 전송 완료: 요청 {RequestId}, 세그먼트 {SegmentCount}개, 토큰 사용: {CreditsUsed}, 잔액: {CreditsRemaining}",
                    context.RequestId, validSegments.Count, tokensUsed, tokensRemaining);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 결과 전송 중 오류 발생: 요청 {RequestId}", context.RequestId);
                throw;
            }
        }

        /// <summary>
        /// 채팅 처리를 위한 토큰 차감
        /// </summary>
        private async Task<CreditTransactionResult> DeductTokensForChatAsync(ChatProcessContext context)
        {
            try
            {
                var transactionId = $"CHAT_{context.RequestId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var result = await _tokenManagementService.DeductCreditsAsync(
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
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 토큰 차감 처리 중 예외 발생: {RequestId}", context.RequestId);
                return CreditTransactionResult.CreateFailure($"토큰 차감 처리 중 예외 발생: {ex.Message}");
            }
        }
    }
}
