using MainAPI_Server.Models.Domain.Characters;

namespace MainAPI_Server.Services.Characters
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