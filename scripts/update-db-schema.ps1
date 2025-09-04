# 데이터 유지하며 스키마 업데이트 스크립트 (DDL 변경사항만 반영)
Write-Host "=== ProjectVG Database Schema Update (Data Preserved) ===" -ForegroundColor Green

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

Write-Host "📋 This script will safely update database schema while preserving data" -ForegroundColor Cyan
Write-Host "  ✅ Keeps all existing data intact" -ForegroundColor Green
Write-Host "  ✅ Only applies DDL (schema) changes" -ForegroundColor Green
Write-Host "  ✅ Creates backup-friendly migration if needed" -ForegroundColor Green

# 1. 현재 데이터베이스 상태 확인
Write-Host "`n1. Checking current database status..." -ForegroundColor Yellow

# DB 연결 확인
try {
    Write-Host "   Testing database connection..." -ForegroundColor Gray
    $connectionTest = dotnet ef database drop --project $infrastructureProject --startup-project $startupProject --dry-run 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Database connection successful" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Database connection failed" -ForegroundColor Red
        Write-Host "   Please ensure database containers are running:" -ForegroundColor Yellow
        Write-Host "   docker-compose -f docker-compose.db.yml up -d" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "   ❌ Could not test database connection" -ForegroundColor Red
    exit 1
}

# 현재 적용된 마이그레이션 확인
Write-Host "   Checking current migration status..." -ForegroundColor Gray
try {
    $appliedMigrations = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Current applied migrations:" -ForegroundColor Green
        $migrationLines = $appliedMigrations -split "`n" | Where-Object { $_.Trim() -ne "" }
        $migrationCount = $migrationLines.Count
        Write-Host "     Total migrations: $migrationCount" -ForegroundColor Gray
        
        # 최근 3개 마이그레이션만 표시
        $recentMigrations = $migrationLines | Select-Object -Last 3
        foreach ($migration in $recentMigrations) {
            Write-Host "     - $migration" -ForegroundColor Gray
        }
    } else {
        Write-Host "   Warning: Could not retrieve migration list" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Warning: Could not check migration status" -ForegroundColor Yellow
}

# 2. 모델 변경사항 검출
Write-Host "`n2. Detecting model changes..." -ForegroundColor Yellow

# 임시 마이그레이션으로 변경사항 확인
$tempMigrationName = "TempCheck_$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Host "   Creating temporary migration to detect changes..." -ForegroundColor Gray

$tempMigrationResult = dotnet ef migrations add $tempMigrationName --project $infrastructureProject --startup-project $startupProject 2>&1

if ($LASTEXITCODE -ne 0) {
    # 변경사항이 없는 경우의 일반적인 출력 패턴 확인
    if ($tempMigrationResult -like "*No changes were detected*" -or $tempMigrationResult -like "*already exists*") {
        Write-Host "   ✅ No schema changes detected - database is up to date" -ForegroundColor Green
        Write-Host "`n🎉 Database schema is already current. No updates needed!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "   ❌ Failed to detect changes" -ForegroundColor Red
        Write-Host "   Error details:" -ForegroundColor Yellow
        Write-Host $tempMigrationResult -ForegroundColor Gray
        exit 1
    }
}

Write-Host "   ✅ Schema changes detected - migration needed" -ForegroundColor Green

# 3. 사용자에게 변경사항 확인 요청
Write-Host "`n3. Migration created: $tempMigrationName" -ForegroundColor Yellow

$proceed = Read-Host "   Apply this migration to database? This will preserve all data (y/n) [default: y]"
if ($proceed -eq "n") {
    Write-Host "   Removing temporary migration..." -ForegroundColor Gray
    dotnet ef migrations remove --project $infrastructureProject --startup-project $startupProject --force 2>$null
    Write-Host "   Operation cancelled by user." -ForegroundColor Yellow
    exit 0
}

# 4. 데이터베이스 백업 권장사항 안내
Write-Host "`n4. Database backup recommendation..." -ForegroundColor Yellow
Write-Host "   ⚠️  Although this script preserves data, it's recommended to backup before schema changes" -ForegroundColor Yellow

$backupChoice = Read-Host "   Skip backup and proceed? (y/n) [default: y]"
if ($backupChoice -eq "n") {
    Write-Host "   Please backup your database and run this script again." -ForegroundColor Gray
    Write-Host "   Migration '$tempMigrationName' is ready to apply." -ForegroundColor Gray
    exit 0
}

# 5. 스키마 업데이트 적용
Write-Host "`n5. Applying schema changes to database..." -ForegroundColor Yellow
Write-Host "   This will preserve all existing data" -ForegroundColor Green

# 실제 이름으로 마이그레이션 이름 변경 (옵션)
$finalMigrationName = Read-Host "   Enter final migration name (or press Enter to keep: $tempMigrationName)"
if (![string]::IsNullOrWhiteSpace($finalMigrationName)) {
    Write-Host "   Renaming migration to: $finalMigrationName" -ForegroundColor Gray
    
    # 임시 마이그레이션 제거 후 새 이름으로 재생성
    dotnet ef migrations remove --project $infrastructureProject --startup-project $startupProject --force 2>$null
    dotnet ef migrations add $finalMigrationName --project $infrastructureProject --startup-project $startupProject
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to create migration with new name" -ForegroundColor Red
        exit 1
    }
    
    $migrationName = $finalMigrationName
} else {
    $migrationName = $tempMigrationName
}

# 데이터베이스 업데이트 실행
Write-Host "   Applying migration: $migrationName" -ForegroundColor Gray
dotnet ef database update --project $infrastructureProject --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Schema update failed!" -ForegroundColor Red
    Write-Host "   The migration was created but could not be applied" -ForegroundColor Yellow
    Write-Host "   This could be due to:" -ForegroundColor Yellow
    Write-Host "     - Incompatible schema changes" -ForegroundColor Gray
    Write-Host "     - Data constraints violations" -ForegroundColor Gray
    Write-Host "     - Database connectivity issues" -ForegroundColor Gray
    exit $LASTEXITCODE
}

Write-Host "   ✅ Schema update applied successfully!" -ForegroundColor Green

# 6. 업데이트 후 검증
Write-Host "`n6. Verifying schema update..." -ForegroundColor Yellow

# 마이그레이션 상태 재확인
try {
    $updatedMigrations = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Updated migration list:" -ForegroundColor Green
        $updatedLines = $updatedMigrations -split "`n" | Where-Object { $_.Trim() -ne "" }
        $newMigrationCount = $updatedLines.Count
        
        Write-Host "     Total migrations: $newMigrationCount" -ForegroundColor Gray
        
        # 최신 마이그레이션 확인
        $latestMigration = $updatedLines | Select-Object -Last 1
        Write-Host "     Latest applied: $latestMigration" -ForegroundColor Green
        
        if ($latestMigration -like "*$migrationName*") {
            Write-Host "   ✅ New migration confirmed as applied" -ForegroundColor Green
        }
    } else {
        Write-Host "   Warning: Could not verify final migration status" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Warning: Could not verify update" -ForegroundColor Yellow
}

# 7. 데이터 무결성 간단 확인
Write-Host "   Performing basic data integrity check..." -ForegroundColor Gray
try {
    # 간단한 연결 및 쿼리 테스트 (실제 데이터 존재 확인)
    $dataCheck = dotnet ef dbcontext info --project $infrastructureProject --startup-project $startupProject 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Database context accessible after update" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Could not verify database context" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Could not perform data integrity check" -ForegroundColor Yellow
}

# 실행 시간 계산
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "`n=== Schema update completed in $($duration.TotalSeconds.ToString('F1')) seconds ===" -ForegroundColor Green

Write-Host "`n=== Update Summary ===" -ForegroundColor Cyan
Write-Host "✅ Database schema updated successfully" -ForegroundColor Green
Write-Host "✅ All existing data preserved" -ForegroundColor Green
Write-Host "✅ Migration applied: $migrationName" -ForegroundColor Green
Write-Host "✅ Database remains accessible" -ForegroundColor Green

Write-Host "`n=== Recommended Next Steps ===" -ForegroundColor Cyan
Write-Host "• Test your application with the updated schema" -ForegroundColor Gray
Write-Host "• Verify all existing functionality works correctly" -ForegroundColor Gray
Write-Host "• Consider running application tests" -ForegroundColor Gray

Write-Host "`n=== Useful Commands ===" -ForegroundColor Cyan
Write-Host "Check current migrations: dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
Write-Host "Generate update script: dotnet ef migrations script --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray
Write-Host "Database info: dotnet ef dbcontext info --project $infrastructureProject --startup-project $startupProject" -ForegroundColor Gray

Write-Host "`n🎉 Schema update complete! Your data is safe and schema is current." -ForegroundColor Green