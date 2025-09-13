#!/bin/bash

# ProjectVG API 배포 스크립트 (실제 서버 환경용)
# GitHub Container Registry에서 이미지를 받아 서비스를 배포합니다.

set -e  # 오류 발생 시 스크립트 종료

echo "ProjectVG API 배포를 시작합니다..."

# 환경 변수 파일 확인
if [ ! -f ".env" ]; then
    echo "ERROR: .env 파일이 존재하지 않습니다."
    echo "CI/CD에서 PROD_APPLICATION_ENV가 올바르게 설정되지 않았습니다."
    exit 1
fi

if [ ! -f "deploy/docker-compose.prod.yml" ]; then
    echo "ERROR: deploy/docker-compose.prod.yml 파일이 존재하지 않습니다."
    echo "Repository에 docker-compose.prod.yml 파일을 확인하세요."
    exit 1
fi

# GitHub Container Registry에서 최신 이미지 풀 (Zero-downtime을 위해 먼저 실행)
echo "최신 이미지 다운로드 중..."
echo "GHCR 이미지 Pull: ghcr.io/projectvg/projectvgapi:latest"
docker-compose -f deploy/docker-compose.prod.yml pull projectvg-api

# 이미지 풀 후 확인
if ! docker images ghcr.io/projectvg/projectvgapi:latest | grep -q latest; then
    echo "ERROR: 이미지 풀에 실패했습니다. GitHub Container Registry 인증을 확인하세요."
    exit 1
fi

# 기존 API 컨테이너만 중지 및 제거 (Zero-downtime strategy)
echo "기존 API 컨테이너 교체 중..."
echo "현재 실행중인 컨테이너 확인:"
docker ps -f "name=projectvg-api"

docker-compose -f deploy/docker-compose.prod.yml stop projectvg-api || true
docker-compose -f deploy/docker-compose.prod.yml rm -f projectvg-api || true

# 새 이미지로 API 서비스만 시작
echo "새 이미지로 API 서비스 시작 중..."
docker-compose -f deploy/docker-compose.prod.yml up -d projectvg-api

# 서비스 상태 확인
# 빌드 후 불필요한 이미지 정리
echo "이전 이미지 정리 중..."
old_images=$(docker images ghcr.io/projectvg/projectvgapi -f "dangling=true" -q)
if [ ! -z "$old_images" ]; then
    docker rmi $old_images -f 2>/dev/null || true
    echo "이전 이미지 정리 완료"
else
    echo "정리할 이전 이미지 없음"
fi

# 컨테이너 상태 확인
echo "API 컨테이너 상태 확인 중..."
sleep 3

# API 컨테이너 상태만 출력
echo "API 컨테이너 상태:"
docker ps -f "name=projectvg-api"

# 헬스체크 (API가 응답하는지 확인)
echo "헬스체크 수행 중..."
max_retries=30
retry_count=0

while [ $retry_count -lt $max_retries ]; do
    # Docker 내장 헬스체크 먼저 확인
    health_status=$(docker inspect --format='{{.State.Health.Status}}' projectvg-api 2>/dev/null || echo "none")
    
    if [ "$health_status" = "healthy" ]; then
        echo "API 서버가 정상적으로 시작되었습니다. (Docker Health: $health_status)"
        break
    elif curl -f -s http://localhost:7910/health > /dev/null 2>&1; then
        echo "API 서버가 정상적으로 시작되었습니다. (HTTP Health Check)"
        break
    else
        echo "API 서버 시작 대기 중... ($((retry_count + 1))/$max_retries) [Docker: $health_status]"
        sleep 5
        retry_count=$((retry_count + 1))
    fi
done

if [ $retry_count -eq $max_retries ]; then
    echo "ERROR: API 서버가 시작되지 않았습니다. 로그를 확인하세요."
    echo "API 컨테이너 상태:"
    docker ps -a --filter name=projectvg-api
    echo "API 컨테이너 로그 (최근 50줄):"
    docker-compose -f deploy/docker-compose.prod.yml logs --tail=50 projectvg-api
    
    echo "롤백을 수행하시겠습니까? 이전 이미지로 복구할 수 있습니다."
    exit 1
fi

# 배포 완료 메시지
echo "배포가 성공적으로 완료되었습니다!"
echo "API 엔드포인트: http://localhost:7910"
echo "헬스체크: http://localhost:7910/health"

# 최근 API 로그 출력
echo "최근 API 로그 (20줄):"
docker-compose -f deploy/docker-compose.prod.yml logs --tail=20 projectvg-api

# 배포 완료 상태 요약
echo ""
echo "=== 배포 완료 상태 요약 ==="
echo "API 컨테이너:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" -f "name=projectvg-api"
echo "이미지 정보:"
docker images --format "table {{.Repository}}:{{.Tag}}\t{{.CreatedAt}}\t{{.Size}}" ghcr.io/projectvg/projectvgapi:latest