using ProjectVG.Domain.Repositories;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Character;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;
using ProjectVG.Domain.Entities.Characters;

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
            var characters = await _characterRepository.GetAllAsync();
            var characterDtos = characters.Select(c => new CharacterDto(c));

            return characterDtos;
        }

        public async Task<CharacterDto> GetCharacterByIdAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            if (character == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, id);
            }

            var characterDto = new CharacterDto(character);
            return characterDto;
        }

        public async Task<CharacterDto> CreateCharacterWithFieldsAsync(CreateCharacterWithFieldsCommand command)
        {
            var character = new ProjectVG.Domain.Entities.Characters.Character
            {
                Name = command.Name,
                Description = command.Description,
                ImageUrl = command.ImageUrl,
                VoiceId = command.VoiceId,
                IsActive = command.IsActive,
                UserId = command.UserId,
                IsPublic = command.IsPublic
            };
            
            character.SetIndividualConfig(command.IndividualConfig);

            var createdCharacter = await _characterRepository.CreateAsync(character);
            var characterDto = new CharacterDto(createdCharacter);

            _logger.LogInformation("개별 설정 캐릭터 생성 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
            return characterDto;
        }

        public async Task<CharacterDto> CreateCharacterWithSystemPromptAsync(CreateCharacterWithSystemPromptCommand command)
        {
            var character = new ProjectVG.Domain.Entities.Characters.Character
            {
                Name = command.Name,
                Description = command.Description,
                ImageUrl = command.ImageUrl,
                VoiceId = command.VoiceId,
                IsActive = command.IsActive,
                UserId = command.UserId,
                IsPublic = command.IsPublic
            };
            
            character.SetSystemPrompt(command.SystemPrompt);

            var createdCharacter = await _characterRepository.CreateAsync(character);
            var characterDto = new CharacterDto(createdCharacter);

            _logger.LogInformation("SystemPrompt 캐릭터 생성 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
            return characterDto;
        }

        public async Task<CharacterDto> UpdateCharacterToIndividualAsync(UpdateCharacterToIndividualCommand command)
        {
            var existingCharacter = await _characterRepository.GetByIdAsync(command.Id);
            if (existingCharacter == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, command.Id);
            }

            existingCharacter.Name = command.Name;
            existingCharacter.Description = command.Description;
            existingCharacter.ImageUrl = command.ImageUrl;
            existingCharacter.VoiceId = command.VoiceId;
            existingCharacter.SetIndividualConfig(command.IndividualConfig);

            var updatedCharacter = await _characterRepository.UpdateAsync(existingCharacter);
            var characterDto = new CharacterDto(updatedCharacter);
            _logger.LogInformation("캐릭터 개별 설정 모드로 수정 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
            return characterDto;
        }

        public async Task<CharacterDto> UpdateCharacterToSystemPromptAsync(UpdateCharacterToSystemPromptCommand command)
        {
            var existingCharacter = await _characterRepository.GetByIdAsync(command.Id);
            if (existingCharacter == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, command.Id);
            }

            existingCharacter.Name = command.Name;
            existingCharacter.Description = command.Description;
            existingCharacter.ImageUrl = command.ImageUrl;
            existingCharacter.VoiceId = command.VoiceId;
            existingCharacter.SetSystemPrompt(command.SystemPrompt);

            var updatedCharacter = await _characterRepository.UpdateAsync(existingCharacter);
            var characterDto = new CharacterDto(updatedCharacter);
            _logger.LogInformation("캐릭터 SystemPrompt 모드로 수정 완료: {CharacterName} (ID: {CharacterId})", characterDto.Name, characterDto.Id);
            return characterDto;
        }

        public async Task DeleteCharacterAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            if (character == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, id);
            }

            await _characterRepository.DeleteAsync(id);
            _logger.LogInformation("캐릭터 삭제 완료: ID {CharacterId}, 이름 {CharacterName}", id, character.Name);
        }

        public async Task<bool> CharacterExistsAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            return character != null;
        }

        public async Task<IEnumerable<CharacterDto>> GetMyCharactersAsync(Guid userId, string orderBy = "latest")
        {
            var characters = await _characterRepository.GetByUserIdAsync(userId);
            
            // 정렬 적용
            var sortedCharacters = orderBy.ToLower() switch
            {
                "latest" => characters.OrderByDescending(c => c.CreatedAt),
                "oldest" => characters.OrderBy(c => c.CreatedAt),
                "name" => characters.OrderBy(c => c.Name),
                _ => characters.OrderByDescending(c => c.CreatedAt)
            };

            var characterDtos = sortedCharacters.Select(c => new CharacterDto(c));
            _logger.LogInformation("사용자 {UserId}의 캐릭터 {Count}개 조회 완료", userId, characterDtos.Count());
            
            return characterDtos;
        }

        public async Task<IEnumerable<CharacterDto>> GetPublicCharactersAsync(string orderBy = "latest")
        {
            var characters = await _characterRepository.GetPublicCharactersAsync();
            
            // 정렬 적용
            var sortedCharacters = orderBy.ToLower() switch
            {
                "latest" => characters.OrderByDescending(c => c.CreatedAt),
                "oldest" => characters.OrderBy(c => c.CreatedAt),
                "name" => characters.OrderBy(c => c.Name),
                _ => characters.OrderByDescending(c => c.CreatedAt)
            };

            var characterDtos = sortedCharacters.Select(c => new CharacterDto(c));
            _logger.LogInformation("공개 캐릭터 {Count}개 조회 완료", characterDtos.Count());
            
            return characterDtos;
        }
    }
}