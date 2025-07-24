using ProjectVG.Domain.Entities.Character;
using ProjectVG.Infrastructure.Repositories;
using ProjectVG.Application.Models.Character;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Character
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterRepository _characterRepository;
        private readonly ILogger<ICharacterService> _logger;

        public CharacterService(ICharacterRepository characterRepository, ILogger<ICharacterService> logger)
        {
            _characterRepository = characterRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CharacterDto>> GetAllCharactersAsync()
        {
            try
            {
                var characters = await _characterRepository.GetAllAsync();
                var characterDtos = characters.Select(c => new CharacterDto(c));
                _logger.LogInformation("캐릭터 목록 조회 완료: {Count}개", characterDtos.Count());
                return characterDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 목록 조회 중 오류 발생");
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
                    _logger.LogWarning("캐릭터를 찾을 수 없음: ID {CharacterId}", id);
                    return null;
                }

                var characterDto = new CharacterDto(character);
                _logger.LogInformation("캐릭터 조회 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 조회 중 오류 발생: ID {CharacterId}", id);
                throw;
            }
        }

        public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterCommand command)
        {
            try
            {
                var character = new ProjectVG.Domain.Entities.Character.Character
                {
                    Name = command.Name,
                    Description = command.Description,
                    Role = command.Role,
                    IsActive = command.IsActive
                };

                var createdCharacter = await _characterRepository.CreateAsync(character);
                var characterDto = new CharacterDto(createdCharacter);
                _logger.LogInformation("캐릭터 생성 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 생성 중 오류 발생: {CharacterName}", command.Name);
                throw;
            }
        }

        public async Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterCommand command)
        {
            try
            {
                var existingCharacter = await _characterRepository.GetByIdAsync(id);
                if (existingCharacter == null)
                {
                    throw new KeyNotFoundException($"캐릭터를 찾을 수 없음: ID {id}");
                }

                existingCharacter.Name = command.Name;
                existingCharacter.Description = command.Description;
                existingCharacter.Role = command.Role;
                existingCharacter.IsActive = command.IsActive;

                var updatedCharacter = await _characterRepository.UpdateAsync(existingCharacter);
                var characterDto = new CharacterDto(updatedCharacter);
                _logger.LogInformation("캐릭터 수정 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
                return characterDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 수정 중 오류 발생: ID {CharacterId}", id);
                throw;
            }
        }

        public async Task DeleteCharacterAsync(Guid id)
        {
            try
            {
                await _characterRepository.DeleteAsync(id);
                _logger.LogInformation("캐릭터 삭제 완료: ID {CharacterId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 삭제 중 오류 발생: ID {CharacterId}", id);
                throw;
            }
        }
    }
} 