using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Clients.Memory;
using MainAPI_Server.Models.Chat;
using MainAPI_Server.Models.External.LLM;
using MainAPI_Server.Models.External.MemorySrore;
using MainAPI_Server.Models.Request;
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
            Console.WriteLine($"Chat 요청 처리 시작: 세션ID={request.Id}");

            try
            {
                // 장기 기억 검색 수행
                List<MemorySearchResult> memorySearchResults = await _memoryStoreClient.SearchAsync(request.Message);
                List<string> memoryContext = memorySearchResults.Select(r => r.Text).ToList();

                // TODO: 최근 기억 조회
                List<string> conversationContext = new List<string>();

                // LLM에 요청 메시지 전송
                var llmResponse = await _llmClient.GenerateResponseAsync(request.Message, conversationContext, memoryContext);

                // TODO: 보이스 서버로 전송
                // await _voiceClient.SendToVoiceServerAsync(llmResponse);

                // 대화 내용 저장
                await _memoryStoreClient.AddMemoryAsync(request.Message);

                // 클라이언트에게 응답 전송
                await SessionManager.SendToClientAsync(request.Id, $"[요청] : {request.Message} [응답] : {memorySearchResults}");

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Chat 요청 처리 완료, 소요시간: {processingTime:F2}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat 요청 처리 중 오류 발생: {ex.Message}");
                await SessionManager.SendToClientAsync(request.Id, $"오류가 발생했습니다: {ex.Message}");
            }
        }
    }
} 