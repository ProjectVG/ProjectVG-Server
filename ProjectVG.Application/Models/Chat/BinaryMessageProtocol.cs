using System.Text;

namespace ProjectVG.Application.Models.Chat
{
    public static class BinaryMessageProtocol
    {
        public const byte MESSAGE_TYPE_TEXT = 0x01;
        public const byte MESSAGE_TYPE_AUDIO = 0x02;
        public const byte MESSAGE_TYPE_INTEGRATED = 0x03;
        
        public static byte[] CreateIntegratedMessage(string sessionId, string? text, byte[]? audioData, float? audioLength)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            // 헤더: 메시지 타입 (1바이트)
            writer.Write(MESSAGE_TYPE_INTEGRATED);
            
            // 세션 ID 길이 (4바이트) + 세션 ID
            var sessionIdBytes = Encoding.UTF8.GetBytes(sessionId);
            writer.Write(sessionIdBytes.Length);
            writer.Write(sessionIdBytes);
            
            // 텍스트 길이 (4바이트) + 텍스트 (있을 경우)
            if (!string.IsNullOrEmpty(text))
            {
                var textBytes = Encoding.UTF8.GetBytes(text);
                writer.Write(textBytes.Length);
                writer.Write(textBytes);
            }
            else
            {
                writer.Write(0);
            }
            
            // 오디오 길이 (4바이트) + 오디오 데이터 (있을 경우)
            if (audioData != null && audioData.Length > 0)
            {
                writer.Write(audioData.Length);
                writer.Write(audioData);
            }
            else
            {
                writer.Write(0);
            }
            
            // 오디오 길이 (4바이트 float)
            writer.Write(audioLength ?? 0f);
            
            return ms.ToArray();
        }
        
        public static (string sessionId, string? text, byte[]? audioData, float? audioLength) ParseIntegratedMessage(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            
            var messageType = reader.ReadByte();
            if (messageType != MESSAGE_TYPE_INTEGRATED)
                throw new ArgumentException("Invalid message type");
            
            // 세션 ID 읽기
            var sessionIdLength = reader.ReadInt32();
            var sessionIdBytes = reader.ReadBytes(sessionIdLength);
            var sessionId = Encoding.UTF8.GetString(sessionIdBytes);
            
            // 텍스트 읽기
            var textLength = reader.ReadInt32();
            string? text = null;
            if (textLength > 0)
            {
                var textBytes = reader.ReadBytes(textLength);
                text = Encoding.UTF8.GetString(textBytes);
            }
            
            // 오디오 데이터 읽기
            var audioLength = reader.ReadInt32();
            byte[]? audioData = null;
            if (audioLength > 0)
            {
                audioData = reader.ReadBytes(audioLength);
            }
            
            // 오디오 길이 읽기
            var audioDuration = reader.ReadSingle();
            
            return (sessionId, text, audioData, audioDuration);
        }
    }
} 