# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 프로젝트 파일만 복사 후 복원(캐시 활용)
COPY ProjectVG.Api/ProjectVG.Api.csproj ProjectVG.Api/
COPY ProjectVG.Application/ProjectVG.Application.csproj ProjectVG.Application/
COPY ProjectVG.Common/ProjectVG.Common.csproj ProjectVG.Common/
COPY ProjectVG.Domain/ProjectVG.Domain.csproj ProjectVG.Domain/
COPY ProjectVG.Infrastructure/ProjectVG.Infrastructure.csproj ProjectVG.Infrastructure/
RUN dotnet restore ProjectVG.Api/ProjectVG.Api.csproj

# 전체 소스 복사
COPY . .

# 빌드 및 퍼블리시
WORKDIR /src/ProjectVG.Api
RUN dotnet publish ProjectVG.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Kestrel 포트(Program.cs에서 7900 사용)
EXPOSE 7900

# 빌드 결과 복사
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProjectVG.Api.dll"]