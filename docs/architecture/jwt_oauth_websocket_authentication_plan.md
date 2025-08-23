# ASP.NET Core + OAuth2 + JWT Refresh/Access + WebSocket 인증 서버 계획서

## 목표와 범위
- **목표**: ASP.NET Core 기반 OAuth2(Code+PKCE) + JWT(Access/Refresh) + WebSocket 인증/알림 서버 설계 및 단계적 전환
- **범위**: 기존 SessionId 기반을 무상태(Stateless) 토큰 기반으로 이행, 장기 연결(WebSocket) 인증/재인증 흐름 확립, 멀티플랫폼(PC/모바일/WebGL) 대응

## 핵심 원칙
- **Stateless**: API는 JWT Access 토큰 검증으로 인증, 서버 측 세션 의존 최소화
- **Refresh 로테이션**: 재사용 감지 및 블랙리스트/리스트아웃 처리
- **멀티플랫폼 안전 저장**: Access=메모리, Refresh=보안 저장소(네이티브)/HttpOnly 쿠키(WebGL)
- **장기연결 보완**: WebSocket 연결 중 토큰 갱신 시 재인증 메시지 프로토콜 지원

## 권장 기술 스택
- **OAuth2/OIDC**: OpenIddict(무상용) 도입 또는 내부 최소구현 선택지 검토
- **JWT**: RS256 서명, 키 롤테이션 지원, 키는 HSM/KeyVault 등 외부 보관(개발은 파일/환경변수)
- **데이터 저장**: EF Core + SQL Server, `RefreshToken` 엔터티 및 인덱스 설계
- **캐시/리스트아웃**: Redis(선호)로 Refresh 재사용 감지/블록, 단기 블랙리스트
- **로그/감사**: 로그인·토큰 교환·재발급·재사용 감지 이벤트 로깅

## 토큰 정책
- **Access 수명**: 5–15분
- **Refresh 수명**: 7–30일, 매 교환 시 로테이션
- **재사용 감지**: 동일 Refresh의 중복 사용 시 계정 리스크 플래그 및 모든 관련 Refresh 무효화
- **스코프/클레임 최소화**: 필요한 권한만 발급, 역할/권한은 서버에서 재검증 가능 구조

## 상위 아키텍처
- `ProjectVG.Api`: OAuth2 엔드포인트, JWT 인증 설정, WebSocket 인증 진입
- `ProjectVG.Application`: 인증/토큰 발급 서비스, 정책/도메인 로직
- `ProjectVG.Infrastructure`: EF 저장소, 키 관리, Redis 통합, 외부 IdP(선택)
- `ProjectVG.Domain`: `User`, `RefreshToken` 등 엔터티, 규칙
- WebSocket: `WebSocketMiddleware` 확장, 연결 시 토큰 검증 및 `SessionInfo` 바인딩

## 도메인/데이터 모델
- **User**: 기존 엔터티 활용
- **RefreshToken**
  - 필드: `Id`, `UserId`, `ClientId`, `DeviceId`, `TokenHash`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenId`, `CreatedAt`, `LastUsedAt`, `Ip`, `UserAgent`
  - 인덱스: `UserId`, `TokenHash(Unique)`, `ExpiresAt`
- **TokenFamily(옵션)**: 토큰 계보 추적 시 계정 단위 일괄 무효화 최적화

## API 엔드포인트(초안)
- `GET /oauth/authorize`: Code + PKCE 시작
- `POST /oauth/token`: 코드 교환, Access/Refresh 발급, Refresh 교환(로테이션)
- `POST /auth/refresh`: Refresh로 Access/Refresh 재발급(로테이션)
- `POST /auth/revoke`: Refresh 무효화(로그아웃/디바이스 해제)
- `GET /auth/userinfo`: 현재 사용자 정보 조회(OIDC 호환)
- `GET /ws` 또는 `/ws/chat`: WebSocket 업그레이드(토큰 포함)

## 인증/인가 설정(서버)
- JWT Bearer 인증 핸들러 설정, RS256 키/키 롤테이션
- 권한/정책 기반 인가(스코프/역할)
- CORS/쿠키 정책(WebGL: SameSite, Secure, HttpOnly)
- OAuth2 Code+PKCE 검증(State/Nonce), 리다이렉트 URI 화이트리스트

## Refresh 로테이션/재사용 감지
- 교환 성공 시:
  - 이전 Refresh `RevokedAt` 설정, `ReplacedByTokenId` 연결
  - 새 Refresh 발급 및 저장
- 재사용 감지 시:
  - 해당 토큰 계보 전체 무효화
  - 사용자 리스크 플래그, 알림/추가 검증 유도
- Redis 키:
  - `refresh:blocked:{tokenHash}` TTL=Refresh 만료까지
  - 최근 토큰 ID/Hash 캐시하여 중복 사용 빠른 탐지

## WebSocket 인증 흐름
- 연결 시점:
  - 옵션 A: `Sec-WebSocket-Protocol` 또는 `Authorization` 헤더에 Bearer 전달
  - 옵션 B: 쿼리 파라미터(HTTPS 필수, 짧은 만료) 사용
- 서버:
  - Access 검증 → `SessionInfo`에 `UserId`, `Claims`, `DeviceId` 바인딩
  - `ConnectionRegistry`에 사용자-연결 매핑
- 만료 처리:
  - 클라이언트가 Refresh 후 `auth.reauth` 메시지로 최신 Access 전달
  - 서버는 서명/만료 검증 후 세션 컨텍스트 갱신
  - 일정 기간 미갱신 시 서버가 Close(Code=4401 유사)로 정리
- 메시지 규약(예시 타입):
  - `auth.reauth`(클라이언트→서버): `accessToken`
  - `job.result`(서버→클라이언트): `correlationId`, `status`, `payload`
  - `auth.error`/`error`: 오류 통지

## 비동기 작업 결과 전송
- API 호출 시 `correlationId` 발급 또는 클라이언트 제공
- 서버 작업 완료 시 `ConnectionRegistry`에서 `UserId` 기반 연결 조회
- `WebSocketMessage` 사용하여 `job.result` 전송
- 연결 부재 시 재시도/오프라인 큐(옵션) 또는 폴백 REST 콜백

## 보안 강화 옵션
- 디바이스 바인딩: `DeviceId`/`UA`/`IP`와 Refresh 연계, 이변 탐지 시 강화 인증
- JTI/Nonce: Access 재생 공격 대비
- 레이트 리밋: `token`, `refresh`, `reauth` 엔드포인트/메시지 제한
- 감사 로그: 성공/실패, 재사용, 로테이션, WebSocket 재인증 이벤트

## 구성 파일/환경 변수
- `appsettings.{Environment}.json`
  - `Authentication:Jwt:Issuer/Audience`
  - `Authentication:Jwt:KeyId`(키 식별자)
  - `Tokens:Access:LifetimeMinutes`
  - `Tokens:Refresh:LifetimeDays`
  - `OAuth:RedirectUris[]`, `OAuth:Clients[]`
  - `Redis:ConnectionString`
- 비밀/키: 환경변수/KeyVault로 관리, 저장소에는 평문 금지

## 프로젝트 구조 반영
- `ProjectVG.Api`
  - 컨트롤러: `OAuthController`, `AuthController`(refresh/revoke/userinfo)
  - 미들웨어: JWT, CORS, 쿠키, `WebSocketMiddleware` 확장
- `ProjectVG.Application`
  - 서비스 인터페이스: `IAuthService`, `ITokenService`, `IRefreshTokenService`
  - 정책/전략: 토큰 로테이션, 재사용 감지, 디바이스 정책
  - DTO: `TokenResponse`, `RefreshRequest`, `RevokeRequest`
- `ProjectVG.Infrastructure`
  - 저장소: `IRefreshTokenRepository` + 구현(EF)
  - 통합: Redis, 키 관리(파일/KeyVault 선택)
- `ProjectVG.Domain`
  - 엔터티: `RefreshToken`
  - 값객체/정책: 필요 시 분리
- 마이그레이션: `Add_RefreshToken_Table` 생성

## 단계별 마이그레이션 플랜
1) 준비
- OpenIddict(또는 내부 구현) 도입 결정, 키 발급/보관 체계 준비
- `RefreshToken` 엔터티 및 마이그레이션 추가
2) 서버 인증 도입
- JWT 인증/인가 구성, 엔드포인트 초안 구현
- Refresh 로테이션/재사용 감지 로직 구현
3) WebSocket 연동
- 연결 시 인증 바인딩, `auth.reauth` 메시지 처리
- `ConnectionRegistry`와 `SessionInfo` 확장
4) 클라이언트 점진 전환
- Access=메모리, Refresh=보안 저장(네이티브)/HttpOnly 쿠키(WebGL)
- 자동 갱신/백오프/오류 핸들링
5) 운영 강화
- 레이트 리밋/감사 로그/모니터링, 키 롤테이션 절차 수립
- 위험 탐지/차단 흐름 활성화

## 품질/테스트
- 단위: 토큰 발급/검증, 로테이션, 재사용 감지
- 통합: OAuth 코드 교환, WebSocket 인증/재인증
- 시나리오: 만료 직후 갱신, 네트워크 불안정, 중복 Refresh 사용, 멀티 디바이스
- 보안: JWT 서명 위변조, 리다이렉트 URI 검사, CSRF(WebGL), XSS 쿠키 보호

## 운영 지표
- 토큰 발급/재발급 성공률·지연
- Refresh 재사용 탐지 수
- WebSocket 재인증 성공률, 강제 종료 수
- 디바이스/지역 이변 탐지 이벤트

## 리스크와 대응
- 장기 WebSocket 세션 만료 이슈 → `auth.reauth` 표준화, 유휴 타임아웃
- 키 유출/회전 → KID 기반 롤테이션, 키 만료 스케줄, 비상 폐기 절차
- WebGL 쿠키/CSRF → SameSite, Double Submit, BFF 가능 시 채택

## 초기 마일스톤(제안)
- M1: 토큰 모델/저장/로테이션/재사용 감지 구현, 마이그레이션 배포
- M2: OAuth Code+PKCE 플로우, 토큰 엔드포인트 공개
- M3: WebSocket 인증/재인증 메시지 프로토콜 적용
- M4: 클라이언트 전환 가이드/SDK 헬퍼, 운영지표·로그 보강
- M5: 키 롤테이션 자동화, 보안 점검 완료

## 핵심 결정 정리
- OAuth2 Code+PKCE, JWT RS256, Access 5–15분·Refresh 7–30일
- Refresh 로테이션·재사용 감지, Redis 보조
- WebSocket `auth.reauth`로 장기 연결 재인증
- 네이티브: Secure Storage, WebGL: Refresh HttpOnly 쿠키 또는 BFF
