# Unity 클라이언트 JWT 인증 가이드

## 개요
이 문서는 Unity 클라이언트(데스크탑, 모바일, WebGL)에서 JWT 인증을 구현하는 방법을 설명합니다.

## 인증 플로우

### 1. 로그인
```csharp
// Unity C# 예시
[System.Serializable]
public class LoginRequest
{
    public string user_id;
    public bool set_cookie = false; // Unity에서는 false
}

[System.Serializable]
public class TokenResponse
{
    public bool success;
    public Tokens tokens;
    public UserData user;
}

[System.Serializable]
public class Tokens
{
    public string access_token;
    public string refresh_token;
    public int expires_in;
    public string token_type;
}

// 로그인 요청
public async Task<TokenResponse> LoginAsync(string userId)
{
    var request = new LoginRequest
    {
        user_id = userId,
        set_cookie = false // Unity에서는 쿠키 사용 안함
    };

    var json = JsonUtility.ToJson(request);
    var requestData = System.Text.Encoding.UTF8.GetBytes(json);

    using (var www = UnityWebRequest.Post("http://localhost:7900/api/auth/test-login", ""))
    {
        www.uploadHandler = new UploadHandlerRaw(requestData);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var operation = www.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<TokenResponse>(www.downloadHandler.text);
            
            // 토큰 저장
            PlayerPrefs.SetString("access_token", response.tokens.access_token);
            PlayerPrefs.SetString("refresh_token", response.tokens.refresh_token);
            PlayerPrefs.SetInt("token_expires_at", (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + response.tokens.expires_in);
            PlayerPrefs.Save();
            
            return response;
        }
        else
        {
            Debug.LogError($"Login failed: {www.error}");
            return null;
        }
    }
}
```

### 2. API 요청 시 토큰 사용
```csharp
public async Task<string> MakeAuthenticatedRequestAsync(string url, string method = "GET", string body = null)
{
    var accessToken = PlayerPrefs.GetString("access_token", "");
    if (string.IsNullOrEmpty(accessToken))
    {
        Debug.LogError("No access token available");
        return null;
    }

    using (var www = new UnityWebRequest(url, method))
    {
        www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        www.SetRequestHeader("Content-Type", "application/json");
        
        if (!string.IsNullOrEmpty(body))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        }
        
        www.downloadHandler = new DownloadHandlerBuffer();

        var operation = www.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (www.result == UnityWebRequest.Result.Success)
        {
            return www.downloadHandler.text;
        }
        else if (www.responseCode == 401) // Unauthorized
        {
            // 토큰 갱신 시도
            var refreshResult = await RefreshTokenAsync();
            if (refreshResult)
            {
                // 재시도
                return await MakeAuthenticatedRequestAsync(url, method, body);
            }
        }
        
        Debug.LogError($"Request failed: {www.error}");
        return null;
    }
}
```

### 3. 토큰 갱신
```csharp
public async Task<bool> RefreshTokenAsync()
{
    var refreshToken = PlayerPrefs.GetString("refresh_token", "");
    if (string.IsNullOrEmpty(refreshToken))
    {
        Debug.LogError("No refresh token available");
        return false;
    }

    var request = new RefreshTokenRequest
    {
        refresh_token = refreshToken,
        set_cookie = false
    };

    var json = JsonUtility.ToJson(request);
    var requestData = System.Text.Encoding.UTF8.GetBytes(json);

    using (var www = UnityWebRequest.Post("http://localhost:7900/api/auth/refresh", ""))
    {
        www.uploadHandler = new UploadHandlerRaw(requestData);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var operation = www.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<TokenResponse>(www.downloadHandler.text);
            
            // 새 토큰 저장
            PlayerPrefs.SetString("access_token", response.tokens.access_token);
            PlayerPrefs.SetString("refresh_token", response.tokens.refresh_token);
            PlayerPrefs.SetInt("token_expires_at", (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + response.tokens.expires_in);
            PlayerPrefs.Save();
            
            return true;
        }
        else
        {
            Debug.LogError($"Token refresh failed: {www.error}");
            // 로그인 페이지로 리다이렉트
            ClearTokens();
            return false;
        }
    }
}

[System.Serializable]
public class RefreshTokenRequest
{
    public string refresh_token;
    public bool set_cookie = false;
}
```

### 4. 로그아웃
```csharp
public async Task<bool> LogoutAsync()
{
    var refreshToken = PlayerPrefs.GetString("refresh_token", "");
    if (string.IsNullOrEmpty(refreshToken))
    {
        ClearTokens();
        return true;
    }

    var request = new LogoutRequest
    {
        refresh_token = refreshToken
    };

    var json = JsonUtility.ToJson(request);
    var requestData = System.Text.Encoding.UTF8.GetBytes(json);

    using (var www = UnityWebRequest.Post("http://localhost:7900/api/auth/logout", ""))
    {
        www.uploadHandler = new UploadHandlerRaw(requestData);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var operation = www.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        ClearTokens();
        return www.result == UnityWebRequest.Result.Success;
    }
}

[System.Serializable]
public class LogoutRequest
{
    public string refresh_token;
}

private void ClearTokens()
{
    PlayerPrefs.DeleteKey("access_token");
    PlayerPrefs.DeleteKey("refresh_token");
    PlayerPrefs.DeleteKey("token_expires_at");
    PlayerPrefs.Save();
}
```

## 토큰 저장 방식

### 1. PlayerPrefs (권장)
- **장점**: 간단하고 Unity 내장
- **단점**: 암호화되지 않음
- **용도**: 개발 및 테스트

### 2. 파일 시스템 + 암호화
```csharp
public class SecureTokenStorage
{
    private static readonly string KEY = "your-encryption-key";
    
    public static void SaveTokens(string accessToken, string refreshToken)
    {
        var data = new TokenData
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            expires_at = DateTime.UtcNow.AddHours(1).Ticks
        };
        
        var json = JsonUtility.ToJson(data);
        var encrypted = EncryptString(json, KEY);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "tokens.dat"), encrypted);
    }
    
    public static TokenData LoadTokens()
    {
        var path = Path.Combine(Application.persistentDataPath, "tokens.dat");
        if (!File.Exists(path)) return null;
        
        var encrypted = File.ReadAllText(path);
        var json = DecryptString(encrypted, KEY);
        return JsonUtility.FromJson<TokenData>(json);
    }
    
    // 간단한 암호화/복호화 (실제로는 더 강력한 방법 사용)
    private static string EncryptString(string text, string key) { /* 구현 */ }
    private static string DecryptString(string text, string key) { /* 구현 */ }
}

[System.Serializable]
public class TokenData
{
    public string access_token;
    public string refresh_token;
    public long expires_at;
}
```

## 플랫폼별 고려사항

### 1. 데스크탑 (Windows/Mac/Linux)
- PlayerPrefs 사용 가능
- 파일 시스템 접근 가능
- 네트워크 제한 없음

### 2. 모바일 (iOS/Android)
- PlayerPrefs 사용 가능
- 파일 시스템 접근 가능
- 네트워크 보안 정책 준수 필요

### 3. WebGL
- PlayerPrefs 사용 가능 (브라우저 저장소)
- 파일 시스템 접근 제한
- CORS 정책 준수 필요

## 보안 고려사항

1. **토큰 암호화**: 프로덕션에서는 토큰을 암호화하여 저장
2. **토큰 만료**: 만료된 토큰은 즉시 삭제
3. **네트워크 보안**: HTTPS 사용 필수
4. **토큰 노출 방지**: 로그에 토큰 출력 금지

## 에러 처리

```csharp
public enum AuthError
{
    None,
    NetworkError,
    InvalidToken,
    TokenExpired,
    RefreshFailed,
    ServerError
}

public class AuthResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public AuthError Error { get; set; }
    public string ErrorMessage { get; set; }
}
```
