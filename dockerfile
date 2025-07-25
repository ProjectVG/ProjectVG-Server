# =========================
# 1) Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 솔루션/프로젝트 파일만 먼저 복사(캐시 극대화)
COPY ProjectVG.sln ./
COPY ProjectVG.Api/ProjectVG.Api.csproj ProjectVG.Api/
COPY ProjectVG.Application/ProjectVG.Application.csproj ProjectVG.Application/
COPY ProjectVG.Domain/ProjectVG.Domain.csproj ProjectVG.Domain/
COPY ProjectVG.Infrastructure/ProjectVG.Infrastructure.csproj ProjectVG.Infrastructure/
COPY ProjectVG.Common/ProjectVG.Common.csproj ProjectVG.Common/

RUN dotnet restore ProjectVG.Api/ProjectVG.Api.csproj

# 나머지 소스 복사
COPY . .

WORKDIR /src/ProjectVG.Api
RUN dotnet publish ProjectVG.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# =========================
# 2) Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 보안상 비루트 사용자 생성
RUN adduser --disabled-password --gecos "" app && chown -R app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

EXPOSE 8080

# Healthcheck (선택적으로 /health 엔드포인트 만들 것)
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "ProjectVG.Api.dll"]
