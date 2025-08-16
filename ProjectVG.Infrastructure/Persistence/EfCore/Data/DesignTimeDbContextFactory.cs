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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<ProjectVGDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ProjectVGDbContext(optionsBuilder.Options);
        }
    }
} 