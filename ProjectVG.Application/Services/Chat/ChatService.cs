using ProjectVG.Application.Services.LLM;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Infrastructure.ExternalApis.MemoryClient;
using ProjectVG.Infrastructure.ExternalApis.LLMClient.Models;
using ProjectVG.Common.Constants;
using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Enums;
using ProjectVG.Application.Services.Voice;
using ProjectVG.Application.Services.MessageBroker;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly ILLMService _llmService;
        private readonly ICharacterService _characterService;
        private readonly IConversationService _conversationService;
        private readonly IClientSessionService _sessionService;
        private readonly IMemoryClient _memoryClient;
        private readonly IVoiceService _voiceService;
        private readonly IMessageBroker _messageBroker;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ILLMService llmService,
            ICharacterService characterService,
            IConversationService conversationService,
            IClientSessionService sessionService,
            IMemoryClient memoryClient,
            IVoiceService voiceService,
            IMessageBroker messageBroker,
            ILogger<ChatService> logger)
        {
            _llmService = llmService;
            _characterService = characterService;
            _conversationService = conversationService;
            _sessionService = sessionService;
            _memoryClient = memoryClient;
            _voiceService = voiceService;
            _messageBroker = messageBroker;
            _logger = logger;
        }

        public async Task ProcessChatRequestAsync(ProcessChatCommand command)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: 세션 {SessionId}, 메시지: {Message}", command.SessionId, command.Message);

                // [전처리] 사용자 입력, 컨텍스트, instructions 등 준비
                var preContext = await PreprocessAsync(command);

                // [프로세스] LLM 호출 및 결과 파싱/포맷팅
                var processResult = await ProcessLLMAsync(preContext);

                // [결과적용] 대화 기록 저장, 클라이언트 응답 등
                await ApplyResultAsync(preContext, processResult);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;
                _logger.LogInformation("채팅 요청 처리 완료: 세션 {SessionId}, 소요시간: {ProcessingTime:F2}ms, 토큰: {TokensUsed}",
                    preContext.SessionId, processingTime, processResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: 세션 {SessionId}", command.SessionId);
                await _messageBroker.SendResultAsync(command.SessionId, "서비스 오류로 답변 생성 실패");
            }
        }

        // [전처리] 사용자 입력, 컨텍스트, instructions 등 준비
        private async Task<ChatPreprocessContext> PreprocessAsync(ProcessChatCommand command)
        {
            // 메모리 로드
            var memoryContext = await CollectMemoryContextAsync(command.Message);
            
            // 대화 기록 조회
            var conversationHistory = await CollectConversationHistoryAsync(command.SessionId);
            
            // VoiceName이 있다면 emotion 제한
            string voiceName = "Hyewon";
            string[] allowedEmotions = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                var voiceProfile = VoiceCatalog.GetProfile(voiceName);
                if (voiceProfile == null)
                    throw new Exception($"지원하지 않는 보이스입니다: {voiceName}");
                allowedEmotions = voiceProfile.SupportedStyles;
            }

            var outputFormat = new ChatOutputFormat(allowedEmotions);
            var instructions = outputFormat.GetFullInstructions(@"- 마스터한텐 반말 써.\n- 귀찮은 듯 말해도 되고, 길게 설명하지 마.\n- 감정 싣지 마.  \n- 멍청한 질문엔 가볍게 비꼬는 거 괜찮아.\n- 긴급하면 바로 진지하게 대답해.\n- 하나의 응답만 써라. 여러 개 쓰면 무시된다.\n- 예시 쓰지 마.");

            return new ChatPreprocessContext
            {
                SessionId = command.SessionId,
                UserMessage = command.Message,
                Actor = command.Actor,
                Action = command.Action,
                CharacterId = command.CharacterId,
                MemoryContext = memoryContext,
                ConversationHistory = conversationHistory,
                SystemMessage = LLMSettings.Chat.SystemPrompt,
                Instructions = instructions,
                VoiceName = voiceName,
                AllowedEmotions = allowedEmotions
            };
        }

        // [프로세스] LLM 호출 및 결과 파싱/포맷팅
        private async Task<ChatProcessResult> ProcessLLMAsync(ChatPreprocessContext context)
        {
            var llmResponse = await _llmService.CreateTextResponseAsync(
                context.SystemMessage,
                context.UserMessage,
                context.Instructions,
                context.MemoryContext,
                context.ConversationHistory,
                LLMSettings.Chat.MaxTokens,
                LLMSettings.Chat.Temperature,
                LLMSettings.Chat.Model
            );
            var outputFormat = new ChatOutputFormat(context.AllowedEmotions);
            var parsed = outputFormat.Parse(llmResponse.Response);

            // Voice Synthesis (TTS)
            byte[]? audioData = null;
            string? audioContentType = null;
            float? audioLength = null;
            if (!string.IsNullOrWhiteSpace(context.VoiceName) && !string.IsNullOrWhiteSpace(parsed.Response))
            {
                try
                {
                    var ttsResult = await _voiceService.TextToSpeechAsync(
                        context.VoiceName,
                        parsed.Response,
                        parsed.Emotion,
                        null // VoiceSettings 확장 가능
                    );
                    if (ttsResult.Success && ttsResult.AudioData != null)
                    {
                        audioData = ttsResult.AudioData;
                        audioContentType = ttsResult.ContentType;
                        audioLength = ttsResult.AudioLength;
                    }
                    else if (!ttsResult.Success)
                    {
                        _logger.LogWarning("TTS 변환 실패: {Error}", ttsResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TTS 변환 중 예외 발생");
                }
            }

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Emotion = parsed.Emotion,
                Summary = parsed.Summary,
                TokensUsed = llmResponse.TokensUsed,
                AudioData = audioData,
                AudioContentType = audioContentType,
                AudioLength = audioLength
            };
        }

        // [결과적용] 대화 기록 저장, 클라이언트 응답 등
        private async Task ApplyResultAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            try
            {
                // 대화 기록
                await _conversationService.AddMessageAsync(context.SessionId, ChatRole.User, context.UserMessage);
                await _conversationService.AddMessageAsync(context.SessionId, ChatRole.Assistant, result.Response);

                // 메모리 클라이언트에 동록
                await _memoryClient.AddMemoryAsync(context.UserMessage);
                await _memoryClient.AddMemoryAsync(result.Response);

                // 결과 전송 (텍스트)
                await _messageBroker.SendResultAsync(context.SessionId, result.Response);

                // 오디오(wav) 전송
                if (result.AudioData != null && result.AudioData.Length > 0)
                {
                    await _messageBroker.SendResultAsync(context.SessionId, result.AudioData, result.AudioContentType, result.AudioLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 적용(저장/응답) 실패: 세션 {SessionId}", context.SessionId);
            }
        }

        // 기존 메모리 컨텍스트 수집
        private async Task<List<string>> CollectMemoryContextAsync(string userMessage)
        {
            try
            {
                var searchResults = await _memoryClient.SearchAsync(userMessage, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        // 기존 대화 기록 수집
        private async Task<List<string>> CollectConversationHistoryAsync(string sessionId)
        {
            try
            {
                var history = await _conversationService.GetConversationHistoryAsync(sessionId, 10);
                return history.Select(h => $"{h.Role}: {h.Content}").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "대화 기록 수집 실패: 세션 {SessionId}", sessionId);
                return new List<string>();
            }
        }
    }
} 