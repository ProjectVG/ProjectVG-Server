using ProjectVG.Application.Services.Conversation;
using ProjectVG.Domain.Enums;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class UserInputActionProcessor
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<UserInputActionProcessor> _logger;

        /// <summary>
        /// UserInputActionProcessor의 인스턴스를 초기화합니다.
        /// </summary>
        public UserInputActionProcessor(
            IConversationService conversationService,
            ILogger<UserInputActionProcessor> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 입력 분석 결과에 따라 처리 흐름을 결정한다.
        /// </summary>
        /// <remarks>
        /// 입력 분석의 Action 값에 따라 다음을 수행한다:
        /// - Ignore: 입력을 무시하고 즉시 ValidationException(ErrorCode.INVALID_INPUT)을 던진다.
        /// - Reject: 부적절한 요청으로 간주하여 ValidationException(ErrorCode.INAPPROPRIATE_REQUEST)을 던진다.
        /// - Chat, Undefined: 정상 대화로 처리하여 메서드를 정상 종료한다.
        /// </remarks>
        /// <param name="command">처리 대상 채팅 명령(메시지 등)을 포함한다.</param>
        /// <param name="inputAnalysis">사용자 입력에 대한 분석 결과(액션 및 실패 사유 등).</param>
        /// <returns>비동기 처리 완료를 나타내는 Task (예외가 없으면 CompletedTask).</returns>
        /// <exception cref="ValidationException">다음 상황에서 발생:
        /// - Action이 Ignore인 경우: ErrorCode.INVALID_INPUT,
        /// - Action이 Reject인 경우: ErrorCode.INAPPROPRIATE_REQUEST,
        /// - 알 수 없는 Action인 경우: ErrorCode.UNKNOWN_ACTION.</exception>
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
