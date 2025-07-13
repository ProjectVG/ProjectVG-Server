using MainAPI_Server.Models.API.Request.Characters;
using MainAPI_Server.Models.Service.Characters;
using MainAPI_Server.Models.Domain.Characters;

namespace MainAPI_Server.Services.Characters
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterRepository _characterRepository;
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(ICharacterRepository characterRepository, ILogger<CharacterService> logger)
        {
            _characterRepository = characterRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CharacterDto>> GetAllCharactersAsync()
        {
            try
            {
                var characters = await _characterRepository.GetAllAsync();
                var characterDtos = characters.Select(MapToDto);
                _logger.LogInformation("Retrieved {Count} characters", characterDtos.Count());
                return characterDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all characters");
                throw;
            }
        }

        public async Task<CharacterDto?> GetCharacterByIdAsync(Guid id)
        {
            try
            {
                var character = await _characterRepository.GetByIdAsync(id);
                if (character == null)
                {
                    _logger.LogWarning("Character not found with ID: {CharacterId}", id);
                    return null;
                }

                var characterDto = MapToDto(character);
                _logger.LogInformation("Retrieved character: {CharacterName} with ID: {CharacterId}", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving character with ID: {CharacterId}", id);
                throw;
            }
        }

        public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterRequest request)
        {
            try
            {

                var character = new Character
                {
                    Name = request.Name,
                    Description = request.Description,
                    Role = request.Role,
                    Avatar = request.Avatar,
                    IsActive = true
                };

                var createdCharacter = await _characterRepository.CreateAsync(character);
                var characterDto = MapToDto(createdCharacter);
                
                _logger.LogInformation("Created character: {CharacterName} with ID: {CharacterId}", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating character: {CharacterName}", request.Name);
                throw;
            }
        }

        public async Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterRequest request)
        {
            try
            {
                var existingCharacter = await _characterRepository.GetByIdAsync(id);
                if (existingCharacter == null)
                {
                    throw new KeyNotFoundException($"Character with ID {id} not found.");
                }

                existingCharacter.Name = request.Name;
                existingCharacter.Description = request.Description;
                existingCharacter.Role = request.Role;
                existingCharacter.Avatar = request.Avatar;
                existingCharacter.IsActive = request.IsActive;

                var updatedCharacter = await _characterRepository.UpdateAsync(existingCharacter);
                var characterDto = MapToDto(updatedCharacter);
                
                _logger.LogInformation("Updated character: {CharacterName} with ID: {CharacterId}", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating character with ID: {CharacterId}", id);
                throw;
            }
        }

        public async Task DeleteCharacterAsync(Guid id)
        {
            try
            {
                await _characterRepository.DeleteAsync(id);
                _logger.LogInformation("Deleted character with ID: {CharacterId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting character with ID: {CharacterId}", id);
                throw;
            }
        }

        private static CharacterDto MapToDto(Character character)
        {
            return new CharacterDto
            {
                Id = character.Id,
                Name = character.Name,
                Description = character.Description,
                Role = character.Role,
                Avatar = character.Avatar,
                IsActive = character.IsActive
            };
        }
    }
} 