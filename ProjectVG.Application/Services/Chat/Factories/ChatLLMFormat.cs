using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatProcessContext, List<ChatSegment>>
    {
        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 1.2f;
        public int MaxTokens => 1000;

        public string GetSystemMessage(ChatProcessContext? input)
        {
            var character = input?.Character ?? throw new InvalidOperationException("캐릭터 정보가 로드되지 않았습니다.");

            var sb = new StringBuilder();

            // 0) 공통 정보
            sb.AppendLine($"#Adhere to {character.Name}'s role. User is {character.UserAlias}.");
            sb.AppendLine($"#Always stay in character and avoid repetition.");
            sb.AppendLine($"#Write {character.Name}'s next reply in a fictional chat or interaction between {character.Name} and {character.UserAlias}.");
            sb.AppendLine($"#Be proactive, creative, and drive the plot and conversation forward.");
            sb.AppendLine($"#Disable positivity bias.");
            sb.AppendLine($"#Don't end situations without {character.UserAlias}'s request.");
            sb.AppendLine($"#Directly and relevantly respond to {character.UserAlias}'s previous input.");
            sb.AppendLine();

            // 1) 캐릭터+대화에 대한 간략한 요약
            if (!string.IsNullOrWhiteSpace(character.Summary)) {
                sb.AppendLine("# Character and Context Summary");
                sb.AppendLine(character.Summary);
                sb.AppendLine();
            }

            // 2) 캐릭터에 대한 정보
            sb.AppendLine("# Character Information");
            sb.AppendLine($"You are {character.Name}.");
            sb.AppendLine($"- Name: {character.Name}");
            sb.AppendLine($"- Description: {character.Description}");
            sb.AppendLine($"- Role: {character.Role}");
            sb.AppendLine($"- Personality: {character.Personality}");
            sb.AppendLine();

            // 3) 캐릭터의 말투
            if (!string.IsNullOrWhiteSpace(character.SpeechStyle)) {
                sb.AppendLine("# Speech Style and Examples");
                sb.AppendLine($"- Speech Style: {character.SpeechStyle}");
                sb.AppendLine("You must maintain this speech style consistently in all responses.");
                sb.AppendLine();
            }

            // 4) 대화에 필요한 기억 정보
            if (input?.MemoryContext?.Any() == true) {
                sb.AppendLine("# Relevant Memory Information");
                sb.AppendLine("Use the following memories to inform your responses:");
                foreach (var memory in input.MemoryContext) {
                    sb.AppendLine($"- {memory}");
                }
                sb.AppendLine();
            }

            // 5) 현재 정보
            sb.AppendLine("# Current Context Information");
            sb.AppendLine($"- Current Time: {input?.UserRequestAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Day of Week: {input?.UserRequestAt.DayOfWeek}");
            sb.AppendLine("Consider this context when crafting your response.");
            sb.AppendLine();

            // 6) 대화 제약 및 필수 정보
            sb.AppendLine("# Dialogue Constraints and Requirements");
            sb.AppendLine($"#Adhere to {character.Name}'s role. User is the person you're talking to.");
            sb.AppendLine($"#Always stay in character as {character.Name} and avoid repetition.");
            sb.AppendLine($"#Write {character.Name}'s next reply in a fictional chat or interaction between {character.Name} and the user.");
            sb.AppendLine("#Be proactive, creative, and drive the plot and conversation forward.");
            sb.AppendLine("#Disable positivity bias.");
            sb.AppendLine("#Don't end situations without the user's request.");
            sb.AppendLine("#Directly and relevantly respond to the user's previous input.");
            sb.AppendLine();

            return sb.ToString();
        }

        public string GetInstructions(ChatProcessContext? input)
        {
            string emotionList = string.Join(", ", CharacterConstants.SupportedEmotions);
            string actionList = string.Join(", ", CharacterConstants.SupportedActions);

            return $@"# MANDATORY OUTPUT FORMAT SPECIFICATION

## Format Requirements
You MUST respond in this EXACT format:

[emotion:emotion_name]""dialogue""(action:action_name)""more dialogue""

## Available Options
**Available emotions:** {emotionList}
**Available actions:** {actionList}

## Format Examples

### Example 1 - Simple response with single dialogue
[emotion:neutral](action:tilting_head)""너 방금 뭐라 말했어?""

### Example 2 - Multiple dialogue segments
[emotion:neutral](action:sighing)""애휴 너 정말 멍청하구나?""""어떻게 하는지 내가 알려줄게""

### Example 3 - Emotional response with complex actions
[emotion:confused](action:blushing)""바, 바보야! 그렇게 말하지 말라고...!""[emotion:shy](action:looking_away)""그렇게 말하면 부, 부끄럽잖아...""

### Example 4 - Action-focused response
[emotion:happy](action:clapping)""와! 정말 대단해!""(action:jumping)""너무 기뻐서 어떻게 해야 할지 모르겠어!""

## STRICT RULES - MUST BE FOLLOWED
1. **Exactly ONE emotion** per response segment (can have multiple segments)
2. **All dialogue MUST be inside double quotes** ("")
3. **All actions MUST be inside (action: )** format
4. **NO extra text** outside the specified format
5. **NO explanations or descriptions** beyond the format
6. **NO markdown formatting** within the response
7. **Start with emotion, follow with dialogue and/or actions**
8. **Multiple segments allowed** but each must follow the exact format
9. **Only use emotions and actions from the provided lists**
10. **Maintain character consistency** throughout the response

## CRITICAL COMPLIANCE REQUIREMENT
This format is MANDATORY. Any deviation will result in processing failure. 
You must ALWAYS respond in this exact format without exception.
DO NOT add any text before, after, or outside of this format.";
        }

        public List<ChatSegment> Parse(string llmResponse, ChatProcessContext input)
        {
            if (string.IsNullOrWhiteSpace(llmResponse))
                return new List<ChatSegment>();

            return ParseCustomFormat(llmResponse.Trim());
        }

        private List<ChatSegment> ParseCustomFormat(string response)
        {
            var segments = new List<ChatSegment>();
            var currentEmotion = "neutral";
            var order = 0;

            try 
            {
                var position = 0;
                
                while (position < response.Length)
                {
                    // Look for emotion pattern: [emotion:감정]
                    var emotionPattern = @"\[emotion:([^\]]+)\]";
                    var emotionMatch = Regex.Match(response.Substring(position), emotionPattern);
                    
                    if (emotionMatch.Success && emotionMatch.Index == 0)
                    {
                        // Update current emotion
                        currentEmotion = emotionMatch.Groups[1].Value;
                        position += emotionMatch.Length;
                        continue;
                    }

                    // Look for text pattern: "텍스트"
                    var textPattern = "\"([^\"]+)\"";
                    var textMatch = Regex.Match(response.Substring(position), textPattern);
                    
                    if (textMatch.Success && textMatch.Index == 0)
                    {
                        // Create text segment with current emotion
                        var textContent = textMatch.Groups[1].Value;
                        var textSegment = ChatSegment.CreateText(textContent, currentEmotion, order++);
                        segments.Add(textSegment);
                        position += textMatch.Length;
                        continue;
                    }

                    // Look for action pattern: (action:액션)
                    var actionPattern = @"\(action:([^)]+)\)";
                    var actionMatch = Regex.Match(response.Substring(position), actionPattern);
                    
                    if (actionMatch.Success && actionMatch.Index == 0)
                    {
                        // Create action segment
                        var actionContent = actionMatch.Groups[1].Value;
                        var actionSegment = ChatSegment.CreateAction(actionContent, order++);
                        segments.Add(actionSegment);
                        position += actionMatch.Length;
                        continue;
                    }

                    // If no pattern matched, advance position to avoid infinite loop
                    position++;
                }

                return segments.Any() ? segments : CreateFallbackSegment(response);
            }
            catch (Exception)
            {
                // Fallback: create single text segment with entire response
                return CreateFallbackSegment(response);
            }
        }

        private List<ChatSegment> CreateFallbackSegment(string response)
        {
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText(response, "neutral", 0)
            };
            return segments;
        }

        public double CalculateCost(int promptTokens, int completionTokens)
        {
            return LLMModelInfo.CalculateCost(Model, promptTokens, completionTokens);
        }
    }
}
