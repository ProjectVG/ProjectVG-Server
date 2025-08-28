# OAuth2 Test Client

ProjectVG OAuth2 Flow를 테스트하기 위한 더미 클라이언트입니다.

## 🚀 실행 방법

### 1. Python 서버 실행
```bash
cd test-clients
python start-oauth2-client.py
```

### 2. 브라우저에서 접속
```
http://localhost:3000
```

## 🔧 설정

### Google OAuth2 설정
1. [Google Cloud Console](https://console.cloud.google.com/)에서 프로젝트 생성
2. OAuth2 클라이언트 ID 생성
3. Authorized redirect URIs에 추가:
   ```
   http://localhost:7900/auth/oauth2/callback
   ```

### 테스트 클라이언트 설정
- **서버 URL**: `http://localhost:7900` (ProjectVG API 서버)
- **Client ID**: Google OAuth2 클라이언트 ID
- **Redirect URI**: `http://localhost:7900/auth/oauth2/callback`
- **Scope**: `openid profile email`

## 📋 테스트 단계

### 1. PKCE 생성
- "🔑 PKCE 생성" 버튼 클릭
- Code Verifier, Code Challenge, State가 자동 생성됨

### 2. OAuth2 로그인
- "🚀 OAuth2 로그인 시작" 버튼 클릭
- Google 로그인 페이지로 리다이렉트
- Google 계정으로 로그인 및 권한 동의

### 3. 결과 확인
- OAuth2 인증 완료 후 테스트 클라이언트로 리다이렉트
- 발급된 JWT 토큰과 사용자 정보 확인

## 🔍 OAuth2 Flow

```
1. 클라이언트 → GET /auth/oauth2/authorize → Google Auth URL 반환
2. 클라이언트 → Google Auth URL → 사용자 로그인 & 동의
3. Google → GET /auth/oauth2/callback?code=xxx&state=xxx
4. 서버 → Google Token API → Access Token 수신
5. 서버 → Google User Info API → 사용자 정보 수신
6. 서버 → 자체 JWT 발급 → 클라이언트로 리다이렉트
```

## 🛠️ 기능

- ✅ PKCE (Proof Key for Code Exchange) 생성
- ✅ OAuth2 Authorization Code Flow 테스트
- ✅ Google OAuth2 연동
- ✅ JWT 토큰 발급 확인
- ✅ 사용자 정보 표시
- ✅ 클립보드 복사 기능

## 📁 파일 구조

```
test-clients/
├── oauth2-test-client.html    # OAuth2 테스트 클라이언트 (HTML)
├── start-oauth2-client.py     # Python HTTP 서버
└── README.md                  # 사용법 설명
```

## 🔧 문제 해결

### 포트 3000이 이미 사용 중인 경우
```bash
# 포트 사용 확인
netstat -an | grep :3000

# 프로세스 종료
lsof -ti:3000 | xargs kill -9
```

### Google OAuth2 에러
- Client ID가 올바른지 확인
- Redirect URI가 Google Cloud Console에 등록되어 있는지 확인
- OAuth2 API가 활성화되어 있는지 확인

### 서버 연결 에러
- ProjectVG API 서버가 7900번 포트에서 실행 중인지 확인
- CORS 설정이 올바른지 확인

