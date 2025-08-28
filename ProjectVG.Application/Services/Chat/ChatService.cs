using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Handlers;
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
        private readonly ICostTrackingDecorator<UserInputAnalysisProcessor> _inputProcessor;
        private readonly UserInputActionProcessor _actionProcessor;

        private readonly ICostTrackingDecorator<ChatLLMProcessor> _llmProcessor;
        private readonly ICostTrackingDecorator<ChatTTSProcessor> _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;
        private readonly IChatMetricsService _metricsService;
        private readonly ChatFailureHandler _failureHandler;

        /// <summary>
        /// ChatService의 인스턴스를 초기화합니다. 채팅 요청의 검증, 컨텍스트 준비, 입력/LLM/TTS 처리, 결과 전송 및 실패/메트릭 관리를 위한 의존성을 주입하고 필드를 설정합니다.
        /// </summary>
        public ChatService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatService> logger,
            IConversationService conversationService,
            ICharacterService characterService,
            ChatRequestValidator validator,
            MemoryContextPreprocessor memoryPreprocessor,
            ICostTrackingDecorator<UserInputAnalysisProcessor> inputProcessor,
            UserInputActionProcessor actionProcessor,
            ICostTrackingDecorator<ChatLLMProcessor> llmProcessor,
            ICostTrackingDecorator<ChatTTSProcessor> ttsProcessor,
            ChatResultProcessor resultProcessor,
            IChatMetricsService metricsService,
            ChatFailureHandler failureHandler
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
            _metricsService = metricsService;
            _failureHandler = failureHandler;
        }

        /// <summary>
        /// 채팅 요청을 큐에 등록하고 비동기 처리 작업을 시작합니다.
        /// </summary>
        /// <remarks>
        /// 함수는 요청 메트릭을 시작하고 요청 유효성 검사를 수행한 뒤 내부 처리에 필요한 컨텍스트를 준비합니다.
        /// 이후 실제 처리(LLM/TTS/전송/영속화)는 백그라운드 작업으로 비동기 실행되며, 호출자는 즉시 수락 응답을 받습니다.
        /// </remarks>
        /// <param name="command">처리할 채팅 요청 정보(세션, 사용자, 캐릭터 식별자와 메시지 등)를 포함합니다.</param>
        /// <returns>요청이 수락되었음을 나타내는 ChatRequestResponse(세션Id, 사용자Id, 캐릭터Id 포함)를 반환합니다.</returns>
        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            _metricsService.StartChatMetrics(command.SessionId, command.UserId.ToString(), command.CharacterId.ToString());
            
            await _validator.ValidateAsync(command);

            var preprocessContext = await PrepareChatRequestAsync(command);

            _ = Task.Run(async () => {
                await ProcessChatRequestInternalAsync(preprocessContext);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }


        /// <summary>
        /// 채팅 요청 준비
        /// <summary>
        /// 채팅 요청을 처리하기 위한 실행 컨텍스트를 준비합니다.
        /// </summary>
        /// <remarks>
        /// 지정된 명령에서 캐릭터 데이터를 조회하고(캐릭터 ID),
        /// 최근 대화 내역을 가져온 후(사용자·캐릭터),
        /// 사용자 입력 분석과 연관된 액션을 처리하고 메모리 컨텍스트를 수집합니다.
        /// 반환되는 ChatProcessContext는 이후 LLM/TTS 및 결과 처리 파이프라인에서 사용됩니다.
        /// </remarks>
        /// <returns>LLM·TTS 처리에 필요한 명령, 캐릭터 정보, 대화 기록 및 메모리 컨텍스트를 포함한 ChatProcessContext 객체.</returns>
        private async Task<ChatProcessContext> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            var characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);
            var conversationHistory = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 10);

            var inputAnalysis = await _inputProcessor.ProcessAsync(command.Message, conversationHistory);
            await _actionProcessor.ProcessAsync(command, inputAnalysis);
            
            var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message, inputAnalysis);

            return new ChatProcessContext(command, characterDto!, conversationHistory, memoryContext);
        }

        /// <summary>
        /// 채팅 요청 처리
        /// <summary>
        /// 채팅 처리 파이프라인(LLM → TTS)을 실행하고 결과를 전송·영속화하며 메트릭과 실패 처리를 관리합니다.
        /// </summary>
        /// <param name="context">처리에 필요한 명령, 캐릭터, 대화 내역 및 메모리 컨텍스트를 포함한 실행 컨텍스트.</param>
        /// <remarks>
        /// - LLM과 TTS 처리 후 DI 범위를 생성하여 ChatResultProcessor로 결과를 전송하고 저장합니다.
        /// - 실행 중 발생한 예외는 ChatFailureHandler에 위임되어 처리되며, 예외는 호출자에게 전파되지 않습니다.
        /// - 호출이 완료되면 메트릭 종료 및 로깅이 수행됩니다.
        /// </remarks>
        private async Task ProcessChatRequestInternalAsync(ChatProcessContext context)
        {
            try {
                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context);
                
                using var scope = _scopeFactory.CreateScope();
                var resultProcessor = scope.ServiceProvider.GetRequiredService<ChatResultProcessor>();
                await resultProcessor.SendResultsAsync(context);
                await resultProcessor.PersistResultsAsync(context);
            }
            catch (Exception ex) {
                await _failureHandler.HandleFailureAsync(context, ex);
            }
            finally {
                _metricsService.EndChatMetrics();
                _metricsService.LogChatMetrics();
            }
        }
    }
}
