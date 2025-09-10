# ProjectVG Load Test Environment

부하 테스트를 위한 더미 서버들과 환경 설정을 포함하는 디렉토리입니다.

## 구성 요소

### 완전 분리된 부하테스트 환경
- **loadtest-sqlserver** (포트 1434): 부하테스트 전용 SQL Server (운영 DB와 완전 분리)
- **loadtest-redis** (포트 6381): 부하테스트 전용 Redis (운영 Redis와 완전 분리)

### 더미 서버들
- **dummy-llm-server** (포트 7808): LLM 서비스 시뮬레이션 (1-2초 응답 딜레이)
- **dummy-memory-server** (포트 7812): Memory/VectorDB 서비스 시뮬레이션 (100ms 응답 딜레이)  
- **dummy-tts-server** (포트 7816): TTS 서비스 시뮬레이션 (2-3초 응답 딜레이)

### 부하 테스트 실행 방법

1. **환경 준비**
   ```bash
   # 프로젝트 루트에서 실행
   cd "C:\Users\imdls\Documents\Project\MainAPI Server"
   ```

2. **부하 테스트 환경 시작 (운영 환경과 완전 분리)**
   ```bash
   # 부하 테스트용 Docker Compose 실행 - 모든 서비스가 독립적으로 실행됨
   docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml up --build
   ```
   
   **⚠️ 주의: 이 환경은 운영 환경과 완전히 분리됩니다**
   - 별도의 데이터베이스 (포트 1434)
   - 별도의 Redis (포트 6381) 
   - 더미 외부 서비스들
   - 운영 데이터에 전혀 영향을 주지 않음

3. **개별 더미 서버 실행 (개발/디버깅용)**
   ```bash
   # LLM 서버
   cd test-loadtest/dummy-llm-server
   npm install && npm start
   
   # Memory 서버  
   cd test-loadtest/dummy-memory-server
   npm install && npm start
   
   # TTS 서버
   cd test-loadtest/dummy-tts-server
   npm install && npm start
   ```

4. **상태 확인**
   ```bash
   # 부하테스트 전용 인프라 서버 확인
   curl http://localhost:1434  # SQL Server (별도 도구 필요)
   redis-cli -p 6381 ping     # Redis: PONG 응답 확인
   
   # 더미 서버 헬스체크
   curl http://localhost:7808/health  # LLM
   curl http://localhost:7812/health  # Memory
   curl http://localhost:7816/health  # TTS
   
   # 메인 API 상태 확인
   curl http://localhost:7804/api/v1/health
   ```

## 더미 서버 API 엔드포인트

### LLM 서버 (포트 7808)
- `POST /api/completion` - 채팅 완성 (1-2초 딜레이)
- `POST /api/completion/stream` - 스트리밍 응답
- `GET /api/models` - 모델 목록
- `GET /health` - 헬스체크

### Memory 서버 (포트 7812)  
- `POST /api/memory/store` - 메모리 저장 (100ms 딜레이)
- `POST /api/memory/search` - 메모리 검색
- `GET /api/memory/:documentId` - 특정 메모리 조회
- `DELETE /api/memory/:documentId` - 메모리 삭제
- `GET /api/memory/user/:userId` - 사용자 메모리 목록
- `GET /api/memory/character/:characterId` - 캐릭터 메모리 목록
- `GET /api/memory/stats` - 메모리 통계
- `DELETE /api/memory/clear` - 모든 메모리 삭제
- `GET /health` - 헬스체크

### TTS 서버 (포트 7816)
- `POST /api/tts/synthesize` - 텍스트 음성 변환 (2-3초 딜레이)
- `POST /api/tts/synthesize/:voiceId` - 특정 음성으로 변환
- `GET /api/tts/voices` - 사용 가능한 음성 목록
- `POST /api/tts/voices/:voiceId/preview` - 음성 미리보기
- `POST /api/tts/batch` - 일괄 변환
- `GET /api/tts/stats` - TTS 통계
- `GET /health` - 헬스체크

## 부하 테스트 설정

### 완전 분리된 환경 (env.loadtest)
- **데이터베이스**: 별도 컨테이너 (포트 1434) - `ProjectVG_LoadTest` DB
- **Redis**: 별도 컨테이너 (포트 6381) - 독립적인 데이터 저장소
- **로그 레벨**: WARN으로 설정하여 성능 최적화
- **외부 서비스**: 더미 서버 포트로 설정
- **OAuth2**: 부하 테스트에서는 비활성화

### 운영 환경 보호
- **별도 포트**: 모든 서비스가 운영 환경과 다른 포트 사용
- **별도 데이터베이스**: `ProjectVG_LoadTest` (운영: `ProjectVG`)  
- **별도 비밀번호**: `LoadTest123!` (운영과 다른 패스워드)
- **독립적 볼륨**: 데이터 저장소 완전 분리

### API 최적화 설정 (appsettings.loadtest.json)
- 상세 로깅 비활성화
- Swagger 비활성화  
- 연결 풀 최적화
- 성능 카운터 활성화

## 성능 특성

### 응답 시간
- **LLM**: 1-2초 (실제 LLM 처리 시간 시뮬레이션)
- **Memory**: 100ms (빠른 벡터 검색 시뮬레이션)
- **TTS**: 2-3초 (음성 합성 처리 시간 시뮬레이션)

### 더미 데이터
- **LLM**: 가짜 채팅 응답 생성
- **Memory**: 10개 초기 더미 메모리, 가짜 벡터 임베딩
- **TTS**: WAV 형식 더미 오디오 데이터 (사인파 440Hz)

## 정리

```bash
# 부하 테스트 환경 종료
docker-compose -p projectvg-loadtest --env-file env.loadtest -f docker-compose.loadtest.yml down

# 이미지 정리 (선택사항)
docker rmi projectvg-loadtest-api:latest
docker rmi projectvg-dummy-llm:latest
docker rmi projectvg-dummy-memory:latest  
docker rmi projectvg-dummy-tts:latest
```

## 주의사항 및 안전성

### 운영 환경 보호 ✅
1. **완전히 분리된 데이터베이스**: 부하 테스트가 운영 데이터에 절대 영향을 미치지 않음
2. **독립적인 Redis**: 운영 세션 데이터와 완전 분리
3. **별도 포트 사용**: 모든 서비스가 운영 환경과 다른 포트 사용
4. **더미 외부 서비스**: 실제 LLM, Memory, TTS 서비스에 부하를 주지 않음

### 데이터 관리
1. **테스트 데이터 초기화**: 컨테이너 재시작시 깨끗한 상태로 시작
2. **볼륨 지속성**: SQL Server와 Redis 데이터는 볼륨에 저장되어 유지
3. **더미 서버 인메모리**: 더미 서버들은 인메모리 저장소 사용

### 성능 최적화
1. **로그 레벨 WARN**: 부하 테스트시 성능 향상을 위해 로깅 최소화
2. **Swagger 비활성화**: 프로덕션과 같은 환경 구성
3. **연결 풀 최적화**: 높은 동시 연결 처리를 위한 설정

### 사용 제한
1. **부하 테스트 전용**: 프로덕션 환경에서는 절대 사용하지 않음
2. **로컬 개발 전용**: 개발 서버나 운영 서버에 배포하지 않음