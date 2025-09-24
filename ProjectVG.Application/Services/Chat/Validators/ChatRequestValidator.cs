using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Session;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class ChatRequestValidator
    {
        private readonly ISessionManager _sessionManager;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ICreditManagementService _tokenManagementService;
        private readonly ILogger<ChatRequestValidator> _logger;

        // 채팅 기본 예상 비용 (실제 비용은 처리 후 결정됨)
        private const decimal ESTIMATED_CHAT_COST = 10m;

        public ChatRequestValidator(
            ISessionManager sessionManager,
            IUserService userService,
            ICharacterService characterService,
            ICreditManagementService tokenManagementService,
            ILogger<ChatRequestValidator> logger)
        {
            _sessionManager = sessionManager;
            _userService = userService;
            _characterService = characterService;
            _tokenManagementService = tokenManagementService;
            _logger = logger;
        }

        public async Task ValidateAsync(ChatRequestCommand command)
        {
            // 세션 검증 - 사용자 활성 세션 확인
            await ValidateUserSessionAsync(command.UserId);

            var userExists = await _userService.ExistsByIdAsync(command.UserId);
            if (!userExists) {
                _logger.LogWarning("사용자 ID 검증 실패: {UserId}", command.UserId);
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, command.UserId);
            }

            var characterExists = await _characterService.CharacterExistsAsync(command.CharacterId);
            if (!characterExists) {
                _logger.LogWarning("캐릭터 ID 검증 실패: {CharacterId}", command.CharacterId);
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, command.CharacterId);
            }

            // 토큰 잔액 검증 - 예상 비용으로 미리 확인
            var balance = await _tokenManagementService.GetCreditBalanceAsync(command.UserId);
            var currentBalance = balance.CurrentBalance;
            
            if (currentBalance <= 0) {
                _logger.LogWarning("토큰 잔액 부족 (0 토큰): UserId={UserId}", command.UserId);
                throw new ValidationException(ErrorCode.INSUFFICIENT_CREDIT_BALANCE, $"토큰이 부족합니다. 현재 잔액: {currentBalance} 토큰, 필요 토큰: {ESTIMATED_CHAT_COST} 토큰");
            }
            
            var hasSufficientTokens = currentBalance >= ESTIMATED_CHAT_COST;
            if (!hasSufficientTokens) {
                _logger.LogWarning("토큰 부족: UserId={UserId}, 현재잔액={CurrentBalance}, 필요토큰={RequiredTokens}", 
                    command.UserId, currentBalance, ESTIMATED_CHAT_COST);
                throw new ValidationException(ErrorCode.INSUFFICIENT_CREDIT_BALANCE, $"토큰이 부족합니다. 현재 잔액: {currentBalance} 토큰, 필요 토큰: {ESTIMATED_CHAT_COST} 토큰");
            }

            _logger.LogDebug("채팅 요청 검증 완료: {UserId}, {CharacterId}", command.UserId, command.CharacterId);
        }

        /// <summary>
        /// 새 아키텍처: 세션 관리자를 통한 세션 유효성 검증
        /// Redis 기반 분산 세션 상태를 확인합니다.
        /// </summary>
        private async Task ValidateUserSessionAsync(Guid userId)
        {
            try
            {
                // [디버그] 세션 관리자 상태 정보 조회
                var activeSessionCount = await _sessionManager.GetActiveSessionCountAsync();
                var activeUserIds = await _sessionManager.GetActiveUserIdsAsync();
                _logger.LogInformation("[ChatRequestValidator] 현재 활성 세션: 총 {Count}개, UserIds=[{ActiveUserIds}]",
                    activeSessionCount, string.Join(", ", activeUserIds.Take(10))); // 너무 많은 로그 방지

                // 세션 관리자에서 세션 상태 확인 (Redis 기반)
                bool isSessionActive = await _sessionManager.IsSessionActiveAsync(userId);

                _logger.LogInformation("[ChatRequestValidator] 세션 상태 확인: UserId={UserId}, IsActive={IsActive}",
                    userId, isSessionActive);

                if (!isSessionActive)
                {
                    _logger.LogWarning("활성 세션이 존재하지 않습니다: {UserId}", userId);
                    throw new ValidationException(ErrorCode.WEBSOCKET_SESSION_REQUIRED,
                        "채팅 요청을 처리하려면 WebSocket 연결이 필요합니다. 먼저 WebSocket에 연결해주세요.");
                }

                _logger.LogDebug("세션 검증 성공: {UserId}", userId);

                // 세션 하트비트 업데이트 (세션 TTL 갱신)
                try
                {
                    await _sessionManager.UpdateSessionHeartbeatAsync(userId);
                    _logger.LogDebug("세션 하트비트 업데이트 완료: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    // 하트비트 업데이트 실패는 로그만 남기고 진행
                    _logger.LogWarning(ex, "세션 하트비트 업데이트 실패 (계속 진행): {UserId}", userId);
                }
            }
            catch (ValidationException)
            {
                // ValidationException은 그대로 다시 던짐
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 검증 중 오류 발생: {UserId}", userId);
                throw new ValidationException(ErrorCode.WEBSOCKET_SESSION_REQUIRED,
                    "세션 상태 확인 중 오류가 발생했습니다. 다시 WebSocket에 연결해주세요.");
            }
        }
    }
}
