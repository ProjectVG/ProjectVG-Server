using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.Users
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(UserCreateCommand command);
        Task<bool> DeleteUserAsync(Guid userId);

        Task<UserDto?> TryGetByIdAsync(Guid userId);
        Task<UserDto?> TryGetByUidAsync(string uid);
        Task<UserDto?> TryGetByUsernameAsync(string username);
        Task<UserDto?> TryGetByProviderAsync(string provider, string providerId);

        Task<bool> ExistsByIdAsync(Guid userId);
        Task<bool> ExistsByUidAsync(string uid);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
    }

    public record UserCreateCommand(
        string Username,
        string Email,
        string ProviderId,
        string Provider
    );
}