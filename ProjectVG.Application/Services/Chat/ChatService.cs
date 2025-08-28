using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;
        private readonly IChatMetricsService _metricsService;

        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;

        private readonly ChatRequestValidator _validator;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly ICostTrackingDecorator<UserInputAnalysisProcessor> _inputProcessor;
        private readonly UserInputActionProcessor _actionProcessor;

        private readonly ICostTrackingDecorator<ChatLLMProcessor> _llmProcessor;
        private readonly ICostTrackingDecorator<ChatTTSProcessor> _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;

        private readonly ChatSuccessHandler _chatSuccessHandler;
        private readonly ChatFailureHandler _chatFailureHandler;

        public ChatService(
            IChatMetricsService metricsService,
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

            ChatSuccessHandler chatSuccessHandler, 
            ChatFailureHandler chatFailureHandler
        ) {
            _metricsService = metricsService;
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
            _chatSuccessHandler = chatSuccessHandler;
            _chatFailureHandler = chatFailureHandler;
        }

        public async Task<ChatRequestResult> EnqueueChatRequestAsync(ChatRequestCommand command)
        {
            _metricsService.StartChatMetrics(command.Id.ToString(), command.UserId.ToString(), command.CharacterId.ToString());
            
            await _validator.ValidateAsync(command);

            var preprocessContext = await PrepareChatRequestAsync(command);

            LogChatRequestCommand(command);

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
            var conversationHistoryContext = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 10);
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
            try {
                await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context);

                await _chatSuccessHandler.HandleAsync(context);

                using var scope = _scopeFactory.CreateScope();
                var resultProcessor = scope.ServiceProvider.GetRequiredService<ChatResultProcessor>();
                await resultProcessor.PersistResultsAsync(context);
            }
            catch (Exception) {
                await _chatFailureHandler.HandleAsync(context);
            }
            finally {
                LogChatProcessContext(context);
                _metricsService.EndChatMetrics();
                _metricsService.LogChatMetrics();
            }
        }

        private void LogChatRequestCommand(ChatRequestCommand command)
        {
            _logger.LogInformation("Starting chat process: {CommandInfo}", command.ToDebugString());
        }

        private void LogChatProcessContext(ChatProcessContext context)
        {
            _logger.LogInformation("Chat process completed: {ContextInfo}", context.ToDebugString());
        }
    }
}
