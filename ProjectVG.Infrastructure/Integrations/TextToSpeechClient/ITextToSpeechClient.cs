using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient
{
    public interface ITextToSpeechClient
    {
        /// <summary>
        /// 텍스트를 음성으로 변환합니다.
        /// </summary>
        /// <param name="request">음성 변환 요청 객체</param>
        /// <returns>음성 변환 응답</returns>
        Task<TextToSpeechResponse> TextToSpeechAsync(TextToSpeechRequest request);

        /// <summary>
        /// 음성 지속 시간을 예측합니다.
        /// </summary>
        /// <param name="request">음성 지속 시간 예측 요청 객체</param>
        /// <returns>음성 지속 시간 예측 응답</returns>
        Task<TextToSpeechDurationResponse> PredictDurationAsync(TextToSpeechDurationRequest request);
    }
} 