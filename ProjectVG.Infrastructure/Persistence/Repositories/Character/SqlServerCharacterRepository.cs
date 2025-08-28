using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// 주어진 <paramref name="character"/>의 변경 내용을 데이터베이스의 활성 캐릭터 엔티티에 적용하고 저장합니다.
        /// </summary>
        /// <param name="character">업데이트할 값이 들어있는 캐릭터 엔티티(식별자(Id) 포함).</param>
        /// <returns>저장된 후의 기존 캐릭터 엔티티(데이터베이스에 있는 인스턴스).</returns>
        /// <exception cref="NotFoundException">지정한 Id의 활성 캐릭터를 찾을 수 없을 경우 발생합니다.</exception>
        public async Task<Character> UpdateAsync(Character character)
        {
            var existingCharacter = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == character.Id && c.IsActive);

            if (existingCharacter == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, "Character", character.Id);
            }

            existingCharacter.Name = character.Name;
            existingCharacter.Description = character.Description;
            existingCharacter.Role = character.Role;
            existingCharacter.Personality = character.Personality;
            existingCharacter.Background = character.Background;
            existingCharacter.IsActive = character.IsActive;
            existingCharacter.Update();

            await _context.SaveChangesAsync();

            return existingCharacter;
        }

        public async Task DeleteAsync(Guid id)
        {
            var character = await _context.Characters.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (character == null) {
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, "Character", id);
            }

            character.IsActive = false;
            character.Update();
            await _context.SaveChangesAsync();

        }
    }
}
