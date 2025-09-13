# 완전 DB 초기화 스크립트 (Docker 볼륨 + 마이그레이션 포함)
Write-Host "=== ProjectVG Complete Database Reset ===" -ForegroundColor Red
Write-Host "⚠️  WARNING: This will completely destroy all database and Redis data!" -ForegroundColor Yellow

# 시작 시간 기록
$startTime = Get-Date

# 프로젝트 경로 확인
$infrastructureProject = "./ProjectVG.Infrastructure"
$startupProject = "./ProjectVG.Api"
$dbComposeFile = "docker-compose.db.yml"

# 파일 존재 확인
if (!(Test-Path $infrastructureProject)) {
    Write-Host "Error: Infrastructure project not found at $infrastructureProject" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $startupProject)) {
    Write-Host "Error: API project not found at $startupProject" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $dbComposeFile)) {
    Write-Host "Error: Docker compose file not found: $dbComposeFile" -ForegroundColor Red
    exit 1
}

# 사용자 확인 프롬프트 (강력한 안전장치)
Write-Host "`n📋 This script will perform COMPLETE database reset:" -ForegroundColor Cyan
Write-Host "  1. Stop all database containers" -ForegroundColor Gray
Write-Host "  2. Remove Docker volumes (mssql_data, redis_data)" -ForegroundColor Gray
Write-Host "  3. Remove Docker networks" -ForegroundColor Gray
Write-Host "  4. Recreate containers with fresh volumes" -ForegroundColor Gray
Write-Host "  5. Run database migrations" -ForegroundColor Gray
Write-Host "  6. Verify system health" -ForegroundColor Gray

Write-Host "`n💥 THIS WILL DELETE ALL DATABASE AND REDIS DATA PERMANENTLY!" -ForegroundColor Red

$confirmation1 = Read-Host "`n❓ Type 'DELETE ALL DATA' to confirm complete reset"
if ($confirmation1 -ne "DELETE ALL DATA") {
    Write-Host "Operation cancelled by user." -ForegroundColor Yellow
    exit 0
}

$confirmation2 = Read-Host "❓ Are you absolutely sure? Type 'YES I UNDERSTAND' to proceed"
if ($confirmation2 -ne "YES I UNDERSTAND") {
    Write-Host "Operation cancelled by user." -ForegroundColor Yellow
    exit 0
}

Write-Host "`n🔥 Starting complete database infrastructure reset..." -ForegroundColor Red

# 1. 현재 상태 확인 및 로그
Write-Host "1. Checking current system state..." -ForegroundColor Yellow

try {
    Write-Host "   Current containers:" -ForegroundColor Gray
    docker-compose -f $dbComposeFile ps 2>$null
    
    Write-Host "   Current volumes:" -ForegroundColor Gray
    docker volume ls | Select-String "projectvg\|mssql\|redis" 2>$null
    
    Write-Host "   Current networks:" -ForegroundColor Gray
    docker network ls | Select-String "projectvg" 2>$null
} catch {
    Write-Host "   Could not check current state" -ForegroundColor Yellow
}

# 2. 컨테이너 중지 및 제거 (볼륨 포함)
Write-Host "2. Stopping and removing all database containers..." -ForegroundColor Yellow

docker-compose -f $dbComposeFile down -v --remove-orphans

if ($LASTEXITCODE -ne 0) {
    Write-Host "   Warning: Some containers may not have been running" -ForegroundColor Yellow
} else {
    Write-Host "   Containers stopped and removed successfully" -ForegroundColor Green
}

# 3. 관련 볼륨 강제 삭제 (혹시 남아있을 수 있는 볼륨들)
Write-Host "3. Force removing all related Docker volumes..." -ForegroundColor Yellow

$volumesToRemove = @(
    "mainapiserver_mssql_data",
    "projectvg_mssql_data",
    "mssql_data",
    "mainapiserver_redis_data", 
    "projectvg_redis_data",
    "redis_data",
    "projectvg-db-data",
    "projectvg-redis-data"
)

$removedVolumes = 0
foreach ($volume in $volumesToRemove) {
    $result = docker volume rm $volume 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Removed volume: $volume" -ForegroundColor Green
        $removedVolumes++
    }
}

if ($removedVolumes -eq 0) {
    Write-Host "   No volumes found to remove" -ForegroundColor Gray
} else {
    Write-Host "   Removed $removedVolumes volumes" -ForegroundColor Green
}

# 4. 관련 네트워크 정리
Write-Host "4. Cleaning up Docker networks..." -ForegroundColor Yellow

$networksToRemove = @(
    "projectvg-external-db",
    "mainapiserver_projectvg-external-db"
)

$removedNetworks = 0
foreach ($network in $networksToRemove) {
    $result = docker network rm $network 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Removed network: $network" -ForegroundColor Green
        $removedNetworks++
    }
}

# 5. 필요한 네트워크 재생성
Write-Host "5. Recreating external networks..." -ForegroundColor Yellow
docker network create projectvg-external-db 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   External network created successfully" -ForegroundColor Green
} else {
    Write-Host "   Network may already exist or creation failed" -ForegroundColor Yellow
}

# 6. 새로운 DB 및 Redis 컨테이너 시작
Write-Host "6. Starting fresh database and Redis containers..." -ForegroundColor Yellow

docker-compose -f $dbComposeFile up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start database containers!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "   Containers started successfully" -ForegroundColor Green

# 7. 컨테이너 상태 확인 (헬스체크 대기)
Write-Host "7. Waiting for containers to be healthy..." -ForegroundColor Yellow

$maxWaitTime = 120 # 2분
$waitTime = 0
$healthyContainers = 0

do {
    Start-Sleep -Seconds 5
    $waitTime += 5
    
    $dbHealth = docker inspect --format='{{.State.Health.Status}}' projectvg-db 2>$null
    $redisHealth = docker inspect --format='{{.State.Health.Status}}' projectvg-redis 2>$null
    
    $healthyContainers = 0
    if ($dbHealth -eq "healthy") { $healthyContainers++ }
    if ($redisHealth -eq "healthy") { $healthyContainers++ }
    
    Write-Host "   Waiting... ($waitTime/$maxWaitTime seconds) - Healthy: $healthyContainers/2" -ForegroundColor Gray
    
    if ($healthyContainers -eq 2) {
        Write-Host "   All containers are healthy!" -ForegroundColor Green
        break
    }
    
} while ($waitTime -lt $maxWaitTime)

if ($healthyContainers -lt 2) {
    Write-Host "   Warning: Not all containers are healthy, but proceeding..." -ForegroundColor Yellow
    docker-compose -f $dbComposeFile ps
}

# 8. 데이터베이스 마이그레이션 실행
Write-Host "8. Running database migrations..." -ForegroundColor Yellow

# 추가 대기 시간 (DB가 완전히 준비될 때까지)
Write-Host "   Waiting additional 10 seconds for database to be fully ready..." -ForegroundColor Gray
Start-Sleep -Seconds 10

dotnet ef database update --project $infrastructureProject --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Database migration failed!" -ForegroundColor Red
    Write-Host "Database containers are running, but migration could not be applied." -ForegroundColor Yellow
    Write-Host "You may need to run migrations manually:" -ForegroundColor Gray
    Write-Host "  dotnet ef database update --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
    exit $LASTEXITCODE
}

Write-Host "   Database migrations applied successfully!" -ForegroundColor Green

# 9. 최종 시스템 상태 확인
Write-Host "9. Final system verification..." -ForegroundColor Yellow

Write-Host "   Container status:" -ForegroundColor Gray
docker-compose -f $dbComposeFile ps

Write-Host "   Volume status:" -ForegroundColor Gray
docker volume ls | Select-String "mssql\|redis"

Write-Host "   Migration status:" -ForegroundColor Gray
try {
    $migrations = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host $migrations -ForegroundColor Gray
    } else {
        Write-Host "   Could not verify migrations" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Could not verify migrations" -ForegroundColor Yellow
}

# 실행 시간 계산
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "`n=== Complete database reset finished in $($duration.TotalSeconds.ToString('F1')) seconds ===" -ForegroundColor Green

Write-Host "`n=== Reset Summary ===" -ForegroundColor Cyan
Write-Host "✅ All containers stopped and removed" -ForegroundColor Green
Write-Host "✅ All Docker volumes destroyed and recreated" -ForegroundColor Green
Write-Host "✅ Networks cleaned and recreated" -ForegroundColor Green
Write-Host "✅ Fresh database and Redis containers started" -ForegroundColor Green
Write-Host "✅ Database migrations applied" -ForegroundColor Green

Write-Host "`n=== Connection Information ===" -ForegroundColor Cyan
Write-Host "Database: localhost:1433" -ForegroundColor White
Write-Host "Redis: localhost:6380" -ForegroundColor White
Write-Host "Database Name: ProjectVG" -ForegroundColor White
Write-Host "SA Password: ProjectVG123!" -ForegroundColor White

Write-Host "`n=== Useful Commands ===" -ForegroundColor Cyan
Write-Host "Check logs: docker-compose -f $dbComposeFile logs" -ForegroundColor Gray
Write-Host "Follow logs: docker-compose -f $dbComposeFile logs -f" -ForegroundColor Gray
Write-Host "Container status: docker-compose -f $dbComposeFile ps" -ForegroundColor Gray

Write-Host "`n🎉 Complete database infrastructure reset successful!" -ForegroundColor Green
Write-Host "   Your database environment is now completely fresh and ready." -ForegroundColor White