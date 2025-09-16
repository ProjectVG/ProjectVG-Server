using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient
{
    public class TextToSpeechClient : ITextToSpeechClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TextToSpeechClient> _logger;
        private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private const int MaxPoolSize = 1024 * 1024; // 1MB max pooled size

        public TextToSpeechClient(HttpClient httpClient, ILogger<TextToSpeechClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TextToSpeechResponse> TextToSpeechAsync(TextToSpeechRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.VoiceId))
                    throw new ArgumentException("VoiceId는 필수입니다.");
                string voiceId = request.VoiceId;

                string json = JsonSerializer.Serialize(request);
                _logger.LogDebug("[TTS][Request JSON] {Json}", json);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                _logger.LogDebug("[TTS] 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                HttpResponseMessage response = await _httpClient.PostAsync($"/v1/text-to-speech/{voiceId}", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var voiceResponse = new TextToSpeechResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = GetErrorMessageForStatusCode((int)response.StatusCode, response.ReasonPhrase ?? "Unknown");
                    _logger.LogDebug("[TTS] 응답 실패: {StatusCode} - {ErrorMsg}", response.StatusCode, errorMsg);
                    voiceResponse.Success = false;
                    voiceResponse.ErrorMessage = errorMsg;
                    return voiceResponse;
                }

                // ArrayPool 기반으로 음성 데이터 읽기 (LOH 방지)
                var (memoryOwner, dataSize) = await ReadAudioDataWithPoolAsync(response.Content);
                voiceResponse.AudioMemoryOwner = memoryOwner;
                voiceResponse.AudioDataSize = dataSize;
                voiceResponse.ContentType = response.Content.Headers.ContentType?.ToString();

                if (response.Headers.Contains("X-Audio-Length"))
                {
                    var audioLengthHeader = response.Headers.GetValues("X-Audio-Length").FirstOrDefault();
                    if (float.TryParse(audioLengthHeader, out float audioLength))
                    {
                        voiceResponse.AudioLength = audioLength;
                    }
                }

                _logger.LogDebug("[TTS][Response] 오디오 길이: {AudioLength:F2}초, ContentType: {ContentType}, 바이트: {Length}, 소요시간: {Elapsed}ms",
                    voiceResponse.AudioLength, voiceResponse.ContentType, voiceResponse.AudioDataSize, elapsed);

                return voiceResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS] 요청 처리 중 예외 발생");
                return new TextToSpeechResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        /// <summary>
        /// ArrayPool을 사용하여 스트림 기반으로 음성 데이터를 읽습니다 (LOH 할당 방지)
        /// </summary>
        private async Task<(IMemoryOwner<byte>?, int)> ReadAudioDataWithPoolAsync(HttpContent content)
        {
            const int chunkSize = 32768; // 32KB 청크 크기
            byte[]? readBuffer = null;
            IMemoryOwner<byte>? owner = null;

            try
            {
                readBuffer = _arrayPool.Rent(chunkSize);
                using var stream = await content.ReadAsStreamAsync();

                // 초기 버퍼 렌트(증분 확장 전략)
                owner = MemoryPool<byte>.Shared.Rent(chunkSize);
                int total = 0;
                while (true)
                {
                    // 여유 공간 없으면 확장
                    if (total == owner.Memory.Length)
                    {
                        var newOwner = MemoryPool<byte>.Shared.Rent(Math.Min(owner.Memory.Length * 2, int.MaxValue));
                        owner.Memory.Span.Slice(0, total).CopyTo(newOwner.Memory.Span);
                        owner.Dispose();
                        owner = newOwner;
                    }

                    int toRead = Math.Min(chunkSize, owner.Memory.Length - total);
                    int bytesRead = await stream.ReadAsync(readBuffer, 0, toRead);
                    if (bytesRead == 0) break;
                    readBuffer.AsSpan(0, bytesRead).CopyTo(owner.Memory.Span.Slice(total));
                    total += bytesRead;
                }

                if (total == 0)
                {
                    owner.Dispose();
                    _logger.LogDebug("[TTS][ArrayPool] 비어있는 오디오 스트림");
                    return (null, 0);
                }

                _logger.LogDebug("[TTS][ArrayPool] 음성 데이터 읽기 완료: {Size} bytes, 청크 크기: {ChunkSize}",
                    total, chunkSize);

                return (owner, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS][ArrayPool] 음성 데이터 읽기 실패");
                owner?.Dispose();
                return (null, 0);
            }
            finally
            {
                if (readBuffer != null)
                {
                    _arrayPool.Return(readBuffer);
                }
                // owner는 정상 경로에서 호출자에게 반환됨. 예외 시 위에서 Dispose 처리.
            }
        }

        private string GetErrorMessageForStatusCode(int statusCode, string reasonPhrase)
        {
            return $"HTTP {statusCode}: {reasonPhrase}";
        }

        public async Task<TextToSpeechDurationResponse> PredictDurationAsync(TextToSpeechDurationRequest request)
        {
            try
            {
                string endpoint = $"/v1/predict-duration/{request.VoiceId}";
                string json = JsonSerializer.Serialize(request);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                _logger.LogDebug("[TTS] 지속 시간 예측 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var durationResponse = new TextToSpeechDurationResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("[TTS] 지속 시간 예측 응답 실패: {StatusCode}", response.StatusCode);
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    return durationResponse;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("[TTS][Duration][Response] {Body}, 소요시간: {Elapsed}ms", responseBody, elapsed);
                var predictedDuration = JsonSerializer.Deserialize<TextToSpeechDurationResponse>(responseBody);

                if (predictedDuration == null)
                {
                    _logger.LogDebug("[TTS] 지속 시간 예측 응답 파싱 실패");
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = "응답을 파싱할 수 없습니다.";
                    return durationResponse;
                }

                durationResponse.Duration = predictedDuration.Duration;
                _logger.LogDebug("[TTS] 지속 시간 예측 완료: {Duration:F2}초", durationResponse.Duration);

                return durationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS] 지속 시간 예측 요청 처리 중 예외 발생");
                return new TextToSpeechDurationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }
    }
} 