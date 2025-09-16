# ProjectVG API Server

**Clean Architecture 기반 AI 채팅 플랫폼 백엔드 시스템**

.NET 8.0 ASP.NET Core API 서버로, JWT/OAuth2 인증, WebSocket 실시간 통신, Redis 세션 관리를 구현한 AI 채팅 플랫폼입니다.

## 🏗️ 시스템 아키텍처

### Clean Architecture 구현
```
┌─────────────────── Presentation Layer ──────────────────┐
│ ProjectVG.Api (Controllers, Middleware, Authentication)  │
├─────────────────── Application Layer ───────────────────┤
│ ProjectVG.Application (Services, DTOs, Business Logic)  │
├─────────────────── Domain Layer ────────────────────────┤
│ ProjectVG.Domain (Entities, Repositories, Domain Rules) │
├─────────────────── Infrastructure Layer ────────────────┤
│ ProjectVG.Infrastructure (Database, External Services)  │
└─────────────────── Common/Tests ────────────────────────┘
```

### 기술 스택
- **.NET 8.0**: C# 12, nullable reference types
- **ASP.NET Core 8.0**: Controllers 기반 REST API
- **Entity Framework Core 8.0**: Code-First, SQL Server
- **Redis**: StackExchange.Redis 세션 관리
- **Authentication**: JWT + Google OAuth2 PKCE
- **Testing**: xUnit + Moq + FluentAssertions
- **Container**: Docker + Docker Compose

## 📋 핵심 기능

### 인증 시스템
- **JWT 토큰**: Access (15분) + Refresh (30일) 이중 토큰
- **OAuth2 PKCE**: Google 인증, 게스트 로그인
- **Redis 세션**: 토큰 관리 및 blacklist 지원
- **다중 헤더**: Authorization, X-Access-Credit, X-Refresh-Credit

### AI 캐릭터 관리
- **하이브리드 설정**: JSON 필드 구성 + 직접 프롬프트 입력
- **소유권 모델**: 시스템/공개/개인 캐릭터 권한 관리
- **프롬프트 생성**: 설정 기반 SystemPrompt 구성

### 채팅 시스템
- **이중 프로토콜**: WebSocket + HTTP REST API
- **메시지 관리**: User/Assistant/System 역할 기반 대화
- **외부 서비스 연동**: LLM, Memory, TTS 서비스 통합
- **페이지네이션**: 대화 기록 조회

### 크레딧 시스템
- **정밀 계산**: Decimal(18,2) 잔액 관리
- **거래 기록**: 완전한 audit trail
- **동시성 제어**: Optimistic concurrency 지원

## 🔧 기술 구현

### 성능 최적화
- **메모리 관리**: ArrayPool 버퍼 재사용, IMemoryOwner 활용
- **비동기 패턴**: I/O 작업 async/await 적용
- **데이터베이스**: 연결 복원력 (지수 백오프 재시도), 자동 마이그레이션

### 보안 구현
- **JWT 보안**: 32자 이상 암호키, 다중 소스 헤더 지원
- **OAuth2 PKCE**: Proof Key for Code Exchange, CSRF 방지
- **입력 검증**: Data Annotations + 도메인 레벨 검증
- **SQL Injection 방지**: EF Core 매개변수화 쿼리

### 테스트 전략
- **인증 시스템 테스트**: JWT Provider, Token Service, Auth Service, JWT Filter 테스트 포함
- **다층 테스트**: Unit Tests (Mock), Integration Tests (실제 DB), End-to-End Tests (API)
- **성능 테스트**: 메모리 최적화 검증

## 🗄️ 데이터 모델

### Core Entities
- **User**: OAuth2 기반 사용자, 고유 UID, 크레딧 잔액
- **Character**: 하이브리드 구성 (JSON/Direct), 공개/개인 구분
- **ConversationHistory**: 역할별 메시지 저장
- **CreditTransaction**: 거래 기록, 소스 추적

### 데이터베이스 설계
- **Optimistic Concurrency**: RowVersion 타임스탬프
- **JSON 컬럼**: 캐릭터 설정 저장
- **Decimal 정밀도**: Decimal(18,2) 크레딧 계산
- **인덱스**: 사용자별, 캐릭터별 조회 최적화

## 🌐 API 엔드포인트

### 인증 (`/api/v1/auth`, `/auth`)
- `POST /api/v1/auth/refresh` - Access Token 갱신
- `POST /api/v1/auth/logout` - 세션 종료
- `POST /api/v1/auth/guest-login` - 게스트 인증
- `GET /auth/oauth2/authorize/{provider}` - OAuth2 시작

### 캐릭터 (`/api/v1/character`)
- `GET /api/v1/character` - 캐릭터 조회
- `POST /api/v1/character/individual` - JSON 설정 캐릭터 생성
- `POST /api/v1/character/systemprompt` - 시스템 프롬프트 캐릭터 생성
- `GET /api/v1/character/my` - 내 캐릭터 조회

### 채팅 및 크레딧
- `POST /api/v1/chat` - 채팅 메시지 처리 (JWT 필수)
- `GET /api/v1/credits/balance` - 크레딧 잔액 조회
- `GET /api/v1/credits/history` - 거래 내역 조회 (페이지네이션)

## 📁 프로젝트 구조

```
ProjectVG.Api/                 # Presentation Layer
├── Controllers/               # REST API 컨트롤러
├── Middleware/               # 전역 예외 처리, WebSocket
├── Filters/                  # JWT 인증 필터
└── Models/                   # Request/Response DTO

ProjectVG.Application/         # Application Layer
├── Services/                 # 비즈니스 로직 서비스
├── Models/                   # 애플리케이션 DTO
└── Commands/                 # 명령 객체

ProjectVG.Domain/             # Domain Layer
├── Entities/                 # 도메인 엔티티
├── Repositories/             # 리포지토리 인터페이스
└── Common/                   # 기본 엔티티, 값 객체

ProjectVG.Infrastructure/      # Infrastructure Layer
├── Persistence/              # EF Core DbContext
├── Auth/                     # JWT, OAuth2 구현
├── Integrations/            # 외부 서비스 클라이언트
└── Services/                 # 인프라 서비스

ProjectVG.Tests/              # Test Suite
├── Api/                     # 컨트롤러, 필터 테스트
├── Application/             # 서비스 통합 테스트
├── Auth/                    # 인증 시스템 테스트
└── Infrastructure/          # 리포지토리, 외부 서비스 테스트
```

## 🔄 외부 서비스 연동

### 연동 서비스
- **LLM Service**: AI 모델 추론 처리
- **Memory Service**: 대화 컨텍스트 관리
- **TTS Service**: 텍스트 음성 변환

### 데이터 저장소
- **SQL Server**: 주 데이터베이스 (사용자, 캐릭터, 대화)
- **Redis**: 세션 관리, 토큰 캐시

## 💡 아키텍처 설계 원칙

### Clean Architecture 적용
- **의존성 역전**: Domain ← Application ← Infrastructure
- **관심사 분리**: 각 레이어별 명확한 책임
- **테스트 가능성**: Mock 기반 단위 테스트
- **기술 독립성**: 프레임워크에 종속되지 않는 비즈니스 로직

### 도메인 중심 설계
- **Rich Domain Model**: 엔티티에 비즈니스 로직 캡슐화
- **Repository Pattern**: 데이터 접근 추상화
- **Value Objects**: 불변 도메인 개념
- **Aggregate Root**: 일관성 경계 정의

### 성능 고려사항
- **비동기 처리**: I/O 바운드 작업 최적화
- **메모리 효율성**: 스트리밍 처리 지원
- **연결 풀링**: 데이터베이스 연결 최적화
- **캐싱**: Redis 기반 세션 캐시