using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Character;
using ProjectVG.Api.Models.Character;
using ProjectVG.Application.Models.Character;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly ICharacterService _characterService;
        private readonly ILogger<CharacterController> _logger;

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CharacterResponseDto>>> GetAllCharacters()
        {
            try
            {
                var characterDtos = await _characterService.GetAllCharactersAsync();
                var responses = characterDtos.Select(CharacterMapper.ToResponseDto);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "모든 캐릭터 조회 중 오류가 발생했습니다");
                return StatusCode(500, "캐릭터 목록을 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterResponseDto>> GetCharacterById(Guid id)
        {
            try
            {
                var characterDto = await _characterService.GetCharacterByIdAsync(id);
                if (characterDto == null)
                {
                    return NotFound($"ID {id}인 캐릭터를 찾을 수 없습니다.");
                }

                var response = CharacterMapper.ToResponseDto(characterDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {CharacterId}인 캐릭터 조회 중 오류가 발생했습니다", id);
                return StatusCode(500, "캐릭터 정보를 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<CharacterResponseDto>> CreateCharacter([FromBody] CreateCharacterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var command = new CreateCharacterCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    Role = request.Role,
                    IsActive = request.IsActive
                };

                var characterDto = await _characterService.CreateCharacterAsync(command);
                var response = CharacterMapper.ToResponseDto(characterDto);
                return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐릭터 생성 중 오류가 발생했습니다");
                return StatusCode(500, "캐릭터 생성 중 내부 서버 오류가 발생했습니다.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CharacterResponseDto>> UpdateCharacter(Guid id, [FromBody] UpdateCharacterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var command = new UpdateCharacterCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    Role = request.Role,
                    IsActive = request.IsActive
                };

                var characterDto = await _characterService.UpdateCharacterAsync(id, command);
                var response = CharacterMapper.ToResponseDto(characterDto);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"ID {id}인 캐릭터를 찾을 수 없습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {CharacterId}인 캐릭터 수정 중 오류가 발생했습니다", id);
                return StatusCode(500, "캐릭터 정보 수정 중 내부 서버 오류가 발생했습니다.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCharacter(Guid id)
        {
            try
            {
                await _characterService.DeleteCharacterAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {CharacterId}인 캐릭터 삭제 중 오류가 발생했습니다", id);
                return StatusCode(500, "캐릭터 삭제 중 내부 서버 오류가 발생했습니다.");
            }
        }
    }
} 