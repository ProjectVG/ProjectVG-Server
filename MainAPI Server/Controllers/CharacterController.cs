using Microsoft.AspNetCore.Mvc;
using MainAPI_Server.Services.Characters;
using MainAPI_Server.Models.Service.Characters;
using MainAPI_Server.Models.API.Request.Characters;
using MainAPI_Server.Models.API.Response.Characters;

namespace MainAPI_Server.Controllers
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
        public async Task<ActionResult<IEnumerable<CharacterResponse>>> GetAllCharacters()
        {
            try
            {
                var characters = await _characterService.GetAllCharactersAsync();
                var responses = characters.Select(MapToResponse);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all characters");
                return StatusCode(500, "Internal server error occurred while retrieving characters");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterResponse>> GetCharacterById(Guid id)
        {
            try
            {
                var character = await _characterService.GetCharacterByIdAsync(id);
                if (character == null)
                {
                    return NotFound($"Character with ID {id} not found");
                }

                var response = MapToResponse(character);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving character with ID: {CharacterId}", id);
                return StatusCode(500, "Internal server error occurred while retrieving character");
            }
        }

        [HttpPost]
        public async Task<ActionResult<CharacterResponse>> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var character = await _characterService.CreateCharacterAsync(request);
                var response = MapToResponse(character);
                return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating character");
                return StatusCode(500, "Internal server error occurred while creating character");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CharacterResponse>> UpdateCharacter(Guid id, [FromBody] UpdateCharacterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var character = await _characterService.UpdateCharacterAsync(id, request);
                var response = MapToResponse(character);
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

        private static CharacterResponse MapToResponse(CharacterDto characterDto)
        {
            return new CharacterResponse
            {
                Id = characterDto.Id,
                Name = characterDto.Name,
                Description = characterDto.Description,
                Role = characterDto.Role,
                Avatar = characterDto.Avatar,
                IsActive = characterDto.IsActive
            };
        }
    }
} 