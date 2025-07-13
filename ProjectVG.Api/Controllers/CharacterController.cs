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
                _logger.LogError(ex, "Error occurred while retrieving all characters");
                return StatusCode(500, "Internal server error occurred while retrieving characters");
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
                    return NotFound($"Character with ID {id} not found");
                }

                var response = CharacterMapper.ToResponseDto(characterDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving character with ID: {CharacterId}", id);
                return StatusCode(500, "Internal server error occurred while retrieving character");
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
                _logger.LogError(ex, "Error occurred while creating character");
                return StatusCode(500, "Internal server error occurred while creating character");
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
                return NotFound($"Character with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating character with ID: {CharacterId}", id);
                return StatusCode(500, "Internal server error occurred while updating character");
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
                _logger.LogError(ex, "Error occurred while deleting character with ID: {CharacterId}", id);
                return StatusCode(500, "Internal server error occurred while deleting character");
            }
        }
    }
} 