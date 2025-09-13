# EF Core 마이그레이션 스크립트 (일반 마이그레이션 - 기존 마이그레이션 유지)
Write-Host "=== ProjectVG Database Migration ===" -ForegroundColor Green

# 시작 시간 기록
$startTime = Get-Date

# 프로젝트 경로 확인
$infrastructureProject = "./ProjectVG.Infrastructure"
$startupProject = "./ProjectVG.Api"

if (!(Test-Path $infrastructureProject)) {
    Write-Host "Error: Infrastructure project not found at $infrastructureProject" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $startupProject)) {
    Write-Host "Error: API project not found at $startupProject" -ForegroundColor Red
    exit 1
}

Write-Host "1. Checking current migration status..." -ForegroundColor Yellow
Write-Host "   Infrastructure Project: $infrastructureProject" -ForegroundColor Gray
Write-Host "   Startup Project: $startupProject" -ForegroundColor Gray

# 현재 마이그레이션 상태 확인
try {
    $migrationsList = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Existing migrations found" -ForegroundColor Green
        Write-Host $migrationsList -ForegroundColor Gray
    } else {
        Write-Host "   No existing migrations or connection issue" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Could not check existing migrations" -ForegroundColor Yellow
}

# 마이그레이션 이름 생성 (타임스탬프 기반)
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$migrationName = Read-Host "Enter migration name (or press Enter for auto-generated name)"

if ([string]::IsNullOrWhiteSpace($migrationName)) {
    $migrationName = "Migration_$timestamp"
    Write-Host "   Using auto-generated name: $migrationName" -ForegroundColor Gray
}

# 2. 마이그레이션 생성
Write-Host "2. Creating new migration: $migrationName..." -ForegroundColor Yellow
dotnet ef migrations add $migrationName --project $infrastructureProject --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration creation failed!" -ForegroundColor Red
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "  - Build errors in the project" -ForegroundColor Gray
    Write-Host "  - Database connection issues" -ForegroundColor Gray
    Write-Host "  - No model changes detected" -ForegroundColor Gray
    exit $LASTEXITCODE
}

Write-Host "   Migration created successfully!" -ForegroundColor Green

# 3. 사용자에게 DB 업데이트 여부 확인
$updateChoice = Read-Host "Apply migration to database now? (y/n) [default: y]"
if ([string]::IsNullOrWhiteSpace($updateChoice) -or $updateChoice.ToLower() -eq "y") {
    
    Write-Host "3. Applying migration to database..." -ForegroundColor Yellow
    
    # DB 연결 확인
    Write-Host "   Checking database connection..." -ForegroundColor Gray
    $dbCheck = dotnet ef database drop --project $infrastructureProject --startup-project $startupProject --dry-run 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Warning: Could not verify database connection" -ForegroundColor Yellow
    } else {
        Write-Host "   Database connection verified" -ForegroundColor Green
    }
    
    # DB 업데이트 실행
    dotnet ef database update --project $infrastructureProject --startup-project $startupProject

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Database update failed!" -ForegroundColor Red
        Write-Host "The migration was created but not applied." -ForegroundColor Yellow
        Write-Host "You can apply it later using:" -ForegroundColor Gray
        Write-Host "  dotnet ef database update --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
        exit $LASTEXITCODE
    }

    Write-Host "   Database updated successfully!" -ForegroundColor Green
    
    # 최종 상태 확인
    Write-Host "4. Verifying final migration status..." -ForegroundColor Yellow
    try {
        $finalMigrations = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Current applied migrations:" -ForegroundColor Green
            Write-Host $finalMigrations -ForegroundColor Gray
        }
    } catch {
        Write-Host "   Could not verify final status" -ForegroundColor Yellow
    }

} else {
    Write-Host "3. Skipping database update" -ForegroundColor Yellow
    Write-Host "   Migration created but not applied to database" -ForegroundColor Gray
    Write-Host "   To apply later, run:" -ForegroundColor Gray
    Write-Host "   dotnet ef database update --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
}

# 실행 시간 계산
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "`n=== Migration completed in $($duration.TotalSeconds.ToString('F1')) seconds ===" -ForegroundColor Green

Write-Host "`n=== Migration Commands Reference ===" -ForegroundColor Cyan
Write-Host "List migrations: dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
Write-Host "Remove last migration: dotnet ef migrations remove --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
Write-Host "Update database: dotnet ef database update --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
Write-Host "Generate SQL script: dotnet ef migrations script --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray

Write-Host "`n✅ Migration process complete!" -ForegroundColor Green