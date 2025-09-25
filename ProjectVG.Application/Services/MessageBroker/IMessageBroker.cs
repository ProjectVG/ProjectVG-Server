namespace ProjectVG.Application.Services.MessageBroker
{
    public interface IMessageBroker
    {
        /// <summary>
        /// 특정 사용자에게 메시지를 전송합니다
        /// </summary>
        Task SendToUserAsync(string userId, object message);

        /// <summary>
        /// 모든 연결된 사용자에게 메시지를 방송합니다
        /// </summary>
        Task BroadcastAsync(object message);

        /// <summary>
        /// 특정 서버로 메시지를 전송합니다
        /// </summary>
        Task SendToServerAsync(string serverId, object message);

        /// <summary>
        /// 메시지 브로커가 분산 모드인지 확인합니다
        /// </summary>
        bool IsDistributed { get; }
    }
}