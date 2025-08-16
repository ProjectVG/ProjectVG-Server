using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Character;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;

        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;

        private readonly ChatRequestValidator _validator;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly UserInputAnalysisProcessor _inputProcessor;
        private readonly UserInputActionProcessor _actionProcessor;

        private readonly ChatLLMProcessor _llmProcessor;
        private readonly ChatTTSProcessor _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;

        /// <summary>
        /// ChatService의 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 전달된 의존성(서비스 및 프로세서)을 필드에 할당하여 ChatService의 채팅 요청 준비 및 비동기 처리 파이프라인을 초기화합니다.
        /// 모든 매개변수는 외부에서 제공되는 서비스/프로세서이며, null 체크는 호출 측에서 보장되어야 합니다.
        /// </remarks>
        public ChatService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatService> logger,
            IConversationService conversationService,
            ICharacterService characterService,
            ChatRequestValidator validator,
            MemoryContextPreprocessor memoryPreprocessor,
            UserInputAnalysisProcessor inputProcessor,
            UserInputActionProcessor actionProcessor,
            ChatLLMProcessor llmProcessor,
            ChatTTSProcessor ttsProcessor,
            ChatResultProcessor resultProcessor
        ) {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _conversationService = conversationService;
            _characterService = characterService;
            _validator = validator;
            _memoryPreprocessor = memoryPreprocessor;
            _inputProcessor = inputProcessor;
            _actionProcessor = actionProcessor;

            _llmProcessor = llmProcessor;
            _ttsProcessor = ttsProcessor;
            _resultProcessor = resultProcessor;
        }

        /// <summary>
        /// 채팅 요청을 검증하고 처리 준비를 한 뒤 비동기 백그라운드 작업으로 처리하도록 큐에 등록하고 즉시 수락 응답을 반환합니다.
        /// </summary>
        /// <param name="command">처리할 채팅 명령(세션, 사용자, 캐릭터 식별자 및 메시지 등 요청 데이터 포함).</param>
        /// <returns>요청이 수락되었음을 나타내는 ChatRequestResponse(세션, 사용자, 캐릭터 ID 포함).</returns>
        /// <remarks>
        /// - 이 메서드는 먼저 입력 명령을 비동기 검증합니다. 검증 실패 시 검증 예외가 호출자에게 전파되며 백그라운드 작업은 시작되지 않습니다.
        /// - 검증이 성공하면 요청을 준비(대화 이력 조회, 입력 분석, 액션 처리, 메모리 수집 등)하고, 준비된 컨텍스트를 사용해 별도의 스코프에서 실행되는 백그라운드 태스크로 실제 처리 절차를 시작합니다.
        /// - 반환은 백그라운드 처리가 완료되기를 기다리지 않고 즉시 수락 응답을 돌려줍니다.
        /// </remarks>
        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            await _validator.ValidateAsync(command);

            var preprocessContext = await PrepareChatRequestAsync(command);

            _ = Task.Run(async () => {
                using var processScope = _scopeFactory.CreateScope();
                await ProcessChatRequestInternalAsync(preprocessContext);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }


        /// <summary>
        /// 채팅 요청 준비
        /// <summary>
        /// 채팅 요청을 처리하기 전에 필요한 데이터를 수집하고 준비합니다.
        /// </summary>
        /// <remarks>
        /// - 지정된 캐릭터를 조회하여 요청(command)에 할당합니다.
        /// - 최근 대화(최대 10개)를 조회하고, 사용자 입력에 대한 분석 및 이에 따른 액션을 실행합니다.
        /// - 분석 결과와 입력을 기반으로 메모리 컨텍스트를 수집합니다.
        /// 반환되는 ChatPreprocessContext는 준비된 요청(command), 수집된 메모리 컨텍스트, 대화 기록을 포함합니다.
        /// </remarks>
        /// <param name="command">처리할 채팅 명령. 호출 후 캐릭터 정보가 설정되고, 액션 처리 결과가 반영됩니다.</param>
        /// <returns>준비된 입력을 포함하는 ChatPreprocessContext 인스턴스.</returns>
        private async Task<ChatPreprocessContext> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            var characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);
            command.SetCharacter(characterDto!);
            
            var conversationHistory = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 10);

            var inputAnalysis = await _inputProcessor.ProcessAsync(command.Message, conversationHistory);
            await _actionProcessor.ProcessAsync(command, inputAnalysis);
            
            var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message, inputAnalysis);

            return new ChatPreprocessContext(
                command,
                memoryContext,
                conversationHistory
            );
        }

        /// <summary>
        /// 채팅 요청 처리
        /// <summary>
        /// 전처리된 채팅 요청 컨텍스트를 받아 백그라운드 처리 파이프라인(LLM → TTS → 전송 → 영구 저장)을 순차적으로 실행한다.
        /// </summary>
        /// <param name="context">PrepareChatRequestAsync로 생성된 ChatPreprocessContext. 세션 식별자, 사용자/캐릭터 정보, 메모리 및 대화 이력이 포함된다.</param>
        /// <remarks>
        /// 처리 중 발생한 예외는 내부에서 캡처하여 로깅만 수행하며 예외를 다시 던지지 않는다.
        /// </remarks>
        private async Task ProcessChatRequestInternalAsync(ChatPreprocessContext context)
        {
            try {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", context.SessionId);

                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                var llmResult = await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context, llmResult);
                await _resultProcessor.SendResultsAsync(context, llmResult);
                await _resultProcessor.PersistResultsAsync(context, llmResult);

                _logger.LogInformation("채팅 요청 처리 완료: {SessionId}, 토큰: {TokensUsed}",
                    context.SessionId, llmResult.TokensUsed);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", context.SessionId);
            }
        }
    }
}
