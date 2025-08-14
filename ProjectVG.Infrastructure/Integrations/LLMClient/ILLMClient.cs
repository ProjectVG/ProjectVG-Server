using ProjectVG.Infrastructure.Integrations.LLMClient.Models;

namespace ProjectVG.Infrastructure.Integrations.LLMClient
{
    public interface ILLMClient
    {
        /// <summary>
        /// LLMClient 서버에 요청을 전송하고 응답을 받습니다.
        /// </summary>
        /// <param name="request">LLMClient 요청 객체</param>
        /// <returns>LLMClient 응답</returns>
        Task<LLMResponse> SendRequestAsync(LLMRequest request);
    }
} 