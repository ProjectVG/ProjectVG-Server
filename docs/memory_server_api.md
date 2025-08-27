# Memory Server API 사용 가이드

## 개요

Memory Server는 인간의 기억 구조를 모방한 2가지 메모리 타입(Episodic, Semantic)을 지원하는 지능형 벡터 데이터베이스입니다.

- **기본 URL**: `http://memory-server`
- **API 버전**: v2.0
- **인증**: User ID 헤더 기반

## 빠른 시작

### 1. 서버 시작
```bash
docker-compose up --build
# 또는
python app.py
```

### 2. 기본 사용 예제
```bash
# 메모리 자동 분류 삽입
curl -X POST http://memory-server/api/memory \
  -H "Content-Type: application/json" \
  -d '{
    "text": "오늘 아침에 맛있는 커피를 마셨어",
    "user_id": "user123"
  }'

# 검색
curl "http://memory-server/api/memory/search?query=커피" \
  -H "X-User-ID: user123"
```

## API 엔드포인트 상세

### 📝 메모리 삽입

#### 1. 자동 분류 삽입
**POST** `/api/memory`

AI가 텍스트를 분석하여 자동으로 Episodic/Semantic 타입을 결정합니다.

**요청 예제:**
```json
{
  "text": "어제 친구와 영화를 봤는데 정말 재밌었어",
  "user_id": "user123",
  "speaker": "user",
  "emotion": {
    "valence": "positive",
    "arousal": "high",
    "labels": ["즐거움", "흥미"],
    "intensity": 0.8
  },
  "context": {
    "location": "cinema",
    "activity": "watching_movie"
  },
  "importance_score": 0.7
}
```

**응답 예제:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "memory_type": "episodic",
  "collection_name": "episodic",
  "user_id": "user123",
  "timestamp": "2025-08-26T07:15:00Z",
  "classification_confidence": 0.85,
  "classification_explanation": "episodic 타입으로 분류 (신뢰도: 0.85) - 시간 표현 1개, 감정 표현 1개 감지"
}
```

#### 2. 수동 타입 지정 삽입
**POST** `/api/memory/{memory_type}`

특정 메모리 타입을 직접 지정하여 삽입합니다.

**Episodic 메모리:**
```bash
curl -X POST http://memory-server/api/memory/episodic \
  -H "Content-Type: application/json" \
  -d '{
    "text": "점심시간에 동료와 프로젝트에 대해 이야기했어",
    "user_id": "user123",
    "speaker": "user",
    "context": {
      "location": "office",
      "conversation_id": "conv_001"
    },
    "importance_score": 0.6
  }'
```

**Semantic 메모리:**
```bash
curl -X POST http://memory-server/api/memory/semantic \
  -H "Content-Type: application/json" \
  -d '{
    "text": "내 생일은 3월 15일이다",
    "user_id": "user123",
    "fact_type": "personal_fact",
    "confidence_score": 1.0,
    "importance_score": 0.9
  }'
```

### 🔍 메모리 검색

#### 1. 단일 타입 검색
**GET** `/api/memory/{memory_type}/search`

특정 메모리 타입에서만 검색합니다.

**파라미터:**
- `query`: 검색 쿼리 (필수)
- `limit`: 결과 수 (기본값: 10)
- `similarity_threshold`: 유사도 임계값 (기본값: 0.0)

**헤더:**
- `X-User-ID`: 사용자 ID (필수)

**예제:**
```bash
# Episodic 메모리에서 검색
curl "http://memory-server/api/memory/episodic/search?query=영화&limit=5" \
  -H "X-User-ID: user123"

# Semantic 메모리에서 검색  
curl "http://memory-server/api/memory/semantic/search?query=생일&limit=3" \
  -H "X-User-ID: user123"
```

**응답 예제:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "text": "어제 친구와 영화를 봤는데 정말 재밌었어",
    "memory_type": "episodic",
    "collection_name": "episodic",
    "score": 0.892,
    "user_id": "user123",
    "timestamp": "2025-08-25T14:30:00Z",
    "importance_score": 0.7,
    "source": "conversation",
    "episodic_data": {
      "speaker": "user",
      "emotion": {
        "valence": "positive",
        "labels": ["즐거움"]
      },
      "context": {
        "location": "cinema"
      }
    }
  }
]
```

#### 2. 다중 타입 통합 검색
**GET** `/api/memory/search/multi`

모든 메모리 타입에서 검색하고 결과를 통합합니다.

**파라미터:**
- `query`: 검색 쿼리 (필수)
- `limit`: 총 결과 수 (기본값: 10)
- `episodic_weight`: Episodic 가중치 (기본값: 1.0)
- `semantic_weight`: Semantic 가중치 (기본값: 1.0)
- `use_intelligent_weights`: 지능형 가중치 사용 (기본값: false)
- `similarity_threshold`: 유사도 임계값 (기본값: 0.0)

**예제:**
```bash
# 수동 가중치 설정
curl "http://memory-server/api/memory/search/multi?query=커피&limit=10&episodic_weight=1.2&semantic_weight=0.8" \
  -H "X-User-ID: user123"

# 지능형 가중치 사용
curl "http://memory-server/api/memory/search/multi?query=커피&limit=10&use_intelligent_weights=true" \
  -H "X-User-ID: user123"
```

**응답 예제:**
```json
{
  "results": [
    {
      "id": "uuid1",
      "text": "오늘 아침에 맛있는 커피를 마셨어",
      "memory_type": "episodic",
      "score": 0.945,
      "user_id": "user123"
    },
    {
      "id": "uuid2", 
      "text": "나는 아메리카노를 좋아한다",
      "memory_type": "semantic",
      "score": 0.823,
      "user_id": "user123"
    }
  ],
  "collection_stats": {
    "episodic": 3,
    "semantic": 2
  },
  "total_results": 5,
  "query_time_ms": 45.2,
  "user_id": "user123",
  "query": "커피",
  "applied_weights": {
    "episodic": 1.2,
    "semantic": 0.8
  },
  "explanation": "수동 가중치 적용"
}
```

### 🧠 분류 및 분석

#### 텍스트 분류
**POST** `/api/classify/`

텍스트가 어떤 메모리 타입인지 미리 확인합니다.

**파라미터:**
- `text`: 분류할 텍스트 (필수)
- `context`: 추가 컨텍스트 (선택)

**예제:**
```bash
curl -X POST "http://memory-server/api/classify/" \
  -H "Content-Type: application/json" \
  -d '{"text": "오늘 기분이 좋아"}'
```

**응답 예제:**
```json
{
  "predicted_type": "episodic",
  "confidence": 0.78,
  "explanation": "episodic 타입으로 분류 (신뢰도: 0.78) - 시간 표현 1개, 감정 표현 1개 감지",
  "episodic_score": 7,
  "semantic_score": 2,
  "features": {
    "temporal_matches": 1,
    "emotional_matches": 1,
    "conversation_matches": 0,
    "factual_matches": 0,
    "profile_matches": 0
  }
}
```

#### 배치 분류
**POST** `/api/classify/batch`

여러 텍스트를 한 번에 분류합니다.

**요청 예제:**
```bash
curl -X POST "http://memory-server/api/classify/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "texts": ["오늘 기분이 좋아", "파리는 프랑스의 수도다", "어제 친구를 만났어"],
    "contexts": [null, null, null]
  }'
```

**응답 예제:**
```json
{
  "classifications": [
    {
      "text": "오늘 기분이 좋아",
      "predicted_type": "episodic",
      "confidence": 0.85,
      "explanation": "감정과 시간 표현 포함",
      "features": {"temporal_matches": 1, "emotional_matches": 1}
    }
  ],
  "statistics": {
    "total_texts": 3,
    "episodic_count": 2,
    "semantic_count": 1,
    "avg_confidence": 0.82
  }
}
```

#### 분류 신뢰도 임계값 조회
**GET** `/api/classify/confidence-threshold`

현재 설정된 분류 신뢰도 임계값을 조회합니다.

**예제:**
```bash
curl "http://memory-server/api/classify/confidence-threshold"
```

**응답 예제:**
```json
{
  "confidence_threshold": 0.7,
  "description": "신뢰도 0.7 이상에서 자동 분류를 신뢰할 수 있습니다.",
  "recommendation": "신뢰도가 0.7 미만인 경우 수동 분류를 고려하세요."
}
```

#### 분류 패턴 분석
**POST** `/api/classify/analyze-patterns`

텍스트 패턴을 분석하고 분류 특성을 추출합니다.

**요청 예제:**
```bash
curl -X POST "http://memory-server/api/classify/analyze-patterns" \
  -H "Content-Type: application/json" \
  -d '{
    "texts": ["오늘 학교에 갔어", "수학은 어려운 과목이다", "친구와 점심을 먹었다"]
  }'
```

#### 분류 검증
**POST** `/api/classify/validate-classification`

분류 결과를 검증하고 개선 제안을 받습니다.

**요청 예제:**
```bash
curl -X POST "http://memory-server/api/classify/validate-classification" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "오늘 기분이 좋아",
    "expected_type": "episodic"
  }'
```

### 👤 사용자 관리

#### 사용자 프로필 조회
**GET** `/api/users/{user_id}/profile`

사용자 프로필 정보를 조회합니다 (Semantic 메모리에서 추출).

**예제:**
```bash
curl "http://memory-server/api/users/user123/profile"
```

**응답 예제:**
```json
{
  "user_id": "user123",
  "profile": {
    "birthday": "3월 15일에 태어났다",
    "occupation": "소프트웨어 개발자로 일한다",
    "interests": "독서와 영화감상을 좋아한다"
  },
  "profile_completeness": 0.75,
  "last_updated": null
}
```

#### 사용자 분류 패턴 분석
**GET** `/api/users/{user_id}/classification-analysis`

사용자의 메모리 분류 패턴을 분석합니다.

**예제:**
```bash
curl "http://memory-server/api/users/user123/classification-analysis"
```

**응답 예제:**
```json
{
  "user_id": "user123",
  "total_memories": 150,
  "episodic_ratio": 0.667,
  "semantic_ratio": 0.333,
  "analysis": "주로 개인 경험과 대화 중심의 기억을 저장합니다.",
  "recommendations": ["지식이나 사실 정보도 함께 저장하면 더 균형잡힌 메모리 관리가 가능합니다."],
  "memory_type_preference": "episodic"
}
```

### 📊 통계 및 관리

#### 1. 사용자 메모리 통계
**GET** `/api/users/{user_id}/stats`

사용자의 메모리 사용 현황을 확인합니다.

**예제:**
```bash
curl http://memory-server/api/users/user123/stats
```

**응답 예제:**
```json
{
  "user_id": "user123",
  "total_memories": 147,
  "episodic_count": 89,
  "semantic_count": 58,
  "oldest_memory": "2025-08-01T10:00:00Z",
  "newest_memory": "2025-08-26T07:15:00Z",
  "daily_average": 4.2,
  "most_active_day": "2025-08-25"
}
```

#### 2. 시스템 전체 통계
**GET** `/api/system/stats`

전체 시스템 상태를 확인합니다.

**예제:**
```bash
curl http://memory-server/api/system/stats
```

#### 3. 사용자 메모리 삭제
**DELETE** `/api/users/{user_id}/memories`

사용자의 메모리를 삭제합니다.

**파라미터:**
- `memory_type`: 삭제할 메모리 타입 (선택, 전체 삭제시 생략)

**예제:**
```bash
# 특정 타입만 삭제
curl -X DELETE "http://memory-server/api/users/user123/memories?memory_type=episodic"

# 전체 삭제
curl -X DELETE "http://memory-server/api/users/user123/memories"
```

#### 4. 컬렉션 초기화
**POST** `/api/collections/{collection_name}/reset`

특정 컬렉션을 완전히 초기화합니다.

**예제:**
```bash
curl -X POST http://memory-server/api/collections/episodic/reset
```

## 메모리 타입별 필드 가이드

### Episodic Memory (경험 기억)
개인적 경험과 대화 중심의 메모리

**핵심 필드:**
- `speaker`: 발화자 ("user" 또는 "ai")
- `emotion`: 감정 정보 객체
  - `valence`: 긍정/부정 ("positive", "negative", "neutral")
  - `arousal`: 각성도 ("high", "medium", "low")
  - `labels`: 구체적 감정 라벨 배열
  - `intensity`: 강도 (0.0-1.0)
- `context`: 상황 정보 객체
  - `location`: 위치
  - `conversation_id`: 대화 세션 ID
  - `device`: 사용 기기
- `links`: 연관된 다른 메모리 ID 배열

**분류 신호:**
- 시간 표현: "오늘", "어제", "지금", "아까"
- 감정 표현: "기쁘다", "슬프다", "피곤해"
- 대화 표현: "말했다", "~라고", "~냐고"

### Semantic Memory (지식 기억)  
사실과 지식 중심의 메모리

**핵심 필드:**
- `fact_type`: 사실 유형
  - `"personal_fact"`: 개인 정보
  - `"world_fact"`: 일반 상식
  - `"ai_persona"`: AI 관련 정보
- `confidence_score`: 신뢰도 (0.0-1.0)
- `last_updated`: 마지막 업데이트 시간

**분류 신호:**
- 사실 표현: "~이다", "~입니다", "~됩니다" 
- 지식 표현: "정보", "사실", "개념"
- 프로필 표현: "생일", "취미", "직업", "좋아하다"

## 사용 패턴 및 Best Practices

### 1. 일반적인 사용 시나리오

#### 대화형 AI 시스템
```python
# 1. 사용자 발언 저장
response = requests.post("/api/memory", json={
    "text": user_input,
    "user_id": user_id,
    "speaker": "user"
})

# 2. AI 응답 저장  
response = requests.post("/api/memory", json={
    "text": ai_response,
    "user_id": user_id,
    "speaker": "ai"
})

# 3. 관련 기억 검색
memories = requests.get(f"/api/memory/search/multi?query={query}", 
                       headers={"X-User-ID": user_id})
```

#### 개인 지식베이스
```python
# 사실 정보 저장
requests.post("/api/memory/semantic", json={
    "text": "Python은 1991년에 만들어진 프로그래밍 언어다",
    "user_id": user_id,
    "fact_type": "world_fact",
    "confidence_score": 1.0
})

# 개인 정보 저장
requests.post("/api/memory/semantic", json={
    "text": "내 취미는 독서와 영화감상이다", 
    "user_id": user_id,
    "fact_type": "personal_fact"
})
```

### 2. 검색 최적화 팁

#### 가중치 조정
```python
# 감정적 경험 중심 검색
params = {
    "query": "기분",
    "episodic_weight": 1.5,
    "semantic_weight": 0.5
}

# 사실 정보 중심 검색
params = {
    "query": "Python 문법",
    "episodic_weight": 0.3,
    "semantic_weight": 1.8
}
```

#### 유사도 임계값 설정
```python
# 정확한 매칭만 원할 때
params = {"similarity_threshold": 0.7}

# 관련성 있는 모든 결과
params = {"similarity_threshold": 0.0}
```

### 3. 에러 처리

#### 일반적인 에러 응답
```json
{
  "detail": "메모리 삽입 실패: 벡터 생성 오류"
}
```

#### 상태 코드
- `200`: 성공
- `400`: 잘못된 요청 (필수 필드 누락 등)
- `404`: 리소스 없음 (사용자 또는 메모리 없음)
- `500`: 서버 내부 오류

### 4. 성능 고려사항

#### 배치 처리
대량의 메모리를 삽입할 때는 적절한 간격을 두고 처리하세요.

#### 캐싱
동일한 검색 쿼리는 자동으로 캐시되어 성능이 향상됩니다.

#### 인덱싱
user_id와 timestamp는 자동으로 인덱싱되어 빠른 검색이 가능합니다.

## 문제 해결

### 1. 자주 발생하는 문제

#### 분류 정확도가 낮을 때
```python
# 컨텍스트 정보를 더 제공
requests.post("/api/memory", json={
    "text": "좋아해",
    "user_id": user_id,
    "context": {
        "conversation_context": "취미에 대한 대화"
    }
})
```

#### 검색 결과가 부정확할 때
```python
# 더 구체적인 쿼리 사용
"커피" → "아침에 마신 커피"
"영화" → "어제 본 액션 영화"
```

### 2. 로그 확인
```bash
# 실시간 로그 확인
tail -f logs/app.log

# 디버그 모드 실행
LOG_LEVEL=DEBUG python app.py
```

### 3. 헬스 체크
```bash
# 서버 상태 확인
curl http://memory-server/health

# 데이터베이스 연결 확인
curl http://memory-server/system/info
```

## 추가 리소스

- **Swagger UI**: http://memory-server/docs
- **ReDoc**: http://memory-server/redoc
- **GitHub 이슈**: [프로젝트 저장소 이슈 페이지]
- **개발자 문서**: README_V2.md

---

이 가이드를 통해 Memory Server를 효과적으로 활용하여 지능적인 메모리 관리 시스템을 구축할 수 있습니다. 추가 질문이나 기능 요청은 언제든지 문의해 주세요.