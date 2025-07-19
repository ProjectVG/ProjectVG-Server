using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectVG.Domain.Entities.User;
using ProjectVG.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                _logger.LogInformation("사용자 {Count}명을 조회했습니다", users.Count());
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "모든 사용자 조회 중 오류가 발생했습니다");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("ID {UserId}인 사용자를 찾을 수 없습니다", id);
                }
                else
                {
                    _logger.LogDebug("ID {UserId}인 사용자를 조회했습니다", id);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 조회 중 오류가 발생했습니다", id);
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("이메일 {Email}인 사용자를 찾을 수 없습니다", email);
                }
                else
                {
                    _logger.LogDebug("이메일 {Email}인 사용자를 조회했습니다", email);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email}인 사용자 조회 중 오류가 발생했습니다", email);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("사용자명 {Username}인 사용자를 찾을 수 없습니다", username);
                }
                else
                {
                    _logger.LogDebug("사용자명 {Username}인 사용자를 조회했습니다", username);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username}인 사용자 조회 중 오류가 발생했습니다", username);
                throw;
            }
        }

        public async Task<User?> GetUserByProviderIdAsync(string providerId)
        {
            try
            {
                var user = await _userRepository.GetByProviderIdAsync(providerId);
                if (user == null)
                {
                    _logger.LogWarning("Provider ID {ProviderId}인 사용자를 찾을 수 없습니다", providerId);
                }
                else
                {
                    _logger.LogDebug("Provider ID {ProviderId}인 사용자를 조회했습니다", providerId);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider ID {ProviderId}인 사용자 조회 중 오류가 발생했습니다", providerId);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                // 중복 검사
                if (await EmailExistsAsync(user.Email))
                {
                    throw new InvalidOperationException($"이메일 '{user.Email}'이 이미 존재합니다.");
                }

                if (await UsernameExistsAsync(user.Username))
                {
                    throw new InvalidOperationException($"사용자명 '{user.Username}'이 이미 존재합니다.");
                }

                var createdUser = await _userRepository.CreateAsync(user);
                _logger.LogInformation("사용자를 생성했습니다. ID: {UserId}, 사용자명: {Username}", 
                    createdUser.Id, createdUser.Username);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email}인 사용자 생성 중 오류가 발생했습니다", user.Email);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _userRepository.GetByIdAsync(user.Id);
                if (existingUser == null)
                {
                    throw new KeyNotFoundException($"ID {user.Id}인 사용자를 찾을 수 없습니다.");
                }

                // 이메일 변경 시 중복 검사
                if (user.Email != existingUser.Email && await EmailExistsAsync(user.Email))
                {
                    throw new InvalidOperationException($"이메일 '{user.Email}'이 이미 존재합니다.");
                }

                // 사용자명 변경 시 중복 검사
                if (user.Username != existingUser.Username && await UsernameExistsAsync(user.Username))
                {
                    throw new InvalidOperationException($"사용자명 '{user.Username}'이 이미 존재합니다.");
                }

                var updatedUser = await _userRepository.UpdateAsync(user);
                _logger.LogInformation("사용자를 수정했습니다. ID: {UserId}, 사용자명: {Username}", 
                    updatedUser.Id, updatedUser.Username);
                return updatedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 수정 중 오류가 발생했습니다", user.Id);
                throw;
            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            try
            {
                var existingUser = await _userRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning("ID {UserId}인 사용자를 삭제하려 했지만 사용자를 찾을 수 없습니다", id);
                    return;
                }

                await _userRepository.DeleteAsync(id);
                _logger.LogInformation("사용자를 삭제했습니다. ID: {UserId}, 사용자명: {Username}", 
                    id, existingUser.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 삭제 중 오류가 발생했습니다", id);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 존재 여부 확인 중 오류가 발생했습니다", id);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email} 존재 여부 확인 중 오류가 발생했습니다", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username} 존재 여부 확인 중 오류가 발생했습니다", username);
                throw;
            }
        }
    }
}
