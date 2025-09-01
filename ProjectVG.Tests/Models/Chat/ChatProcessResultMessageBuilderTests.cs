using FluentAssertions;
using ProjectVG.Application.Models.Chat;
using Xunit;

namespace ProjectVG.Tests.Models.Chat
{
    public class ChatProcessResultMessageBuilderTests
    {
        [Fact]
        public void Build_WithDefaultValues_ShouldCreateValidMessage()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.Build();

            message.Type.Should().Be("chat");
            message.MessageType.Should().Be("json");
            message.Text.Should().BeNull();
            message.AudioData.Should().BeNull();
            message.AudioFormat.Should().BeNull();
            message.AudioLength.Should().BeNull();
            message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            message.Metadata.Should().BeNull();
        }

        [Fact]
        public void SetType_ShouldSetTypeCorrectly()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetType("action").Build();

            message.Type.Should().Be("action");
        }

        [Fact]
        public void SetType_WithNullValue_ShouldDefaultToChat()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetType(null!).Build();

            message.Type.Should().Be("chat");
        }

        [Fact]
        public void SetText_ShouldSetTextCorrectly()
        {
            const string expectedText = "Hello, World!";
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetText(expectedText).Build();

            message.Text.Should().Be(expectedText);
        }

        [Fact]
        public void SetAudioData_WithValidBytes_ShouldEncodeToBase64()
        {
            var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
            var expectedBase64 = Convert.ToBase64String(audioBytes);
            
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetAudioData(audioBytes).Build();

            message.AudioData.Should().Be(expectedBase64);
        }

        [Fact]
        public void SetAudioData_WithNullBytes_ShouldSetToNull()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetAudioData(null).Build();

            message.AudioData.Should().BeNull();
        }

        [Fact]
        public void SetAudioData_WithEmptyBytes_ShouldSetToNull()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder.SetAudioData(Array.Empty<byte>()).Build();

            message.AudioData.Should().BeNull();
        }

        [Fact]
        public void AddMetadata_ShouldAddMetadataCorrectly()
        {
            var builder = new ChatProcessResultMessageBuilder();
            var message = builder
                .AddMetadata("emotion", "happy")
                .AddMetadata("order", 1)
                .Build();

            message.Metadata.Should().NotBeNull();
            message.Metadata!["emotion"].Should().Be("happy");
            message.Metadata["order"].Should().Be(1);
        }

        [Fact]
        public void SetMetadata_ShouldReplaceExistingMetadata()
        {
            var initialMetadata = new Dictionary<string, object> { { "key1", "value1" } };
            var newMetadata = new Dictionary<string, object> { { "key2", "value2" } };

            var builder = new ChatProcessResultMessageBuilder();
            var message = builder
                .SetMetadata(initialMetadata)
                .SetMetadata(newMetadata)
                .Build();

            message.Metadata.Should().BeEquivalentTo(newMetadata);
            message.Metadata.Should().NotContainKey("key1");
        }

        [Fact]
        public void FromSegment_WithTextSegment_ShouldCreateCorrectBuilder()
        {
            var segment = ChatSegment.CreateText("Hello World", "happy", 1);
            var builder = ChatProcessResultMessageBuilder.FromSegment(segment);
            var message = builder.Build();

            message.Type.Should().Be("chat");
            message.Text.Should().Be("Hello World");
            message.Metadata!["emotion"].Should().Be("happy");
            message.Metadata["order"].Should().Be(1);
        }

        [Fact]
        public void FromSegment_WithActionSegment_ShouldCreateCorrectBuilder()
        {
            var segment = ChatSegment.CreateAction("*waves hand*", 2);
            var builder = ChatProcessResultMessageBuilder.FromSegment(segment);
            var message = builder.Build();

            message.Type.Should().Be("action");
            message.Text.Should().Be("*waves hand*");
            message.Metadata!["order"].Should().Be(2);
        }

        [Fact]
        public void FromSegment_WithAudioData_ShouldIncludeAudioProperties()
        {
            var audioBytes = new byte[] { 1, 2, 3, 4, 5 };
            var segment = ChatSegment.CreateText("Hello with audio")
                .WithAudioData(audioBytes, "mp3", 5.5f);

            var builder = ChatProcessResultMessageBuilder.FromSegment(segment);
            var message = builder.Build();

            message.AudioData.Should().Be(Convert.ToBase64String(audioBytes));
            message.AudioFormat.Should().Be("mp3");
            message.AudioLength.Should().Be(5.5f);
        }

        [Fact]
        public void CreateFromSegment_ShouldCreateMessageDirectly()
        {
            var segment = ChatSegment.CreateText("Direct creation test", "excited");
            var message = ChatProcessResultMessageBuilder.CreateFromSegment(segment);

            message.Type.Should().Be("chat");
            message.Text.Should().Be("Direct creation test");
            message.Metadata!["emotion"].Should().Be("excited");
        }

        [Fact]
        public void MethodChaining_ShouldAllowFluentInterface()
        {
            var timestamp = DateTime.UtcNow.AddMinutes(-5);
            var audioBytes = new byte[] { 10, 20, 30 };

            var message = new ChatProcessResultMessageBuilder()
                .SetType("action")
                .SetMessageType("custom")
                .SetText("Chained method test")
                .SetAudioData(audioBytes)
                .SetAudioFormat("wav")
                .SetAudioLength(3.2f)
                .SetTimestamp(timestamp)
                .AddMetadata("test", true)
                .Build();

            message.Type.Should().Be("action");
            message.MessageType.Should().Be("custom");
            message.Text.Should().Be("Chained method test");
            message.AudioData.Should().Be(Convert.ToBase64String(audioBytes));
            message.AudioFormat.Should().Be("wav");
            message.AudioLength.Should().Be(3.2f);
            message.Timestamp.Should().Be(timestamp);
            message.Metadata!["test"].Should().Be(true);
        }
    }
}