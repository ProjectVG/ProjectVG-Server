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
        Task<CharacterDto> GetCharacterByIdAsync(Guid id);

        /// <summary>
        /// 개별 설정으로 새 캐릭터를 생성합니다
        /// </summary>
        /// <param name="command">개별 설정 캐릭터 생성 명령</param>
        /// <returns>생성된 캐릭터</returns>
        Task<CharacterDto> CreateCharacterWithFieldsAsync(CreateCharacterWithFieldsCommand command);

        /// <summary>
        /// SystemPrompt로 새 캐릭터를 생성합니다
        /// </summary>
        /// <param name="command">SystemPrompt 캐릭터 생성 명령</param>
        /// <returns>생성된 캐릭터</returns>
        Task<CharacterDto> CreateCharacterWithSystemPromptAsync(CreateCharacterWithSystemPromptCommand command);

        /// <summary>
        /// 캐릭터를 개별 설정 모드로 수정합니다
        /// </summary>
        /// <param name="command">개별 설정 수정 명령</param>
        /// <returns>수정된 캐릭터</returns>
        Task<CharacterDto> UpdateCharacterToIndividualAsync(UpdateCharacterToIndividualCommand command);

        /// <summary>
        /// 캐릭터를 SystemPrompt 모드로 수정합니다
        /// </summary>
        /// <param name="command">SystemPrompt 수정 명령</param>
        /// <returns>수정된 캐릭터</returns>
        Task<CharacterDto> UpdateCharacterToSystemPromptAsync(UpdateCharacterToSystemPromptCommand command);

        /// <summary>
        /// 캐릭터를 삭제합니다
        /// </summary>
        /// <param name="id">캐릭터 ID</param>
        Task DeleteCharacterAsync(Guid id);

        /// <summary>
        /// 캐릭터가 존재하는지 확인합니다
        /// </summary>
        /// <param name="id">캐릭터 ID</param>
        /// <returns>캐릭터 존재 여부</returns>
        Task<bool> CharacterExistsAsync(Guid id);
    }
} 