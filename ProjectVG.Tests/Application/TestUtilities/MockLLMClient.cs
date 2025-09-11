using ProjectVG.Infrastructure.Integrations.LLMClient;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;

namespace ProjectVG.Tests.Application.TestUtilities
{
    /// <summary>
    /// Mock implementation of ILLMClient for testing scenarios
    /// Provides configurable responses and error simulation
    /// </summary>
    public class MockLLMClient : ILLMClient
    {
        private readonly Queue<LLMResponse> _responseQueue;
        private readonly Queue<Exception> _exceptionQueue;
        private LLMResponse? _defaultResponse;
        private Exception? _defaultException;

        public List<LLMRequest> SentRequests { get; } = new();
        public int CallCount { get; private set; }

        public MockLLMClient()
        {
            _responseQueue = new Queue<LLMResponse>();
            _exceptionQueue = new Queue<Exception>();
        }

        public MockLLMClient WithDefaultResponse(LLMResponse response)
        {
            _defaultResponse = response;
            return this;
        }

        public MockLLMClient WithQueuedResponse(LLMResponse response)
        {
            _responseQueue.Enqueue(response);
            return this;
        }

        public MockLLMClient WithQueuedResponses(params LLMResponse[] responses)
        {
            foreach (var response in responses)
            {
                _responseQueue.Enqueue(response);
            }
            return this;
        }

        public MockLLMClient WithDefaultException(Exception exception)
        {
            _defaultException = exception;
            return this;
        }

        public MockLLMClient WithQueuedException(Exception exception)
        {
            _exceptionQueue.Enqueue(exception);
            return this;
        }

        public MockLLMClient WithDelay(TimeSpan delay)
        {
            DelayBeforeResponse = delay;
            return this;
        }

        public TimeSpan DelayBeforeResponse { get; set; } = TimeSpan.Zero;

        public async Task<LLMResponse> SendRequestAsync(LLMRequest request)
        {
            CallCount++;
            SentRequests.Add(request);

            if (DelayBeforeResponse > TimeSpan.Zero)
            {
                await Task.Delay(DelayBeforeResponse);
            }

            // Check for queued exceptions first
            if (_exceptionQueue.Count > 0)
            {
                throw _exceptionQueue.Dequeue();
            }

            // Check for default exception
            if (_defaultException != null && _responseQueue.Count == 0)
            {
                throw _defaultException;
            }

            // Return queued response if available
            if (_responseQueue.Count > 0)
            {
                return _responseQueue.Dequeue();
            }

            // Return default response
            if (_defaultResponse != null)
            {
                return _defaultResponse;
            }

            // Fallback response
            return new LLMResponse
            {
                OutputText = "Mock response",
                InputTokens = 10,
                OutputTokens = 5,
                TotalTokens = 15
            };
        }

        public async Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage, 
            string userMessage, 
            string? instructions = "", 
            List<History>? conversationHistory = null, 
            string? model = "gpt-4o-mini", 
            int? maxTokens = 1000, 
            float? temperature = 0.7f)
        {
            var request = new LLMRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                SystemPrompt = systemMessage,
                UserPrompt = userMessage,
                Instructions = instructions ?? "",
                ConversationHistory = conversationHistory ?? new List<History>(),
                Model = model ?? "gpt-4o-mini",
                MaxTokens = maxTokens ?? 1000,
                Temperature = temperature ?? 0.7f,
                OpenAiApiKey = "",
                UseUserApiKey = false
            };

            return await SendRequestAsync(request);
        }

        public void Reset()
        {
            _responseQueue.Clear();
            _exceptionQueue.Clear();
            SentRequests.Clear();
            CallCount = 0;
            _defaultResponse = null;
            _defaultException = null;
            DelayBeforeResponse = TimeSpan.Zero;
        }

        public LLMRequest? GetLastRequest()
        {
            return SentRequests.LastOrDefault();
        }

        public LLMRequest? GetRequest(int index)
        {
            return index < SentRequests.Count ? SentRequests[index] : null;
        }
    }

    /// <summary>
    /// Builder for creating common LLM response scenarios
    /// </summary>
    public static class LLMResponseBuilder
    {
        public static LLMResponse Success(string outputText = "Mock success response", int inputTokens = 50, int outputTokens = 25)
        {
            return new LLMResponse
            {
                OutputText = outputText,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = inputTokens + outputTokens
            };
        }

        public static LLMResponse LargeResponse(int approximateTokens = 2000)
        {
            var text = new string('a', approximateTokens * 4); // Approximate 4 chars per token
            return new LLMResponse
            {
                OutputText = text,
                InputTokens = 100,
                OutputTokens = approximateTokens,
                TotalTokens = 100 + approximateTokens
            };
        }

        public static LLMResponse EmptyResponse()
        {
            return new LLMResponse
            {
                OutputText = "",
                InputTokens = 10,
                OutputTokens = 0,
                TotalTokens = 10
            };
        }

        public static LLMResponse HighCostResponse()
        {
            return new LLMResponse
            {
                OutputText = "This is an expensive response with many tokens used for processing complex requests.",
                InputTokens = 1500,
                OutputTokens = 800,
                TotalTokens = 2300
            };
        }

        public static LLMResponse ChatResponse(string message)
        {
            return new LLMResponse
            {
                OutputText = message,
                InputTokens = message.Length / 4, // Rough estimation
                OutputTokens = message.Length / 4,
                TotalTokens = message.Length / 2
            };
        }
    }

    /// <summary>
    /// Common LLM service exceptions for testing
    /// </summary>
    public static class LLMExceptions
    {
        public static HttpRequestException ServiceUnavailable()
        {
            return new HttpRequestException("LLM service is temporarily unavailable");
        }

        public static HttpRequestException RateLimited()
        {
            return new HttpRequestException("Rate limit exceeded");
        }

        public static TimeoutException RequestTimeout()
        {
            return new TimeoutException("LLM request timed out");
        }

        public static InvalidOperationException InvalidRequest()
        {
            return new InvalidOperationException("Invalid request parameters");
        }

        public static ArgumentException InvalidModel(string model)
        {
            return new ArgumentException($"Unsupported model: {model}");
        }
    }
}