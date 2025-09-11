using System.Text;

namespace ProjectVG.Tests.Application.TestUtilities
{
    /// <summary>
    /// Mock implementation of TTS service client for testing scenarios
    /// </summary>
    public class MockTTSClient
    {
        private readonly Queue<TTSResponse> _responseQueue;
        private readonly Queue<Exception> _exceptionQueue;
        private readonly List<TTSRequest> _requests;
        private TTSResponse? _defaultResponse;
        private Exception? _defaultException;

        public MockTTSClient()
        {
            _responseQueue = new Queue<TTSResponse>();
            _exceptionQueue = new Queue<Exception>();
            _requests = new List<TTSRequest>();
        }

        public List<TTSRequest> Requests => _requests.AsReadOnly().ToList();
        public int RequestCount => _requests.Count;
        public TimeSpan ProcessingDelay { get; set; } = TimeSpan.Zero;

        public MockTTSClient WithDefaultResponse(TTSResponse response)
        {
            _defaultResponse = response;
            return this;
        }

        public MockTTSClient WithQueuedResponse(TTSResponse response)
        {
            _responseQueue.Enqueue(response);
            return this;
        }

        public MockTTSClient WithDefaultException(Exception exception)
        {
            _defaultException = exception;
            return this;
        }

        public MockTTSClient WithQueuedException(Exception exception)
        {
            _exceptionQueue.Enqueue(exception);
            return this;
        }

        public MockTTSClient WithProcessingDelay(TimeSpan delay)
        {
            ProcessingDelay = delay;
            return this;
        }

        public async Task<TTSResponse> GenerateSpeechAsync(string text, string voice = "default", string language = "ko-KR")
        {
            var request = new TTSRequest
            {
                Text = text,
                Voice = voice,
                Language = language,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            _requests.Add(request);

            if (ProcessingDelay > TimeSpan.Zero)
            {
                await Task.Delay(ProcessingDelay);
            }

            // Check for queued exceptions
            if (_exceptionQueue.Count > 0)
            {
                throw _exceptionQueue.Dequeue();
            }

            // Check for default exception
            if (_defaultException != null && _responseQueue.Count == 0)
            {
                throw _defaultException;
            }

            // Return queued response
            if (_responseQueue.Count > 0)
            {
                return _responseQueue.Dequeue();
            }

            // Return default response
            if (_defaultResponse != null)
            {
                return _defaultResponse;
            }

            // Generate mock audio data
            return GenerateMockResponse(request);
        }

        public async Task<TTSResponse> GenerateSpeechWithOptionsAsync(
            string text, 
            TTSOptions options)
        {
            return await GenerateSpeechAsync(text, options.Voice, options.Language);
        }

        private TTSResponse GenerateMockResponse(TTSRequest request)
        {
            // Generate mock audio data based on text length
            var audioDataSize = Math.Max(1024, request.Text.Length * 10); // Minimum 1KB
            var mockAudioData = new byte[audioDataSize];
            new Random().NextBytes(mockAudioData);

            return new TTSResponse
            {
                RequestId = request.RequestId,
                AudioData = mockAudioData,
                Format = "wav",
                SampleRate = 22050,
                Duration = TimeSpan.FromMilliseconds(request.Text.Length * 50), // ~50ms per character
                Size = audioDataSize,
                Voice = request.Voice,
                Language = request.Language,
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public void Reset()
        {
            _responseQueue.Clear();
            _exceptionQueue.Clear();
            _requests.Clear();
            _defaultResponse = null;
            _defaultException = null;
            ProcessingDelay = TimeSpan.Zero;
        }

        public TTSRequest? GetLastRequest()
        {
            return _requests.LastOrDefault();
        }

        public bool HasProcessedText(string text)
        {
            return _requests.Any(r => r.Text == text);
        }

        public int GetRequestCountForVoice(string voice)
        {
            return _requests.Count(r => r.Voice == voice);
        }
    }

    /// <summary>
    /// Mock WebSocket manager for testing real-time communication
    /// </summary>
    public class MockWebSocketManager
    {
        private readonly Dictionary<string, MockWebSocketConnection> _connections;
        private readonly List<WebSocketEvent> _events;

        public MockWebSocketManager()
        {
            _connections = new Dictionary<string, MockWebSocketConnection>();
            _events = new List<WebSocketEvent>();
        }

        public List<WebSocketEvent> Events => _events.AsReadOnly().ToList();
        public int ConnectionCount => _connections.Count;

        public async Task<bool> SendMessageAsync(string connectionId, object message)
        {
            _events.Add(new WebSocketEvent("SendMessage", connectionId, message));

            if (_connections.TryGetValue(connectionId, out var connection))
            {
                await connection.ReceiveMessage(message);
                return true;
            }

            return false;
        }

        public async Task<bool> SendToUserAsync(Guid userId, object message)
        {
            var userConnections = _connections.Values.Where(c => c.UserId == userId);
            var success = false;

            foreach (var connection in userConnections)
            {
                _events.Add(new WebSocketEvent("SendToUser", connection.ConnectionId, message, userId));
                await connection.ReceiveMessage(message);
                success = true;
            }

            return success;
        }

        public async Task<bool> BroadcastAsync(object message)
        {
            _events.Add(new WebSocketEvent("Broadcast", "all", message));

            foreach (var connection in _connections.Values)
            {
                await connection.ReceiveMessage(message);
            }

            return _connections.Count > 0;
        }

        public void AddConnection(string connectionId, Guid userId)
        {
            var connection = new MockWebSocketConnection(connectionId, userId);
            _connections[connectionId] = connection;
            _events.Add(new WebSocketEvent("Connect", connectionId, null, userId));
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                _connections.Remove(connectionId);
                _events.Add(new WebSocketEvent("Disconnect", connectionId, null, connection.UserId));
            }
        }

        public bool IsConnected(string connectionId)
        {
            return _connections.ContainsKey(connectionId);
        }

        public List<string> GetUserConnections(Guid userId)
        {
            return _connections.Values
                .Where(c => c.UserId == userId)
                .Select(c => c.ConnectionId)
                .ToList();
        }

        public void Reset()
        {
            _connections.Clear();
            _events.Clear();
        }

        public MockWebSocketConnection? GetConnection(string connectionId)
        {
            return _connections.TryGetValue(connectionId, out var connection) ? connection : null;
        }
    }

    /// <summary>
    /// Data models for mock TTS and WebSocket services
    /// </summary>
    public class TTSRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Voice { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class TTSResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
        public string Format { get; set; } = "wav";
        public int SampleRate { get; set; } = 22050;
        public TimeSpan Duration { get; set; }
        public int Size { get; set; }
        public string Voice { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class TTSOptions
    {
        public string Voice { get; set; } = "default";
        public string Language { get; set; } = "ko-KR";
        public float Speed { get; set; } = 1.0f;
        public float Pitch { get; set; } = 1.0f;
        public string Format { get; set; } = "wav";
        public int SampleRate { get; set; } = 22050;
    }

    public class MockWebSocketConnection
    {
        public string ConnectionId { get; }
        public Guid UserId { get; }
        public List<object> ReceivedMessages { get; }
        public DateTime ConnectedAt { get; }

        public MockWebSocketConnection(string connectionId, Guid userId)
        {
            ConnectionId = connectionId;
            UserId = userId;
            ReceivedMessages = new List<object>();
            ConnectedAt = DateTime.UtcNow;
        }

        public async Task ReceiveMessage(object message)
        {
            ReceivedMessages.Add(message);
            await Task.CompletedTask;
        }

        public bool HasReceived<T>(Func<T, bool>? predicate = null) where T : class
        {
            var messages = ReceivedMessages.OfType<T>();
            return predicate != null ? messages.Any(predicate) : messages.Any();
        }

        public T? GetLastMessage<T>() where T : class
        {
            return ReceivedMessages.OfType<T>().LastOrDefault();
        }
    }

    public class WebSocketEvent
    {
        public string EventType { get; }
        public string ConnectionId { get; }
        public object? Message { get; }
        public Guid? UserId { get; }
        public DateTime Timestamp { get; }

        public WebSocketEvent(string eventType, string connectionId, object? message, Guid? userId = null)
        {
            EventType = eventType;
            ConnectionId = connectionId;
            Message = message;
            UserId = userId;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Builder for creating common TTS response scenarios
    /// </summary>
    public static class TTSResponseBuilder
    {
        public static TTSResponse Success(string requestId = "", string text = "Hello World")
        {
            var audioSize = Math.Max(1024, text.Length * 10);
            var audioData = new byte[audioSize];
            new Random().NextBytes(audioData);

            return new TTSResponse
            {
                RequestId = requestId.IsNullOrEmpty() ? Guid.NewGuid().ToString() : requestId,
                AudioData = audioData,
                Format = "wav",
                SampleRate = 22050,
                Duration = TimeSpan.FromMilliseconds(text.Length * 50),
                Size = audioSize,
                Voice = "default",
                Language = "ko-KR",
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public static TTSResponse Failure(string requestId = "", string errorMessage = "TTS generation failed")
        {
            return new TTSResponse
            {
                RequestId = requestId.IsNullOrEmpty() ? Guid.NewGuid().ToString() : requestId,
                AudioData = Array.Empty<byte>(),
                Success = false,
                ErrorMessage = errorMessage,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public static TTSResponse LargeAudio(int sizeInKB = 100)
        {
            var audioData = new byte[sizeInKB * 1024];
            new Random().NextBytes(audioData);

            return new TTSResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                AudioData = audioData,
                Format = "wav",
                SampleRate = 22050,
                Duration = TimeSpan.FromSeconds(sizeInKB / 10), // Rough estimation
                Size = audioData.Length,
                Voice = "default",
                Language = "ko-KR",
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Common TTS service exceptions for testing
    /// </summary>
    public static class TTSExceptions
    {
        public static HttpRequestException ServiceUnavailable()
        {
            return new HttpRequestException("TTS service is temporarily unavailable");
        }

        public static ArgumentException UnsupportedVoice(string voice)
        {
            return new ArgumentException($"Unsupported voice: {voice}");
        }

        public static ArgumentException UnsupportedLanguage(string language)
        {
            return new ArgumentException($"Unsupported language: {language}");
        }

        public static InvalidOperationException TextTooLong(int maxLength = 5000)
        {
            return new InvalidOperationException($"Text exceeds maximum length of {maxLength} characters");
        }

        public static TimeoutException ProcessingTimeout()
        {
            return new TimeoutException("TTS processing timed out");
        }
    }
}

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str);
    }
}