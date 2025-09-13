# ProjectVG API 배포 스크립트 (PowerShell)
# Windows 환경에서 사용하는 배포 스크립트입니다.

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "prod")]
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-DockerCompose {
    try {
        docker-compose --version | Out-Null
        return $true
    }
    catch {
        Write-ColorOutput "❌ Docker Compose가 설치되어 있지 않습니다." "Red"
        return $false
    }
}

function Test-Docker {
    try {
        docker --version | Out-Null
        return $true
    }
    catch {
        Write-ColorOutput "❌ Docker가 설치되어 있지 않습니다." "Red"
        return $false
    }
}

function Wait-ForApiHealth {
    param([int]$MaxRetries = 30, [string]$Url = "http://localhost:7910/health")
    
    Write-ColorOutput "🏥 헬스체크 수행 중..." "Yellow"
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-ColorOutput "✅ API 서버가 정상적으로 시작되었습니다." "Green"
                return $true
            }
        }
        catch {
            Write-ColorOutput "⏳ API 서버 시작 대기 중... ($i/$MaxRetries)" "Yellow"
            Start-Sleep -Seconds 5
        }
    }
    
    Write-ColorOutput "❌ API 서버가 시작되지 않았습니다. 로그를 확인하세요." "Red"
    return $false
}

# 메인 배포 로직
Write-ColorOutput "🚀 ProjectVG API 배포를 시작합니다... (환경: $Environment)" "Cyan"

# GitHub Secrets에서 base64 인코딩된 파일들 처리
Write-ColorOutput "🔐 환경 설정 파일 준비 중..." "Yellow"

# PROD_APPLICATION_ENV가 설정되어 있으면 .env 파일 생성
if ($env:PROD_APPLICATION_ENV) {
    Write-ColorOutput "📝 .env 파일을 GitHub Secrets에서 생성 중..." "Yellow"
    try {
        $envContent = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($env:PROD_APPLICATION_ENV))
        Set-Content -Path ".env" -Value $envContent -Encoding UTF8
        Write-ColorOutput "✅ .env 파일 생성 완료" "Green"
    }
    catch {
        Write-ColorOutput "❌ .env 파일 디코딩 실패: $($_.Exception.Message)" "Red"
        exit 1
    }
}

# PROD_DOCKER_COMPOSE가 설정되어 있으면 docker-compose.yml 파일 생성
if ($env:PROD_DOCKER_COMPOSE) {
    Write-ColorOutput "📝 docker-compose.yml 파일을 GitHub Secrets에서 생성 중..." "Yellow"
    try {
        $composeContent = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($env:PROD_DOCKER_COMPOSE))
        Set-Content -Path "docker-compose.yml" -Value $composeContent -Encoding UTF8
        Write-ColorOutput "✅ docker-compose.yml 파일 생성 완료" "Green"
    }
    catch {
        Write-ColorOutput "❌ docker-compose.yml 파일 디코딩 실패: $($_.Exception.Message)" "Red"
        exit 1
    }
}

# 선행 조건 확인
if (-not (Test-Docker)) { exit 1 }
if (-not (Test-DockerCompose)) { exit 1 }

# Docker Compose 파일 확인
if (-not (Test-Path "docker-compose.yml")) {
    Write-ColorOutput "❌ docker-compose.yml 파일이 존재하지 않습니다." "Red"
    exit 1
}

# 환경별 설정
if ($Environment -eq "dev") {
    # 개발 환경: .env 파일이 없으면 env.example에서 복사
    if (-not (Test-Path ".env") -and (Test-Path "env.example")) {
        Write-ColorOutput "📝 .env 파일을 env.example에서 생성합니다..." "Yellow"
        Copy-Item "env.example" ".env"
    }
} else {
    # 프로덕션 환경: .env 파일 필수
    if (-not (Test-Path ".env")) {
        Write-ColorOutput "❌ .env 파일이 존재하지 않습니다." "Red"
        exit 1
    }
}

# 기존 컨테이너 중지
Write-ColorOutput "📦 기존 컨테이너 중지 중..." "Yellow"
try {
    docker-compose down --remove-orphans
} catch {
    Write-ColorOutput "⚠️ 기존 컨테이너 중지 중 오류 발생 (계속 진행)" "Yellow"
}

if ($Environment -eq "dev") {
    # 개발 환경: 로컬 빌드
    Write-ColorOutput "🔨 Docker 이미지 빌드 중..." "Yellow"
    docker-compose build
} else {
    # 프로덕션 환경: 최신 이미지 풀
    Write-ColorOutput "⬇️ 최신 이미지 다운로드 중..." "Yellow"
    docker-compose pull
}

# 서비스 시작
Write-ColorOutput "🔄 서비스 시작 중..." "Yellow"
docker-compose up -d

# 잠시 대기
Start-Sleep -Seconds 10

# 컨테이너 상태 확인
Write-ColorOutput "📊 컨테이너 상태:" "Cyan"
docker-compose ps

# 헬스체크
if (Wait-ForApiHealth) {
    Write-ColorOutput "🎉 배포가 성공적으로 완료되었습니다!" "Green"
    Write-ColorOutput "📍 API 엔드포인트: http://localhost:7910" "Green"
    Write-ColorOutput "🏥 헬스체크: http://localhost:7910/health" "Green"
    
    if ($Environment -eq "dev") {
        Write-ColorOutput "📋 API 문서: http://localhost:7910/swagger" "Green"
    }
} else {
    Write-ColorOutput "🔍 API 컨테이너 로그:" "Red"
    docker-compose logs --tail=50 projectvg-api
    exit 1
}

# 최근 로그 출력
Write-ColorOutput "📋 최근 로그 (마지막 20줄):" "Cyan"
docker-compose logs --tail=20