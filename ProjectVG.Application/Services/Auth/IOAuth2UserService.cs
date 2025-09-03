using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public interface IOAuth2UserService
    {
        Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string providerName);
    }
}