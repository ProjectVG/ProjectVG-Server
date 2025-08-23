using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Auth;

public class JwtKeyProviderService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JwtKeyProviderService> _logger;
    private Timer? _timer;

    public JwtKeyProviderService(IServiceProvider serviceProvider, ILogger<JwtKeyProviderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureKeysExistAsync();
        
        // 1시간마다 키 회전 확인
        _timer = new Timer(async _ => await CheckKeyRotationAsync(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task EnsureKeysExistAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var keyStore = scope.ServiceProvider.GetRequiredService<IKeyStore>();
            
            var validationKeys = await keyStore.GetValidationKeysAsync();
            if (!validationKeys.Any())
            {
                _logger.LogInformation("No validation keys found. Generating new key pair.");
                await keyStore.RotateKeyAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure keys exist");
        }
    }

    private async Task CheckKeyRotationAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var keyStore = scope.ServiceProvider.GetRequiredService<IKeyStore>();
            
            if (await keyStore.IsKeyRotationNeededAsync())
            {
                _logger.LogInformation("Key rotation needed. Rotating keys.");
                await keyStore.RotateKeyAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check key rotation");
        }
    }
}
