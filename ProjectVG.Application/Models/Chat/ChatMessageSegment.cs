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
        /// 세그먼트의 요약 문자열을 반환합니다.
        /// </summary>
        /// <returns>Order 값을 항상 포함하고, 텍스트가 있으면 텍스트(따옴표 포함), 오디오가 있으면 바이트 크기·콘텐츠 타입·오디오 길이(소수점 둘째 자리까지)를, 감정이 있으면 감정 라벨을 각각 포함한 "Segment(...)" 형식의 문자열.</returns>
        public override string ToString()
        {
            var parts = new List<string>();
            
            parts.Add($"Order: {Order}");
            
            if (HasText)
            {
                parts.Add($"Text: \"{Text}\"");
            }
            
            if (HasAudio)
            {
                parts.Add($"Audio: {AudioData!.Length} bytes, {AudioContentType}, {AudioLength:F2}s");
            }
            
            if (!string.IsNullOrEmpty(Emotion))
            {
                parts.Add($"Emotion: {Emotion}");
            }
            
            return $"Segment({string.Join(", ", parts)})";
        }
        
        /// <summary>
        /// 세그먼트의 간단한 사람이 읽기 쉬운 표현을 반환합니다.
        /// </summary>
        /// <returns>
        /// 텍스트가 있을 경우 큰따옴표로 감싼 텍스트를 포함하고, 오디오가 있을 경우 "[Audio: {길이}s]" 형식(소수점 한 자리)으로 오디오 표시를 포함한 문자열을 반환합니다. 항목들은 공백으로 구분되며, 텍스트와 오디오가 모두 없으면 빈 문자열을 반환합니다.
        /// </returns>
        public string ToShortString()
        {
            var parts = new List<string>();
            
            if (HasText)
            {
                parts.Add($"\"{Text}\"");
            }
            
            if (HasAudio)
            {
                parts.Add($"[Audio: {AudioLength:F1}s]");
            }
            
            return string.Join(" ", parts);
        }
        
        /// <summary>
        /// 세그먼트의 내부 상태를 사람이 읽기 쉬운 형태로 반환합니다. 
        /// 반환 문자열에는 Order, 텍스트 존재 여부, 오디오 존재 여부, 감정(없으면 "none"), 오디오 바이트 크기, 오디오 길이(초, 소수점 둘째 자리) 정보가 포함됩니다.
        /// </summary>
        /// <returns>세그먼트 상태를 포함한 디버그용 문자열.</returns>
        public string ToDebugString()
        {
            return $"Segment[Order={Order}, Text={HasText}, Audio={HasAudio}, Emotion={Emotion ?? "none"}, AudioSize={AudioData?.Length ?? 0} bytes, AudioLength={AudioLength:F2}s]";
        }
        
        /// <summary>
        /// 텍스트 전용 채팅 세그먼트를 생성합니다.
        /// </summary>
        /// <param name="text">세그먼트에 저장할 텍스트(Nullable 허용).</param>
        /// <param name="order">세그먼트의 표시 순서(기본값 0).</param>
        /// <returns>지정된 텍스트와 순서를 가진 새로운 <see cref="ChatMessageSegment"/> 인스턴스.</returns>
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Order = order
            };
        }
        
        /// <summary>
        /// 오디오 전용 메시지 세그먼트를 생성합니다.
        /// </summary>
        /// <param name="audioData">세그먼트에 포함할 오디오 바이트 배열.</param>
        /// <param name="contentType">오디오의 MIME 타입(예: "audio/mpeg").</param>
        /// <param name="audioLength">오디오 길이(초). 알 수 없으면 null.</param>
        /// <param name="order">세그먼트의 표시 순서(기본값 0).</param>
        /// <returns>AudioData, AudioContentType, AudioLength 및 Order가 설정된 <see cref="ChatMessageSegment"/> 인스턴스.</returns>
        public static ChatMessageSegment CreateAudioOnly(byte[] audioData, string contentType, float? audioLength, int order = 0)
        {
            return new ChatMessageSegment
            {
                AudioData = audioData,
                AudioContentType = contentType,
                AudioLength = audioLength,
                Order = order
            };
        }
        
        /// <summary>
        /// 텍스트와 오디오(및 선택적 감정)를 함께 포함하는 ChatMessageSegment 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="text">세그먼트의 텍스트 콘텐츠(없을 수 있음).</param>
        /// <param name="audioData">오디오 바이트 배열(빈 값 또는 null 가능하지만 HasAudio 계산에 영향).</param>
        /// <param name="contentType">오디오의 MIME 타입/콘텐츠 타입(예: "audio/mpeg").</param>
        /// <param name="audioLength">오디오 길이(초). 알 수 없으면 null.</param>
        /// <param name="emotion">선택적 감정 라벨(예: "happy").</param>
        /// <param name="order">세그먼트의 표시 순서(기본값 0).</param>
        /// <returns>지정한 필드들이 설정된 ChatMessageSegment 객체.</returns>
        public static ChatMessageSegment CreateIntegrated(string text, byte[] audioData, string contentType, float? audioLength, string? emotion = null, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                AudioData = audioData,
                AudioContentType = contentType,
                AudioLength = audioLength,
                Emotion = emotion,
                Order = order
            };
        }
    }
}
