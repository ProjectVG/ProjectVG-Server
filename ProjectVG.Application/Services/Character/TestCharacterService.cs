using ProjectVG.Application.Models.Character;
using System.Collections.Concurrent;

namespace ProjectVG.Application.Services.Character
{
    /// <summary>
    /// 테스트/개발용 인메모리 캐릭터 서비스
    /// </summary>
    public class TestCharacterService : ICharacterService
    {
        private readonly ConcurrentDictionary<Guid, CharacterDto> _characters = new();

        public TestCharacterService()
        {
            // AppConstants의 캐릭터 풀을 CharacterDto로 변환하여 미리 등록
            foreach (var profile in AppConstants.DefaultCharacterPool)
            {
                var dto = new CharacterDto
                {
                    Id = profile.Id,
                    Name = profile.Name,
                    Description = profile.Description,
                    Role = profile.Role,
                    Personality = profile.Personality,
                    SpeechStyle = profile.SpeechStyle,
                    IsActive = profile.IsActive,
                    VoiceId = profile.VoiceId
                };
                _characters[dto.Id] = dto;
            }
        }

        public Task<IEnumerable<CharacterDto>> GetAllCharactersAsync()
        {
            return Task.FromResult(_characters.Values.AsEnumerable());
        }

        public Task<CharacterDto?> GetCharacterByIdAsync(Guid id)
        {
            _characters.TryGetValue(id, out var character);
            return Task.FromResult(character);
        }

        public Task<CharacterDto?> GetCharacterByNameAsync(string name)
        {
            var character = _characters.Values.FirstOrDefault(c => c.Name == name);
            return Task.FromResult(character);
        }

        public Task<CharacterDto> CreateCharacterAsync(CreateCharacterCommand command)
        {
            var character = new CharacterDto
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Description = command.Description,
                Role = command.Role,
                IsActive = command.IsActive,
                Personality = string.Empty,
                SpeechStyle = string.Empty,
                VoiceId = string.Empty
            };
            _characters[character.Id] = character;
            return Task.FromResult(character);
        }

        public Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterCommand command)
        {
            if (!_characters.TryGetValue(id, out var existing))
                throw new KeyNotFoundException($"캐릭터를 찾을 수 없음: ID {id}");
            existing.Name = command.Name;
            existing.Description = command.Description;
            existing.Role = command.Role;
            existing.IsActive = command.IsActive;
            // 테스트용: 기본값 유지
            _characters[id] = existing;
            return Task.FromResult(existing);
        }

        public Task DeleteCharacterAsync(Guid id)
        {
            _characters.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> CharacterExistsAsync(Guid id)
        {
            var exists = _characters.ContainsKey(id);
            return Task.FromResult(exists);
        }
    }
} 