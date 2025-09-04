# EF Core 완전 초기화 마이그레이션 스크립트 (모든 기존 마이그레이션 제거 후 새로 생성)
Write-Host "=== ProjectVG Database Complete Reset Migration ===" -ForegroundColor Red
Write-Host "⚠️  WARNING: This will remove ALL existing migrations and data!" -ForegroundColor Yellow

# 시작 시간 기록
$startTime = Get-Date

# 프로젝트 경로 확인
$infrastructureProject = "./ProjectVG.Infrastructure"
$startupProject = "./ProjectVG.Api"
$migrationsPath = "./ProjectVG.Infrastructure/Migrations"

if (!(Test-Path $infrastructureProject)) {
    Write-Host "Error: Infrastructure project not found at $infrastructureProject" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $startupProject)) {
    Write-Host "Error: API project not found at $startupProject" -ForegroundColor Red
    exit 1
}

# 사용자 확인 프롬프트 (안전장치)
Write-Host "`n📋 This script will:" -ForegroundColor Cyan
Write-Host "  1. Drop the existing database" -ForegroundColor Gray
Write-Host "  2. Remove all migration files" -ForegroundColor Gray
Write-Host "  3. Create a new InitialCreate migration" -ForegroundColor Gray
Write-Host "  4. Apply the new migration" -ForegroundColor Gray

$confirmation = Read-Host "`n❓ Are you sure you want to proceed? This will DELETE ALL DATA! (type 'RESET' to confirm)"

if ($confirmation -ne "RESET") {
    Write-Host "Operation cancelled by user." -ForegroundColor Yellow
    exit 0
}

Write-Host "`n🔥 Starting complete database reset..." -ForegroundColor Red

# 1. 기존 데이터베이스 드롭
Write-Host "1. Dropping existing database..." -ForegroundColor Yellow
try {
    dotnet ef database drop --project $infrastructureProject --startup-project $startupProject --force

    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Database drop failed (may not exist)" -ForegroundColor Yellow
    } else {
        Write-Host "   Database dropped successfully" -ForegroundColor Green
    }
} catch {
    Write-Host "   Database drop failed (may not exist)" -ForegroundColor Yellow
}

# 2. 기존 마이그레이션 파일들 삭제
Write-Host "2. Removing all existing migration files..." -ForegroundColor Yellow

if (Test-Path $migrationsPath) {
    $migrationFiles = Get-ChildItem -Path $migrationsPath -Filter "*.cs" | Where-Object { $_.Name -ne "ProjectVGDbContextModelSnapshot.cs" }
    $designerFiles = Get-ChildItem -Path $migrationsPath -Filter "*.Designer.cs"
    
    $totalFiles = $migrationFiles.Count + $designerFiles.Count
    
    if ($totalFiles -gt 0) {
        Write-Host "   Found $totalFiles migration files to remove:" -ForegroundColor Gray
        
        foreach ($file in $migrationFiles) {
            Write-Host "   Removing: $($file.Name)" -ForegroundColor Gray
            Remove-Item $file.FullName -Force
        }
        
        foreach ($file in $designerFiles) {
            Write-Host "   Removing: $($file.Name)" -ForegroundColor Gray
            Remove-Item $file.FullName -Force
        }
        
        Write-Host "   All migration files removed" -ForegroundColor Green
    } else {
        Write-Host "   No migration files found to remove" -ForegroundColor Gray
    }
    
    # ModelSnapshot 파일도 삭제 (새로 생성되도록)
    $snapshotFile = Join-Path $migrationsPath "ProjectVGDbContextModelSnapshot.cs"
    if (Test-Path $snapshotFile) {
        Write-Host "   Removing model snapshot file" -ForegroundColor Gray
        Remove-Item $snapshotFile -Force
    }
} else {
    Write-Host "   Migrations folder not found: $migrationsPath" -ForegroundColor Gray
}

# 3. 새로운 InitialCreate 마이그레이션 생성
Write-Host "3. Creating new InitialCreate migration..." -ForegroundColor Yellow

# 타임스탬프를 포함한 고유한 마이그레이션 이름
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$initialMigrationName = "InitialCreate_$timestamp"

dotnet ef migrations add $initialMigrationName --project $infrastructureProject --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create initial migration!" -ForegroundColor Red
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "  - Build errors in the project" -ForegroundColor Gray
    Write-Host "  - DbContext configuration issues" -ForegroundColor Gray
    exit $LASTEXITCODE
}

Write-Host "   Initial migration created successfully: $initialMigrationName" -ForegroundColor Green

# 4. 새 마이그레이션 적용
Write-Host "4. Applying new migration to database..." -ForegroundColor Yellow

dotnet ef database update --project $infrastructureProject --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to apply migration to database!" -ForegroundColor Red
    Write-Host "The migration was created but could not be applied." -ForegroundColor Yellow
    exit $LASTEXITCODE
}

Write-Host "   Migration applied successfully!" -ForegroundColor Green

# 5. 최종 확인
Write-Host "5. Verifying reset completion..." -ForegroundColor Yellow

# 마이그레이션 목록 확인
try {
    $newMigrations = dotnet ef migrations list --project $infrastructureProject --startup-project $startupProject --no-build 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Current migrations:" -ForegroundColor Green
        Write-Host $newMigrations -ForegroundColor Gray
    }
} catch {
    Write-Host "   Could not verify migration status" -ForegroundColor Yellow
}

# 생성된 파일 확인
if (Test-Path $migrationsPath) {
    $newFiles = Get-ChildItem -Path $migrationsPath -Filter "*.cs"
    Write-Host "   Generated files: $($newFiles.Count)" -ForegroundColor Green
    foreach ($file in $newFiles) {
        Write-Host "     - $($file.Name)" -ForegroundColor Gray
    }
}

# 실행 시간 계산
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "`n=== Complete reset completed in $($duration.TotalSeconds.ToString('F1')) seconds ===" -ForegroundColor Green

Write-Host "`n=== Reset Summary ===" -ForegroundColor Cyan
Write-Host "✅ Database dropped and recreated" -ForegroundColor Green
Write-Host "✅ All old migrations removed" -ForegroundColor Green
Write-Host "✅ New InitialCreate migration created: $initialMigrationName" -ForegroundColor Green
Write-Host "✅ Migration applied to database" -ForegroundColor Green

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "• Database is now clean with latest schema" -ForegroundColor Gray
Write-Host "• You can start fresh development from this point" -ForegroundColor Gray
Write-Host "• Future migrations will be incremental from this baseline" -ForegroundColor Gray

Write-Host "`n🎉 Database reset complete! Ready for fresh start." -ForegroundColor Green