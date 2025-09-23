# 분산 서버 시스템 가이드

## 개요

ProjectVG API 서버가 이제 분산 환경을 지원합니다. Redis Pub/Sub를 통해 여러 서버 인스턴스 간 WebSocket 메시지를 라우팅하고, 사용자 세션을 추적할 수 있습니다.

## 아키텍처

### 기존 단일 서버 구조
```
Unity Client ──WebSocket──→ API Server ──→ ChatService ──→ WebSocketManager
```

### 새로운 분산 서버 구조
```
Unity Client ──WebSocket──→ API Server A
                              ↓
                         MessageBroker
                              ↓
                         Redis Pub/Sub
                              ↓
API Server B ──WebSocket──→ Unity Client (실제 연결된 서버)
```

## 주요 구성 요소

### 1. 서버 등록 시스템
- **목적**: 각 서버 인스턴스를 Redis에 등록하고 관리
- **구현**: `RedisServerRegistrationService`, `ServerLifecycleService`
- **기능**:
  - 서버 시작 시 자동 등록
  - 30초마다 헬스체크 전송
  - 오프라인 서버 자동 정리
  - 사용자-서버 매핑 관리

### 2. MessageBroker 추상화
- **목적**: 단일/분산 환경을 투명하게 지원
- **구현**: `IMessageBroker`, `LocalMessageBroker`, `DistributedMessageBroker`
- **기능**:
  - 사용자별 메시지 전송
  - 서버 간 메시지 라우팅
  - 브로드캐스트 메시지

### 3. Redis Pub/Sub 시스템
- **채널 구조**:
  - `user:{userId}` - 특정 사용자 메시지
  - `server:{serverId}` - 특정 서버 메시지
  - `broadcast` - 전체 방송 메시지
- **메시지 라우팅**: 사용자가 연결된 서버로 자동 라우팅

### 4. WebSocket 세션 관리
- **분산 세션 추적**: Redis에서 사용자-서버 매핑 관리
- **연결/해제 처리**: 자동 채널 구독/해제
- **세션 TTL**: 30분 자동 만료

## 설정 방법

### 환경 변수

```bash
# 분산 모드 활성화
DISTRIBUTED_MODE=true

# 서버 고유 ID (자동 생성 가능)
SERVER_ID=api-server-001

# Redis 연결 문자열 (필수)
REDIS_CONNECTION_STRING=localhost:6380
```

### appsettings.json

```json
{
  "DistributedSystem": {
    "Enabled": true,
    "ServerId": "api-server-001",
    "HeartbeatIntervalSeconds": 30,
    "CleanupIntervalMinutes": 5,
    "ServerTimeoutMinutes": 2
  }
}
```

## 사용법

### 단일 서버 모드 (기본)

```bash
# 환경 변수 설정
DISTRIBUTED_MODE=false

# 또는 appsettings.json
{
  "DistributedSystem": {
    "Enabled": false
  }
}
```

- `LocalMessageBroker` 사용
- 기존 `WebSocketManager` 사용
- Redis 연결 불필요

### 분산 서버 모드

```bash
# 환경 변수 설정
DISTRIBUTED_MODE=true
SERVER_ID=api-server-001
REDIS_CONNECTION_STRING=localhost:6380

# 서버 시작
dotnet run --project ProjectVG.Api
```

- `DistributedMessageBroker` 사용
- `DistributedWebSocketManager` 사용
- Redis 연결 필수

## 테스트 방법

### 1. 단일 서버 테스트

```powershell
# 환경 변수 설정
$env:DISTRIBUTED_MODE="false"

# 서버 시작
dotnet run --project ProjectVG.Api --urls "http://localhost:7910"
```

### 2. 다중 서버 테스트

```powershell
# 서버 1 시작
$env:DISTRIBUTED_MODE="true"
$env:SERVER_ID="api-server-001"
$env:REDIS_CONNECTION_STRING="localhost:6380"
dotnet run --project ProjectVG.Api --urls "http://localhost:7910"

# 서버 2 시작 (새 터미널)
$env:DISTRIBUTED_MODE="true"
$env:SERVER_ID="api-server-002"
$env:REDIS_CONNECTION_STRING="localhost:6380"
dotnet run --project ProjectVG.Api --urls "http://localhost:7911"

# 서버 3 시작 (새 터미널)
$env:DISTRIBUTED_MODE="true"
$env:SERVER_ID="api-server-003"
$env:REDIS_CONNECTION_STRING="localhost:6380"
dotnet run --project ProjectVG.Api --urls "http://localhost:7912"
```

### 3. Redis 모니터링

```bash
# Redis CLI 접속
redis-cli -p 6380

# 서버 등록 상태 확인
SMEMBERS servers:active

# 특정 서버 정보 확인
GET servers:active:api-server-001

# 사용자 세션 확인
KEYS user:server:*

# 메시지 채널 모니터링
MONITOR
```

### 4. WebSocket 연결 테스트

```javascript
// 각기 다른 서버에 연결
const ws1 = new WebSocket('ws://localhost:7910/ws?token=JWT_TOKEN');
const ws2 = new WebSocket('ws://localhost:7911/ws?token=JWT_TOKEN');
const ws3 = new WebSocket('ws://localhost:7912/ws?token=JWT_TOKEN');

// 메시지 전송 테스트
ws1.send(JSON.stringify({
  type: 'chat',
  data: { message: 'Hello from server 1' }
}));
```

## 로그 확인

### 서버 등록 로그
```
서버 등록 서비스 초기화: ServerId=api-server-001, Timeout=00:02:00
서버 등록 완료: api-server-001
분산 시스템 모드 활성화
```

### 메시지 라우팅 로그
```
분산 사용자 메시지 전송: user123 -> api-server-002
분산 메시지 브로커 구독 초기화 완료: 서버 api-server-001
사용자 채널 구독 시작: user123
```

### 세션 관리 로그
```
새 분산 WebSocket 세션 생성: user123
분산 사용자 채널 구독 완료: user123
분산 WebSocket 세션 해제: user123
분산 사용자 채널 구독 해제 완료: user123
```

## 문제 해결

### Redis 연결 실패
```
Redis 연결 실패, In-Memory로 대체: Connection timeout
```
- Redis 서버가 실행 중인지 확인
- 연결 문자열이 올바른지 확인
- 방화벽 설정 확인

### 서버 등록 실패
```
서버 등록 실패: api-server-001
```
- Redis 연결 상태 확인
- SERVER_ID 중복 여부 확인
- Redis 메모리 용량 확인

### 메시지 라우팅 실패
```
사용자가 연결된 서버를 찾을 수 없음: user123
```
- 사용자 세션이 만료되었을 가능성
- 대상 서버가 오프라인일 가능성
- Redis에서 사용자 매핑 확인: `GET user:server:user123`

### WebSocket 연결 끊김
```
분산 환경에서 사용자를 찾을 수 없음: user123
```
- 사용자가 다른 서버로 이동했을 가능성
- 네트워크 연결 상태 확인
- 서버 간 시간 동기화 확인

## 성능 고려사항

### 메모리 사용량
- 서버당 약 1KB 메타데이터
- 사용자당 약 100B 세션 데이터
- Redis 키 TTL로 자동 정리

### 네트워크 대역폭
- 헬스체크: 30초마다 소량 데이터
- 메시지 라우팅: 실제 메시지 크기에 비례
- Redis Pub/Sub: 저지연 메시지 전달

### 확장성
- 수평적 확장: 서버 인스턴스 추가 가능
- Redis 단일 장애점: Redis Cluster 고려
- 로드밸런싱: API Gateway 또는 로드밸런서 사용

## 운영 가이드

### 모니터링 지표
- 활성 서버 수: `SCARD servers:active`
- 총 사용자 세션 수: `KEYS user:server:* | wc -l`
- 메시지 처리량: Redis MONITOR 활용
- 서버 헬스체크 간격: 로그 분석

### 유지보수
- 정기적 Redis 메모리 모니터링
- 오프라인 서버 자동 정리 확인
- 네트워크 지연 모니터링
- 로그 레벨 조정 (개발: DEBUG, 운영: INFO)

### 백업 및 복구
- Redis 데이터는 일시적 (서버 재시작 시 초기화)
- 서버 메타데이터만 저장하므로 별도 백업 불필요
- 장애 시 서버 재시작으로 자동 복구

이제 ProjectVG API 서버는 완전한 분산 환경을 지원합니다!