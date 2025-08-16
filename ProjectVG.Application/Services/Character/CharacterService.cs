using ProjectVG.Infrastructure.Persistence.Repositories.Characters;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Character;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

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

        /// <summary>
        /// 모든 캐릭터를 조회하여 CharacterDto 컬렉션으로 반환합니다.
        /// </summary>
        /// <returns>저장소의 모든 캐릭터를 매핑한 <see cref="CharacterDto"/> 열거형 컬렉션.</returns>
        public async Task<IEnumerable<CharacterDto>> GetAllCharactersAsync()
        {
            var characters = await _characterRepository.GetAllAsync();
            var characterDtos = characters.Select(c => new CharacterDto(c));

            return characterDtos;
        }

        /// <summary>
        /// 지정한 ID의 캐릭터를 조회하여 DTO로 반환합니다.
        /// </summary>
        /// <param name="id">조회할 캐릭터의 고유 식별자(Guid).</param>
        /// <returns>조회된 캐릭터를 나타내는 CharacterDto.</returns>
        /// <exception cref="NotFoundException">해당 ID의 캐릭터가 존재하지 않을 경우 발생합니다 (ErrorCode.CHARACTER_NOT_FOUND).</exception>
        public async Task<CharacterDto> GetCharacterByIdAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            if (character == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, id);
            }

            var characterDto = new CharacterDto(character);
            return characterDto;
        }

        /// <summary>
        /// 새로운 캐릭터를 생성하고 저장한 뒤 생성된 캐릭터의 DTO를 반환합니다.
        /// </summary>
        /// <param name="command">생성할 캐릭터의 이름, 설명, 역할 및 활성화 여부를 포함한 명령 객체.</param>
        /// <returns>영구 저장된 새 캐릭터를 나타내는 <see cref="CharacterDto"/>.</returns>
        public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterCommand command)
        {
            var character = new ProjectVG.Domain.Entities.Characters.Character {
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

        /// <summary>
        /// 지정된 ID의 캐릭터를 업데이트하고 업데이트된 캐릭터 정보를 반환합니다.
        /// </summary>
        /// <param name="id">업데이트할 캐릭터의 고유 식별자.</param>
        /// <param name="command">업데이트할 필드(이름, 설명, 역할, 활성 여부)를 담은 명령 객체.</param>
        /// <returns>업데이트된 캐릭터를 표현하는 <see cref="CharacterDto"/>.</returns>
        /// <exception cref="NotFoundException">지정한 ID의 캐릭터가 존재하지 않을 경우 발생합니다 (ErrorCode.CHARACTER_NOT_FOUND).</exception>
        public async Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterCommand command)
        {
            var existingCharacter = await _characterRepository.GetByIdAsync(id);
            if (existingCharacter == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, id);
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

        /// <summary>
        /// 지정한 ID에 해당하는 캐릭터를 삭제한다.
        /// </summary>
        /// <param name="id">삭제할 캐릭터의 식별자(Guid).</param>
        /// <exception cref="NotFoundException">지정한 ID의 캐릭터가 존재하지 않을 경우(ErrorCode.CHARACTER_NOT_FOUND).</exception>
        public async Task DeleteCharacterAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            if (character == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, id);
            }

            await _characterRepository.DeleteAsync(id);
            _logger.LogInformation("캐릭터 삭제 완료: ID {CharacterId}, 이름 {CharacterName}", id, character.Name);
        }

        /// <summary>
        /// 지정한 ID를 가진 캐릭터의 존재 여부를 비동기로 확인합니다.
        /// </summary>
        /// <param name="id">확인할 캐릭터의 식별자(Guid).</param>
        /// <returns>캐릭터가 존재하면 <c>true</c>, 존재하지 않으면 <c>false</c>를 반환합니다.</returns>
        public async Task<bool> CharacterExistsAsync(Guid id)
        {
            var character = await _characterRepository.GetByIdAsync(id);
            return character != null;
        }
    }
}