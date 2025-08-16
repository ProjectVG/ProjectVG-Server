using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.WebSocket
{
    public class WebSocketMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public object Data { get; set; } = new();
        
        public WebSocketMessage() { }
        
        /// <summary>
        /// 메시지 유형과 페이로드를 지정하여 WebSocketMessage 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="type">메시지의 식별자 또는 종류를 나타내는 문자열(예: 이벤트 이름).</param>
        /// <param name="data">전송할 페이로드 객체(직렬화 가능한 데이터).</param>
        public WebSocketMessage(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}
