using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Clients.Memory;
using MainAPI_Server.Models.Chat;
using MainAPI_Server.Models.External.MemorySrore;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Services.Conversation;
using MainAPI_Server.Services.Session;

namespace MainAPI_Server.Services.Chat
{
    public interface IChatService
    {
        Task ProcessChatRequestAsync(ChatRequest request);
    }

    public class ChatService : IChatService
    {
        private readonly IMemoryStoreClient _memoryStoreClient;
        private readonly ILLMClient _llmClient;
        private readonly IConversationService _conversationService;

        public ChatService(IMemoryStoreClient memoryStoreClient, ILLMClient llmClient, IConversationService conversationService)
        {
            _memoryStoreClient = memoryStoreClient;
            _llmClient = llmClient;
            _conversationService = conversationService;
        }

        public async Task ProcessChatRequestAsync(ChatRequest request)
        {
            var startTime = DateTime.UtcNow;
            Console.WriteLine($"Chat 요청 처리 시작: 세션ID={request.SessionId}");

            try
            {
                // [1] 장기 기억 검색 수행
                List<MemorySearchResult> memorySearchResults = await _memoryStoreClient.SearchAsync(request.Message);
                List<string> memoryContext = memorySearchResults.Select(r => r.Text).ToList();

                // [2] 최근 대화 기록 가져오기
                var recentMessages = _conversationService.GetConversationHistory(request.SessionId, 10);
                List<string> conversationContext = recentMessages.Select(m => $"{m.Role}: {m.Content}").ToList();

                string systemMessage = "";

                // [3] LLM에 요청 메시지 전송
                var llmResponse = await _llmClient.GenerateResponseAsync(systemMessage, request.Message, conversationContext, memoryContext);


                // [4] 사용자 메시지 & 응답 저장 저장
                _conversationService.AddMessage(request.SessionId, MessageRole.User, request.Message);                
                _conversationService.AddMessage(request.SessionId, MessageRole.Assistant, llmResponse.Success ? llmResponse.Response : $"오류로 답변 생성 실패");


                // TODO: [5] 보이스 서버로 전송
                // await _voiceClient.SendToVoiceServerAsync(llmResponse);

                // [6] 대화 내용을 장기 기억에 저장
                await _memoryStoreClient.AddMemoryAsync(request.Message);
                if (llmResponse.Success)
                {
                    await _memoryStoreClient.AddMemoryAsync(llmResponse.Response);
                }

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;
                
                // [7] 클라이언트에게 응답 전송
                await SessionManager.SendToClientAsync(request.SessionId, 
                    $"[요청] : {request.Message} " +
                    $"[응답] : {llmResponse.Response} " +
                    $"[응답 소요 시간] : {processingTime:F2}ms");

                Console.WriteLine($"Chat 요청 처리 완료, 소요시간: {processingTime:F2}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat 요청 처리 중 오류 발생: {ex.Message}");
                await SessionManager.SendToClientAsync(request.SessionId, $"오류가 발생했습니다: {ex.Message}");
            }
        }
    }
} 