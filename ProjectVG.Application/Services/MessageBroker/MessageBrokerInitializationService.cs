using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.MessageBroker
{
    /// <summary>
    /// 애플리케이션 시작 시 DistributedMessageBroker를 강제로 초기화하는 서비스
    /// </summary>
    public class MessageBrokerInitializationService : IHostedService
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ILogger<MessageBrokerInitializationService> _logger;

        public MessageBrokerInitializationService(
            IMessageBroker messageBroker,
            ILogger<MessageBrokerInitializationService> logger)
        {
            _messageBroker = messageBroker;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[초기화서비스] MessageBroker 초기화 시작");

                // DistributedMessageBroker의 IsDistributed 속성에 접근하여 강제 초기화 트리거
                var isDistributed = _messageBroker.IsDistributed;

                _logger.LogInformation("[초기화서비스] MessageBroker 초기화 완료: IsDistributed={IsDistributed}", isDistributed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[초기화서비스] MessageBroker 초기화 실패");
                // 애플리케이션이 시작되지 않도록 예외를 다시 던집니다.
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[초기화서비스] MessageBroker 초기화 서비스 중지");
            await Task.CompletedTask;
        }
    }
}