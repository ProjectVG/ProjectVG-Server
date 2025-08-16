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

        /// <summary>
        /// 새 Character 엔티티에 식별자와 생성/갱신 시각을 설정하고 활성화한 뒤 데이터베이스에 저장합니다.
        /// </summary>
        /// <param name="character">생성할 Character 객체(이 메서드에서 Id, CreatedAt, UpdatedAt, IsActive 값이 설정됩니다).</param>
        /// <returns>저장되어 Id 및 타임스탬프가 설정된 Character 객체.</returns>
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
        /// 지정된 캐릭터의 활성 상태 레코드를 갱신하고 변경된 엔티티를 반환합니다.
        /// </summary>
        /// <param name="character">갱신할 필드(Id로 대상 레코드를 식별)와 새 값을 담은 Character 객체.</param>
        /// <returns>갱신된 활성 Character 엔티티.</returns>
        /// <exception cref="NotFoundException">요청한 Id를 가진 활성 캐릭터가 존재하지 않을 경우 발생합니다 (ErrorCode.CHARACTER_NOT_FOUND).</exception>
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
            existingCharacter.Metadata = character.Metadata;
            existingCharacter.IsActive = character.IsActive;
            existingCharacter.Update();

            await _context.SaveChangesAsync();

            return existingCharacter;
        }

        /// <summary>
        /// 지정한 Id를 가진 활성 Character를 소프트 삭제합니다.
        /// </summary>
        /// <param name="id">삭제할 Character의 식별자(Guid).</param>
        /// <remarks>
        /// 해당 엔티티의 IsActive를 false로 설정하고 엔티티의 Update()를 호출한 뒤 변경사항을 저장합니다.
        /// </remarks>
        /// <exception cref="NotFoundException">지정된 Id를 가진 활성 Character를 찾을 수 없을 때 던져집니다 (ErrorCode.CHARACTER_NOT_FOUND).</exception>
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
