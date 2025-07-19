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
        /// 특정 IP:Port로 처리 결과 전송
        /// </summary>
        /// <param name="ip">대상 IP</param>
        /// <param name="port">대상 Port</param>
        /// <param name="result">처리 결과</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendToEndpointAsync(string ip, int port, string result);
    }
} 