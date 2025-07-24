# migrate.ps1

# 1. 마이그레이션 생성
dotnet ef migrations add InitialCreate --project "../ProjectVG.Infrastructure" --startup-project "."

if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration 생성 실패"
    exit $LASTEXITCODE
}

# 2. DB 업데이트
dotnet ef database update --project "../ProjectVG.Infrastructure" --startup-project "."

if ($LASTEXITCODE -ne 0) {
    Write-Error "DB 업데이트 실패"
    exit $LASTEXITCODE
}

Write-Host "마이그레이션 및 DB 업데이트 완료"
