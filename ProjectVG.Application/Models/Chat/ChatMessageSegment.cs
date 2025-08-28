namespace ProjectVG.Application.Models.Chat
{
    public class ChatMessageSegment
    {
        public string? Text { get; set; }
        public byte[]? AudioData { get; set; }
        public string? AudioContentType { get; set; }
        public float? AudioLength { get; set; }
        public string? Emotion { get; set; }
        public int Order { get; set; }
        
        public bool HasText => !string.IsNullOrEmpty(Text);
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        public bool IsEmpty => !HasText && !HasAudio;
        
        
        /// <summary>
        /// 텍스트만 포함하는 새 ChatMessageSegment 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="text">세그먼트의 텍스트 내용(빈 문자열 허용).</param>
        /// <param name="order">세그먼트의 순서(기본값 0).</param>
        /// <returns>Text가 설정되고 오디오 관련 필드는 null 또는 기본값인 ChatMessageSegment.</returns>
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Order = order
            };
        }

        /// <summary>
        /// 이 인스턴스의 오디오 관련 속성(AudioData, AudioContentType, AudioLength)을 설정하거나 null을 전달해 해당 값을 제거합니다.
        /// </summary>
        /// <param name="audioData">원시 오디오 바이트 배열. null이면 기존 오디오 데이터를 제거합니다.</param>
        /// <param name="audioContentType">오디오의 MIME 타입(예: "audio/mpeg"). null이면 기존 값을 제거합니다.</param>
        /// <param name="audioLength">오디오 길이(초 단위). null이면 기존 값을 제거합니다.</param>
        public void SetAudioData(byte[]? audioData, string? audioContentType, float? audioLength)
        {
            AudioData = audioData;
            AudioContentType = audioContentType;
            AudioLength = audioLength;
        }
        

    }
}
