using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.Auth
{
    public interface IOAuth2AccountManager
    {
        Task<AuthResult> ProcessOAuth2LoginAsync(string provider, OAuth2UserInfo userInfo);
    }
}