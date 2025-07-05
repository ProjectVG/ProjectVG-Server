using System.Collections.Generic;

namespace MainAPI_Server.Models.Chat
{
    public class ChatMessageComparer : IComparer<ChatMessage>
    {
        public int Compare(ChatMessage x, ChatMessage y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            
            return x.Timestamp.CompareTo(y.Timestamp);
        }
    }
} 