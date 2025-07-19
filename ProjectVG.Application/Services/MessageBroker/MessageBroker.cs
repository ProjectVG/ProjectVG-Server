using System.Net;
using System.Net.Sockets;
using ProjectVG.Application.Services.Session;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.MessageBroker
{
    public class MessageBroker : IMessageBroker
    {
        private readonly IClientSessionService _clientSessionService;
        private readonly ILogger<MessageBroker> _logger;
        private readonly UdpClient _udpClient;

        public MessageBroker(IClientSessionService clientSessionService, ILogger<MessageBroker> logger)
        {
            _clientSessionService = clientSessionService;
            _logger = logger;
            _udpClient = new UdpClient();
        }

        public async Task<bool> SendResultAsync(string sessionId, string result)
        {
            try
            {
                var session = await _clientSessionService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
                    return false;
                }

                if (string.IsNullOrEmpty(session.ClientIP) || session.ClientPort == 0)
                {
                    _logger.LogWarning("클라이언트 IP/Port 정보 없음: {SessionId}", sessionId);
                    return false;
                }

                return await SendToEndpointAsync(session.ClientIP, session.ClientPort, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션에 처리 결과 전송 중 오류: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> SendToEndpointAsync(string ip, int port, string result)
        {
            try
            {
                if (!IPAddress.TryParse(ip, out var ipAddress))
                {
                    _logger.LogWarning("유효하지 않은 IP 주소: {IP}", ip);
                    return false;
                }

                var endpoint = new IPEndPoint(ipAddress, port);
                var resultBytes = System.Text.Encoding.UTF8.GetBytes(result);
                
                var sentBytes = await _udpClient.SendAsync(resultBytes, resultBytes.Length, endpoint);
                
                _logger.LogDebug("처리 결과 전송 완료: {IP}:{Port} -> {ResultLength} bytes", ip, port, sentBytes);
                return sentBytes > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "처리 결과 전송 중 오류: {IP}:{Port}", ip, port);
                return false;
            }
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
        }
    }
} 