using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;

namespace ProjectVG.Application.Services.Voice
{
    public interface IVoiceService
    {
        /// <summary>
        /// 텍스트를 음성으로 변환합니다.
        /// </summary>
        /// <param name="voiceName">VoiceCatalog에 정의된 고유 보이스 이름</param>
        /// <param name="text">변환할 텍스트</param>
        /// <param name="emotion">감정 스타일 (null이면 기본값)</param>
        /// <param name="voiceSettings">음성 설정 (null이면 기본값)</param>
        /// <returns>음성 변환 응답</returns>
        Task<TextToSpeechResponse> TextToSpeechAsync(
            string voiceName,
            string text,
            string? emotion = null,
            string? language = null,
            VoiceSettings? voiceSettings = null);
    }
} 