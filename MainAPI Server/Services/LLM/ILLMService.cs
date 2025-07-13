using MainAPI_Server.Models.External.LLM;
using MainAPI_Server.Models.DTOs.Chat;

namespace MainAPI_Server.Services.LLM
{
    public interface ILLMService
    {
        /// <summary>
        /// 텍스트 응답 생성 (DTO 기반)
        /// </summary>
        /// <param name="contextDto">채팅 컨텍스트 DTO</param>
        /// <returns>채팅 결과 DTO</returns>
        Task<ChatResultDto> CreateTextResponseAsync(ChatContextDto contextDto);
    }
} 