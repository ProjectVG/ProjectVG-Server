# Unity OAuth2 인증 가이드

## 개요

ProjectVG API는 Google과 Apple OAuth2 인증을 지원합니다. Unity 클라이언트에서 OAuth2 인증을 구현하는 방법을 설명합니다.

## 지원하는 OAuth2 제공자

- **Google** (`google`): Google OAuth2
- **Apple** (`apple`): Apple Sign-In

## API 엔드포인트

### 1. 지원하는 제공자 목록 조회

```http
GET /auth/oauth2/providers
```

**응답:**
```json
{
  "success": true,
  "providers": ["google", "apple"]
}
```

### 2. OAuth2 인증 URL 생성

```http
GET /auth/oauth2/authorize/{provider}?state={state}&code_challenge={code_challenge}&code_challenge_method=S256&code_verifier={code_verifier}&client_redirect_uri={client_redirect_uri}
```

**파라미터:**
- `provider`: 제공자 이름 (`google` 또는 `apple`)
- `state`: CSRF 보호를 위한 상태값
- `code_challenge`: PKCE code challenge
- `code_challenge_method`: PKCE challenge 방법 (항상 `S256`)
- `code_verifier`: PKCE code verifier
- `client_redirect_uri`: 클라이언트 리다이렉트 URI

**응답:**
```json
{
  "success": true,
  "provider": "google",
  "auth_url": "https://accounts.google.com/o/oauth2/v2/auth?..."
}
```

### 3. OAuth2 콜백 처리

```http
GET /auth/oauth2/callback?code={code}&state={state}
GET /auth/oauth2/callback/{provider}?code={code}&state={state}
```

### 4. 토큰 조회

```http
GET /auth/oauth2/token?state={state}
```

**응답 헤더:**
- `X-Access-Token`: 액세스 토큰
- `X-Refresh-Token`: 리프레시 토큰
- `X-Expires-In`: 토큰 만료 시간 (초)
- `X-UID`: 사용자 UID

## Unity 구현 예시

### 1. PKCE 생성

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public class PKCEGenerator
{
    public static (string codeVerifier, string codeChallenge) GeneratePKCE()
    {
        // Code Verifier 생성 (43-128자 랜덤 문자열)
        var codeVerifier = GenerateRandomString(64);
        
        // Code Challenge 생성 (SHA256 해시)
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        return (codeVerifier, codeChallenge);
    }
    
    private static string GenerateRandomString(int length)
    {
        const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        var result = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(charset[random.Next(charset.Length)]);
        }
        
        return result.ToString();
    }
    
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(codeVerifier);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
```

### 2. OAuth2 인증 플로우

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class OAuth2Manager : MonoBehaviour
{
    private const string SERVER_URL = "http://localhost:7900";
    private string accessToken;
    private string refreshToken;
    
    public async void StartOAuth2Flow(string provider)
    {
        try
        {
            // 1. PKCE 생성
            var (codeVerifier, codeChallenge) = PKCEGenerator.GeneratePKCE();
            var state = PKCEGenerator.GenerateRandomString(32);
            
            // 2. OAuth2 인증 URL 요청
            var authUrl = await GetOAuth2AuthUrl(provider, state, codeChallenge, codeVerifier);
            
            // 3. 브라우저에서 OAuth2 인증
            Application.OpenURL(authUrl);
            
            // 4. 콜백 처리 (Unity WebGL 또는 모바일에서 처리)
            StartCoroutine(HandleOAuth2Callback(state));
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OAuth2 인증 실패: {e.Message}");
        }
    }
    
    private async Task<string> GetOAuth2AuthUrl(string provider, string state, string codeChallenge, string codeVerifier)
    {
        var clientRedirectUri = "your-app-scheme://oauth2-callback";
        var url = $"{SERVER_URL}/auth/oauth2/authorize/{provider}?" +
                 $"state={state}&" +
                 $"code_challenge={codeChallenge}&" +
                 $"code_challenge_method=S256&" +
                 $"code_verifier={UnityWebRequest.EscapeURL(codeVerifier)}&" +
                 $"client_redirect_uri={UnityWebRequest.EscapeURL(clientRedirectUri)}";
        
        using (var request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
                await Task.Yield();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<OAuth2AuthResponse>(request.downloadHandler.text);
                return response.auth_url;
            }
            else
            {
                throw new System.Exception($"OAuth2 URL 요청 실패: {request.error}");
            }
        }
    }
    
    private IEnumerator HandleOAuth2Callback(string state)
    {
        // 실제 구현에서는 콜백 URL을 처리하는 방법이 필요
        // Unity WebGL: URL 파라미터 확인
        // 모바일: 커스텀 URL 스킴 처리
        
        yield return new WaitForSeconds(2); // 임시 대기
        
        // 토큰 요청
        yield return StartCoroutine(GetOAuth2Token(state));
    }
    
    private IEnumerator GetOAuth2Token(string state)
    {
        var url = $"{SERVER_URL}/auth/oauth2/token?state={state}";
        
        using (var request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                accessToken = request.GetResponseHeader("X-Access-Token");
                refreshToken = request.GetResponseHeader("X-Refresh-Token");
                
                Debug.Log($"OAuth2 로그인 성공! Access Token: {accessToken?.Substring(0, 20)}...");
                
                // 토큰을 안전하게 저장 (PlayerPrefs + 암호화 권장)
                PlayerPrefs.SetString("AccessToken", accessToken);
                PlayerPrefs.SetString("RefreshToken", refreshToken);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError($"토큰 요청 실패: {request.error}");
            }
        }
    }
}

[System.Serializable]
public class OAuth2AuthResponse
{
    public bool success;
    public string provider;
    public string auth_url;
}
```

### 3. 토큰 관리

```csharp
public class TokenManager
{
    private const string ACCESS_TOKEN_KEY = "AccessToken";
    private const string REFRESH_TOKEN_KEY = "RefreshToken";
    
    public static string GetAccessToken()
    {
        return PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
    }
    
    public static string GetRefreshToken()
    {
        return PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
    }
    
    public static void SaveTokens(string accessToken, string refreshToken)
    {
        PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
        PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);
        PlayerPrefs.Save();
    }
    
    public static void ClearTokens()
    {
        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.Save();
    }
    
    public static bool HasValidToken()
    {
        return !string.IsNullOrEmpty(GetAccessToken());
    }
}
```

### 4. API 요청에 토큰 사용

```csharp
public class APIClient
{
    private const string SERVER_URL = "http://localhost:7900";
    
    public static IEnumerator MakeAuthenticatedRequest(string endpoint, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var accessToken = TokenManager.GetAccessToken();
        
        if (string.IsNullOrEmpty(accessToken))
        {
            onError?.Invoke("인증 토큰이 없습니다.");
            yield break;
        }
        
        var url = $"{SERVER_URL}{endpoint}";
        
        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }
}
```

## 보안 고려사항

1. **토큰 저장**: PlayerPrefs는 암호화 없이 사용하지 마세요
2. **HTTPS 사용**: 프로덕션에서는 반드시 HTTPS를 사용하세요
3. **토큰 만료 처리**: 액세스 토큰이 만료되면 리프레시 토큰으로 갱신하세요
4. **상태값 검증**: OAuth2 state 파라미터를 항상 검증하세요

## 에러 처리

OAuth2 인증 중 발생할 수 있는 주요 에러:

- `OAUTH2_PROVIDER_NOT_SUPPORTED`: 지원하지 않는 제공자
- `OAUTH2_PKCE_INVALID`: 유효하지 않은 PKCE 파라미터
- `OAUTH2_REQUEST_NOT_FOUND`: 만료되거나 유효하지 않은 요청
- `OAUTH2_TOKEN_EXCHANGE_FAILED`: 토큰 교환 실패

## 테스트

OAuth2 기능을 테스트하려면 `test-clients/oauth2-test-client.html`을 사용하세요.
