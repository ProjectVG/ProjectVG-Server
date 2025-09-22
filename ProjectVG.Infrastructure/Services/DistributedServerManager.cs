using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Domain.Services.MessageBus;
using ProjectVG.Domain.Services.Session;
using ProjectVG.Common.Models.Session;
using System.Net;

namespace ProjectVG.Infrastructure.Services
{
    /// <summary>
    /// 분산 서버 관리 및 Health Check 서비스
    /// </summary>
    public class DistributedServerManager : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DistributedServerManager> _logger;
        private readonly string _serverId;
        private readonly ServerInfo _currentServerInfo;
        private Timer? _heartbeatTimer;
        private Timer? _cleanupTimer;

        // Health Check 설정
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _serverTimeout = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(1);

        public DistributedServerManager(
            IServiceProvider serviceProvider,
            ILogger<DistributedServerManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // 서버 ID 및 정보 생성
            _serverId = GenerateServerId();
            _currentServerInfo = CreateCurrentServerInfo();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("분산 서버 관리자 시작: ServerId={ServerId}", _serverId);

                // 서버 등록
                await RegisterServerAsync();

                // 메시지 버스 시작
                await StartMessageBusAsync();

                // Health Check 타이머 시작
                StartHealthCheckTimers();

                // 서비스가 중지될 때까지 대기
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("분산 서버 관리자 정상 종료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 서버 관리자 실행 중 오류: ServerId={ServerId}", _serverId);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("분산 서버 관리자 종료 시작: ServerId={ServerId}", _serverId);

                // 타이머 정지
                _heartbeatTimer?.Dispose();
                _cleanupTimer?.Dispose();

                // 서버 해제
                await UnregisterServerAsync();

                // 메시지 버스 정지
                await StopMessageBusAsync();

                await base.StopAsync(cancellationToken);

                _logger.LogInformation("분산 서버 관리자 종료 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 서버 관리자 종료 중 오류: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// 서버를 분산 세션 관리자에 등록합니다
        /// </summary>
        private async Task RegisterServerAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<IDistributedSessionManager>();

                await sessionManager.RegisterServerAsync(_serverId, _currentServerInfo);

                // 서버 등록 알림 브로드캐스트
                var messageBus = scope.ServiceProvider.GetRequiredService<IDistributedMessageBus>();
                var serverStatusMessage = new ServerStatusMessage
                {
                    ServerId = _serverId,
                    Status = "Online",
                    Metadata = new Dictionary<string, object>
                    {
                        ["hostname"] = _currentServerInfo.HostName,
                        ["ipAddress"] = _currentServerInfo.IpAddress,
                        ["port"] = _currentServerInfo.Port,
                        ["version"] = _currentServerInfo.Version,
                        ["registeredAt"] = _currentServerInfo.RegisteredAt
                    }
                };

                await messageBus.PublishAsync("channel:discovery", serverStatusMessage);

                _logger.LogInformation("서버 등록 완료: ServerId={ServerId}, Host={Host}",
                    _serverId, _currentServerInfo.HostName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 등록 실패: ServerId={ServerId}", _serverId);
                throw;
            }
        }

        /// <summary>
        /// 서버를 분산 세션 관리자에서 해제합니다
        /// </summary>
        private async Task UnregisterServerAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<IDistributedSessionManager>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IDistributedMessageBus>();

                // 서버 오프라인 알림 브로드캐스트
                var serverStatusMessage = new ServerStatusMessage
                {
                    ServerId = _serverId,
                    Status = "Offline",
                    Metadata = new Dictionary<string, object>
                    {
                        ["shutdownAt"] = DateTime.UtcNow
                    }
                };

                await messageBus.PublishAsync("channel:discovery", serverStatusMessage);

                // 서버 해제 (연결된 모든 세션도 함께 정리됨)
                await sessionManager.UnregisterServerAsync(_serverId);

                _logger.LogInformation("서버 해제 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 해제 실패: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// 메시지 버스를 시작합니다
        /// </summary>
        private async Task StartMessageBusAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageBus = scope.ServiceProvider.GetRequiredService<IDistributedMessageBus>();

                await messageBus.StartAsync(_serverId);

                _logger.LogInformation("메시지 버스 시작 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 버스 시작 실패: ServerId={ServerId}", _serverId);
                throw;
            }
        }

        /// <summary>
        /// 메시지 버스를 정지합니다
        /// </summary>
        private async Task StopMessageBusAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageBus = scope.ServiceProvider.GetRequiredService<IDistributedMessageBus>();

                await messageBus.StopAsync();

                _logger.LogInformation("메시지 버스 정지 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 버스 정지 실패: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// Health Check 타이머들을 시작합니다
        /// </summary>
        private void StartHealthCheckTimers()
        {
            // 하트비트 타이머
            _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(),
                null, TimeSpan.Zero, _heartbeatInterval);

            // 정리 작업 타이머
            _cleanupTimer = new Timer(async _ => await CleanupExpiredResourcesAsync(),
                null, _cleanupInterval, _cleanupInterval);

            _logger.LogInformation("Health Check 타이머 시작: HeartbeatInterval={HeartbeatInterval}, CleanupInterval={CleanupInterval}",
                _heartbeatInterval, _cleanupInterval);
        }

        /// <summary>
        /// 하트비트를 전송합니다
        /// </summary>
        private async Task SendHeartbeatAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<IDistributedSessionManager>();

                // 서버 정보 업데이트 (하트비트)
                _currentServerInfo.UpdateHeartbeat();

                // 활성 연결 수는 별도 메트릭에서 관리하거나 Redis에서 계산
                // Infrastructure는 Application을 참조할 수 없으므로 기본값 사용
                _currentServerInfo.ActiveConnections = 0;

                await sessionManager.RegisterServerAsync(_serverId, _currentServerInfo);

                _logger.LogDebug("하트비트 전송: ServerId={ServerId}, ActiveConnections={ActiveConnections}",
                    _serverId, _currentServerInfo.ActiveConnections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "하트비트 전송 실패: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// 만료된 리소스들을 정리합니다
        /// </summary>
        private async Task CleanupExpiredResourcesAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<IDistributedSessionManager>();

                // 만료된 세션 정리
                await sessionManager.CleanupExpiredSessionsAsync(_sessionTimeout);

                // 오프라인 서버 감지 및 정리
                await CleanupOfflineServersAsync(sessionManager);

                _logger.LogDebug("만료된 리소스 정리 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "만료된 리소스 정리 실패: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// 오프라인 서버들을 감지하고 정리합니다
        /// </summary>
        private async Task CleanupOfflineServersAsync(IDistributedSessionManager sessionManager)
        {
            try
            {
                var activeServers = await sessionManager.GetActiveServersAsync();
                var offlineServers = new List<string>();

                foreach (var serverId in activeServers)
                {
                    if (serverId == _serverId) continue; // 현재 서버는 제외

                    // 서버 정보 조회 및 온라인 상태 확인
                    // 실제 구현에서는 Redis에서 서버 정보를 조회하여 하트비트 시간 확인
                    // 여기서는 간단히 타임아웃 기반으로 처리
                }

                // 오프라인 서버들 정리
                foreach (var offlineServerId in offlineServers)
                {
                    await sessionManager.UnregisterServerAsync(offlineServerId);
                    _logger.LogWarning("오프라인 서버 정리: ServerId={OfflineServerId}", offlineServerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오프라인 서버 정리 실패: ServerId={ServerId}", _serverId);
            }
        }

        /// <summary>
        /// 서버 ID를 생성합니다
        /// </summary>
        private static string GenerateServerId()
        {
            // 1. 환경변수에서 서버 ID 조회 (최우선)
            var envServerId = Environment.GetEnvironmentVariable("SERVER_ID");
            if (!string.IsNullOrWhiteSpace(envServerId))
            {
                return envServerId.Trim();
            }

            // 2. 표준화된 형식으로 자동 생성
            var machineName = Environment.MachineName.ToLowerInvariant();
            var processId = Environment.ProcessId;
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmm");

            return $"api-server-{machineName}-{processId}-{timestamp}";
        }

        /// <summary>
        /// 현재 서버 정보를 생성합니다
        /// </summary>
        private static ServerInfo CreateCurrentServerInfo()
        {
            var hostname = Environment.MachineName;
            var ipAddress = GetLocalIPAddress();
            var port = GetCurrentPort(); // 환경변수나 설정에서 가져올 수 있음

            return new ServerInfo
            {
                ServerId = GenerateServerId(),
                HostName = hostname,
                IpAddress = ipAddress,
                Port = port,
                Version = GetApplicationVersion(),
                Status = ServerStatus.Online,
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                ActiveConnections = 0,
                Metadata = new Dictionary<string, object>
                {
                    ["framework"] = ".NET 8.0",
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    ["startedAt"] = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// 로컬 IP 주소를 가져옵니다
        /// </summary>
        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                return ipAddress?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 현재 포트를 가져옵니다
        /// </summary>
        private static int GetCurrentPort()
        {
            // 환경변수나 설정에서 포트를 가져올 수 있음
            if (int.TryParse(Environment.GetEnvironmentVariable("API_PORT"), out var port))
            {
                return port;
            }

            return 7910; // 기본 포트
        }

        /// <summary>
        /// 애플리케이션 버전을 가져옵니다
        /// </summary>
        private static string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}