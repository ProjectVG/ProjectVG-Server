using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectVG.Infrastructure.Auth.Models;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace ProjectVG.Infrastructure.Auth;

public class FileSystemKeyStore : IKeyStore
{
    private readonly JwtSettings _jwtSettings;
    private readonly string _keysDirectory;
    private readonly string _metadataFilePath;

    public FileSystemKeyStore(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _keysDirectory = _jwtSettings.KeysDirectory ?? "./keys";
        _metadataFilePath = Path.Combine(_keysDirectory, "metadata.json");
        
        EnsureDirectoryExists();
    }

    public async Task<RsaSecurityKey> GetCurrentSigningKeyAsync()
    {
        var metadata = await LoadMetadataAsync();
        var currentKey = metadata.Keys.FirstOrDefault(k => k.KeyId == metadata.CurrentKeyId && k.Status == "active");
        
        if (currentKey == null)
        {
            await RotateKeyAsync();
            metadata = await LoadMetadataAsync();
            currentKey = metadata.Keys.First(k => k.KeyId == metadata.CurrentKeyId);
        }

        return await LoadPrivateKeyAsync(currentKey.KeyId);
    }

    public async Task<IEnumerable<RsaSecurityKey>> GetValidationKeysAsync()
    {
        var metadata = await LoadMetadataAsync();
        var validKeys = metadata.Keys.Where(k => k.Status == "active" && k.ExpiresAt > DateTime.UtcNow);
        
        var keys = new List<RsaSecurityKey>();
        foreach (var keyInfo in validKeys)
        {
            try
            {
                var publicKey = await LoadPublicKeyAsync(keyInfo.KeyId);
                keys.Add(publicKey);
            }
            catch
            {
                // 키 로드 실패 시 무시
            }
        }

        return keys;
    }

    public async Task<string> RotateKeyAsync()
    {
        var keyId = GenerateKeyId();
        var (privateKey, publicKey) = GenerateKeyPair();

        await SaveKeyPairAsync(keyId, privateKey, publicKey);
        await UpdateMetadataAsync(keyId);

        return keyId;
    }

    public async Task RevokeKeyAsync(string keyId)
    {
        var metadata = await LoadMetadataAsync();
        var key = metadata.Keys.FirstOrDefault(k => k.KeyId == keyId);
        
        if (key != null)
        {
            key.Status = "revoked";
            await SaveMetadataAsync(metadata);
        }
    }

    public async Task<bool> IsKeyRotationNeededAsync()
    {
        var metadata = await LoadMetadataAsync();
        var currentKey = metadata.Keys.FirstOrDefault(k => k.KeyId == metadata.CurrentKeyId);
        
        if (currentKey == null) return true;
        
        var rotationInterval = TimeSpan.FromDays(_jwtSettings.RotationIntervalDays);
        return DateTime.UtcNow - currentKey.CreatedAt > rotationInterval;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_keysDirectory))
        {
            Directory.CreateDirectory(_keysDirectory);
        }
    }

    private async Task<KeyMetadata> LoadMetadataAsync()
    {
        if (!File.Exists(_metadataFilePath))
        {
            var newMetadata = new KeyMetadata();
            await SaveMetadataAsync(newMetadata);
            return newMetadata;
        }

        var json = await File.ReadAllTextAsync(_metadataFilePath);
        return JsonSerializer.Deserialize<KeyMetadata>(json) ?? new KeyMetadata();
    }

    private async Task SaveMetadataAsync(KeyMetadata metadata)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_metadataFilePath, json);
    }

    private async Task UpdateMetadataAsync(string newKeyId)
    {
        var metadata = await LoadMetadataAsync();
        
        metadata.CurrentKeyId = newKeyId;
        metadata.Keys.Add(new KeyInfo
        {
            KeyId = newKeyId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RotationIntervalDays + 30), // 30일 겹침
            Status = "active",
            Algorithm = "RS256"
        });

        await SaveMetadataAsync(metadata);
    }

    private static (RSA privateKey, RSA publicKey) GenerateKeyPair()
    {
        var rsa = RSA.Create(2048);
        var publicRsa = RSA.Create();
        publicRsa.ImportRSAPublicKey(rsa.ExportRSAPublicKey(), out _);
        
        return (rsa, publicRsa);
    }

    private async Task SaveKeyPairAsync(string keyId, RSA privateKey, RSA publicKey)
    {
        var privateKeyPath = Path.Combine(_keysDirectory, $"private-{keyId}.pem");
        var publicKeyPath = Path.Combine(_keysDirectory, $"public-{keyId}.pem");

        var privateKeyPem = privateKey.ExportPkcs8PrivateKeyPem();
        var publicKeyPem = publicKey.ExportRSAPublicKeyPem();

        await File.WriteAllTextAsync(privateKeyPath, privateKeyPem);
        await File.WriteAllTextAsync(publicKeyPath, publicKeyPem);

        // 파일 권한 설정 (Unix 계열)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            File.SetUnixFileMode(privateKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.SetUnixFileMode(publicKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private async Task<RsaSecurityKey> LoadPrivateKeyAsync(string keyId)
    {
        var privateKeyPath = Path.Combine(_keysDirectory, $"private-{keyId}.pem");
        var pem = await File.ReadAllTextAsync(privateKeyPath);
        
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        
        return new RsaSecurityKey(rsa) { KeyId = keyId };
    }

    private async Task<RsaSecurityKey> LoadPublicKeyAsync(string keyId)
    {
        var publicKeyPath = Path.Combine(_keysDirectory, $"public-{keyId}.pem");
        var pem = await File.ReadAllTextAsync(publicKeyPath);
        
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        
        return new RsaSecurityKey(rsa) { KeyId = keyId };
    }

    private static string GenerateKeyId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var guid = Guid.NewGuid().ToString();
        return $"dev-{timestamp}-{guid}";
    }
}
