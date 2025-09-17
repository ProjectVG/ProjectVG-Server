using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Credit;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Domain.Services.Session;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class ChatRequestValidator
    {
        private readonly ISessionStorage _sessionStorage;
        private readonly IDistributedSessionManager? _distributedSessionManager;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ICreditManagementService _tokenManagementService;
        private readonly ILogger<ChatRequestValidator> _logger;

        // 채팅 기본 예상 비용 (실제 비용은 처리 후 결정됨)
        private const decimal ESTIMATED_CHAT_COST = 10m;

        public ChatRequestValidator(
            ISessionStorage sessionStorage,
            IUserService userService,
            ICharacterService characterService,
            ICreditManagementService tokenManagementService,
            ILogger<ChatRequestValidator> logger,
            IDistributedSessionManager? distributedSessionManager = null)
        {
            _sessionStorage = sessionStorage;
            _distributedSessionManager = distributedSessionManager;
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
        /// 사용자 세션 유효성 검증
        /// </summary>
        private async Task ValidateUserSessionAsync(Guid userId)
        {
            try {
                bool hasValidSession = false;

                // 분산 세션 관리자가 있으면 분산 세션 확인
                if (_distributedSessionManager != null)
                {
                    hasValidSession = await _distributedSessionManager.IsSessionActiveAsync(userId.ToString());
                    _logger.LogDebug("분산 세션 검증: {UserId}, 연결됨: {IsConnected}", userId, hasValidSession);
                }
                else
                {
                    // 레거시 모드: 로컬 세션 저장소 사용
                    var userSessions = (await _sessionStorage
                        .GetSessionsByUserIdAsync(userId.ToString()))
                        .ToList();
                    hasValidSession = userSessions.Count > 0;
                    _logger.LogDebug("로컬 세션 검증: {UserId}, 활성 세션 수: {SessionCount}", userId, userSessions.Count);
                }

                if (!hasValidSession) {
                    _logger.LogWarning("유효하지 않은 사용자 세션: {UserId}", userId);
                    throw new ValidationException(ErrorCode.SESSION_EXPIRED, "세션이 만료되었습니다. 다시 로그인해 주세요.");
                }

                _logger.LogDebug("세션 검증 성공: {UserId}", userId);
            }
            catch (ValidationException) {
                throw; // 검증 예외는 그대로 전파
            }
            catch (Exception ex) {
                _logger.LogError(ex, "세션 검증 중 예상치 못한 오류: {UserId}", userId);
                // 세션 스토리지 오류 시에는 검증을 통과시키되 로그는 남김 (서비스 가용성 우선)
                _logger.LogWarning("세션 스토리지 오류로 인해 세션 검증을 건너뜁니다: {UserId}", userId);
            }
        }
    }
}
