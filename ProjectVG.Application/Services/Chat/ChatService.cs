using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.LLM;
using ProjectVG.Application.Services.Voice;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Messaging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Common.Constants;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISessionStorage _sessionStorage;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IServiceScopeFactory scopeFactory,
            ISessionStorage sessionStorage,
            IUserService userService,
            ICharacterService characterService,
            ILogger<ChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _sessionStorage = sessionStorage;
            _userService = userService;
            _characterService = characterService;
            _logger = logger;
        }

        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            // 사전 준비 단계: 검증 + 전처리 (동기 처리)
            var validationResult = await ValidateChatRequestAsync(command);
            if (!validationResult.IsValid)
            {
                return ChatRequestResponse.Rejected(validationResult.ErrorMessage, validationResult.ErrorCode, command.SessionId, command.UserId, command.CharacterId);
            }

            var preprocessContext = await PrepareChatRequestAsync(command);
            if (!preprocessContext.IsValid)
            {
                return preprocessContext.RequestResponse;
            }

            // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장 (비동기 처리)
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                await ProcessChatRequestInternalAsync(preprocessContext.Context, scope.ServiceProvider);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }

        private async Task<ChatPreprocessResult> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var services = scope.ServiceProvider;
                
                var memoryClient = services.GetRequiredService<IMemoryClient>();
                var conversationService = services.GetRequiredService<IConversationService>();
                var characterService = services.GetRequiredService<ICharacterService>();
                var memoryPreprocessor = new MemoryContextPreprocessor(memoryClient, services.GetRequiredService<ILogger<MemoryContextPreprocessor>>());
                var conversationPreprocessor = new ConversationHistoryPreprocessor(conversationService, services.GetRequiredService<ILogger<ConversationHistoryPreprocessor>>());
                var systemPromptGenerator = new SystemPromptGenerator();
                var instructionGenerator = new InstructionGenerator();

                var memoryContext = await memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message);
                var conversationHistory = await conversationPreprocessor.CollectConversationHistoryAsync(command.UserId, command.CharacterId);
                var characterDto = await characterService.GetCharacterByIdAsync(command.CharacterId);
                var systemMessage = systemPromptGenerator.Generate(characterDto);
                var instructions = instructionGenerator.Generate();

                var context = new ChatPreprocessContext
                {
                    SessionId = command.SessionId,
                    UserId = command.UserId,
                    CharacterId = command.CharacterId,
                    SystemMessage = systemMessage,
                    Instructions = instructions,
                    UserMessage = command.Message,
                    MemoryStore = command.UserId.ToString(),
                    Action = command.Action,
                    MemoryContext = memoryContext,
                    ConversationHistory = conversationHistory,
                    VoiceName = characterDto.VoiceId,
                };

                return ChatPreprocessResult.Success(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 사전 준비 중 오류 발생: {SessionId}", command.SessionId);
                return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected("사전 준비 중 오류가 발생했습니다.", "PREPROCESS_ERROR", command.SessionId, command.UserId, command.CharacterId));
            }
        }

        private async Task ProcessChatRequestInternalAsync(ChatPreprocessContext context, IServiceProvider services)
        {
            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", context.SessionId);

                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                var llmResult = await ProcessLLMAsync(context, services);
                await ProcessTTSAsync(context, llmResult, services);
                await SendResultsAsync(context, llmResult, services);
                await PersistResultsAsync(context, llmResult, services);

                _logger.LogInformation("채팅 요청 처리 완료: {SessionId}, 토큰: {TokensUsed}",
                    context.SessionId, llmResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", context.SessionId);
            }
        }



        private async Task<ChatProcessResult> ProcessLLMAsync(ChatPreprocessContext context, IServiceProvider services)
        {
            var llmService = services.GetRequiredService<ILLMService>();

            var llmResponse = await llmService.CreateTextResponseAsync(
                context.SystemMessage,
                context.UserMessage,
                context.Instructions,
                context.MemoryContext,
                context.ConversationHistory,
                LLMSettings.Chat.MaxTokens,
                LLMSettings.Chat.Temperature,
                LLMSettings.Chat.Model
            );

            var parsed = ChatOutputFormat.Parse(llmResponse.Response, context.VoiceName);
            var cost = CalculateCost(llmResponse.TokensUsed);
            var segments = CreateSegments(parsed);

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Segments = segments,
                TokensUsed = llmResponse.TokensUsed,
                Cost = cost
            };
        }

        private async Task ProcessTTSAsync(ChatPreprocessContext context, ChatProcessResult result, IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(context.VoiceName) || result.Segments?.Count == 0)
            {
                return;
            }

            var voiceService = services.GetRequiredService<IVoiceService>();
            var ttsTasks = new List<Task<(int idx, ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models.TextToSpeechResponse)>>();

            for (int i = 0; i < result.Segments.Count; i++)
            {
                int idx = i;
                var segment = result.Segments[idx];

                if (!segment.HasText) continue;

                string emotion = segment.Emotion ?? "neutral";
                ttsTasks.Add(Task.Run(async () => (idx, await voiceService.TextToSpeechAsync(
                    context.VoiceName,
                    segment.Text!,
                    emotion,
                    "ko",
                    null
                ))));
            }

            var ttsResults = await Task.WhenAll(ttsTasks);

            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx))
            {
                if (ttsResult?.Success == true && ttsResult.AudioData != null)
                {
                    var segment = result.Segments[idx];
                    segment.AudioData = ttsResult.AudioData;
                    segment.AudioContentType = ttsResult.ContentType;
                    segment.AudioLength = ttsResult.AudioLength;

                    if (ttsResult.AudioLength.HasValue)
                    {
                        result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
                    }
                }
            }
        }

        private async Task PersistResultsAsync(ChatPreprocessContext context, ChatProcessResult result, IServiceProvider services)
        {
            var conversationService = services.GetRequiredService<IConversationService>();
            var memoryClient = services.GetRequiredService<IMemoryClient>();

            await conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, result.Response);
            await memoryClient.AddMemoryAsync(context.MemoryStore, result.Response);
        }

        private async Task SendResultsAsync(ChatPreprocessContext context, ChatProcessResult result, IServiceProvider services)
        {
            var messageBroker = services.GetRequiredService<IMessageBroker>();

            foreach (var segment in result.Segments.OrderBy(s => s.Order))
            {
                if (segment.IsEmpty) continue;

                var integratedMessage = new IntegratedChatMessage
                {
                    SessionId = context.SessionId,
                    Text = segment.Text,
                    AudioFormat = segment.AudioContentType ?? "wav",
                    AudioLength = segment.AudioLength,
                    Timestamp = DateTime.UtcNow
                };

                integratedMessage.SetAudioData(segment.AudioData);

                var wsMessage = new WebSocketMessage("chat", integratedMessage);
                await messageBroker.SendWebSocketMessageAsync(context.SessionId, wsMessage);
            }
        }

        private async Task<ChatValidationResult> ValidateChatRequestAsync(ProcessChatCommand command)
        {
            if (!string.IsNullOrEmpty(command.SessionId))
            {
                var sessionExists = await _sessionStorage.ExistsAsync(command.SessionId);
                if (!sessionExists)
                {
                    _logger.LogWarning("세션 ID 검증 실패: {SessionId}", command.SessionId);
                    return ChatValidationResult.Failure("유효하지 않은 세션 ID입니다.", "INVALID_SESSION_ID");
                }
            }

            var userExists = await _userService.UserExistsAsync(command.UserId);
            if (!userExists)
            {
                _logger.LogWarning("사용자 ID 검증 실패: {UserId}", command.UserId);
                return ChatValidationResult.Failure("존재하지 않는 사용자 ID입니다.", "INVALID_USER_ID");
            }

            var characterExists = await _characterService.CharacterExistsAsync(command.CharacterId);
            if (!characterExists)
            {
                _logger.LogWarning("캐릭터 ID 검증 실패: {CharacterId}", command.CharacterId);
                return ChatValidationResult.Failure("존재하지 않는 캐릭터 ID입니다.", "INVALID_CHARACTER_ID");
            }

            return ChatValidationResult.Success();
        }

        private static double CalculateCost(int tokensUsed)
        {
            return tokensUsed > 0 ? Math.Ceiling(tokensUsed / 25.0) : 0;
        }

        private static List<ChatMessageSegment> CreateSegments(ChatOutputFormatResult parsed)
        {
            var segments = new List<ChatMessageSegment>();
            for (int i = 0; i < parsed.Text.Count; i++)
            {
                var emotion = parsed.Emotion.Count > i ? parsed.Emotion[i] : "neutral";
                var segment = ChatMessageSegment.CreateTextOnly(parsed.Text[i], i);
                segment.Emotion = emotion;
                segments.Add(segment);
            }
            return segments;
        }
    }
} 