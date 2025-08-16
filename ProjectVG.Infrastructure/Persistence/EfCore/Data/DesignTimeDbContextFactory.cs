using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProjectVGDbContext>
    {
        /// <summary>
        /// 디자인 타임에 사용되는 ProjectVGDbContext 인스턴스를 구성하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// - 현재 작업 디렉터리 기준으로 "../ProjectVG.Api" 폴더를 베이스 경로로 사용하여 설정을 로드합니다.
        /// - 해당 경로의 "appsettings.json"(optional: true)과 환경 변수를 합쳐 구성하며, 연결 문자열 이름은 "DefaultConnection"을 사용합니다.
        /// - 반환되는 DbContext는 SqlServer 제공자로 구성됩니다.
        /// - 설정에 "DefaultConnection"이 없거나 잘못되면 DbContext 생성/연결 시 예외가 발생할 수 있습니다.
        /// </remarks>
        /// <param name="args">IDesignTimeDbContextFactory 인터페이스 시그니처를 위한 매개변수로 사용되며, 이 구현에서는 사용되지 않습니다.</param>
        /// <returns>SqlServer로 구성된 ProjectVGDbContext의 새 인스턴스.</returns>
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