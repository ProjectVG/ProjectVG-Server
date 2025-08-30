using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Tests.Application.TestUtilities
{
    public static class TestDataBuilder
    {
        #region Character Test Data

        public static ProjectVG.Domain.Entities.Characters.Character CreateCharacterEntity(
            string name = "TestCharacter",
            Guid? id = null,
            string description = "Test character description",
            string role = "Assistant",
            bool isActive = true,
            string personality = "Friendly and helpful",
            string speechStyle = "Casual",
            string summary = "Test character summary",
            string userAlias = "User",
            string voiceId = "test-voice")
        {
            return new ProjectVG.Domain.Entities.Characters.Character
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = description,
                Role = role,
                IsActive = isActive,
                Personality = personality,
                SpeechStyle = speechStyle,
                Summary = summary,
                UserAlias = userAlias,
                VoiceId = voiceId
            };
        }

        public static CharacterDto CreateCharacterDto(
            string name = "TestCharacter",
            Guid? id = null,
            string description = "Test character description",
            string role = "Assistant",
            bool isActive = true,
            string personality = "Friendly and helpful",
            string speechStyle = "Casual",
            string summary = "Test character summary",
            string userAlias = "User",
            string voiceId = "test-voice")
        {
            var entity = CreateCharacterEntity(name, id, description, role, isActive, personality, speechStyle, summary, userAlias, voiceId);
            return new CharacterDto(entity);
        }

        public static CreateCharacterCommand CreateCreateCharacterCommand(
            string name = "TestCharacter",
            string description = "Test character description",
            string role = "Assistant",
            bool isActive = true)
        {
            return new CreateCharacterCommand
            {
                Name = name,
                Description = description,
                Role = role,
                IsActive = isActive
            };
        }

        public static UpdateCharacterCommand CreateUpdateCharacterCommand(
            string name = "UpdatedCharacter",
            string description = "Updated character description",
            string role = "Updated role",
            bool isActive = true)
        {
            return new UpdateCharacterCommand
            {
                Name = name,
                Description = description,
                Role = role,
                IsActive = isActive
            };
        }

        #endregion

        #region User Test Data

        public static ProjectVG.Domain.Entities.Users.User CreateUserEntity(
            string username = "testuser",
            string email = "test@example.com",
            Guid? id = null,
            string uid = "TEST123",
            string provider = "test",
            string providerId = "test123",
            AccountStatus status = AccountStatus.Active)
        {
            return new ProjectVG.Domain.Entities.Users.User
            {
                Id = id ?? Guid.NewGuid(),
                UID = uid,
                Username = username,
                Email = email,
                Provider = provider,
                ProviderId = providerId,
                Status = status
            };
        }

        public static UserDto CreateUserDto(
            string username = "testuser",
            string email = "test@example.com",
            Guid? id = null,
            string uid = "TEST123",
            string provider = "test",
            string providerId = "test123",
            AccountStatus status = AccountStatus.Active)
        {
            var entity = CreateUserEntity(username, email, id, uid, provider, providerId, status);
            return new UserDto(entity);
        }

        public static UserCreateCommand CreateUserCreateCommand(
            string username = "testuser",
            string email = "test@example.com",
            string providerId = "provider123",
            string provider = "google")
        {
            return new UserCreateCommand(username, email, providerId, provider);
        }

        #endregion

        #region Conversation Test Data

        public static ConversationHistory CreateConversationHistory(
            Guid? userId = null,
            Guid? characterId = null,
            ChatRole role = ChatRole.User,
            string content = "Test message",
            Guid? id = null,
            DateTime? timestamp = null)
        {
            return new ConversationHistory
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                CharacterId = characterId ?? Guid.NewGuid(),
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Timestamp = timestamp ?? DateTime.UtcNow,
                MetadataJson = "{}",
                IsDeleted = false
            };
        }

        public static List<ConversationHistory> CreateConversationHistoryList(
            Guid? userId = null,
            Guid? characterId = null,
            int count = 5)
        {
            var actualUserId = userId ?? Guid.NewGuid();
            var actualCharacterId = characterId ?? Guid.NewGuid();
            
            var histories = new List<ConversationHistory>();
            
            for (int i = 0; i < count; i++)
            {
                var role = i % 2 == 0 ? ChatRole.User : ChatRole.Assistant;
                var content = role == ChatRole.User ? $"User message {i + 1}" : $"Assistant response {i + 1}";
                
                histories.Add(CreateConversationHistory(
                    actualUserId, 
                    actualCharacterId, 
                    role, 
                    content,
                    timestamp: DateTime.UtcNow.AddMinutes(-count + i)
                ));
            }
            
            return histories;
        }

        #endregion

        #region Chat Test Data

        public static ChatRequestCommand CreateChatRequestCommand(
            Guid? userId = null,
            Guid? characterId = null,
            string userPrompt = "Hello, how are you?",
            bool useTTS = true,
            DateTime? requestedAt = null)
        {
            return new ChatRequestCommand(
                userId ?? Guid.NewGuid(),
                characterId ?? Guid.NewGuid(),
                userPrompt,
                requestedAt ?? DateTime.UtcNow,
                useTTS
            );
        }

        public static ChatProcessContext CreateChatProcessContext(
            ChatRequestCommand? command = null,
            CharacterDto? character = null,
            List<ConversationHistory>? conversationHistory = null,
            List<string>? memoryContext = null)
        {
            var actualCommand = command ?? CreateChatRequestCommand();
            var actualCharacter = character ?? CreateCharacterDto();
            var actualHistory = conversationHistory ?? CreateConversationHistoryList(actualCommand.UserId, actualCommand.CharacterId);
            var actualMemory = memoryContext ?? new List<string> { "Previous context 1", "Previous context 2" };

            return new ChatProcessContext(
                actualCommand,
                actualCharacter,
                actualHistory,
                actualMemory
            );
        }

        public static ChatRequestResult CreateChatRequestResultAccepted(
            string sessionId = "test-session",
            Guid? userId = null,
            Guid? characterId = null)
        {
            return ChatRequestResult.Accepted(
                sessionId,
                userId ?? Guid.NewGuid(),
                characterId ?? Guid.NewGuid()
            );
        }

        public static ChatRequestResult CreateChatRequestResultRejected(
            string message = "Test rejection",
            string errorCode = "TEST_ERROR",
            string sessionId = "test-session",
            Guid? userId = null,
            Guid? characterId = null)
        {
            return ChatRequestResult.Rejected(
                message,
                errorCode,
                sessionId,
                userId ?? Guid.NewGuid(),
                characterId ?? Guid.NewGuid()
            );
        }

        #endregion

        #region Helper Methods

        public static T CreateWithCustomId<T>(Func<Guid, T> factory)
        {
            return factory(Guid.NewGuid());
        }

        public static List<T> CreateList<T>(Func<T> factory, int count)
        {
            var list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                list.Add(factory());
            }
            return list;
        }

        public static async Task<T> CreateAsync<T>(Func<Task<T>> factory)
        {
            return await factory();
        }

        #endregion
    }
}