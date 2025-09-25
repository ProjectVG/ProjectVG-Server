using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.MessageBroker;
using Xunit;

namespace ProjectVG.Tests.Services.Chat.Handlers
{
    public class ChatSuccessHandlerTests
    {
        private readonly Mock<ILogger<ChatSuccessHandler>> _mockLogger;
        private readonly Mock<IMessageBroker> _mockMessageBroker;
        private readonly Mock<ICreditManagementService> _mockCreditManagementService;
        private readonly ChatSuccessHandler _handler;

        public ChatSuccessHandlerTests()
        {
            _mockLogger = new Mock<ILogger<ChatSuccessHandler>>();
            _mockMessageBroker = new Mock<IMessageBroker>();
            _mockCreditManagementService = new Mock<ICreditManagementService>();
            _handler = new ChatSuccessHandler(_mockLogger.Object, _mockMessageBroker.Object, _mockCreditManagementService.Object);
        }

        [Fact]
        public async Task HandleAsync_WithEmptySegments_ShouldLogWarningAndReturn()
        {
            var context = CreateTestContext();
            context.SetResponse("", new List<ChatSegment>(), 0.0);

            await _handler.HandleAsync(context);

            VerifyWarningLogged("채팅 처리 결과에 유효한 세그먼트가 없습니다");
            _mockMessageBroker.Verify(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithValidSegments_ShouldSendAllMessages()
        {
            var context = CreateTestContext();
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText("Hello", order: 0),
                ChatSegment.CreateAction("*waves*", order: 1),
                ChatSegment.CreateText("How are you?", order: 2)
            };
            context.SetResponse("Hello", segments, 0.0);

            await _handler.HandleAsync(context);

            _mockMessageBroker.Verify(
                x => x.SendToUserAsync(context.UserId.ToString(), It.IsAny<object>()),
                Times.Exactly(3));

            VerifyDebugLogged("채팅 결과 전송 완료");
        }

        [Fact]
        public async Task HandleAsync_WithMixedValidAndEmptySegments_ShouldOnlySendValidOnes()
        {
            var context = CreateTestContext();
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText("Valid text", order: 0),
                ChatSegment.CreateText("", order: 1), // Empty segment
                ChatSegment.CreateAction("Valid action", order: 2)
            };
            context.SetResponse("Test", segments, 0.0);

            await _handler.HandleAsync(context);

            _mockMessageBroker.Verify(
                x => x.SendToUserAsync(context.UserId.ToString(), It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_WithAudioSegment_ShouldIncludeAudioData()
        {
            var context = CreateTestContext();
            var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
            var segment = ChatSegment.CreateText("Audio message")
                .WithAudioData(audioBytes, "wav", 2.5f);
            context.SetResponse("Test", new List<ChatSegment> { segment }, 0.0);

            WebSocketMessage? sentMessage = null;
            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, message) => sentMessage = message as WebSocketMessage);

            await _handler.HandleAsync(context);

            sentMessage.Should().NotBeNull();
            var resultMessage = sentMessage!.Data as ChatProcessResultMessage;
            resultMessage.Should().NotBeNull();
            resultMessage!.AudioData.Should().Be(Convert.ToBase64String(audioBytes));
            resultMessage.AudioFormat.Should().Be("wav");
            resultMessage.AudioLength.Should().Be(2.5f);
        }

        [Fact]
        public async Task HandleAsync_WithWebSocketFailure_ShouldThrowImmediately()
        {
            var context = CreateTestContext();
            var segment = ChatSegment.CreateText("Test message");
            context.SetResponse("Test", new List<ChatSegment> { segment }, 0.0);

            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Connection failed"));

            var act = async () => await _handler.HandleAsync(context);

            await act.Should().ThrowAsync<Exception>().WithMessage("Connection failed");

            _mockMessageBroker.Verify(
                x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()),
                Times.Once);

            VerifyErrorLogged("채팅 결과 전송 중 오류 발생");
        }

        [Fact]
        public async Task HandleAsync_WithCorrectOrder_ShouldSendInOrderedSequence()
        {
            var context = CreateTestContext();
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText("Third", order: 2),
                ChatSegment.CreateText("First", order: 0),
                ChatSegment.CreateText("Second", order: 1)
            };
            context.SetResponse("Test", segments, 0.0);

            var sentMessages = new List<WebSocketMessage>();
            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, message) =>
                {
                    if (message is WebSocketMessage wsMessage)
                        sentMessages.Add(wsMessage);
                });

            await _handler.HandleAsync(context);

            sentMessages.Should().HaveCount(3);
            var resultMessages = sentMessages.Select(m => m.Data as ChatProcessResultMessage).ToList();
            
            resultMessages[0]!.Text.Should().Be("First");
            resultMessages[1]!.Text.Should().Be("Second");
            resultMessages[2]!.Text.Should().Be("Third");
        }

        [Fact]
        public async Task HandleAsync_WithDifferentSegmentTypes_ShouldAllUseChatType()
        {
            var context = CreateTestContext();
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText("Chat message"),
                ChatSegment.CreateAction("Action message")
            };
            context.SetResponse("Test", segments, 0.0);

            var sentMessages = new List<WebSocketMessage>();
            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, message) =>
                {
                    if (message is WebSocketMessage wsMessage)
                        sentMessages.Add(wsMessage);
                });

            await _handler.HandleAsync(context);

            var textMessage = sentMessages[0].Data as ChatProcessResultMessage;
            var actionMessage = sentMessages[1].Data as ChatProcessResultMessage;

            textMessage!.Type.Should().Be("chat");
            actionMessage!.Type.Should().Be("chat");
            
            // Actions should be included in the Actions field
            actionMessage.Actions.Should().NotBeNull().And.Contain("Action message");
        }

        [Fact]
        public async Task HandleAsync_ShouldIncludeRequestIdInMessages()
        {
            var context = CreateTestContext();
            var segment = ChatSegment.CreateText("Test message with request ID");
            context.SetResponse("Test", new List<ChatSegment> { segment }, 0.0);

            WebSocketMessage? sentMessage = null;
            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, message) => sentMessage = message as WebSocketMessage);

            await _handler.HandleAsync(context);

            sentMessage.Should().NotBeNull();
            var resultMessage = sentMessage!.Data as ChatProcessResultMessage;
            resultMessage.Should().NotBeNull();
            resultMessage!.RequestId.Should().Be(context.RequestId.ToString());
        }

        [Fact]
        public async Task HandleAsync_ShouldUseConsistentWebSocketMessageType()
        {
            var context = CreateTestContext();
            var segments = new List<ChatSegment>
            {
                ChatSegment.CreateText("Text message"),
                ChatSegment.CreateAction("Action message")
            };
            context.SetResponse("Test", segments, 0.0);

            var sentMessages = new List<WebSocketMessage>();
            _mockMessageBroker.Setup(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, message) =>
                {
                    if (message is WebSocketMessage wsMessage)
                        sentMessages.Add(wsMessage);
                });

            await _handler.HandleAsync(context);

            sentMessages.Should().HaveCount(2);
            sentMessages.Should().AllSatisfy(msg => msg.Type.Should().Be("chat"));
        }

        private static ChatProcessContext CreateTestContext()
        {
            var command = new ChatRequestCommand(
                userId: Guid.NewGuid(),
                characterId: Guid.NewGuid(),
                userPrompt: "Test prompt",
                requestedAt: DateTime.UtcNow,
                useTTS: false
            );

            return new ChatProcessContext(command);
        }

        private void VerifyWarningLogged(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyDebugLogged(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyErrorLogged(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}