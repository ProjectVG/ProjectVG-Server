# ProjectVG API Server

JWT + OAuth2 + WebSocket 인증 시스템을 포함한 ASP.NET Core API 서버입니다.

## 🚀 빠른 시작

### 1. 전체 개발 환경 설정 (최초 1회)
```powershell
# DB와 Redis를 포함한 전체 환경 설정
.\scripts\dev-setup.ps1
```

### 2. API만 빠르게 재빌드 (개발 중)
```powershell
# API 코드 변경 후 빠른 재빌드
.\scripts\start-api.ps1
```

### 3. 서비스 중지
```powershell
# 모든 서비스 중지
.\scripts\stop-all.ps1
```

## 📁 프로젝트 구조

```
ProjectVG Server/
├── ProjectVG.Api/           # API 레이어
├── ProjectVG.Application/   # 애플리케이션 레이어
├── ProjectVG.Domain/        # 도메인 레이어
├── ProjectVG.Infrastructure/# 인프라 레이어
├── ProjectVG.Common/        # 공통 라이브러리
├── ProjectVG.Tests/         # 테스트 프로젝트
├── scripts/                 # Docker 스크립트
├── test-clients/           # 테스트 클라이언트
└── docker-compose.yml      # API 서비스 설정
```

## 🔧 Docker 최적화 설정

### 분리된 서비스 구조
- **API 서비스**: `docker-compose.yml` (빠른 재빌드)
- **DB & Redis**: `docker-compose.db.yml` (별도 관리)

### 빌드 최적화
- 멀티스테이지 빌드
- 레이어 캐시 활용
- .dockerignore로 불필요한 파일 제외
- NuGet 복원 캐시

### 네트워크 구성
- API: `projectvg-network`
- DB/Redis: `projectvg-external-db` (외부 네트워크)
- localhost 연결로 통신

## 🛠️ 개발 워크플로우

### 초기 설정 (1회만)
```powershell
# 전체 개발 환경 설정 (DB + Redis + API)
.\scripts\dev-setup.ps1
```

### 개발 중 (매일)
```powershell
# 코드 변경 후 API만 재빌드 (빠름!)
.\scripts\start-api.ps1
```

### 개별 서비스 관리
```powershell
# DB와 Redis만 시작
.\scripts\start-db.ps1

# API만 시작
.\scripts\start-api.ps1

# 모든 서비스 중지 및 정리
.\scripts\stop-all.ps1
```

### 테스트 실행
```powershell
# JWT 인증 시스템 테스트
dotnet test ProjectVG.Tests/ProjectVG.Tests.csproj
```

## 🌐 서비스 연결 정보

| 서비스 | URL | 포트 | 설명 |
|--------|-----|------|------|
| API | http://localhost:7910 | 7910 | 메인 API 서버 |
| SQL Server | localhost | 1433 | 데이터베이스 |
| Redis | localhost | 6380 | 캐시/세션 저장소 |

### 데이터베이스 접속 정보
- **Server**: localhost:1433
- **Database**: ProjectVG
- **Username**: sa
- **Password**: ProjectVG123!

## 🔐 JWT 인증 시스템

### 주요 기능
- ✅ JWT Access/Refresh 토큰
- ✅ OAuth2 로그인 (Google)
- ✅ 토큰 자동 갱신
- ✅ Redis 기반 세션 관리
- ✅ WebSocket 인증 준비

### API 엔드포인트
- `POST /api/auth/test-login` - 테스트 로그인
- `POST /api/auth/refresh` - 토큰 갱신
- `POST /api/auth/logout` - 로그아웃
- `GET /api/test/me` - 사용자 정보 (인증 필요)

## 🧪 테스트

### 테스트 실행
```powershell
# 전체 테스트
dotnet test ProjectVG.Tests/ProjectVG.Tests.csproj

# 특정 테스트 클래스
dotnet test --filter "FullyQualifiedName~JwtProviderTests"
```

### 테스트 커버리지
- **JWT Provider**: 100% (12개 테스트)
- **Token Service**: 100% (15개 테스트)
- **Auth Service**: 100% (15개 테스트)
- **JWT Filter**: 100% (10개 테스트)

### OAuth2 테스트 클라이언트

#### 자동 실행 스크립트
```powershell
# PowerShell 스크립트 (권장)
.\scripts\start-oauth2-client.ps1

# 배치 파일
.\scripts\start-oauth2-client.bat
```

#### 수동 실행
```powershell
# 테스트 클라이언트 디렉토리로 이동
cd test-clients

# Python 서버 실행
python start-oauth2-client.py
```

#### 브라우저 접속
```
http://localhost:3000
```

#### OAuth2 테스트 기능
- ✅ PKCE (Proof Key for Code Exchange) 생성
- ✅ OAuth2 Authorization Code Flow 테스트
- ✅ Google OAuth2 연동
- ✅ JWT 토큰 발급 확인
- ✅ 사용자 정보 표시
- ✅ 클립보드 복사 기능

## 📝 환경 변수

### 환경 변수 설정
1. `env.example` 파일을 `.env`로 복사
2. 필요한 값들을 수정

### 주요 환경 변수
```bash
# 외부 서비스 연결
LLM_BASE_URL=http://localhost:5601
MEMORY_BASE_URL=http://localhost:5602
TTS_API_KEY=your-tts-api-key

# 데이터베이스 연결
DB_CONNECTION_STRING=Server=localhost,1433;Database=ProjectVG;User Id=sa;Password=ProjectVG123!;TrustServerCertificate=true;MultipleActiveResultSets=true

# Redis 연결
REDIS_CONNECTION_STRING=localhost:6380

# JWT 설정
JWT_SECRET_KEY=your-super-secret-jwt-key-here-minimum-32-characters
JWT_ACCESS_TOKEN_LIFETIME_MINUTES=15
JWT_REFRESH_TOKEN_LIFETIME_DAYS=30

# OAuth2 설정 (선택사항)
GOOGLE_OAUTH_CLIENT_ID=your-google-client-id
GOOGLE_OAUTH_CLIENT_SECRET=your-google-client-secret
```

## 🐛 문제 해결

### 빌드 시간 단축 팁
1. **DB/Redis 분리**: 한 번 시작하면 재시작 불필요
2. **캐시 활용**: Docker 레이어 캐시 사용
3. **선택적 빌드**: API만 재빌드

### 일반적인 문제
- **포트 충돌**: 다른 서비스가 7910, 1433, 6380 포트 사용 중
- **DB 연결 실패**: DB 초기화 대기 시간 필요 (30초)
- **Redis 연결 실패**: Redis 컨테이너 상태 확인

## 📚 추가 문서

- [API 참조](docs/api_reference.md)
- [아키텍처 가이드](docs/architecture/)
- [개발 가이드](docs/development/)
- [코딩 컨벤션](docs/coding_conventions.md)
