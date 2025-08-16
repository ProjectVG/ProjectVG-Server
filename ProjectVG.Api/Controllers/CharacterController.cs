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

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CharacterResponse>>> GetAllCharacters()
        {
            var characterDtos = await _characterService.GetAllCharactersAsync();
            var responses = characterDtos.Select(CharacterResponse.ToResponseDto);
            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterResponse>> GetCharacterById(Guid id)
        {
            var characterDto = await _characterService.GetCharacterByIdAsync(id);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<CharacterResponse>> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            var command = request.ToCreateCharacterCommand();
            var characterDto = await _characterService.CreateCharacterAsync(command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CharacterResponse>> UpdateCharacter(Guid id, [FromBody] UpdateCharacterRequest request)
        {
            var command = request.ToUpdateCharacterCommand();
            var characterDto = await _characterService.UpdateCharacterAsync(id, command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCharacter(Guid id)
        {
            await _characterService.DeleteCharacterAsync(id);
            return NoContent();
        }
    }
} 