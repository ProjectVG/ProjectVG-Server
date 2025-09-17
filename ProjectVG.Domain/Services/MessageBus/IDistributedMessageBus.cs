using ProjectVG.Domain.Models.MessageBus;

namespace ProjectVG.Domain.Services.MessageBus
{
    /// <summary>
    /// 분산 메시지 버스 인터페이스
    /// </summary>
    public interface IDistributedMessageBus
    {
        /// <summary>
        /// 특정 사용자에게 메시지를 발송합니다
        /// </summary>
        /// <param name="userId">대상 사용자 ID</param>
        /// <param name="message">메시지</param>
        Task SendToUserAsync(string userId, DistributedMessage message);

        /// <summary>
        /// 특정 서버에 메시지를 발송합니다
        /// </summary>
        /// <param name="serverId">대상 서버 ID</param>
        /// <param name="message">메시지</param>
        Task SendToServerAsync(string serverId, DistributedMessage message);

        /// <summary>
        /// 모든 서버에 브로드캐스트합니다
        /// </summary>
        /// <param name="message">메시지</param>
        Task BroadcastAsync(DistributedMessage message);

        /// <summary>
        /// 특정 채널에 메시지를 발송합니다
        /// </summary>
        /// <param name="channel">채널명</param>
        /// <param name="message">메시지</param>
        Task PublishAsync(string channel, DistributedMessage message);

        /// <summary>
        /// 채널을 구독합니다
        /// </summary>
        /// <param name="channel">채널명</param>
        /// <param name="handler">메시지 처리 핸들러</param>
        Task SubscribeAsync(string channel, Func<DistributedMessage, Task> handler);

        /// <summary>
        /// 채널 구독을 해제합니다
        /// </summary>
        /// <param name="channel">채널명</param>
        Task UnsubscribeAsync(string channel);

        /// <summary>
        /// 메시지 버스를 시작합니다 (구독 시작)
        /// </summary>
        /// <param name="serverId">현재 서버 ID</param>
        Task StartAsync(string serverId);

        /// <summary>
        /// 메시지 버스를 중지합니다
        /// </summary>
        Task StopAsync();
    }
}