using ProjectVG.Domain.Entities.Characters;

namespace ProjectVG.Domain.Repositories
{
    public interface ICharacterRepository
    {
        Task<IEnumerable<Character>> GetAllAsync();
        Task<Character?> GetByIdAsync(Guid id);
        Task<Character> CreateAsync(Character character);
        Task<Character> UpdateAsync(Character character);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<Character>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Character>> GetPublicCharactersAsync();
    }
}