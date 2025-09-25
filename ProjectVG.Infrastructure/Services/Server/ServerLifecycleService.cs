using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Services.Server;

namespace ProjectVG.Infrastructure.Services.Server
{
    /// <summary>
    /// 서버 생명주기 관리 백그라운드 서비스
    /// - 30초마다 하트비트 전송
    /// - 5분마다 오프라인 서버 정리
    /// </summary>
    public class ServerLifecycleService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ServerLifecycleService> _logger;

        private static readonly TimeSpan HEARTBEAT_INTERVAL = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromMinutes(5);

        private DateTime _lastCleanup = DateTime.UtcNow;

        public ServerLifecycleService(
            IServiceScopeFactory scopeFactory,
            ILogger<ServerLifecycleService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("서버 생명주기 서비스 시작");

            // 서버 등록
            using var scope = _scopeFactory.CreateScope();
            var serverRegistration = scope.ServiceProvider.GetRequiredService<IServerRegistrationService>();
            await serverRegistration.RegisterServerAsync();

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("서버 생명주기 서비스 중지");

            // 서버 등록 해제
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var serverRegistration = scope.ServiceProvider.GetRequiredService<IServerRegistrationService>();
                await serverRegistration.UnregisterServerAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 등록 해제 중 오류");
            }

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("서버 생명주기 루프 시작 - 하트비트 간격: {HeartbeatInterval}초, 정리 간격: {CleanupInterval}분",
                HEARTBEAT_INTERVAL.TotalSeconds, CLEANUP_INTERVAL.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var serverRegistration = scope.ServiceProvider.GetRequiredService<IServerRegistrationService>();

                    // 1. 하트비트 전송 (30초마다)
                    await serverRegistration.SendHeartbeatAsync();

                    // 2. 정리 작업 (5분마다)
                    if (DateTime.UtcNow - _lastCleanup >= CLEANUP_INTERVAL)
                    {
                        _logger.LogDebug("오프라인 서버 정리 시작");
                        await serverRegistration.CleanupOfflineServersAsync();
                        _lastCleanup = DateTime.UtcNow;
                        _logger.LogDebug("오프라인 서버 정리 완료");
                    }

                    // 다음 하트비트까지 대기
                    await Task.Delay(HEARTBEAT_INTERVAL, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("서버 생명주기 서비스 취소됨");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "서버 생명주기 루프 중 오류");

                    // 오류 발생 시 30초 대기 후 재시도
                    try
                    {
                        await Task.Delay(HEARTBEAT_INTERVAL, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("서버 생명주기 서비스 종료");
        }
    }
}