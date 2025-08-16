using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class UserInputActionProcessor
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<UserInputActionProcessor> _logger;

        public UserInputActionProcessor(
            IConversationService conversationService,
            ILogger<UserInputActionProcessor> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        public Task ProcessAsync(ProcessChatCommand command, UserInputAnalysis inputAnalysis)
        {
            switch (inputAnalysis.Action) {
                case UserInputAction.Ignore:
                    // 무시: 즉시 프로세스 종료, HTTP 반환
                    _logger.LogInformation("입력 무시: {Message}", command.Message);
                    throw new ValidationException(ErrorCode.INVALID_INPUT, inputAnalysis.FailureReason ?? "잘못된 입력입니다.");

                case UserInputAction.Reject:
                    // 거절: 캐시된 대화 사용, 대화 내용 저장 후 종료
                    _logger.LogInformation("입력 거절: {Message}, 사유: {Reason}", command.Message, inputAnalysis.FailureReason);
                    throw new ValidationException(ErrorCode.INAPPROPRIATE_REQUEST, inputAnalysis.FailureReason ?? "부적절한 요청입니다.");

                case UserInputAction.Chat:
                    // 대화: 정상적인 처리 계속
                    _logger.LogDebug("정상 대화 처리: {Message}", command.Message);
                    break;

                case UserInputAction.Undefined:
                    // 미정: 현재는 대화로 처리 (향후 확장 가능)
                    _logger.LogInformation("미정 액션을 대화로 처리: {Message}", command.Message);
                    break;

                default:
                    _logger.LogWarning("알 수 없는 액션: {Action}, 메시지: {Message}",
                        inputAnalysis.Action, command.Message);
                    throw new ValidationException(ErrorCode.UNKNOWN_ACTION, "알 수 없는 액션입니다.");
            }
            
            return Task.CompletedTask;
        }
    }
}
