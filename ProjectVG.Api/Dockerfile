# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 프로젝트 파일만 복사 후 복원(캐시 활용)
COPY ProjectVG.Api/ProjectVG.Api.csproj ProjectVG.Api/
COPY ProjectVG.Application/ProjectVG.Application.csproj ProjectVG.Application/
COPY ProjectVG.Common/ProjectVG.Common.csproj ProjectVG.Common/
COPY ProjectVG.Domain/ProjectVG.Domain.csproj ProjectVG.Domain/
COPY ProjectVG.Infrastructure/ProjectVG.Infrastructure.csproj ProjectVG.Infrastructure/

# NuGet 복원 (캐시 레이어)
RUN dotnet restore ProjectVG.Api/ProjectVG.Api.csproj

# 소스 코드 복사
COPY . .

# 빌드 및 퍼블리시
WORKDIR /src/ProjectVG.Api
RUN dotnet publish ProjectVG.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app

# Kestrel 포트(Program.cs에서 7900 사용)
EXPOSE 7900

# 빌드 결과 복사
COPY --from=build /app/publish .

# 헬스체크 추가
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:7900/health || exit 1

ENTRYPOINT ["dotnet", "ProjectVG.Api.dll"]