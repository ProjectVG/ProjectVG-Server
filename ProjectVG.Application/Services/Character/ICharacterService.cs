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

        /// <summary>
        /// 특정 사용자가 소유한 캐릭터들을 조회합니다 (공개/비공개 모두 포함)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="orderBy">정렬 방식 (latest: 최신순)</param>
        /// <returns>사용자 소유 캐릭터 목록</returns>
        Task<IEnumerable<CharacterDto>> GetMyCharactersAsync(Guid userId, string orderBy = "latest");

        /// <summary>
        /// 공개 캐릭터들을 조회합니다 (시스템 캐릭터 + 모든 사용자의 공개 캐릭터)
        /// </summary>
        /// <param name="orderBy">정렬 방식 (latest: 최신순)</param>
        /// <returns>공개 캐릭터 목록</returns>
        Task<IEnumerable<CharacterDto>> GetPublicCharactersAsync(string orderBy = "latest");
    }
} 