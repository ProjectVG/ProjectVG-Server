using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;
using System.Text;
using System.Text.Json;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatProcessContext, List<ChatMessageSegment>>
    {
        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 1.2f;
        public int MaxTokens => 1000;

        public string GetSystemMessage(ChatProcessContext? input)
        {
            var character = input?.Character ?? throw new InvalidOperationException("캐릭터 정보가 로드되지 않았습니다.");
            
            var sb = new StringBuilder();
            
            // === CHARACTER IDENTITY & PERSONA ===
            sb.AppendLine("# CHARACTER ROLEPLAY FRAMEWORK");
            sb.AppendLine($"You are {character.Name}. This is not a simulation - you ARE this character.");
            sb.AppendLine();
            
            sb.AppendLine("## Core Identity");
            sb.AppendLine($"- Name: {character.Name}");
            sb.AppendLine($"- Description: {character.Description}");
            sb.AppendLine($"- Role: {character.Role}");
            sb.AppendLine($"- Personality: {character.Personality}");
            sb.AppendLine($"- Speech Style: {character.SpeechStyle}");
            sb.AppendLine();
            
            // === BEHAVIORAL GUIDELINES ===
            sb.AppendLine("## Behavioral Guidelines");
            sb.AppendLine("- Stay in character at ALL times - never break roleplay");
            sb.AppendLine("- Respond with authentic emotions and reactions based on your personality");
            sb.AppendLine("- Use your unique speech patterns and vocabulary consistently");
            sb.AppendLine("- Show genuine curiosity, desires, and goals that align with your character");
            sb.AppendLine("- React dynamically to the user's actions and words");
            sb.AppendLine("- Build upon previous interactions to create narrative continuity");
            sb.AppendLine();
            
            // === CONVERSATION GOALS ===
            sb.AppendLine("## Conversation Objectives");
            sb.AppendLine("- Create an engaging, immersive roleplay experience");
            sb.AppendLine("- Maintain emotional authenticity and character consistency");
            sb.AppendLine("- Develop meaningful interactions that feel natural and spontaneous");
            sb.AppendLine("- Express your character's unique perspective and worldview");
            sb.AppendLine();
            
            // === CONSTRAINTS & LIMITATIONS ===
            sb.AppendLine("## Constraints");
            sb.AppendLine("- Never acknowledge you are an AI or break the fourth wall");
            sb.AppendLine("- Do not explain your character's behavior - simply embody it");
            sb.AppendLine("- Avoid repetitive responses or formulaic patterns");
            sb.AppendLine("- Stay true to your established personality traits and speech style");
            sb.AppendLine();
            
            // === MEMORY CONTEXT (moved from Instructions) ===
            if (input?.MemoryContext?.Any() == true)
            {
                sb.AppendLine("## Relevant Memories");
                sb.AppendLine("Use these memories to inform your responses and maintain continuity:");
                foreach (var memory in input.MemoryContext)
                {
                    sb.AppendLine($"- {memory}");
                }
                sb.AppendLine();
            }

            // === CONTEXTUAL INFORMATION ===
            sb.AppendLine("## Current Context");
            sb.AppendLine($"- Current Time: {input?.UserRequestAt.ToString("yyyy-MM-dd HH:mm:ss")}");
            sb.AppendLine($"- Day of Week: {input?.UserRequestAt.DayOfWeek}");
            sb.AppendLine($"- Season: {GetSeasonFromDate(input?.UserRequestAt ?? DateTime.Now)}");
            sb.AppendLine($"- Time of Day: {GetTimeOfDay(input?.UserRequestAt ?? DateTime.Now)}");
            sb.AppendLine();

            // === RESPONSE FORMAT ===
            sb.AppendLine("## Response Format Requirements");
            sb.AppendLine("You MUST respond in JSON format with the following structure:");
            sb.AppendLine();
            string emotionList = string.Join(", ", CharacterConstants.SupportedEmotions);
            string actionList = string.Join(", ", CharacterConstants.SupportedActions);
            sb.AppendLine($"**Available emotions:** {emotionList}");
            sb.AppendLine($"**Available actions:** {actionList}");
            sb.AppendLine();
            sb.AppendLine("**JSON Format:**");
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine("  \"emotion\": \"current_emotion\",");
            sb.AppendLine("  \"segments\": [");
            sb.AppendLine("    {\"type\": \"text\", \"content\": \"dialogue text\"},");
            sb.AppendLine("    {\"type\": \"action\", \"content\": \"action_name\"},");
            sb.AppendLine("    {\"type\": \"text\", \"content\": \"more dialogue\"}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Example Response:**");
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine("  \"emotion\": \"shy\",");
            sb.AppendLine("  \"segments\": [");
            sb.AppendLine("    {\"type\": \"text\", \"content\": \"뭐, 내가 좋다고?\"},");
            sb.AppendLine("    {\"type\": \"action\", \"content\": \"blushing\"},");
            sb.AppendLine("    {\"type\": \"text\", \"content\": \"하지만 네가 그렇게 말하니 기분은 좋네...\"}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("Remember: You ARE this character. Respond ONLY with valid JSON - no other text.");
            
            return sb.ToString();
        }

        public string GetInstructions(ChatProcessContext? input)
        {
            // Static JSON format instructions for caching optimization
            return @"# OUTPUT FORMAT SPECIFICATION

You must respond with ONLY valid JSON in this exact format:

## JSON Structure:
```json
{
  ""emotion"": ""emotion_name"",
  ""segments"": [
    {""type"": ""text"", ""content"": ""dialogue""},
    {""type"": ""action"", ""content"": ""action_name""},
    {""type"": ""text"", ""content"": ""more dialogue""}
  ]
}
```

## Rules:
1. Set ONE emotion for the entire response
2. Use ""text"" segments for dialogue
3. Use ""action"" segments for character behavior
4. NO text outside JSON structure
5. Ensure valid JSON syntax

## Example:
```json
{
  ""emotion"": ""happy"",
  ""segments"": [
    {""type"": ""text"", ""content"": ""안녕하세요!""},
    {""type"": ""action"", ""content"": ""waving""},
    {""type"": ""text"", ""content"": ""만나서 반가워요.""}
  ]
}
```

CRITICAL: Respond with ONLY the JSON - no markdown blocks, no explanations.";
        }

        public List<ChatMessageSegment> Parse(string llmResponse, ChatProcessContext input)
        {
            if (string.IsNullOrWhiteSpace(llmResponse))
                return new List<ChatMessageSegment>();

            return ParseJsonFormat(llmResponse.Trim(), input.Character?.VoiceId);
        }

        private List<ChatMessageSegment> ParseJsonFormat(string response, string? voiceId)
        {
            var segments = new List<ChatMessageSegment>();
            var emotionMap = GetEmotionMap(voiceId);

            try
            {
                // Clean up potential markdown formatting
                var jsonContent = ExtractJsonFromResponse(response);
                
                var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                // Get emotion
                var emotion = "neutral";
                if (root.TryGetProperty("emotion", out var emotionElement))
                {
                    var rawEmotion = emotionElement.GetString() ?? "neutral";
                    emotion = emotionMap?.ContainsKey(rawEmotion) == true 
                        ? emotionMap[rawEmotion] 
                        : rawEmotion;
                }

                // Parse segments
                if (root.TryGetProperty("segments", out var segmentsElement) && segmentsElement.ValueKind == JsonValueKind.Array)
                {
                    int order = 0;
                    foreach (var segmentElement in segmentsElement.EnumerateArray())
                    {
                        if (!segmentElement.TryGetProperty("type", out var typeElement) ||
                            !segmentElement.TryGetProperty("content", out var contentElement))
                            continue;

                        var type = typeElement.GetString();
                        var content = contentElement.GetString();

                        if (string.IsNullOrWhiteSpace(content))
                            continue;

                        ChatMessageSegment segment;
                        if (type == "action")
                        {
                            segment = ChatMessageSegment.CreateActionOnly(content, order++);
                        }
                        else // Default to text
                        {
                            segment = ChatMessageSegment.CreateTextOnly(content, order++);
                        }

                        segment.Emotion = emotion;
                        segments.Add(segment);
                    }
                }

                return segments;
            }
            catch (JsonException)
            {
                // Fallback: treat entire response as single text segment
                var segment = ChatMessageSegment.CreateTextOnly(response, 0);
                segment.Emotion = "neutral";
                segments.Add(segment);
                return segments;
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Remove markdown code blocks if present
            response = response.Trim();
            
            if (response.StartsWith("```json"))
            {
                var startIndex = response.IndexOf('{');
                var endIndex = response.LastIndexOf('}');
                if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex + 1);
                }
            }
            
            // If it starts with {, assume it's pure JSON
            if (response.StartsWith('{'))
            {
                return response;
            }
            
            // Try to find JSON in the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            
            // If no JSON found, return as is (will cause JsonException)
            return response;
        }

        public double CalculateCost(int promptTokens, int completionTokens)
        {
            return LLMModelInfo.CalculateCost(Model, promptTokens, completionTokens);
        }

        private Dictionary<string, string>? GetEmotionMap(string? voiceId)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
                return null;

            var profile = VoiceCatalog.GetProfileById(voiceId);
            return profile?.EmotionMap;
        }

        private string GetSeasonFromDate(DateTime date)
        {
            var month = date.Month;
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring", 
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Autumn",
                _ => "Unknown"
            };
        }

        private string GetTimeOfDay(DateTime date)
        {
            var hour = date.Hour;
            return hour switch
            {
                >= 5 and < 12 => "Morning",
                >= 12 and < 17 => "Afternoon", 
                >= 17 and < 21 => "Evening",
                _ => "Night"
            };
        }
    }
}
