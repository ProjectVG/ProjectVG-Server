using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class ChatRequestValidator
    {
        private readonly ISessionStorage _sessionStorage;
        private readonly IWebSocketManager _webSocketManager;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ICreditManagementService _tokenManagementService;
        private readonly ILogger<ChatRequestValidator> _logger;

        // 채팅 기본 예상 비용 (실제 비용은 처리 후 결정됨)
        private const decimal ESTIMATED_CHAT_COST = 10m;

        public ChatRequestValidator(
            ISessionStorage sessionStorage,
            IWebSocketManager webSocketManager,
            IUserService userService,
            ICharacterService characterService,
            ICreditManagementService tokenManagementService,
            ILogger<ChatRequestValidator> logger)
        {
            _sessionStorage = sessionStorage;
            _webSocketManager = webSocketManager;
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
        /// 사용자 WebSocket 세션 유효성 검증
        /// 분산 환경에서 WebSocket 연결이 활성화되어 있는지 확인합니다.
        /// </summary>
        private async Task ValidateUserSessionAsync(Guid userId)
        {
            var userIdString = userId.ToString();

            // 분산 WebSocket 매니저에서 실제 연결 상태 확인
            bool isWebSocketConnected = _webSocketManager.IsSessionActive(userIdString);

            if (!isWebSocketConnected)
            {
                _logger.LogWarning("WebSocket 세션이 연결되어 있지 않습니다: {UserId}", userId);
                throw new ValidationException(ErrorCode.WEBSOCKET_SESSION_REQUIRED,
                    "채팅 요청을 처리하려면 WebSocket 연결이 필요합니다. 먼저 WebSocket에 연결해주세요.");
            }

            _logger.LogDebug("WebSocket 세션 검증 성공: {UserId}", userId);

            // 추가로 세션 하트비트 업데이트 (선택사항)
            try
            {
                await _webSocketManager.UpdateSessionHeartbeatAsync(userIdString);
                _logger.LogDebug("세션 하트비트 업데이트 완료: {UserId}", userId);
            }
            catch (Exception ex)
            {
                // 하트비트 업데이트 실패는 로그만 남기고 진행
                _logger.LogWarning(ex, "세션 하트비트 업데이트 실패 (계속 진행): {UserId}", userId);
            }
        }
    }
}
