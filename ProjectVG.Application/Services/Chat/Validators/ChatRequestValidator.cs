using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Token;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class ChatRequestValidator
    {
        private readonly ISessionStorage _sessionStorage;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ITokenManagementService _tokenManagementService;
        private readonly ILogger<ChatRequestValidator> _logger;

        // 채팅 기본 예상 비용 (실제 비용은 처리 후 결정됨)
        private const decimal ESTIMATED_CHAT_COST = 10m;

        public ChatRequestValidator(
            ISessionStorage sessionStorage,
            IUserService userService,
            ICharacterService characterService,
            ITokenManagementService tokenManagementService,
            ILogger<ChatRequestValidator> logger)
        {
            _sessionStorage = sessionStorage;
            _userService = userService;
            _characterService = characterService;
            _tokenManagementService = tokenManagementService;
            _logger = logger;
        }

        public async Task ValidateAsync(ChatRequestCommand command)
        {
            // TODO : 세션 검증

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
            var balance = await _tokenManagementService.GetTokenBalanceAsync(command.UserId);
            var currentBalance = balance.CurrentBalance;
            
            if (currentBalance <= 0) {
                _logger.LogWarning("토큰 잔액 부족 (0 토큰): UserId={UserId}", command.UserId);
                throw new ValidationException(ErrorCode.INSUFFICIENT_TOKEN_BALANCE, $"토큰이 부족합니다. 현재 잔액: {currentBalance} 토큰, 필요 토큰: {ESTIMATED_CHAT_COST} 토큰");
            }
            
            var hasSufficientTokens = currentBalance >= ESTIMATED_CHAT_COST;
            if (!hasSufficientTokens) {
                _logger.LogWarning("토큰 부족: UserId={UserId}, 현재잔액={CurrentBalance}, 필요토큰={RequiredTokens}", 
                    command.UserId, currentBalance, ESTIMATED_CHAT_COST);
                throw new ValidationException(ErrorCode.INSUFFICIENT_TOKEN_BALANCE, $"토큰이 부족합니다. 현재 잔액: {currentBalance} 토큰, 필요 토큰: {ESTIMATED_CHAT_COST} 토큰");
            }

            _logger.LogDebug("채팅 요청 검증 완료: {UserId}, {CharacterId}", command.UserId, command.CharacterId);
        }
    }
}
