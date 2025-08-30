using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProjectVGDbContext>
    {
        public ProjectVGDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../ProjectVG.Api"));
            
            // Load .env file from the root directory
            var envPath = Path.Combine(basePath, ".env");
            if (File.Exists(envPath))
            {
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#") && line.Contains("="))
                    {
                        var parts = line.Split('=', 2);
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Try to get connection string from environment variable or configuration
            var connectionString = configuration["DB_CONNECTION_STRING"] 
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost,1433;Database=ProjectVG;User Id=sa;Password=ProjectVG123!;TrustServerCertificate=true;MultipleActiveResultSets=true";

            var optionsBuilder = new DbContextOptionsBuilder<ProjectVGDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ProjectVGDbContext(optionsBuilder.Options);
        }
    }
} 