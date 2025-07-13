using ProjectVG.Infrastructure.ExternalApis.LLM.Models;

namespace ProjectVG.Infrastructure.ExternalApis.LLM
{
    public interface ILLMClient
    {
        /// <summary>
        /// LLM 서버에 요청을 전송하고 응답을 받습니다.
        /// </summary>
        /// <param name="request">LLM 요청 객체</param>
        /// <returns>LLM 응답</returns>
        Task<LLMResponse> SendRequestAsync(LLMRequest request);
    }
} 