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

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly ILLMService _llmService;
        private readonly ICharacterService _characterService;
        private readonly IConversationService _conversationService;
        private readonly ISessionService _sessionService;
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ILLMService llmService,
            ICharacterService characterService,
            IConversationService conversationService,
            ISessionService sessionService,
            IMemoryClient memoryClient,
            ILogger<ChatService> logger)
        {
            _llmService = llmService;
            _characterService = characterService;
            _conversationService = conversationService;
            _sessionService = sessionService;
            _memoryClient = memoryClient;
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
                await _sessionService.SendMessageAsync(command.SessionId, "서비스 오류로 답변 생성 실패");
            }
        }

        // [전처리] DTO
        private class ChatPreprocessContext
        {
            public string SessionId { get; set; } = string.Empty;
            public string UserMessage { get; set; } = string.Empty;
            public string Actor { get; set; } = string.Empty;
            public string? Action { get; set; }
            public Guid? CharacterId { get; set; }
            public List<string> MemoryContext { get; set; } = new();
            public List<string> ConversationHistory { get; set; } = new();
            public string SystemMessage { get; set; } = string.Empty;
            public string Instructions { get; set; } = string.Empty;
            public string VoiceName { get; set; } = string.Empty;
            public string[] AllowedEmotions { get; set; } = Array.Empty<string>();
        }

        // [프로세스] DTO
        private class ChatProcessResult
        {
            public string Response { get; set; } = string.Empty;
            public string Emotion { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public int TokensUsed { get; set; }
        }

        // [전처리] 사용자 입력, 컨텍스트, instructions 등 준비
        private async Task<ChatPreprocessContext> PreprocessAsync(ProcessChatCommand command)
        {
            // 메모리 로드
            var memoryContext = await CollectMemoryContextAsync(command.Message);
            
            // 대화 기록 조회
            var conversationHistory = await CollectConversationHistoryAsync(command.SessionId);
            
            // 시스템 메시지 작성
            var systemMessage = await PrepareSystemMessageAsync(command.CharacterId);

            // VoiceName이 있다면 emotion 제한
            string voiceName = "Zero";
            string[] allowedEmotions = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                var voiceProfile = VoiceCatalog.GetProfile(voiceName);
                if (voiceProfile == null)
                    throw new Exception($"지원하지 않는 보이스입니다: {voiceName}");
                allowedEmotions = voiceProfile.SupportedStyles;
            }

            var instructions = BuildInstructions(allowedEmotions);

            return new ChatPreprocessContext
            {
                SessionId = command.SessionId,
                UserMessage = command.Message,
                Actor = command.Actor,
                Action = command.Action,
                CharacterId = command.CharacterId,
                MemoryContext = memoryContext,
                ConversationHistory = conversationHistory,
                SystemMessage = systemMessage,
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

            // YAML 파싱 예시 (실제 파싱 로직 필요)
            var parsed = ParseLlmContext(llmResponse.Response);

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Emotion = parsed.Emotion,
                Summary = parsed.Summary,
                TokensUsed = llmResponse.TokensUsed
            };
        }

        // [결과적용] 대화 기록 저장, 클라이언트 응답 등
        private async Task ApplyResultAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            try
            {
                await SaveConversationAsync(context.SessionId, context.UserMessage, result.Response);
                await SaveToMemoryAsync(context.UserMessage, result.Response);

                // 결과 전송
                await _sessionService.SendMessageAsync(context.SessionId, result.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 적용(저장/응답) 실패: 세션 {SessionId}", context.SessionId);
            }
        }

        // 동적으로 Instructions 생성 (emotion은 allowedEmotions 중 하나만 허용)
        private string BuildInstructions(string[] allowedEmotions)
        {
            string emotionList = allowedEmotions.Length > 0 ? string.Join(", ", allowedEmotions) : "(감정 없음)";
            return $@"
답변할 때 아래 규칙 지켜.

- 마스터한텐 반말 써.
- 귀찮은 듯 말해도 되고, 길게 설명하지 마.
- 감정 싣지 마.  
- 멍청한 질문엔 가볍게 비꼬는 거 괜찮아.
- 긴급하면 바로 진지하게 대답해.
- 하나의 응답만 써라. 여러 개 쓰면 무시된다.
- 예시 쓰지 마.

응답은 아래 형식만 써. 딴 포맷 쓰지 마:

[start]
응답: (여기에 네 대답 작성)
상황: (지금 상황을 한두 줄로 요약)
감정: ({emotionList} 중 하나 골라서 작성)
[end]

예시:
[start]
응답: 알아서 해. 그게 빠르겠다.
요약: 마스터가 귀찮은 질문을 함.
감정: annoyed 
[end]
";
        }

        // LLM 결과 파싱 ([start]~[end] 블록에서 응답/상황/감정 추출)
        private (string Response, string Emotion, string Summary) ParseLlmContext(string llmText)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return ("", "", "");

            // [start] ~ [end] 블록 추출
            var startIdx = llmText.IndexOf("[start]");
            var endIdx = llmText.IndexOf("[end]", startIdx + 7);
            if (startIdx == -1 || endIdx == -1)
                return (llmText.Trim(), "", "");

            var block = llmText.Substring(startIdx + 7, endIdx - (startIdx + 7)).Trim();
            string response = "", summary = "", emotion = "";
            foreach (var line in block.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("응답:") || trimmed.StartsWith("응답 :"))
                    response = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
                else if (trimmed.StartsWith("상황:") || trimmed.StartsWith("요약:") || trimmed.StartsWith("상황 :") || trimmed.StartsWith("요약 :"))
                    summary = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
                else if (trimmed.StartsWith("감정:") || trimmed.StartsWith("감정 :"))
                    emotion = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
            }
            return (response, emotion, summary);
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

        // 기존 시스템 메시지 준비
        private async Task<string> PrepareSystemMessageAsync(Guid? characterId)
        {
            if (characterId.HasValue)
            {
                var character = await _characterService.GetCharacterByIdAsync(characterId.Value);
                if (character != null)
                {
                    return character.Role;
                }
            }
            return LLMSettings.Chat.SystemPrompt;
        }

        // 기존 대화 기록 저장
        private async Task SaveConversationAsync(string sessionId, string userMessage, string aiResponse)
        {
            try
            {
                await _conversationService.AddMessageAsync(sessionId, ChatRole.User, userMessage);
                await _conversationService.AddMessageAsync(sessionId, ChatRole.Assistant, aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 저장 실패: 세션 {SessionId}", sessionId);
            }
        }

        // 기존 메모리 저장
        private async Task SaveToMemoryAsync(string userMessage, string aiResponse)
        {
            try
            {
                await _memoryClient.AddMemoryAsync(userMessage);
                await _memoryClient.AddMemoryAsync(aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메모리 저장 실패");
            }
        }
    }
} 