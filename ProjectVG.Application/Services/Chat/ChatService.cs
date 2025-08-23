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
        /// </summary>
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
        /// </summary>
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
