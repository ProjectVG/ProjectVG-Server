using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Users;
using ProjectVG.Domain.Repositories;
using ProjectVG.Infrastructure.Persistence.Repositories.Characters;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using Xunit;

namespace ProjectVG.Tests.Application.Integration.TestBase
{
    public abstract class ApplicationIntegrationTestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ProjectVGDbContext DbContext;

        protected ApplicationIntegrationTestBase()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            DbContext = ServiceProvider.GetRequiredService<ProjectVGDbContext>();
            
            // Ensure database is created and clean
            EnsureDatabaseCreated();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
                })
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);

            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Add Entity Framework with in-memory database
            services.AddDbContext<ProjectVGDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors());

            // Add repositories
            services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
            services.AddScoped<IUserRepository, SqlServerUserRepository>();
            services.AddScoped<IConversationRepository, SqlServerConversationRepository>();

            // Add application services
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IConversationService, ConversationService>();
        }

        private void EnsureDatabaseCreated()
        {
            DbContext.Database.EnsureCreated();
        }

        protected async Task ClearDatabaseAsync()
        {
            // Remove all data from tables
            DbContext.ConversationHistories.RemoveRange(DbContext.ConversationHistories);
            DbContext.Characters.RemoveRange(DbContext.Characters);
            DbContext.Users.RemoveRange(DbContext.Users);
            
            await DbContext.SaveChangesAsync();
        }

        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        protected T? GetOptionalService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        public void Dispose()
        {
            DbContext.Dispose();
            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }
    }

    /// <summary>
    /// Test fixture specifically for xUnit Collection Fixture
    /// </summary>
    public class ApplicationIntegrationTestFixture : IDisposable
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly ProjectVGDbContext DbContext;

        public ApplicationIntegrationTestFixture()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            DbContext = ServiceProvider.GetRequiredService<ProjectVGDbContext>();
            
            // Ensure database is created and clean
            DbContext.Database.EnsureCreated();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
                })
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);

            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Add Entity Framework with in-memory database
            services.AddDbContext<ProjectVGDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors());

            // Add repositories
            services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
            services.AddScoped<IUserRepository, SqlServerUserRepository>();
            services.AddScoped<IConversationRepository, SqlServerConversationRepository>();

            // Add application services
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IConversationService, ConversationService>();
        }

        public T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public T? GetOptionalService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        public async Task ClearDatabaseAsync()
        {
            // Remove all data from tables
            DbContext.ConversationHistories.RemoveRange(DbContext.ConversationHistories);
            DbContext.Characters.RemoveRange(DbContext.Characters);
            DbContext.Users.RemoveRange(DbContext.Users);
            
            await DbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            DbContext.Dispose();
            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }
    }

    /// <summary>
    /// Collection fixture for integration tests to share database context across tests
    /// </summary>
    [CollectionDefinition("ApplicationIntegration")]
    public class ApplicationIntegrationTestCollection : ICollectionFixture<ApplicationIntegrationTestFixture>
    {
    }
}