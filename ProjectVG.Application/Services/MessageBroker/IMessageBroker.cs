namespace ProjectVG.Application.Services.MessageBroker
{
    public interface IMessageBroker
    {
        /// <summary>
        /// 특정 세션의 클라이언트에게 처리 결과 전송
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="result">처리 결과</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendResultAsync(string sessionId, string result);

        /// <summary>
        /// 특정 세션의 클라이언트에게 오디오 데이터 전송
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="audioData">오디오 데이터</param>
        /// <param name="contentType">콘텐츠 타입 (예: audio/wav)</param>
        /// <param name="audioLength">오디오 길이 (초)</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendResultAsync(string sessionId, byte[] audioData, string? contentType, float? audioLength);

        /// <summary>
        /// 특정 IP:Port로 처리 결과 전송
        /// </summary>
        /// <param name="ip">대상 IP</param>
        /// <param name="port">대상 Port</param>
        /// <param name="result">처리 결과</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendToEndpointAsync(string ip, int port, string result);

        /// <summary>
        /// 특정 IP:Port로 오디오 데이터 전송
        /// </summary>
        /// <param name="ip">대상 IP</param>
        /// <param name="port">대상 Port</param>
        /// <param name="audioData">오디오 데이터</param>
        /// <param name="contentType">콘텐츠 타입 (예: audio/wav)</param>
        /// <param name="audioLength">오디오 길이 (초)</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendToEndpointAsync(string ip, int port, byte[] audioData, string? contentType, float? audioLength);
    }
} 