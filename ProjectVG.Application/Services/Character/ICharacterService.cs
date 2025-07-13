using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Services.Character
{
    public interface ICharacterService
    {
        /// <summary>
        /// 모든 캐릭터를 조회합니다
        /// </summary>
        /// <returns>캐릭터 목록</returns>
        Task<IEnumerable<CharacterDto>> GetAllCharactersAsync();

        /// <summary>
        /// ID로 캐릭터를 조회합니다
        /// </summary>
        /// <param name="id">캐릭터 ID</param>
        /// <returns>캐릭터 정보</returns>
        Task<CharacterDto?> GetCharacterByIdAsync(Guid id);

        /// <summary>
        /// 새 캐릭터를 생성합니다
        /// </summary>
        /// <param name="command">캐릭터 생성 명령</param>
        /// <returns>생성된 캐릭터</returns>
        Task<CharacterDto> CreateCharacterAsync(CreateCharacterCommand command);

        /// <summary>
        /// 캐릭터를 수정합니다
        /// </summary>
        /// <param name="id">캐릭터 ID</param>
        /// <param name="command">캐릭터 수정 명령</param>
        /// <returns>수정된 캐릭터</returns>
        Task<CharacterDto> UpdateCharacterAsync(Guid id, UpdateCharacterCommand command);

        /// <summary>
        /// 캐릭터를 삭제합니다
        /// </summary>
        /// <param name="id">캐릭터 ID</param>
        Task DeleteCharacterAsync(Guid id);
    }
} 