using FluentAssertions;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Services.Chat.Factories;
using Xunit;

namespace ProjectVG.Tests.Services.Chat.Factories
{
    public class ChatLLMFormatTests
    {
        private readonly ChatLLMFormat _format;
        private readonly ChatProcessContext _context;

        public ChatLLMFormatTests()
        {
            _format = new ChatLLMFormat();
            
            var character = new CharacterDto
            {
                Id = Guid.NewGuid(),
                Name = "TestCharacter",
                Description = "Test character description",
                Role = "Assistant",
                Personality = "Friendly and helpful",
                SpeechStyle = "Casual",
                VoiceId = "test-voice"
            };

            var command = new ChatRequestCommand(
                Guid.NewGuid(), 
                character.Id, 
                "Test message", 
                DateTime.Now, 
                true
            );

            _context = new ChatProcessContext(
                command,
                character,
                new List<ProjectVG.Domain.Entities.ConversationHistorys.ConversationHistory>(),
                new List<string> { "Previous conversation memory" }
            );
        }

        [Fact]
        public void Parse_ValidJsonResponse_ShouldReturnCorrectSegments()
        {
            // Arrange
            var llmResponse = @"{
                ""emotion"": ""happy"",
                ""segments"": [
                    {""type"": ""text"", ""content"": ""안녕하세요!""},
                    {""type"": ""action"", ""content"": ""waving""},
                    {""type"": ""text"", ""content"": ""반가워요!""}
                ]
            }";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(3);
            
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Text.Should().Be("안녕하세요!");
            result[0].Emotion.Should().Be("happy");
            result[0].Order.Should().Be(0);

            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Action.Should().Be("waving");
            result[1].Emotion.Should().Be("happy");
            result[1].Order.Should().Be(1);

            result[2].Type.Should().Be(SegmentType.Text);
            result[2].Text.Should().Be("반가워요!");
            result[2].Emotion.Should().Be("happy");
            result[2].Order.Should().Be(2);
        }

        [Fact]
        public void Parse_JsonWithMarkdownBlocks_ShouldExtractJson()
        {
            // Arrange
            var llmResponse = @"```json
{
  ""emotion"": ""neutral"",
  ""segments"": [
    {""type"": ""text"", ""content"": ""처음엔 중성이에요""}
  ]
}
```";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Text.Should().Be("처음엔 중성이에요");
            result[0].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void Parse_TextOnly_ShouldReturnSingleSegmentWithNeutralEmotion()
        {
            // Arrange
            var llmResponse = "안녕하세요! 반갑습니다.";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Text.Should().Be("안녕하세요! 반갑습니다.");
            result[0].Emotion.Should().Be("neutral");
            result[0].Order.Should().Be(0);
        }

        [Fact]
        public void Parse_EmptyResponse_ShouldReturnEmptyList()
        {
            // Arrange
            var llmResponse = "";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_WhitespaceOnlyResponse_ShouldReturnEmptyList()
        {
            // Arrange
            var llmResponse = "   \n\t   ";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_OnlyEmotionMarker_ShouldReturnEmptyList()
        {
            // Arrange
            var llmResponse = "<#:happy/>";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_JsonWithEmptyContent_ShouldSkipEmptySegments()
        {
            // Arrange
            var llmResponse = @"{
                ""emotion"": ""happy"",
                ""segments"": [
                    {""type"": ""text"", ""content"": ""유효한 텍스트""},
                    {""type"": ""text"", ""content"": """"},
                    {""type"": ""text"", ""content"": ""다른 유효한 텍스트""}
                ]
            }";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(2);
            result[0].Text.Should().Be("유효한 텍스트");
            result[1].Text.Should().Be("다른 유효한 텍스트");
        }

        [Fact]
        public void Parse_JsonWithMultipleActions_ShouldMaintainOrder()
        {
            // Arrange
            var llmResponse = @"{
                ""emotion"": ""shy"",
                ""segments"": [
                    {""type"": ""action"", ""content"": ""blushing""},
                    {""type"": ""action"", ""content"": ""looking_away""},
                    {""type"": ""text"", ""content"": ""부끄러워요...""}
                ]
            }";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(3);
            result[0].Type.Should().Be(SegmentType.Action);
            result[0].Action.Should().Be("blushing");
            result[0].Order.Should().Be(0);
            
            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Action.Should().Be("looking_away");
            result[1].Order.Should().Be(1);
            
            result[2].Type.Should().Be(SegmentType.Text);
            result[2].Text.Should().Be("부끄러워요...");
            result[2].Order.Should().Be(2);
        }

        [Fact]
        public void Parse_InvalidJson_ShouldReturnFallbackSegment()
        {
            // Arrange
            var llmResponse = "This is not valid JSON at all";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Text.Should().Be("This is not valid JSON at all");
            result[0].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void GetSystemMessage_WithValidContext_ShouldIncludeCharacterInfoAndJsonFormat()
        {
            // Act
            var result = _format.GetSystemMessage(_context);

            // Assert
            result.Should().Contain("TestCharacter");
            result.Should().Contain("Test character description");
            result.Should().Contain("Friendly and helpful");
            result.Should().Contain("Casual");
            result.Should().Contain("Previous conversation memory");
            result.Should().Contain("Current Time:");
            result.Should().Contain("JSON format");
            result.Should().Contain("\"emotion\":");
            result.Should().Contain("\"segments\":");
            result.Should().Contain("neutral, happy, sad, angry, shy, surprised, embarrassed, painful, sleepy");
            result.Should().Contain("blushing, nodding, shaking_head, waving, smiling");
        }

        [Fact]
        public void GetSystemMessage_WithoutMemoryContext_ShouldNotIncludeMemorySection()
        {
            // Arrange
            var character = new CharacterDto
            {
                Id = Guid.NewGuid(),
                Name = "TestCharacter",
                Description = "Test character description",
                Role = "Assistant",
                Personality = "Friendly and helpful",
                SpeechStyle = "Casual",
                VoiceId = "test-voice"
            };

            var command = new ChatRequestCommand(Guid.NewGuid(), character.Id, "Test message", DateTime.Now, true);
            var contextWithoutMemory = new ChatProcessContext(
                command,
                character,
                new List<ProjectVG.Domain.Entities.ConversationHistorys.ConversationHistory>(),
                new List<string>()
            );

            // Act
            var result = _format.GetSystemMessage(contextWithoutMemory);

            // Assert
            result.Should().NotContain("## Relevant Memories");
            result.Should().NotContain("Previous conversation memory");
        }

        [Fact]
        public void GetSystemMessage_WithNullCharacter_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var command = new ChatRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.Now, true);
            var contextWithoutCharacter = new ChatProcessContext(command);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _format.GetSystemMessage(contextWithoutCharacter));
        }

        [Fact]
        public void GetInstructions_ShouldReturnJsonFormatInstructions()
        {
            // Act
            var result = _format.GetInstructions(_context);

            // Assert
            result.Should().Contain("OUTPUT FORMAT SPECIFICATION");
            result.Should().Contain("JSON");
            result.Should().Contain("\"emotion\":");
            result.Should().Contain("\"segments\":");
            result.Should().Contain("\"type\":");
            result.Should().Contain("\"content\":");
            result.Should().Contain("ONLY the JSON");
        }

        [Fact]
        public void CalculateCost_ShouldReturnValidCost()
        {
            // Act
            var cost = _format.CalculateCost(1000, 500);

            // Assert
            cost.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("GPT-4o-mini")]
        public void Model_ShouldReturnExpectedModelName(string expectedModel)
        {
            // Act & Assert
            _format.Model.Should().Be(expectedModel);
        }

        [Fact]
        public void Temperature_ShouldBe1Point2()
        {
            // Act & Assert
            _format.Temperature.Should().Be(1.2f);
        }

        [Fact]
        public void MaxTokens_ShouldBe1000()
        {
            // Act & Assert
            _format.MaxTokens.Should().Be(1000);
        }

        [Fact]
        public void Parse_JsonWithMixedTextAndActions_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = @"{
                ""emotion"": ""excited"",
                ""segments"": [
                    {""type"": ""text"", ""content"": ""정말 기대돼요!""},
                    {""type"": ""action"", ""content"": ""jumping""},
                    {""type"": ""text"", ""content"": ""언제 시작하죠?""},
                    {""type"": ""action"", ""content"": ""clapping""}
                ]
            }";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(4);
            result[0].Type.Should().Be(SegmentType.Text);
            result[1].Type.Should().Be(SegmentType.Action);
            result[2].Type.Should().Be(SegmentType.Text);
            result[3].Type.Should().Be(SegmentType.Action);
            
            // All should have same emotion
            result.Should().AllSatisfy(s => s.Emotion.Should().Be("excited"));
        }

        [Fact]
        public void Parse_JsonWithJustEmotion_ShouldReturnEmptySegments()
        {
            // Arrange
            var llmResponse = @"{""emotion"": ""sad""}";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_JsonWithNestedJsonInContent_ShouldHandleCorrectly()
        {
            // Arrange
            var llmResponse = @"{
                ""emotion"": ""confused"",
                ""segments"": [
                    {""type"": ""text"", ""content"": ""JSON은 {key: value} 형태예요""}
                ]
            }";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Text.Should().Be("JSON은 {key: value} 형태예요");
        }
    }
}