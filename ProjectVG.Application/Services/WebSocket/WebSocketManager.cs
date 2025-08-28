using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.WebSocket
{
    public class WebSocketManager : IWebSocketManager
    {
        private readonly ILogger<IWebSocketManager> _logger;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly ISessionStorage _sessionStorage;

        /// <summary>
        /// WebSocketManager 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 로깅, 연결 레지스트리 및 세션 저장소에 대한 의존성을 주입하여 이 매니저를 사용 가능한 상태로 설정합니다.
        /// </remarks>
        public WebSocketManager(
            ILogger<IWebSocketManager> logger,
            IConnectionRegistry connectionRegistry,
            ISessionStorage sessionStorage)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _sessionStorage = sessionStorage;
        }

        /// <summary>
        /// 지정된 사용자 ID로 새로운 WebSocket 세션 정보를 저장하고 해당 사용자 ID를 반환합니다.
        /// </summary>
        /// <param name="userId">세션 식별 및 연결 대상으로 사용할 사용자 고유 ID.</param>
        /// <returns>생성된 세션의 식별자(입력한 <c>userId</c>와 동일).</returns>
        public async Task<string> ConnectAsync(string userId)
        {
            _logger.LogInformation("새 WebSocket 세션 생성: {UserId}", userId);

            await _sessionStorage.CreateAsync(new SessionInfo {
                SessionId = userId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });

            return userId;
        }

        /// <summary>
        /// 지정된 사용자에게 WebSocket 메시지를 JSON 텍스트로 직렬화하여 비동기 전송합니다.
        /// </summary>
        /// <param name="userId">메시지 수신 대상 사용자 ID.</param>
        /// <param name="message">전송할 WebSocket 메시지 객체.</param>
        /// <returns>메시지 전송이 완료될 때까지 대기하는 비동기 작업.</returns>
        public async Task SendAsync(string userId, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(userId, json);
            _logger.LogDebug("WebSocket 메시지 전송: {UserId}, 타입: {MessageType}", userId, message.Type);
        }

        /// <summary>
        /// 지정한 사용자 ID에 대응하는 활성 WebSocket 연결으로 텍스트 메시지를 비동기 전송합니다.
        /// </summary>
        /// <param name="userId">메시지 수신 대상의 사용자 ID.</param>
        /// <param name="text">전송할 텍스트 메시지 내용.</param>
        /// <returns>메시지 전송이 완료될 때까지 대기하는 Task. 대상 연결이 없으면 메시지는 전송되지 않고 바로 완료됩니다.</returns>
        public async Task SendTextAsync(string userId, string text)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null) {
                await connection.SendTextAsync(text);
                _logger.LogDebug("WebSocket 텍스트 전송: {UserId}", userId);
            }
            else {
                _logger.LogWarning("사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        /// <summary>
        /// 지정된 사용자에게 바이너리 데이터를 비동기적으로 전송합니다.
        /// </summary>
        /// <param name="userId">수신자 식별자(연결 조회에 사용). 활성 연결이 없으면 전송되지 않습니다.</param>
        /// <param name="data">전송할 바이너리 데이터.</param>
        /// <returns>전송 작업이 완료될 때까지 대기하는 비동기 작업.</returns>
        public async Task SendBinaryAsync(string userId, byte[] data)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null) {
                await connection.SendBinaryAsync(data);
                _logger.LogDebug("WebSocket 바이너리 전송: {UserId}, {Length} bytes", userId, data?.Length ?? 0);
            }
            else {
                _logger.LogWarning("사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        /// <summary>
        /// 지정한 사용자(userId)에 대한 WebSocket 연결을 등록 해제합니다.
        /// </summary>
        /// <param name="userId">등록 해제할 사용자의 식별자.</param>
        /// <returns>작업이 즉시 완료된 완료된 <see cref="Task"/>.</returns>
        public Task DisconnectAsync(string userId)
        {
            _connectionRegistry.Unregister(userId);
            _logger.LogInformation("WebSocket 세션 해제: {UserId}", userId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 지정한 사용자 ID가 현재 활성 WebSocket 세션(연결)을 보유하고 있는지 여부를 반환합니다.
        /// </summary>
        /// <param name="userId">확인할 사용자의 고유 식별자.</param>
        /// <returns>사용자가 연결되어 있으면 true, 그렇지 않으면 false.</returns>
        public bool IsSessionActive(string userId)
        {
            return _connectionRegistry.IsConnected(userId);
        }
    }
}
