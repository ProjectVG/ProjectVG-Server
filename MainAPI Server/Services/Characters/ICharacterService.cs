using MainAPI_Server.Models.Service.Characters;
using MainAPI_Server.Models.API.Request.Characters;

namespace MainAPI_Server.Services.Characters
{
    public interface ICharacterService
    {
        Task<IEnumerable<CharacterDto>> GetAllCharactersAsync();
        Task<CharacterDto?> GetCharacterByIdAsync(Guid id);
        Task<CharacterDto> CreateCharacterAsync(CreateCharacterRequest request);
        Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterRequest request);
        Task DeleteCharacterAsync(Guid id);
    }
} 