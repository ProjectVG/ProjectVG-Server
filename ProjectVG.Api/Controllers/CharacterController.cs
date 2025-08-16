using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Models.Character;
using ProjectVG.Api.Models.Character.Request;
using ProjectVG.Api.Models.Character.Response;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly ICharacterService _characterService;
        private readonly ILogger<CharacterController> _logger;

        /// <summary>
        /// CharacterController의 인스턴스를 초기화하고 필요한 의존성을 주입합니다.
        /// </summary>
        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        /// <summary>
        /// 모든 캐릭터를 비동기로 조회하여 API 응답 모델 목록으로 반환합니다.
        /// </summary>
        /// <returns>HTTP 200 OK와 함께 서비스에서 가져온 캐릭터들을 CharacterResponse 형식의 열거로 반환합니다.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CharacterResponse>>> GetAllCharacters()
        {
            var characterDtos = await _characterService.GetAllCharactersAsync();
            var responses = characterDtos.Select(CharacterResponse.ToResponseDto);
            return Ok(responses);
        }

        /// <summary>
        /// 지정된 ID를 가진 캐릭터를 조회하여 응답 모델로 반환합니다.
        /// </summary>
        /// <param name="id">조회할 캐릭터의 GUID 식별자.</param>
        /// <returns>HTTP 200 OK와 해당 캐릭터를 표현하는 <see cref="CharacterResponse"/>를 포함한 <see cref="ActionResult{T}"/>.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterResponse>> GetCharacterById(Guid id)
        {
            var characterDto = await _characterService.GetCharacterByIdAsync(id);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return Ok(response);
        }

        /// <summary>
        /// 새 캐릭터를 생성하고 생성된 캐릭터의 정보를 201 Created 응답으로 반환합니다.
        /// </summary>
        /// <param name="request">요청 본문으로 전달된 캐릭터 생성 데이터. 내부에서 생성 명령으로 변환되어 서비스에 전달됩니다.</param>
        /// <returns>생성된 캐릭터를 포함한 201 Created 응답(응답 헤더의 Location은 GetCharacterById 엔드포인트를 가리킵니다).</returns>
        [HttpPost]
        public async Task<ActionResult<CharacterResponse>> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            var command = request.ToCreateCharacterCommand();
            var characterDto = await _characterService.CreateCharacterAsync(command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
        }

        /// <summary>
        /// 지정된 ID의 캐릭터를 업데이트하고 업데이트된 캐릭터 정보를 반환합니다.
        /// </summary>
        /// <param name="id">업데이트할 캐릭터의 고유 식별자(Guid).</param>
        /// <param name="request">업데이트에 사용할 데이터(요청 모델). 내부에서 UpdateCharacterCommand로 변환됩니다.</param>
        /// <returns>업데이트된 캐릭터를 담은 <see cref="CharacterResponse"/>와 함께 200 OK 응답을 반환합니다.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<CharacterResponse>> UpdateCharacter(Guid id, [FromBody] UpdateCharacterRequest request)
        {
            var command = request.ToUpdateCharacterCommand();
            var characterDto = await _characterService.UpdateCharacterAsync(id, command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return Ok(response);
        }

        /// <summary>
        /// 지정한 ID의 캐릭터를 삭제합니다.
        /// </summary>
        /// <param name="id">삭제할 캐릭터의 고유 식별자(Guid).</param>
        /// <returns>성공 시 HTTP 204 No Content 응답을 반환합니다.</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCharacter(Guid id)
        {
            await _characterService.DeleteCharacterAsync(id);
            return NoContent();
        }
    }
} 