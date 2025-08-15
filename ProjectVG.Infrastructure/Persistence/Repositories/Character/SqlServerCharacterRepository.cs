using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.Characters;
using ProjectVG.Infrastructure.Persistence.EfCore;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Characters
{
    public class SqlServerCharacterRepository : ICharacterRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerCharacterRepository> _logger;

        public SqlServerCharacterRepository(ProjectVGDbContext context, ILogger<SqlServerCharacterRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Character>> GetAllAsync()
        {
            return await _context.Characters
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Character?> GetByIdAsync(Guid id)
        {
            return await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<Character> CreateAsync(Character character)
        {
            character.Id = Guid.NewGuid();
            character.CreatedAt = DateTime.UtcNow;
            character.UpdatedAt = DateTime.UtcNow;
            character.IsActive = true;

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();

            return character;
        }

        public async Task<Character> UpdateAsync(Character character)
        {
            var existingCharacter = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == character.Id && c.IsActive);

            if (existingCharacter == null) {
                throw new KeyNotFoundException($"ID {character.Id}인 캐릭터를 찾을 수 없습니다.");
            }

            existingCharacter.Name = character.Name;
            existingCharacter.Description = character.Description;
            existingCharacter.Role = character.Role;
            existingCharacter.Personality = character.Personality;
            existingCharacter.Background = character.Background;
            existingCharacter.Metadata = character.Metadata;
            existingCharacter.IsActive = character.IsActive;
            existingCharacter.Update();

            await _context.SaveChangesAsync();

            return existingCharacter;
        }

        public async Task DeleteAsync(Guid id)
        {
            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (character == null) {
                throw new KeyNotFoundException($"ID {id}인 캐릭터를 찾을 수 없습니다.");
            }

            character.IsActive = false;
            character.Update();
            await _context.SaveChangesAsync();

        }
    }
}
