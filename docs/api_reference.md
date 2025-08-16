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