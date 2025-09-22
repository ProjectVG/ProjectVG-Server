# ProjectVG API Reference

## 공통 사항
- **Base URL (개발)**: `http://localhost:7901/api/v1/`
- 모든 응답은 JSON 형식입니다.
- 인증 필요 없음 (`[AllowAnonymous]`)

---

## 1. Chat API

### **POST /api/v1/chat**

채팅 요청을 큐에 등록합니다.

#### 요청

- **Content-Type**: `application/json`
- **Body 예시**:
    ```json
    {
      "sessionId": "string (optional)",
      "actor": "string",
      "message": "string",
      "action": "chat",
      "character_id": "string (GUID)",
      "user_id": "string (GUID)"
    }
    ```

#### 응답

- **성공 (200 OK)**
    ```json
    {
      "success": true,
      "status": "ACCEPTED",
      "message": "채팅 요청이 성공적으로 수락되었습니다. 처리 중입니다.",
      "sessionId": "string",
      "userId": "string (GUID)",
      "characterId": "string (GUID)",
      "requestedAt": "2024-01-01T00:00:00Z"
    }
    ```

- **실패 (400 Bad Request)**
    ```json
    {
      "success": false,
      "status": "REJECTED",
      "message": "오류 메시지",
      "errorCode": "ERROR_CODE",
      "requestedAt": "2024-01-01T00:00:00Z"
    }
    ```

---

## 2. Character API

### **GET /api/v1/character**

모든 캐릭터 목록을 조회합니다.

#### 응답

- **성공 (200 OK)**
    ```json
    [
      {
        "id": "string (GUID)",
        "name": "string",
        "description": "string",
        "role": "string",
        "isActive": true
      }
    ]
    ```

---

### **GET /api/v1/character/{id}**

특정 캐릭터 정보를 조회합니다.

#### 응답

- **성공 (200 OK)**
    ```json
    {
      "id": "string (GUID)",
      "name": "string",
      "description": "string",
      "role": "string",
      "isActive": true
    }
    ```
- **실패 (404 Not Found)**
    ```json
    {
      "error": "ID {id}인 캐릭터를 찾을 수 없습니다."
    }
    ```

---

### **POST /api/v1/character**

캐릭터를 생성합니다.

#### 요청

- **Content-Type**: `application/json`
- **Body 예시**:
    ```json
    {
      "name": "string",
      "description": "string",
      "role": "string",
      "isActive": true
    }
    ```

#### 응답

- **성공 (201 Created)**
    ```json
    {
      "id": "string (GUID)",
      "name": "string",
      "description": "string",
      "role": "string",
      "isActive": true
    }
    ```
- **실패 (400 Bad Request)**
    - 유효성 검사 실패 시

---

### **PUT /api/v1/character/{id}**

캐릭터 정보를 수정합니다.

#### 요청

- **Content-Type**: `application/json`
- **Body 예시**:
    ```json
    {
      "name": "string",
      "description": "string",
      "role": "string",
      "isActive": true
    }
    ```

#### 응답

- **성공 (200 OK)**
    ```json
    {
      "id": "string (GUID)",
      "name": "string",
      "description": "string",
      "role": "string",
      "isActive": true
    }
    ```
- **실패 (404 Not Found)**
    ```json
    {
      "error": "ID {id}인 캐릭터를 찾을 수 없습니다."
    }
    ```

---

### **DELETE /api/v1/character/{id}**

캐릭터를 삭제합니다.

#### 응답

- **성공 (204 No Content)**
- **실패 (500 Internal Server Error)**
    ```json
    {
      "error": "캐릭터 삭제 중 내부 서버 오류가 발생했습니다."
    }
    ```

---

## 3. WebSocket Chat API

### **WebSocket Endpoint: `/ws`**

실시간 채팅 메시지를 수신합니다.

#### 연결
- **URL**: `ws://localhost:7901/ws`
- **인증**: JWT 토큰을 쿼리 파라미터 또는 헤더로 전송

#### 수신 메시지 구조

모든 WebSocket 메시지는 다음과 같은 일관된 구조를 따릅니다:

```json
{
  "type": "chat",
  "message_type": "json",
  "data": {
    "text": "안녕하세요! 반가워요!",
    "emotion": "happy",
    "actions": ["clapping", "jumping"],
    "order": 0,
    "request_id": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-01T00:00:00.000Z",
    "audio_data": "UklGRnoGAABXQVZFZm10IBAAAA...",
    "audio_format": "wav",
    "audio_length": 3.5
  }
}
```

#### 필드 설명

- **type** (string): 항상 `"chat"`
- **message_type** (string): 메시지 포맷, 항상 `"json"`
- **data.text** (string): 메시지 텍스트 내용
- **data.emotion** (string, optional): 감정 상태 (예: `"happy"`, `"sad"`, `"neutral"`)
- **data.actions** (array, optional): 액션/행동 배열 (예: `["clapping", "jumping"]`)
- **data.order** (number): 메시지 순서
- **data.request_id** (string): 원본 요청의 고유 ID (GUID)
- **data.timestamp** (string): ISO 8601 형식의 타임스탬프
- **data.audio_data** (string, optional): Base64로 인코딩된 오디오 데이터
- **data.audio_format** (string, optional): 오디오 형식 (`"wav"`, `"mp3"` 등)
- **data.audio_length** (number, optional): 오디오 길이 (초 단위)

---

## 4. Unity 클라이언트 구현 가이드

### C# 데이터 모델

```csharp
[System.Serializable]
public class WebSocketResponse
{
    public string type;          // "chat"
    public string message_type;  // "json"
    public ChatData data;
}

[System.Serializable]
public class ChatData
{
    public string text;
    public string emotion;       // "happy", "sad", "neutral" etc (optional)
    public string[] actions;     // ["clapping", "jumping"] (optional)
    public int order;            // 메시지 순서
    public string request_id;    // GUID
    public string timestamp;     // ISO 8601
    public string audio_data;    // Base64 encoded (optional)
    public string audio_format;  // "wav", "mp3" etc (optional)
    public float audio_length;   // 초 단위 (optional)
}
```

### 메시지 수신 처리

```csharp
void OnWebSocketMessage(string message)
{
    try
    {
        var response = JsonUtility.FromJson<WebSocketResponse>(message);
        
        if (response.type == "chat")
        {
            var chatData = response.data;
            
            // 통합된 메시지 처리
            DisplayChatMessage(chatData.text, chatData.request_id, chatData.order);
            
            // 감정 처리
            if (!string.IsNullOrEmpty(chatData.emotion))
            {
                ProcessEmotion(chatData.emotion, chatData.request_id);
            }
            
            // 액션 처리
            if (chatData.actions != null && chatData.actions.Length > 0)
            {
                ProcessActions(chatData.actions, chatData.request_id);
            }
            
            // 오디오 데이터 처리
            if (!string.IsNullOrEmpty(chatData.audio_data))
            {
                PlayAudioFromBase64(chatData.audio_data, chatData.audio_format);
            }
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"WebSocket 메시지 파싱 오류: {ex.Message}");
    }
}

void DisplayChatMessage(string text, string requestId, int order)
{
    // UI에 채팅 메시지 표시
    Debug.Log($"[Chat] {text} (Request: {requestId}, Order: {order})");
}

void ProcessEmotion(string emotion, string requestId)
{
    // 감정 상태 처리 (예: 표정 변경, UI 색상 변경)
    Debug.Log($"[Emotion] {emotion} (Request: {requestId})");
}

void ProcessActions(string[] actions, string requestId)
{
    // 액션 배열 처리 (예: 순차적 애니메이션 실행)
    foreach (var action in actions)
    {
        Debug.Log($"[Action] {action} (Request: {requestId})");
        // 각 액션에 대한 애니메이션이나 효과 트리거
        TriggerAnimation(action);
    }
}

void PlayAudioFromBase64(string audioData, string format)
{
    // Base64 오디오 데이터를 AudioClip으로 변환 후 재생
    byte[] audioBytes = System.Convert.FromBase64String(audioData);
    // AudioClip 생성 및 재생 로직...
}
```

### 요청 추적

`request_id`를 사용하여 특정 채팅 요청에 대한 모든 응답을 추적할 수 있습니다:

```csharp
private Dictionary<string, List<ChatData>> responseTracker = 
    new Dictionary<string, List<ChatData>>();

void TrackResponse(ChatData chatData)
{
    string requestId = chatData.request_id;
    
    if (!responseTracker.ContainsKey(requestId))
    {
        responseTracker[requestId] = new List<ChatData>();
    }
    
    responseTracker[requestId].Add(chatData);
    
    // order 필드를 사용하여 메시지 순서 관리
    responseTracker[requestId].Sort((a, b) => a.order.CompareTo(b.order));
    
    // 요청 완료 여부 확인 (예: 마지막 세그먼트인지)
    if (IsLastSegment(chatData))
    {
        OnRequestComplete(requestId, responseTracker[requestId]);
        responseTracker.Remove(requestId);
    }
}
``` 