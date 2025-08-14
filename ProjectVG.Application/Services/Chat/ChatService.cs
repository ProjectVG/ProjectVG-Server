using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IServiceScopeFactory scopeFactory, ILogger<ChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<ChatValidationResult> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            // 검증 수행
            var validationResult = await ValidateChatRequestAsync(command);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }

            // 검증 통과 시 처리 진행
            await Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ChatOrchestrator>();
                await orchestrator.ProcessChatRequestAsync(command);
            });

            return ChatValidationResult.Success();
        }

        private async Task<ChatValidationResult> ValidateChatRequestAsync(ProcessChatCommand command)
        {
            using var scope = _scopeFactory.CreateScope();
            
            _logger.LogInformation("검증 시작: SessionId={SessionId}, UserId={UserId}, CharacterId={CharacterId}", 
                command.SessionId, command.UserId, command.CharacterId);
            
            // 1. 세션 ID 검증
            if (!string.IsNullOrEmpty(command.SessionId))
            {
                _logger.LogDebug("세션 ID 검증 중: {SessionId}", command.SessionId);
                var sessionStorage = scope.ServiceProvider.GetRequiredService<ISessionStorage>();
                var sessionExists = await sessionStorage.ExistsAsync(command.SessionId);
                if (!sessionExists)
                {
                    _logger.LogWarning("세션 ID 검증 실패: {SessionId}", command.SessionId);
                    return ChatValidationResult.Failure("유효하지 않은 세션 ID입니다.", "INVALID_SESSION_ID");
                }
                _logger.LogDebug("세션 ID 검증 성공: {SessionId}", command.SessionId);
            }

            // 2. 사용자 ID 검증
            _logger.LogDebug("사용자 ID 검증 중: {UserId}", command.UserId);
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var userExists = await userService.UserExistsAsync(command.UserId);
            if (!userExists)
            {
                _logger.LogWarning("사용자 ID 검증 실패: {UserId}", command.UserId);
                return ChatValidationResult.Failure("존재하지 않는 사용자 ID입니다.", "INVALID_USER_ID");
            }
            _logger.LogDebug("사용자 ID 검증 성공: {UserId}", command.UserId);

            // 3. 캐릭터 ID 검증
            _logger.LogDebug("캐릭터 ID 검증 중: {CharacterId}", command.CharacterId);
            var characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();
            var characterExists = await characterService.CharacterExistsAsync(command.CharacterId);
            if (!characterExists)
            {
                _logger.LogWarning("캐릭터 ID 검증 실패: {CharacterId}", command.CharacterId);
                return ChatValidationResult.Failure("존재하지 않는 캐릭터 ID입니다.", "INVALID_CHARACTER_ID");
            }
            _logger.LogDebug("캐릭터 ID 검증 성공: {CharacterId}", command.CharacterId);

            _logger.LogInformation("모든 검증 완료");
            return ChatValidationResult.Success();
        }
    }
} 