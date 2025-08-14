using ProjectVG.Domain.Entities.Characters;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Characters
{
    public class InMemoryCharacterRepository : ICharacterRepository
    {
        private readonly Dictionary<Guid, Character> _characters = new();
        private readonly ILogger<InMemoryCharacterRepository> _logger;

        public InMemoryCharacterRepository(ILogger<InMemoryCharacterRepository> logger)
        {
            _logger = logger;
            InitializeDefaultCharacters();
        }

        public Task<IEnumerable<Character>> GetAllAsync()
        {
            return Task.FromResult(_characters.Values.AsEnumerable());
        }

        public Task<Character?> GetByIdAsync(Guid id)
        {
            _characters.TryGetValue(id, out var character);
            return Task.FromResult(character);
        }

        public Task<Character> CreateAsync(Character character)
        {
            character.Id = Guid.NewGuid();
            character.CreatedAt = DateTime.UtcNow;
            character.UpdatedAt = DateTime.UtcNow;
            
            _characters[character.Id] = character;
            _logger.LogInformation("캐릭터를 생성했습니다. 이름: {CharacterName}, ID: {CharacterId}", character.Name, character.Id);
            
            return Task.FromResult(character);
        }

        public Task<Character> UpdateAsync(Character character)
        {
            if (!_characters.ContainsKey(character.Id))
            {
                throw new KeyNotFoundException($"ID {character.Id}인 캐릭터를 찾을 수 없습니다.");
            }

            character.UpdatedAt = DateTime.UtcNow;
            _characters[character.Id] = character;
            _logger.LogInformation("캐릭터를 수정했습니다. 이름: {CharacterName}, ID: {CharacterId}", character.Name, character.Id);
            
            return Task.FromResult(character);
        }

        public Task DeleteAsync(Guid id)
        {
            if (_characters.Remove(id))
            {
                _logger.LogInformation("캐릭터를 삭제했습니다. ID: {CharacterId}", id);
            }
            else
            {
                _logger.LogWarning("ID {CharacterId}인 캐릭터를 삭제하려 했지만 캐릭터를 찾을 수 없습니다", id);
            }
            
            return Task.CompletedTask;
        }

        private void InitializeDefaultCharacters()
        {
            var defaultCharacters = new List<Character>
            {
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "AI 어시스턴트",
                    Description = "일반적인 AI 어시스턴트입니다. 다양한 질문에 답변하고 도움을 드립니다.",
                    Role = "당신은 친근하고 도움이 되는 AI 어시스턴트입니다. 사용자의 질문에 정확하고 유용한 답변을 제공하세요.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "코딩 전문가",
                    Description = "프로그래밍과 소프트웨어 개발에 특화된 AI입니다.",
                    Role = "당신은 경험 많은 소프트웨어 개발자입니다. 다양한 프로그래밍 언어와 기술에 대해 전문적인 조언을 제공하고, 코드 리뷰와 문제 해결을 도와주세요.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "창작 도우미",
                    Description = "창작 활동을 돕는 AI입니다. 글쓰기, 아이디어 발상 등을 지원합니다.",
                    Role = "당신은 창의적이고 영감을 주는 창작 도우미입니다. 글쓰기, 스토리텔링, 아이디어 발상, 창작 활동 전반에 대해 도움을 주세요.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "학습 도우미",
                    Description = "학습과 교육을 돕는 AI입니다. 개념 설명과 학습 방법을 제시합니다.",
                    Role = "당신은 교육 전문가입니다. 복잡한 개념을 쉽게 설명하고, 효과적인 학습 방법을 제시하며, 학생들의 이해를 돕는 역할을 합니다.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            foreach (var character in defaultCharacters)
            {
                _characters[character.Id] = character;
            }

            _logger.LogInformation("기본 캐릭터 {Count}개를 초기화했습니다", defaultCharacters.Count);
        }
    }
} 