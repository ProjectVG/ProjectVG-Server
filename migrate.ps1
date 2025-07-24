# 0. 기존 DB 드롭
dotnet ef database drop --project "./ProjectVG.Infrastructure" --startup-project "./ProjectVG.Api" --force --yes

if ($LASTEXITCODE -ne 0) {
    Write-Error "DB 드롭 실패"
    exit $LASTEXITCODE
}

# 1. 마이그레이션 생성
dotnet ef migrations add InitialCreate --project "./ProjectVG.Infrastructure" --startup-project "./ProjectVG.Api"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration 생성 실패"
    exit $LASTEXITCODE
}

# 2. DB 업데이트
dotnet ef database update --project "./ProjectVG.Infrastructure" --startup-project "./ProjectVG.Api"

if ($LASTEXITCODE -ne 0) {
    Write-Error "DB 업데이트 실패"
    exit $LASTEXITCODE
}

Write-Host "✅ DB 초기화 및 마이그레이션 완료"
