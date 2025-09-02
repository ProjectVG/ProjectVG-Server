using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Models.Character;
using ProjectVG.Api.Models.Character.Request;
using ProjectVG.Api.Models.Character.Response;
using ProjectVG.Api.Filters;
using System.Security.Claims;

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

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
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

        [HttpPost("individual")]
        [JwtAuthentication]
        public async Task<ActionResult<CharacterResponse>> CreateCharacterWithFields([FromBody] CreateCharacterWithFieldsRequest request)
        {
            var userId = GetCurrentUserId();
            var command = request.ToCommand(userId);
            var characterDto = await _characterService.CreateCharacterWithFieldsAsync(command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
        }

        [HttpPost("systemprompt")]
        [JwtAuthentication]
        public async Task<ActionResult<CharacterResponse>> CreateCharacterWithSystemPrompt([FromBody] CreateCharacterWithSystemPromptRequest request)
        {
            var userId = GetCurrentUserId();
            var command = request.ToCommand(userId);
            var characterDto = await _characterService.CreateCharacterWithSystemPromptAsync(command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return CreatedAtAction(nameof(GetCharacterById), new { id = response.Id }, response);
        }

        [HttpPut("{id}/individual")]
        public async Task<ActionResult<CharacterResponse>> UpdateCharacterToIndividual(Guid id, [FromBody] UpdateCharacterToIndividualRequest request)
        {
            var command = request.ToCommand(id);
            var characterDto = await _characterService.UpdateCharacterToIndividualAsync(command);
            var response = CharacterResponse.ToResponseDto(characterDto);
            return Ok(response);
        }

        [HttpPut("{id}/systemprompt")]
        public async Task<ActionResult<CharacterResponse>> UpdateCharacterToSystemPrompt(Guid id, [FromBody] UpdateCharacterToSystemPromptRequest request)
        {
            var command = request.ToCommand(id);
            var characterDto = await _characterService.UpdateCharacterToSystemPromptAsync(command);
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