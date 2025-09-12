# 배포 가이드

ProjectVG API 서버의 배포 방법에 대한 가이드입니다.

## 🚀 자동 배포 (CI/CD)

### GitHub Secrets 설정 (최초 1회)

배포하기 전에 GitHub Secrets에 환경변수를 설정해야 합니다.

```bash
# Secrets 생성 헬퍼 스크립트 실행
./scripts/generate-secrets.sh

# 또는 Windows에서
.\scripts\Generate-Secrets.ps1
```

**필요한 GitHub Secrets**:
- `PROD_APPLICATION_ENV`: .env 파일의 base64 인코딩
- `GHCR_TOKEN`: GitHub Container Registry 접근 토큰

### Release 브랜치 자동 배포
`release` 브랜치에 푸시하면 자동으로 배포가 진행됩니다.

1. **빌드 & 테스트**: Ubuntu에서 빌드 및 테스트 실행
2. **Docker 이미지**: GHCR에 이미지 푸시
3. **환경변수 복원**: GitHub Secrets에서 base64 디코딩
4. **배포**: self-hosted runner에서 자동 배포

```bash
git checkout release
git merge develop
git push origin release
```

### 배포 프로세스
- **빌드**: `dotnet build` + `dotnet test`
- **Docker 이미지**: `ghcr.io/projectvg/projectvgapi:latest`
- **환경변수**: GitHub Secrets → base64 decode → `.env`
- **Docker Compose**: Repository의 `docker-compose.prod.yml` 사용
- **배포 실행**: CI/CD 파이프라인에서 직접 실행
- **헬스체크**: API 서버 응답 확인 (30회 재시도)

## 🛠️ 수동 배포

### 프로덕션 환경 수동 배포

```bash
# 1. 최신 코드 가져오기
git pull origin release

# 2. 환경 설정 파일 준비
cp env.prod.example .env
cp docker-compose.prod.yml docker-compose.yml

# 실제 환경변수 값으로 수정
vi .env

# 3. 배포 스크립트 실행
chmod +x deploy.sh
./deploy.sh
```

### 개발 환경 배포

```bash
# 개발용 배포 스크립트 사용
chmod +x deploy-dev.sh
./deploy-dev.sh
```

## 📋 배포 스크립트 상세

### `deploy.sh` (프로덕션)
- **용도**: 프로덕션 환경 배포
- **이미지**: GHCR에서 최신 이미지 풀
- **헬스체크**: 30회 재시도 (최대 2.5분)
- **로그**: 배포 후 최근 로그 20줄 출력

### `deploy-dev.sh` (개발)
- **용도**: 로컬 개발 환경 배포
- **이미지**: 로컬에서 빌드
- **환경 파일**: `env.example`에서 자동 생성
- **포트**: 7910 (API), Swagger UI 포함

## 🔧 배포 스크립트 수정

배포 로직을 수정하려면 repository의 `deploy.sh` 파일을 편집하세요:

```bash
# 배포 스크립트 편집
vi deploy.sh

# 변경사항 커밋
git add deploy.sh
git commit -m "배포 스크립트 업데이트"
git push origin develop
```

## 📊 모니터링 & 확인

### 배포 상태 확인
```bash
# 컨테이너 상태
docker-compose ps

# 로그 확인
docker-compose logs -f projectvg-api

# 헬스체크
curl http://localhost:7910/health
```

### 주요 엔드포인트
- **API**: http://localhost:7910
- **헬스체크**: http://localhost:7910/health
- **Swagger**: http://localhost:7910/swagger (개발 환경)

## 🚨 트러블슈팅

### 배포 실패 시
1. **로그 확인**: `docker-compose logs --tail=50`
2. **컨테이너 상태**: `docker-compose ps`
3. **이미지 확인**: `docker images | grep projectvg`
4. **포트 충돌**: `netstat -tlnp | grep 7910`

### 롤백 방법
```bash
# 이전 이미지로 롤백 (태그 사용 시)
docker-compose down
docker-compose pull  # 또는 특정 태그 지정
docker-compose up -d
```

## 🔐 보안 고려사항

### 환경 변수
- `.env` 파일은 GitHub Secrets로 관리
- 민감한 정보는 평문으로 저장하지 않음
- 프로덕션과 개발 환경 분리

### Docker 이미지
- GHCR을 통한 이미지 배포
- 정기적인 base 이미지 업데이트
- 보안 스캔 고려

## 📝 배포 히스토리

배포 이력은 GitHub Actions에서 확인할 수 있습니다:
- **Repository** → **Actions** → **Release CI/CD**