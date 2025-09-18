# ProjectVG API Server 문서

ProjectVG API Server의 종합 문서 모음입니다. 각 카테고리별로 정리된 문서들을 통해 프로젝트의 모든 측면을 이해하고 활용할 수 있습니다.

## 📂 문서 구조

### 📋 [Overview](./overview/) - 프로젝트 개요
프로젝트의 핵심 기능과 개발 표준에 대한 개요를 제공합니다.

- **[features.md](./overview/features.md)** - 핵심 기능 상세 가이드
  - 채팅 시스템 (WebSocket + HTTP)
  - 인증 시스템 (JWT + OAuth2)
  - 크레딧 시스템 (정밀 계산)
  - AI 캐릭터 관리 (하이브리드 설정)

- **[coding-standards.md](./overview/coding-standards.md)** - 코딩 컨벤션 및 표준
  - 네이밍 규칙
  - 프로젝트 구조
  - 코드 스타일 가이드

### 🏗️ [Architecture](./architecture/) - 시스템 아키텍처
시스템의 기술적 아키텍처와 설계 패턴에 대한 문서입니다.

- **[websocket_connection_management.md](./architecture/websocket_connection_management.md)** - WebSocket 연결 관리 설계
- **[websocket_http_architecture.md](./architecture/websocket_http_architecture.md)** - WebSocket + HTTP 하이브리드 아키텍처

### 🔌 [API](./api/) - API 참조
REST API 엔드포인트와 사용법에 대한 상세 가이드입니다.

- **[rest-endpoints.md](./api/rest-endpoints.md)** - REST API 엔드포인트 레퍼런스
  - 인증 API
  - 채팅 API
  - 캐릭터 관리 API
  - 크레딧 시스템 API

### 🔗 [Integrations](./integrations/) - 외부 연동
외부 서비스 및 클라이언트와의 연동 가이드입니다.

#### Unity 클라이언트 연동
Unity 게임 엔진과의 연동을 위한 종합 가이드입니다.

- **[authentication-guide.md](./integrations/unity/authentication-guide.md)** - Unity OAuth2 인증 가이드
- **[oauth2-implementation.md](./integrations/unity/oauth2-implementation.md)** - OAuth2 PKCE 구현 상세
- **[oauth2-handler.md](./integrations/unity/oauth2-handler.md)** - OAuth2 핸들러 구현
- **[cookie-handling.md](./integrations/unity/cookie-handling.md)** - 쿠키 처리 가이드

#### 외부 서비스 연동
외부 AI 및 음성 서비스와의 API 연동 가이드입니다.

- **[memory-service.md](./integrations/external-services/memory-service.md)** - Memory Server API 연동
- **[voice-service.md](./integrations/external-services/voice-service.md)** - Voice Server API 연동 (TTS)

#### LLM 서비스 연동
언어 모델 서비스와의 연동 및 최적화 가이드입니다.

- **[prompt-engineering.md](./integrations/llm/prompt-engineering.md)** - 프롬프트 엔지니어링 가이드

### 🔐 [Security](./security/) - 보안
보안 가이드라인과 베스트 프랙티스입니다.

- **[security-guidelines.md](./security/security-guidelines.md)** - 보안 가이드라인 및 베스트 프랙티스
  - JWT 토큰 보안
  - 데이터베이스 보안
  - 인증 플로우 보안
  - 프로덕션 보안 체크리스트

### 🚀 [Deployment](./deployment/) - 배포 및 운영
배포, CI/CD, 운영 환경 설정에 대한 가이드입니다.

- **[ci-cd-setup.md](./deployment/ci-cd-setup.md)** - CI/CD 파이프라인 설정 가이드

## 🚀 빠른 시작

### 새로운 개발자를 위한 추천 읽기 순서

1. **프로젝트 이해**: [overview/features.md](./overview/features.md) - 전체 기능 파악
2. **개발 환경**: [deployment/ci-cd-setup.md](./deployment/ci-cd-setup.md) - 환경 설정
3. **코딩 표준**: [overview/coding-standards.md](./overview/coding-standards.md) - 개발 규칙
4. **API 사용법**: [api/rest-endpoints.md](./api/rest-endpoints.md) - API 이해
5. **보안 가이드**: [security/security-guidelines.md](./security/security-guidelines.md) - 보안 요구사항

### Unity 개발자를 위한 가이드

Unity 클라이언트 개발을 위해서는 다음 문서들을 순서대로 참고하세요:

1. [integrations/unity/authentication-guide.md](./integrations/unity/authentication-guide.md) - 인증 개요
2. [integrations/unity/oauth2-implementation.md](./integrations/unity/oauth2-implementation.md) - OAuth2 구현
3. [integrations/unity/oauth2-handler.md](./integrations/unity/oauth2-handler.md) - 핸들러 구현
4. [integrations/unity/cookie-handling.md](./integrations/unity/cookie-handling.md) - 쿠키 관리

### 외부 서비스 연동 개발자를 위한 가이드

외부 AI 서비스와의 연동을 개발하는 경우:

1. [integrations/llm/prompt-engineering.md](./integrations/llm/prompt-engineering.md) - LLM 연동
2. [integrations/external-services/memory-service.md](./integrations/external-services/memory-service.md) - 메모리 서비스
3. [integrations/external-services/voice-service.md](./integrations/external-services/voice-service.md) - 음성 서비스

## 📚 추가 리소스

- **메인 프로젝트 문서**: [../CLAUDE.md](../CLAUDE.md) - 프로젝트 전체 가이드
- **API 서버 코드**: [../](../) - 소스 코드 베이스
- **테스트 가이드**: [../ProjectVG.Tests/](../ProjectVG.Tests/) - 테스트 코드

## 🤝 기여 가이드

문서 개선이나 추가가 필요한 경우:

1. 관련 카테고리의 적절한 폴더를 선택
2. kebab-case 네이밍 컨벤션 사용
3. 기존 문서 구조와 스타일 일관성 유지
4. 상호 참조 링크 추가

---

💡 **도움이 필요하신가요?** 각 문서의 상세 내용을 확인하거나, 프로젝트 메인테이너에게 문의해주세요.