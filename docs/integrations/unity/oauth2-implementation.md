# Unity OAuth2 Authorization Code + PKCE 구현 가이드

## 개요
Unity에서 Authorization Code + PKCE 방식을 사용한 OAuth2 인증 구현 방법을 설명합니다.

## 1. Unity OAuth2 PKCE 핸들러

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class OAuth2PKCEHandler : MonoBehaviour
{
    [Header("OAuth2 Settings")]
    public string clientId = "unity_client";
    public string redirectUri = "projectvg://oauth-callback";
    public string authServerUrl = "http://localhost:7900";
    
    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject loadingPanel;
    public GameObject mainGamePanel;
    
    private string currentState;
    private string currentCodeVerifier;
    private bool isAuthenticated = false;
    
    void Start()
    {
        // Deep Link 이벤트 등록
        Application.deepLinkActivated += OnDeepLinkActivated;
        
        // 저장된 토큰 확인
        CheckSavedTokens();
    }
    
    void OnDestroy()
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }
    
    // PKCE Code Verifier 생성
    private string GenerateCodeVerifier()
    {
        var randomBytes = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
    
    // PKCE Code Challenge 생성
    private string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
    
    // OAuth2 로그인 시작
    public void StartOAuth2Login()
    {
        currentState = Guid.NewGuid().ToString();
        currentCodeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(currentCodeVerifier);
        
        var authUrl = $"{authServerUrl}/api/auth/oauth2/authorize?" +
                     $"client_id={clientId}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"response_type=code&" +
                     $"scope=chat&" +
                     $"state={currentState}&" +
                     $"code_challenge={codeChallenge}&" +
                     $"code_challenge_method=S256";
        
        Debug.Log($"OAuth2 URL: {authUrl}");
        
        // 플랫폼별 브라우저 열기
        OpenOAuth2Url(authUrl);
        
        // 로딩 UI 표시
        ShowLoadingPanel();
    }
    
    // 플랫폼별 URL 열기
    private void OpenOAuth2Url(string url)
    {
        #if UNITY_ANDROID
            // Android에서 브라우저 열기
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
                intent.Call<AndroidJavaObject>("setData", new AndroidJavaObject("android.net.Uri", url));
                activity.Call("startActivity", intent);
            }
        #elif UNITY_IOS
            // iOS에서 브라우저 열기
            Application.OpenURL(url);
        #elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            // PC에서 브라우저 열기
            Application.OpenURL(url);
        #endif
    }
    
    // Deep Link 콜백 처리
    void OnDeepLinkActivated(string url)
    {
        Debug.Log($"Deep Link Activated: {url}");
        
        try
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            
            // state 확인
            var state = query["state"];
            if (state != currentState)
            {
                Debug.LogError("State mismatch in OAuth2 callback");
                ShowLoginPanel();
                return;
            }
            
            // 에러 처리
            var error = query["error"];
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"OAuth2 Error: {error}");
                ShowLoginPanel();
                return;
            }
            
            // Authorization Code 추출
            var code = query["code"];
            if (!string.IsNullOrEmpty(code))
            {
                // Authorization Code로 토큰 교환
                ExchangeCodeForTokens(code);
            }
            else
            {
                Debug.LogError("No authorization code received");
                ShowLoginPanel();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing OAuth2 callback: {ex.Message}");
            ShowLoginPanel();
        }
    }
    
    // Authorization Code로 토큰 교환
    private async void ExchangeCodeForTokens(string code)
    {
        try
        {
            var request = new TokenExchangeRequest
            {
                client_id = clientId,
                redirect_uri = redirectUri,
                code = code,
                code_verifier = currentCodeVerifier,
                grant_type = "authorization_code"
            };
            
            var json = JsonUtility.ToJson(request);
            var requestData = Encoding.UTF8.GetBytes(json);
            
            using (var www = UnityWebRequest.Post($"{authServerUrl}/api/auth/oauth2/exchange", ""))
            {
                www.uploadHandler = new UploadHandlerRaw(requestData);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                var operation = www.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<TokenResponse>(www.downloadHandler.text);
                    
                    if (response.success)
                    {
                        // 토큰 저장
                        SaveTokens(response.tokens.access_token, response.tokens.refresh_token, response.tokens.expires_in);
                        
                        // 인증 완료
                        OnAuthenticationSuccess();
                    }
                    else
                    {
                        Debug.LogError($"Token exchange failed: {response.message}");
                        ShowLoginPanel();
                    }
                }
                else
                {
                    Debug.LogError($"Token exchange request failed: {www.error}");
                    ShowLoginPanel();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error exchanging code for tokens: {ex.Message}");
            ShowLoginPanel();
        }
    }
    
    // 토큰 저장
    private void SaveTokens(string accessToken, string refreshToken, int expiresIn)
    {
        var jwtManager = GetComponent<JWTManager>();
        jwtManager.SaveJWT(accessToken, refreshToken, expiresIn);
        
        Debug.Log("Tokens saved successfully");
    }
    
    // 저장된 토큰 확인
    private void CheckSavedTokens()
    {
        var jwtManager = GetComponent<JWTManager>();
        if (jwtManager.IsJWTValid())
        {
            // 유효한 토큰이 있음
            OnAuthenticationSuccess();
        }
        else
        {
            // 토큰이 없거나 만료됨
            ShowLoginPanel();
        }
    }
    
    // 인증 성공 처리
    private void OnAuthenticationSuccess()
    {
        isAuthenticated = true;
        ShowMainGamePanel();
        
        Debug.Log("OAuth2 authentication successful");
    }
    
    // 로그아웃
    public void Logout()
    {
        var jwtManager = GetComponent<JWTManager>();
        jwtManager.ClearJWT();
        
        isAuthenticated = false;
        ShowLoginPanel();
        
        Debug.Log("Logged out successfully");
    }
    
    // UI 패널 관리
    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        loadingPanel.SetActive(false);
        mainGamePanel.SetActive(false);
    }
    
    private void ShowLoadingPanel()
    {
        loginPanel.SetActive(false);
        loadingPanel.SetActive(true);
        mainGamePanel.SetActive(false);
    }
    
    private void ShowMainGamePanel()
    {
        loginPanel.SetActive(false);
        loadingPanel.SetActive(false);
        mainGamePanel.SetActive(true);
    }
}

[System.Serializable]
public class TokenExchangeRequest
{
    public string client_id;
    public string redirect_uri;
    public string code;
    public string code_verifier;
    public string grant_type;
}

[System.Serializable]
public class TokenResponse
{
    public bool success;
    public Tokens tokens;
    public UserData user;
    public string message;
}

[System.Serializable]
public class Tokens
{
    public string access_token;
    public string refresh_token;
    public int expires_in;
    public string token_type;
}

[System.Serializable]
public class UserData
{
    public string user_id;
    public string username;
    // 기타 사용자 데이터
}
```

## 2. 서버 측 OAuth2 PKCE 구현

### **OAuth2 컨트롤러 확장**
```csharp
[HttpGet("oauth2/authorize")]
public IActionResult OAuth2Authorize(
    [FromQuery] string client_id, 
    [FromQuery] string redirect_uri, 
    [FromQuery] string response_type, 
    [FromQuery] string scope,
    [FromQuery] string state,
    [FromQuery] string code_challenge,
    [FromQuery] string code_challenge_method)
{
    // PKCE 검증
    if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256")
    {
        return BadRequest("Invalid PKCE parameters");
    }
    
    // state와 code_challenge를 임시 저장
    var authRequest = new OAuth2AuthRequest
    {
        ClientId = client_id,
        RedirectUri = redirect_uri,
        State = state,
        CodeChallenge = code_challenge,
        CodeChallengeMethod = code_challenge_method,
        CreatedAt = DateTime.UtcNow
    };
    
    // Redis나 메모리에 임시 저장 (5분 만료)
    _authService.StoreOAuth2Request(state, authRequest);
    
    // 로그인 페이지로 리다이렉트
    return Redirect($"/oauth2/login?client_id={client_id}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}");
}

[HttpPost("oauth2/exchange")]
public async Task<IActionResult> OAuth2Exchange([FromBody] TokenExchangeRequest request)
{
    try
    {
        // PKCE 검증
        var authRequest = await _authService.GetOAuth2Request(request.state);
        if (authRequest == null)
        {
            return BadRequest(new { success = false, message = "Invalid or expired authorization request" });
        }
        
        // code_verifier로 code_challenge 검증
        var expectedChallenge = GenerateCodeChallenge(request.code_verifier);
        if (expectedChallenge != authRequest.CodeChallenge)
        {
            return BadRequest(new { success = false, message = "Invalid code verifier" });
        }
        
        // Authorization Code로 토큰 교환
        var tokenResponse = await _authService.ExchangeAuthorizationCodeAsync(
            request.code, 
            request.client_id, 
            request.redirect_uri);
        
        if (tokenResponse.IsSuccess)
        {
            // 사용된 authorization request 삭제
            await _authService.DeleteOAuth2Request(request.state);
            
            return Ok(new
            {
                success = true,
                tokens = new
                {
                    access_token = tokenResponse.Tokens.AccessToken,
                    refresh_token = tokenResponse.Tokens.RefreshToken,
                    expires_in = tokenResponse.Tokens.ExpiresIn,
                    token_type = "Bearer"
                },
                user = tokenResponse.User
            });
        }
        
        return BadRequest(new
        {
            success = false,
            message = tokenResponse.ErrorMessage
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Internal server error",
            error = ex.Message
        });
    }
}

private string GenerateCodeChallenge(string codeVerifier)
{
    using (var sha256 = SHA256.Create())
    {
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

[HttpGet("oauth2/callback")]
public async Task<IActionResult> OAuth2Callback([FromQuery] string code, [FromQuery] string state)
{
    try
    {
        // Authorization Code 생성
        var authCode = await _authService.CreateAuthorizationCodeAsync(code, state);
        
        if (authCode.IsSuccess)
        {
            // Unity 앱으로 리다이렉트
            var redirectUrl = $"projectvg://oauth-callback?code={authCode.Code}&state={state}";
            return Redirect(redirectUrl);
        }
        
        return BadRequest("Failed to create authorization code");
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Internal server error");
    }
}
```

## 3. 보안 고려사항

### **PKCE 보안 이점**
1. **Authorization Code Interception 방지**: code_verifier로 추가 검증
2. **CSRF 공격 방지**: state 파라미터 사용
3. **Code Replay 공격 방지**: 일회성 authorization code

### **추가 보안 조치**
```csharp
// Authorization Code 만료 시간 설정 (10분)
public class AuthorizationCode
{
    public string Code { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    public bool IsUsed { get; set; } = false;
}

// OAuth2 Request 만료 시간 설정 (5분)
public class OAuth2AuthRequest
{
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string State { get; set; }
    public string CodeChallenge { get; set; }
    public string CodeChallengeMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
}
```

## 4. 플랫폼별 설정

### **Android Manifest**
```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="projectvg" />
    </intent-filter>
</activity>
```

### **iOS Info.plist**
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.projectvg.oauth</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>projectvg</string>
        </array>
    </dict>
</array>
```

## 5. 테스트 방법

### **Unity 에디터에서 테스트**
```csharp
// 에디터에서 테스트용
#if UNITY_EDITOR
[ContextMenu("Test OAuth2 Flow")]
public void TestOAuth2Flow()
{
    StartOAuth2Login();
}
#endif
```

### **실제 디바이스 테스트**
```bash
# Android ADB 테스트
adb shell am start -W -a android.intent.action.VIEW -d "projectvg://oauth-callback?code=test123&state=test" com.yourcompany.yourapp

# iOS Simulator 테스트
xcrun simctl openurl booted "projectvg://oauth-callback?code=test123&state=test"
```

이 방식으로 **보안성과 구현 난이도를 모두 만족**하는 OAuth2 인증을 구현할 수 있습니다! 🚀

