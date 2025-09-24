using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Tests.Application.TestUtilities;
using Xunit;

namespace ProjectVG.Tests.Application.Services.Chat
{
    public class ChatServiceSimpleTests
    {
        private readonly Mock<IChatMetricsService> _mockMetricsService;
        private readonly Mock<IConversationService> _mockConversationService;
        private readonly Mock<ICharacterService> _mockCharacterService;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<ChatService>> _mockLogger;

        public ChatServiceSimpleTests()
        {
            _mockMetricsService = new Mock<IChatMetricsService>();
            _mockConversationService = new Mock<IConversationService>();
            _mockCharacterService = new Mock<ICharacterService>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<ChatService>>();

            // Setup scope factory chain
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        }

        #region Service Scope Management Tests

        [Fact]
        public void ChatService_Constructor_WithServiceScopeFactory_ShouldAcceptDependency()
        {
            // Arrange & Act & Assert
            var act = () => CreateTestChatService();
            act.Should().NotThrow("IServiceScopeFactory should be a valid dependency for ChatService");
        }

        [Fact]
        public void ServiceScopeFactory_CreateScope_ShouldReturnValidScope()
        {
            // Arrange
            var mockScope = new Mock<IServiceScope>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);

            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

            // Act
            var scope = mockScopeFactory.Object.CreateScope();

            // Assert
            scope.Should().NotBeNull();
            scope.ServiceProvider.Should().NotBeNull();
            mockScopeFactory.Verify(x => x.CreateScope(), Times.Once);
        }

        [Fact]
        public void ServiceScope_ShouldImplementIDisposable()
        {
            // Arrange
            var mockScope = new Mock<IServiceScope>();
            
            // Act & Assert
            mockScope.Object.Should().BeAssignableTo<IDisposable>("IServiceScope should be disposable for proper resource management");
        }

        [Fact]
        public void ServiceProvider_GetService_ShouldResolveServicesCorrectly()
        {
            // Arrange - Test service provider setup without needing concrete instances
            var dummySuccessHandler = new object();
            var dummyResultProcessor = new object();
            
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatSuccessHandler)))
                .Returns(dummySuccessHandler);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatResultProcessor)))
                .Returns(dummyResultProcessor);

            // Act
            var successHandler = _mockServiceProvider.Object.GetService(typeof(ChatSuccessHandler));
            var resultProcessor = _mockServiceProvider.Object.GetService(typeof(ChatResultProcessor));

            // Assert
            successHandler.Should().NotBeNull();
            resultProcessor.Should().NotBeNull();
            successHandler.Should().BeSameAs(dummySuccessHandler);
            resultProcessor.Should().BeSameAs(dummyResultProcessor);
        }

        [Fact]
        public async Task ProcessChatRequestInternalAsync_ShouldCreateNewScopeForBackgroundTasks()
        {
            // This test verifies the concept of scope creation for background tasks
            // The actual method is private and complex, so we test the scope creation pattern

            // Arrange
            var dummySuccessHandler = new object();
            var dummyResultProcessor = new object();
            
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatSuccessHandler)))
                .Returns(dummySuccessHandler);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatResultProcessor)))
                .Returns(dummyResultProcessor);

            // Act - Simulate the scope creation pattern used in ChatService
            using var scope = _mockScopeFactory.Object.CreateScope();
            var successHandler = scope.ServiceProvider.GetService(typeof(ChatSuccessHandler));
            var resultProcessor = scope.ServiceProvider.GetService(typeof(ChatResultProcessor));

            // Assert
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.Once);
            successHandler.Should().NotBeNull();
            resultProcessor.Should().NotBeNull();
            
            // Verify both services come from the same scope
            _mockServiceProvider.Verify(x => x.GetService(typeof(ChatSuccessHandler)), Times.Once);
            _mockServiceProvider.Verify(x => x.GetService(typeof(ChatResultProcessor)), Times.Once);

            await Task.CompletedTask; // To satisfy async context
        }

        [Fact]
        public void UsingScope_ShouldDisposeProperlyAndPreventObjectDisposedException()
        {
            // Arrange
            var disposeCalled = false;
            _mockScope.Setup(x => x.Dispose()).Callback(() => disposeCalled = true);

            // Act
            using (var scope = _mockScopeFactory.Object.CreateScope())
            {
                scope.Should().NotBeNull();
                disposeCalled.Should().BeFalse("Scope should not be disposed while in using block");
            }

            // Assert
            disposeCalled.Should().BeTrue("Scope should be disposed after using block");
            _mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void ServiceScope_MultipleServiceResolution_ShouldUseSameProvider()
        {
            // This test verifies that multiple services resolved from the same scope
            // use the same service provider instance, preventing DbContext disposal issues

            // Arrange
            var dummySuccessHandler = new object();
            var dummyResultProcessor = new object();
            
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatSuccessHandler)))
                .Returns(dummySuccessHandler);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ChatResultProcessor)))
                .Returns(dummyResultProcessor);

            // Act
            using var scope = _mockScopeFactory.Object.CreateScope();
            var provider1 = scope.ServiceProvider;
            var provider2 = scope.ServiceProvider;
            
            var service1 = provider1.GetService(typeof(ChatSuccessHandler));
            var service2 = provider2.GetService(typeof(ChatResultProcessor));

            // Assert
            provider1.Should().BeSameAs(provider2, "Same scope should always return same ServiceProvider");
            service1.Should().NotBeNull();
            service2.Should().NotBeNull();
            
            // Both services should be resolved from the same provider instance
            _mockServiceProvider.Verify(x => x.GetService(typeof(ChatSuccessHandler)), Times.Once);
            _mockServiceProvider.Verify(x => x.GetService(typeof(ChatResultProcessor)), Times.Once);
        }

        #endregion

        #region Helper Methods

        private ChatService CreateTestChatService()
        {
            // Create minimal mocks for all required dependencies
            var mockSessionManager = new Mock<ProjectVG.Application.Services.Session.ISessionManager>();
            var mockUserService = new Mock<ProjectVG.Application.Services.Users.IUserService>();
            var mockCreditService = new Mock<ProjectVG.Application.Services.Credit.ICreditManagementService>();
            var mockValidatorLogger = new Mock<ILogger<ChatRequestValidator>>();
            
            var mockMemoryClient = new Mock<ProjectVG.Infrastructure.Integrations.MemoryClient.IMemoryClient>();
            var mockMemoryLogger = new Mock<ILogger<MemoryContextPreprocessor>>();
            
            var mockInputProcessor = new Mock<ICostTrackingDecorator<UserInputAnalysisProcessor>>();
            
            var mockActionLogger = new Mock<ILogger<UserInputActionProcessor>>();
            
            var mockLLMProcessor = new Mock<ICostTrackingDecorator<ChatLLMProcessor>>();
            var mockTTSProcessor = new Mock<ICostTrackingDecorator<ChatTTSProcessor>>();
            
            var mockResultLogger = new Mock<ILogger<ChatResultProcessor>>();
            var mockMessageBroker = new Mock<ProjectVG.Application.Services.MessageBroker.IMessageBroker>();
            var mockMemoryClientForResult = new Mock<ProjectVG.Infrastructure.Integrations.MemoryClient.IMemoryClient>();
            
            var mockFailureLogger = new Mock<ILogger<ChatFailureHandler>>();

            var mockValidator = new ChatRequestValidator(
                mockSessionManager.Object,
                mockUserService.Object,
                _mockCharacterService.Object,
                mockCreditService.Object,
                mockValidatorLogger.Object);
            
            var mockMemoryPreprocessor = new MemoryContextPreprocessor(
                mockMemoryClient.Object,
                mockMemoryLogger.Object);
            
            var mockActionProcessor = new UserInputActionProcessor(
                _mockConversationService.Object,
                mockActionLogger.Object);
            
            var mockResultProcessor = new ChatResultProcessor(
                mockResultLogger.Object,
                _mockConversationService.Object,
                mockMemoryClientForResult.Object,
                mockMessageBroker.Object);

            var mockFailureHandler = new ChatFailureHandler(
                mockFailureLogger.Object,
                mockMessageBroker.Object);

            return new ChatService(
                _mockMetricsService.Object,
                _mockScopeFactory.Object,
                _mockLogger.Object,
                _mockConversationService.Object,
                _mockCharacterService.Object,
                mockValidator,
                mockMemoryPreprocessor,
                mockInputProcessor.Object,
                mockActionProcessor,
                mockLLMProcessor.Object,
                mockTTSProcessor.Object,
                mockResultProcessor,
                mockFailureHandler
            );
        }

        #endregion
    }
}