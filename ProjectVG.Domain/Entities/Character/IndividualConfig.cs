using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectVG.Domain.Entities.Characters
{
    /// <summary>
    /// 개별 설정 모드에서 사용하는 캐릭터 설정 정보
    /// JSON으로 저장되어 스키마 변경 없이 필드 추가/수정 가능
    /// </summary>
    public class IndividualConfig
    {
        /// <summary>
        /// 캐릭터 성격
        /// </summary>
        [JsonPropertyName("personality")]
        public string? Personality { get; set; }
        
        /// <summary>
        /// 캐릭터 말투/화법
        /// </summary>
        [JsonPropertyName("speech_style")]
        public string? SpeechStyle { get; set; }
        
        /// <summary>
        /// 유저 별칭 (예: 주인님, 오너 등)
        /// </summary>
        [JsonPropertyName("user_alias")]
        public string? UserAlias { get; set; }
        
        /// <summary>
        /// 캐릭터 배경
        /// </summary>
        [JsonPropertyName("background")]
        public string? Background { get; set; }
        
        /// <summary>
        /// 캐릭터 역할/타입
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        /// <summary>
        /// 캐릭터+대화에 대한 간단한 요약
        /// </summary>
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
        
        /// <summary>
        /// 확장성을 위한 추가 데이터
        /// 새로운 필드 추가 시 기존 데이터와의 호환성 보장
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
        
        /// <summary>
        /// 개별 설정이 유효한지 검증
        /// </summary>
        /// <returns>유효하면 true, 그렇지 않으면 false</returns>
        public bool IsValid()
        {
            // 최소한 하나의 설정은 있어야 함
            return !string.IsNullOrWhiteSpace(Personality) ||
                   !string.IsNullOrWhiteSpace(SpeechStyle) ||
                   !string.IsNullOrWhiteSpace(UserAlias) ||
                   !string.IsNullOrWhiteSpace(Background) ||
                   !string.IsNullOrWhiteSpace(Role) ||
                   !string.IsNullOrWhiteSpace(Summary);
        }
        
        /// <summary>
        /// 개별 설정을 SystemPrompt 형태로 변환
        /// </summary>
        /// <returns>생성된 SystemPrompt</returns>
        public string BuildSystemPrompt()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(Role))
                parts.Add($"역할: {Role}");
                
            if (!string.IsNullOrWhiteSpace(Personality))
                parts.Add($"성격: {Personality}");
                
            if (!string.IsNullOrWhiteSpace(SpeechStyle))
                parts.Add($"말투: {SpeechStyle}");
                
            if (!string.IsNullOrWhiteSpace(Background))
                parts.Add($"배경: {Background}");
                
            if (!string.IsNullOrWhiteSpace(UserAlias))
                parts.Add($"사용자 호칭: {UserAlias}");
                
            if (!string.IsNullOrWhiteSpace(Summary))
                parts.Add($"요약: {Summary}");
            
            return string.Join("\n\n", parts);
        }
    }
}