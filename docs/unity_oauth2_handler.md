# Unity OAuth2 핸들러 구현 가이드

## 개요
Unity 클라이언트에서 OAuth2 인증을 처리하는 방법을 설명합니다.

## 1. Custom URL Scheme 설정

### Android 설정
```xml
<!-- Assets/Plugins/Android/AndroidManifest.xml -->
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application>
        <activity android:name="com.unity3d.player.UnityPlayerActivity">
            <intent-filter>
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data android:scheme="projectvg" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

### iOS 설정
```xml
<!-- Assets/Plugins/iOS/Info.plist -->
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

### Windows 설정
```csharp
// Windows Registry 설정 (설치 시)
// HKEY_CLASSES_ROOT\projectvg\shell\open\command
// 값: "C:\Path\To\YourGame.exe" "%1"
```

## 2. Unity OAuth2 핸들러 구현

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;

public class OAuth2Handler : MonoBehaviour
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
    
    // OAuth2 로그인 시작
    public void StartOAuth2Login()
    {
        currentState = Guid.NewGuid().ToString();
        
        var authUrl = $"{authServerUrl}/api/auth/oauth2/authorize?" +
                     $"client_id={clientId}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"response_type=code&" +
                     $"scope=chat&" +
                     $"state={currentState}";
        
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
            
            // 토큰 추출
            var accessToken = query["access_token"];
            var refreshToken = query["refresh_token"];
            var expiresIn = query["expires_in"];
            
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                // 토큰 저장
                SaveTokens(accessToken, refreshToken, int.Parse(expiresIn));
                
                // 인증 완료
                OnAuthenticationSuccess();
            }
            else
            {
                Debug.LogError("Missing tokens in OAuth2 callback");
                ShowLoginPanel();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing OAuth2 callback: {ex.Message}");
            ShowLoginPanel();
        }
    }
    
    // 토큰 저장
    private void SaveTokens(string accessToken, string refreshToken, int expiresIn)
    {
        PlayerPrefs.SetString("access_token", accessToken);
        PlayerPrefs.SetString("refresh_token", refreshToken);
        PlayerPrefs.SetInt("token_expires_at", (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + expiresIn);
        PlayerPrefs.Save();
        
        Debug.Log("Tokens saved successfully");
    }
    
    // 저장된 토큰 확인
    private void CheckSavedTokens()
    {
        var accessToken = PlayerPrefs.GetString("access_token", "");
        var expiresAt = PlayerPrefs.GetInt("token_expires_at", 0);
        
        if (!string.IsNullOrEmpty(accessToken) && expiresAt > DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond)
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
        // 저장된 토큰 삭제
        PlayerPrefs.DeleteKey("access_token");
        PlayerPrefs.DeleteKey("refresh_token");
        PlayerPrefs.DeleteKey("token_expires_at");
        PlayerPrefs.Save();
        
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
    
    // 토큰 갱신 (기존 RefreshTokenAsync와 동일)
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

        using (var www = UnityWebRequest.Post($"{authServerUrl}/api/auth/refresh", ""))
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
                SaveTokens(response.tokens.access_token, response.tokens.refresh_token, response.tokens.expires_in);
                
                return true;
            }
            else
            {
                Debug.LogError($"Token refresh failed: {www.error}");
                Logout(); // 로그아웃 처리
                return false;
            }
        }
    }
}

[System.Serializable]
public class RefreshTokenRequest
{
    public string refresh_token;
    public bool set_cookie = false;
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

[System.Serializable]
public class UserData
{
    public string user_id;
    public string username;
    // 기타 사용자 데이터
}
```

## 3. WebView 방식 (모바일 대안)

```csharp
public class OAuth2WebViewHandler : MonoBehaviour
{
    public void StartOAuth2WithWebView()
    {
        var authUrl = $"{authServerUrl}/api/auth/oauth2/authorize?" +
                     $"client_id={clientId}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"response_type=code&" +
                     $"scope=chat&" +
                     $"state={currentState}";
        
        // WebView 플러그인 사용 (예: UniWebView)
        OpenWebView(authUrl);
    }
    
    void OnWebViewCallback(string url)
    {
        if (url.StartsWith("projectvg://oauth-callback"))
        {
            // Deep Link와 동일한 처리
            OnDeepLinkActivated(url);
            CloseWebView();
        }
    }
}
```

## 4. 보안 고려사항

### 1. State 파라미터 검증
```csharp
// CSRF 공격 방지를 위한 state 검증
private Dictionary<string, DateTime> pendingStates = new Dictionary<string, DateTime>();

public void StartOAuth2Login()
{
    var state = Guid.NewGuid().ToString();
    pendingStates[state] = DateTime.UtcNow;
    currentState = state;
    // ...
}

void OnDeepLinkActivated(string url)
{
    var state = query["state"];
    
    // state 유효성 검사
    if (!pendingStates.ContainsKey(state))
    {
        Debug.LogError("Invalid state parameter");
        return;
    }
    
    // 만료된 state 제거 (5분)
    if (DateTime.UtcNow - pendingStates[state] > TimeSpan.FromMinutes(5))
    {
        pendingStates.Remove(state);
        Debug.LogError("State expired");
        return;
    }
    
    pendingStates.Remove(state);
    // ...
}
```

### 2. 토큰 암호화 저장
```csharp
private void SaveTokensSecurely(string accessToken, string refreshToken, int expiresIn)
{
    // 간단한 암호화 (실제로는 더 강력한 방법 사용)
    var encryptedAccess = EncryptString(accessToken, "your-secret-key");
    var encryptedRefresh = EncryptString(refreshToken, "your-secret-key");
    
    PlayerPrefs.SetString("encrypted_access_token", encryptedAccess);
    PlayerPrefs.SetString("encrypted_refresh_token", encryptedRefresh);
    PlayerPrefs.SetInt("token_expires_at", (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + expiresIn);
    PlayerPrefs.Save();
}

private string GetAccessToken()
{
    var encrypted = PlayerPrefs.GetString("encrypted_access_token", "");
    if (string.IsNullOrEmpty(encrypted)) return "";
    
    return DecryptString(encrypted, "your-secret-key");
}
```

## 5. 플랫폼별 테스트

### Android 테스트
```bash
# ADB를 사용한 Deep Link 테스트
adb shell am start -W -a android.intent.action.VIEW -d "projectvg://oauth-callback?access_token=test&refresh_token=test&expires_in=3600&state=test" com.yourcompany.yourapp
```

### iOS 테스트
```bash
# Simulator에서 URL 열기
xcrun simctl openurl booted "projectvg://oauth-callback?access_token=test&refresh_token=test&expires_in=3600&state=test"
```

### Windows 테스트
```cmd
# Registry 등록 후
start projectvg://oauth-callback?access_token=test&refresh_token=test&expires_in=3600&state=test
```

이 방식으로 Unity 클라이언트에서 OAuth2 인증을 완전히 처리할 수 있습니다! 🚀

