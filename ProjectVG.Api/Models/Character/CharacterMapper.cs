using ProjectVG.Application.Models.Character;

namespace ProjectVG.Api.Models.Character
{
    public static class CharacterMapper
    {
        /// <summary>
        /// Application DTO를 API Response DTO로 변환
        /// </summary>
        /// <param name="characterDto">Application CharacterDto</param>
        /// <returns>API CharacterResponseDto</returns>
        public static CharacterResponseDto ToResponseDto(CharacterDto characterDto)
        {
            return new CharacterResponseDto
            {
                Id = characterDto.Id,
                Name = characterDto.Name,
                Description = characterDto.Description,
                Role = characterDto.Role,
                IsActive = characterDto.IsActive
            };
        }
    }
} 