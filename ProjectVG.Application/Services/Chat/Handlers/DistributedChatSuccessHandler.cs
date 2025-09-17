using ProjectVG.Application.Models.Chat;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Domain.Services.MessageBus;
using ProjectVG.Domain.Services.Session;
using ProjectVG.Application.Services.WebSocket;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    /// <summary>
    /// 분산 환경에서 채팅 결과를 처리하는 핸들러
    /// </summary>
    public class DistributedChatSuccessHandler
    {
        private readonly ILogger<DistributedChatSuccessHandler> _logger;
        private readonly IDistributedWebSocketManager _distributedWebSocketManager;
        private readonly IDistributedMessageBus _messageBus;
        private readonly IDistributedSessionManager _sessionManager;
        private readonly ICreditManagementService _creditManagementService;

        public DistributedChatSuccessHandler(
            ILogger<DistributedChatSuccessHandler> logger,
            IDistributedWebSocketManager distributedWebSocketManager,
            IDistributedMessageBus messageBus,
            IDistributedSessionManager sessionManager,
            ICreditManagementService creditManagementService)
        {
            _logger = logger;
            _distributedWebSocketManager = distributedWebSocketManager;
            _messageBus = messageBus;
            _sessionManager = sessionManager;
            _creditManagementService = creditManagementService;
        }

        /// <summary>
        /// 분산 환경에서 채팅 결과를 처리합니다
        /// </summary>
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

                var userId = context.UserId.ToString();

                // 1. 토큰 차감 및 잔액 정보 수집
                decimal? creditsUsed = null;
                decimal? creditsRemaining = null;

                if (context.Cost > 0)
                {
                    var creditDeductionResult = await DeductCreditsForChatAsync(context);
                    if (creditDeductionResult.Success)
                    {
                        creditsUsed = (decimal)context.Cost;
                        creditsRemaining = creditDeductionResult.BalanceAfter;
                    }
                }

                // 2. 사용자 세션이 있는지 확인
                var isSessionActive = await _sessionManager.IsSessionActiveAsync(userId);
                if (!isSessionActive)
                {
                    _logger.LogWarning("사용자 세션이 활성화되지 않음: UserId={UserId}, RequestId={RequestId}",
                        userId, context.RequestId);
                    return;
                }

                // 3. 분산 방식으로 결과 전달
                await SendChatResultsDistributedAsync(context, validSegments, creditsUsed, creditsRemaining);

                _logger.LogInformation("분산 채팅 결과 전송 완료: RequestId={RequestId}, Segments={SegmentCount}, CreditsUsed={CreditsUsed}",
                    context.RequestId, validSegments.Count, creditsUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 채팅 결과 처리 중 오류 발생: RequestId={RequestId}", context.RequestId);

                // 실패 알림도 분산으로 전달
                await SendChatErrorDistributedAsync(context, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 분산 방식으로 채팅 결과를 전달합니다
        /// </summary>
        private async Task SendChatResultsDistributedAsync(
            ChatProcessContext context,
            List<ChatSegment> validSegments,
            decimal? creditsUsed,
            decimal? creditsRemaining)
        {
            var userId = context.UserId.ToString();
            var requestId = context.RequestId.ToString();

            foreach (var segment in validSegments)
            {
                try
                {
                    // WebSocket 메시지 생성
                    var chatMessage = ChatProcessResultMessage.FromSegment(segment, requestId)
                        .WithCreditInfo(creditsUsed, creditsRemaining);

                    var webSocketMessage = new Models.WebSocket.WebSocketMessage("chat", chatMessage);

                    // 분산 WebSocket 관리자를 통해 전송
                    await _distributedWebSocketManager.SendAsync(userId, webSocketMessage);

                    _logger.LogDebug("분산 채팅 세그먼트 전송: UserId={UserId}, Order={Order}, HasContent={HasContent}",
                        userId, segment.Order, segment.HasContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "분산 채팅 세그먼트 전송 실패: UserId={UserId}, Order={Order}",
                        userId, segment.Order);

                    // 개별 세그먼트 실패는 전체 실패로 이어지지 않도록 함
                }
            }
        }

        /// <summary>
        /// 분산 방식으로 채팅 오류를 전달합니다
        /// </summary>
        private async Task SendChatErrorDistributedAsync(ChatProcessContext context, string errorMessage)
        {
            try
            {
                var userId = context.UserId.ToString();

                var errorResponse = new
                {
                    requestId = context.RequestId.ToString(),
                    success = false,
                    error = errorMessage,
                    timestamp = DateTime.UtcNow
                };

                var webSocketMessage = new Models.WebSocket.WebSocketMessage("chat_error", errorResponse);
                await _distributedWebSocketManager.SendAsync(userId, webSocketMessage);

                _logger.LogDebug("분산 채팅 오류 전송: UserId={UserId}, Error={Error}", userId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 채팅 오류 전송 실패: UserId={UserId}", context.UserId);
            }
        }

        /// <summary>
        /// 채팅 처리를 위한 크레딧 차감
        /// </summary>
        private async Task<CreditTransactionResult> DeductCreditsForChatAsync(ChatProcessContext context)
        {
            try
            {
                var transactionId = $"CHAT_{context.RequestId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var result = await _creditManagementService.DeductCreditsAsync(
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
                    _logger.LogInformation("분산 채팅 크레딧 차감 완료: UserId={UserId}, Cost={Cost}, Balance={Balance}",
                        context.UserId, context.Cost, result.BalanceAfter);
                }
                else
                {
                    _logger.LogError("분산 채팅 크레딧 차감 실패: UserId={UserId}, Error={Error}",
                        context.UserId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 채팅 크레딧 차감 처리 중 예외 발생: RequestId={RequestId}", context.RequestId);
                return CreditTransactionResult.CreateFailure($"크레딧 차감 처리 중 예외 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 버스를 통해 채팅 결과를 브로드캐스트합니다 (필요한 경우)
        /// </summary>
        public async Task BroadcastChatResultAsync(ChatProcessContext context)
        {
            try
            {
                var chatResultMessage = new ChatResultMessage
                {
                    TargetUserId = context.UserId.ToString(),
                    ConversationId = context.CharacterId.ToString(),
                    MessageId = context.RequestId.ToString(),
                    Content = context.Response ?? string.Empty,
                    AudioData = null,
                    Cost = context.Cost,
                    Success = true
                };

                await _messageBus.PublishAsync("channel:chat_results", chatResultMessage);

                _logger.LogDebug("채팅 결과 브로드캐스트: RequestId={RequestId}, UserId={UserId}",
                    context.RequestId, context.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 결과 브로드캐스트 실패: RequestId={RequestId}", context.RequestId);
            }
        }
    }
}