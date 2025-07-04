using MainAPI_Server.Clients;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Services.Session;

namespace MainAPI_Server.Services
{
    public interface IChatService
    {
        Task ProcessChatRequestAsync(ChatRequest request);
    }

    public class ChatService : IChatService
    {
        private readonly IMemoryStoreClient _ragClient;

        public ChatService(IMemoryStoreClient ragClient)
        {
            _ragClient = ragClient;
        }

        public async Task ProcessChatRequestAsync(ChatRequest request)
        {
            var startTime = DateTime.UtcNow;
            Console.WriteLine($"Chat 요청 처리 시작: 세션ID={request.Id}");

            try
            {
                // RAG 검색 수행
                var ragResult = await _ragClient.SearchAsync(request.Message);

                // TODO: 해당 데이터를 LLM 모델로 전송
                // var llmResponse = await _llmClient.GenerateResponseAsync(request.Message, ragResult);

                // TODO: 보이스 서버로 전송
                // await _voiceClient.SendToVoiceServerAsync(llmResponse);

                // 대화 내용 저장
                await _ragClient.AddMemoryAsync(request.Message);

                // 클라이언트에게 응답 전송
                await SessionManager.SendToClientAsync(request.Id, $"[요청] : {request.Message} [응답] : {ragResult}");

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