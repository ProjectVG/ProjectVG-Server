using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Domain.Services.MessageBus;

namespace ProjectVG.Application.Services.MessageBus
{
    /// <summary>
    /// 메시지 버스를 위한 확장 메서드들
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// 분산 메시지 버스에 기본 핸들러들을 등록합니다
        /// </summary>
        public static async Task RegisterDefaultHandlersAsync(
            this IDistributedMessageBus messageBus,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IDistributedMessageBus>>();

            try
            {
                // WebSocket 메시지 핸들러
                await messageBus.SubscribeAsync("user_message", async (message) =>
                {
                    if (message is WebSocketMessage wsMessage)
                    {
                        var handler = serviceProvider.GetRequiredService<DistributedChatResultHandler>();
                        await handler.HandleWebSocketMessageAsync(wsMessage);
                    }
                });

                // 채팅 결과 핸들러
                await messageBus.SubscribeAsync("chat_result", async (message) =>
                {
                    if (message is ChatResultMessage chatResult)
                    {
                        var handler = serviceProvider.GetRequiredService<DistributedChatResultHandler>();
                        await handler.HandleChatResultAsync(chatResult);
                    }
                });

                // 세션 업데이트 핸들러
                await messageBus.SubscribeAsync("session_update", async (message) =>
                {
                    if (message is SessionUpdateMessage sessionUpdate)
                    {
                        var handler = serviceProvider.GetRequiredService<DistributedChatResultHandler>();
                        await handler.HandleSessionUpdateAsync(sessionUpdate);
                    }
                });

                // 서버 상태 핸들러
                await messageBus.SubscribeAsync("server_status", async (message) =>
                {
                    if (message is ServerStatusMessage serverStatus)
                    {
                        var handler = serviceProvider.GetRequiredService<DistributedChatResultHandler>();
                        await handler.HandleServerStatusAsync(serverStatus);
                    }
                });

                logger.LogInformation("분산 메시지 버스 기본 핸들러 등록 완료");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "분산 메시지 버스 핸들러 등록 실패");
                throw;
            }
        }
    }
}