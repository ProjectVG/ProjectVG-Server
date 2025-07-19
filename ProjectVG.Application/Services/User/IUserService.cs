using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.User
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(UserDto userDto);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
    }
} 