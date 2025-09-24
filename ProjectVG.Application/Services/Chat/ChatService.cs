using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.WebSocket;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;
        private readonly IChatMetricsService _metricsService;

        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;
        private readonly IWebSocketManager _webSocketManager;

        private readonly ChatRequestValidator _validator;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly ICostTrackingDecorator<UserInputAnalysisProcessor> _inputProcessor;
        private readonly UserInputActionProcessor _actionProcessor;

        private readonly ICostTrackingDecorator<ChatLLMProcessor> _llmProcessor;
        private readonly ICostTrackingDecorator<ChatTTSProcessor> _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;

        private readonly ChatFailureHandler _chatFailureHandler;

        public ChatService(
            IChatMetricsService metricsService,
            IServiceScopeFactory scopeFactory,
            ILogger<ChatService> logger,
            IConversationService conversationService,
            ICharacterService characterService,
            IWebSocketManager webSocketManager,
            ChatRequestValidator validator,
            MemoryContextPreprocessor memoryPreprocessor,
            ICostTrackingDecorator<UserInputAnalysisProcessor> inputProcessor,
            UserInputActionProcessor actionProcessor,
            ICostTrackingDecorator<ChatLLMProcessor> llmProcessor,
            ICostTrackingDecorator<ChatTTSProcessor> ttsProcessor,
            ChatResultProcessor resultProcessor,

            ChatFailureHandler chatFailureHandler
        ) {
            _metricsService = metricsService;
            _scopeFactory = scopeFactory;
            _logger = logger;

            _conversationService = conversationService;
            _characterService = characterService;
            _webSocketManager = webSocketManager;
            _validator = validator;
            _memoryPreprocessor = memoryPreprocessor;
            _inputProcessor = inputProcessor;
            _actionProcessor = actionProcessor;
            _llmProcessor = llmProcessor;
            _ttsProcessor = ttsProcessor;
            _resultProcessor = resultProcessor;
            _chatFailureHandler = chatFailureHandler;
        }

        public async Task<ChatRequestResult> EnqueueChatRequestAsync(ChatRequestCommand command)
        {
            _metricsService.StartChatMetrics(command.Id.ToString(), command.UserId.ToString(), command.CharacterId.ToString());
            
            await _validator.ValidateAsync(command);
            _logger.LogDebug("[채팅서비스] 요청 검증 완료: UserId={UserId}, CharacterId={CharacterId}",
                command.UserId, command.CharacterId);

            var preprocessContext = await PrepareChatRequestAsync(command);

            _ = Task.Run(async () => {
                await ProcessChatRequestInternalAsync(preprocessContext);
            });

            return ChatRequestResult.Accepted(command.Id.ToString(), command.UserId, command.CharacterId);
        }


        /// <summary>
        /// 채팅 요청 전처리 
        /// </summary>
        private async Task<ChatProcessContext> PrepareChatRequestAsync(ChatRequestCommand command)
        {
            await _inputProcessor.ProcessAsync(command);
            await _actionProcessor.ProcessAsync(command);

            var characterInfo = await _characterService.GetCharacterByIdAsync(command.CharacterId);
            
            var conversationHistoryContext = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 1, 10);
            
            var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command);

            return new ChatProcessContext(
                command,
                characterInfo, 
                conversationHistoryContext, 
                memoryContext
            );
        }

        /// <summary>
        /// 채팅 요청 처리
        /// </summary>
        private async Task ProcessChatRequestInternalAsync(ChatProcessContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            try {
                await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context);

                var successHandler = scope.ServiceProvider.GetRequiredService<ChatSuccessHandler>();
                var resultProcessor = scope.ServiceProvider.GetRequiredService<ChatResultProcessor>();

                await successHandler.HandleAsync(context);
                await resultProcessor.PersistResultsAsync(context);
            }
            catch (Exception) {
                var failureHandler = scope.ServiceProvider.GetRequiredService<ChatFailureHandler>();
                await failureHandler.HandleAsync(context);
            }
            finally {
                _metricsService.EndChatMetrics();
                _metricsService.LogChatMetrics();
            }
        }

    }
}
