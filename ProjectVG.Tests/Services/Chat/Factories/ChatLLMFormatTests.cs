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
            
            var characterEntity = new ProjectVG.Domain.Entities.Characters.Character
            {
                Id = Guid.NewGuid(),
                Name = "TestCharacter",
                Description = "Test character description",
                Role = "Assistant",
                Personality = "Friendly and helpful",
                SpeechStyle = "Casual",
                Summary = "Character summary for testing",
                VoiceId = "test-voice",
                IsActive = true,
                UserAlias = "TestUser"
            };
            
            var character = new CharacterDto(characterEntity);

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
        public void Parse_UserSpecificationExample_ShouldParseCorrectly()
        {
            // Arrange - Example from user specification
            var llmResponse = "[emotion:confused](action:blushing)\"바, 바보야! 그렇게 말하지 말라고...!\"[emotion:shy](action:looking_away)\"그렇게 말하면 부, 부끄럽잖아...\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(4);
            
            result[0].Type.Should().Be(SegmentType.Action);
            result[0].Content.Should().Be("blushing");
            result[0].Order.Should().Be(0);

            result[1].Type.Should().Be(SegmentType.Text);
            result[1].Content.Should().Be("바, 바보야! 그렇게 말하지 말라고...!");
            result[1].Emotion.Should().Be("confused");
            result[1].Order.Should().Be(1);

            result[2].Type.Should().Be(SegmentType.Action);
            result[2].Content.Should().Be("looking_away");
            result[2].Order.Should().Be(2);

            result[3].Type.Should().Be(SegmentType.Text);
            result[3].Content.Should().Be("그렇게 말하면 부, 부끄럽잖아...");
            result[3].Emotion.Should().Be("shy");
            result[3].Order.Should().Be(3);
        }

        [Fact]
        public void Parse_SimpleEmotionAndText_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:happy]\"안녕하세요! 반가워요!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("안녕하세요! 반가워요!");
            result[0].Emotion.Should().Be("happy");
            result[0].Order.Should().Be(0);
        }

        [Fact]
        public void Parse_OnlyAction_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "(action:waving)";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Action);
            result[0].Content.Should().Be("waving");
            result[0].Order.Should().Be(0);
        }

        [Fact]
        public void Parse_MultipleTextSegmentsWithSameEmotion_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:excited]\"첫 번째 메시지\"\"두 번째 메시지\"\"세 번째 메시지\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(3);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("첫 번째 메시지");
            result[0].Emotion.Should().Be("excited");
            result[0].Order.Should().Be(0);

            result[1].Type.Should().Be(SegmentType.Text);
            result[1].Content.Should().Be("두 번째 메시지");
            result[1].Emotion.Should().Be("excited");
            result[1].Order.Should().Be(1);

            result[2].Type.Should().Be(SegmentType.Text);
            result[2].Content.Should().Be("세 번째 메시지");
            result[2].Emotion.Should().Be("excited");
            result[2].Order.Should().Be(2);
        }

        [Fact]
        public void Parse_MultipleActionsInSequence_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "(action:nodding)(action:smiling)(action:waving)";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(3);
            result[0].Type.Should().Be(SegmentType.Action);
            result[0].Content.Should().Be("nodding");
            result[0].Order.Should().Be(0);

            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Content.Should().Be("smiling");
            result[1].Order.Should().Be(1);

            result[2].Type.Should().Be(SegmentType.Action);
            result[2].Content.Should().Be("waving");
            result[2].Order.Should().Be(2);
        }

        [Fact]
        public void Parse_ComplexSequenceWithMultipleEmotionChanges_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:happy]\"좋은 아침이에요!\"(action:stretching)[emotion:surprised]\"어? 벌써 이 시간이네요!\"(action:looking_at_clock)[emotion:neutral]\"일어나야겠어요.\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(5);
            
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("좋은 아침이에요!");
            result[0].Emotion.Should().Be("happy");

            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Content.Should().Be("stretching");

            result[2].Type.Should().Be(SegmentType.Text);
            result[2].Content.Should().Be("어? 벌써 이 시간이네요!");
            result[2].Emotion.Should().Be("surprised");

            result[3].Type.Should().Be(SegmentType.Action);
            result[3].Content.Should().Be("looking_at_clock");

            result[4].Type.Should().Be(SegmentType.Text);
            result[4].Content.Should().Be("일어나야겠어요.");
            result[4].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void Parse_EmotionWithoutTextOrAction_ShouldNotCreateSegment()
        {
            // Arrange
            var llmResponse = "[emotion:happy][emotion:sad][emotion:angry]";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            // Only emotion markers without text or actions should result in empty list
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_TextWithoutEmotion_ShouldUseDefaultNeutralEmotion()
        {
            // Arrange
            var llmResponse = "\"안녕하세요!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("안녕하세요!");
            result[0].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void Parse_EmptyQuotes_ShouldNotCreateSegments()
        {
            // Arrange
            var llmResponse = "[emotion:happy]\"\"(action:waving)\"\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            // Empty quotes should be ignored, but action should be parsed
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Action);
            result[0].Content.Should().Be("waving");
        }

        [Fact]
        public void Parse_MalformedEmotion_ShouldIgnoreAndContinueParsing()
        {
            // Arrange
            var llmResponse = "[emotion:happy\"안녕하세요!\"(action:waving)";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(2);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("안녕하세요!");
            result[0].Emotion.Should().Be("neutral"); // Default emotion

            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Content.Should().Be("waving");
        }

        [Fact]
        public void Parse_MalformedAction_ShouldIgnoreAndContinueParsing()
        {
            // Arrange
            var llmResponse = "(action:waving\"안녕하세요!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("안녕하세요!");
            result[0].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void Parse_EmptyString_ShouldReturnEmptyList()
        {
            // Arrange
            var llmResponse = "";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_WhitespaceOnly_ShouldReturnEmptyList()
        {
            // Arrange
            var llmResponse = "   \n\t   ";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_InvalidInput_ShouldReturnFallbackSegment()
        {
            // Arrange
            var llmResponse = "This is completely invalid input with no patterns";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("This is completely invalid input with no patterns");
            result[0].Emotion.Should().Be("neutral");
        }

        [Fact]
        public void Parse_TextWithSpecialCharacters_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:confused]\"어? 이건... (정말?) 뭔가 이상해!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("어? 이건... (정말?) 뭔가 이상해!");
            result[0].Emotion.Should().Be("confused");
        }

        [Fact]
        public void Parse_ActionsWithUnderscores_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "(action:looking_away)(action:tilting_head)";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(2);
            result[0].Content.Should().Be("looking_away");
            result[1].Content.Should().Be("tilting_head");
        }

        [Fact]
        public void Parse_EmotionsWithUnderscores_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:very_happy]\"정말 기뻐요!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("정말 기뻐요!");
            result[0].Emotion.Should().Be("very_happy");
        }

        [Fact]
        public void Parse_LongComplexDialogue_ShouldParseCorrectly()
        {
            // Arrange
            var llmResponse = "[emotion:excited]\"오늘 날씨가 정말 좋네요!\"(action:looking_out_window)\"밖에 나가서 산책이라도 하고 싶어져요.\"(action:stretching)[emotion:thoughtful]\"음... 그런데 할 일이 있었던 것 같은데...\"(action:scratching_head)[emotion:determined]\"아! 맞다! 오늘은 친구를 만나기로 했었죠!\"";

            // Act
            var result = _format.Parse(llmResponse, _context);

            // Assert
            result.Should().HaveCount(7);

            result[0].Type.Should().Be(SegmentType.Text);
            result[0].Content.Should().Be("오늘 날씨가 정말 좋네요!");
            result[0].Emotion.Should().Be("excited");

            result[1].Type.Should().Be(SegmentType.Action);
            result[1].Content.Should().Be("looking_out_window");

            result[2].Type.Should().Be(SegmentType.Text);
            result[2].Content.Should().Be("밖에 나가서 산책이라도 하고 싶어져요.");
            result[2].Emotion.Should().Be("excited");

            result[3].Type.Should().Be(SegmentType.Action);
            result[3].Content.Should().Be("stretching");

            result[4].Type.Should().Be(SegmentType.Text);
            result[4].Content.Should().Be("음... 그런데 할 일이 있었던 것 같은데...");
            result[4].Emotion.Should().Be("thoughtful");

            result[5].Type.Should().Be(SegmentType.Action);
            result[5].Content.Should().Be("scratching_head");

            result[6].Type.Should().Be(SegmentType.Text);
            result[6].Content.Should().Be("아! 맞다! 오늘은 친구를 만나기로 했었죠!");
            result[6].Emotion.Should().Be("determined");
        }

        // System Message and Instructions tests
        [Fact]
        public void GetSystemMessage_WithValidContext_ShouldIncludeAllSections()
        {
            // Act
            var result = _format.GetSystemMessage(_context);

            // Assert
            result.Should().Contain("TestCharacter");
            result.Should().Contain("Test character description");
            result.Should().Contain("Friendly and helpful");
            result.Should().Contain("Casual");
            result.Should().Contain("Character summary for testing");
            result.Should().Contain("Previous conversation memory");
            result.Should().Contain("Current Time:");
            result.Should().Contain("Character Information");
            result.Should().Contain("Speech Style and Examples");
            result.Should().Contain("Relevant Memory Information");
            result.Should().Contain("Current Context Information");
            result.Should().Contain("Dialogue Constraints and Requirements");
        }

        [Fact]
        public void GetInstructions_ShouldReturnCorrectFormat()
        {
            // Act
            var result = _format.GetInstructions(_context);

            // Assert
            result.Should().Contain("MANDATORY OUTPUT FORMAT SPECIFICATION");
            result.Should().Contain("[emotion:emotion_name]");
            result.Should().Contain("(action:action_name)");
            result.Should().Contain("dialogue");
            result.Should().Contain("neutral, happy, sad, angry, shy, surprised, embarrassed, sleepy, confused, proud");
            result.Should().Contain("blushing, nodding, shaking_head, waving, smiling");
        }

        [Fact]
        public void GetSystemMessage_WithNullCharacter_ShouldThrowException()
        {
            // Arrange
            var command = new ChatRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.Now, true);
            var contextWithoutCharacter = new ChatProcessContext(command);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _format.GetSystemMessage(contextWithoutCharacter));
        }

        // Property tests
        [Fact]
        public void Model_ShouldReturnGPT4oMini()
        {
            // Act & Assert
            _format.Model.Should().Be("gpt-4o-mini");
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
        public void CalculateCost_WithValidTokens_ShouldReturnPositiveCost()
        {
            // Act
            var cost = _format.CalculateCost(1000, 500);

            // Assert
            cost.Should().BeGreaterThan(0);
        }
    }
}