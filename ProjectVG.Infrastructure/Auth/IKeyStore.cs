using Microsoft.IdentityModel.Tokens;

namespace ProjectVG.Infrastructure.Auth;

public interface IKeyStore
{
    Task<RsaSecurityKey> GetCurrentSigningKeyAsync();
    Task<IEnumerable<RsaSecurityKey>> GetValidationKeysAsync();
    Task<string> RotateKeyAsync();
    Task RevokeKeyAsync(string keyId);
    Task<bool> IsKeyRotationNeededAsync();
}
