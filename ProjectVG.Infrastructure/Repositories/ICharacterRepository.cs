using ProjectVG.Domain.Entities.Character;

namespace ProjectVG.Infrastructure.Repositories
{
    public interface ICharacterRepository
    {
        Task<IEnumerable<Character>> GetAllAsync();
        Task<Character?> GetByIdAsync(Guid id);
        Task<Character> CreateAsync(Character character);
        Task<Character> UpdateAsync(Character character);
        Task DeleteAsync(Guid id);
    }
} 