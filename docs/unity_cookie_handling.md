# Unity 쿠키 처리 가이드

## 개요
Unity에서 HttpOnly 쿠키를 포함한 쿠키를 수동으로 처리하는 방법을 설명합니다.

## 1. Unity에서 쿠키 처리 방법

### **방법 1: Set-Cookie 헤더 파싱 (권장)**

```csharp
public class UnityCookieManager : MonoBehaviour
{
    private Dictionary<string, string> cookies = new Dictionary<string, string>();
    
    // HTTP 요청 시 쿠키 전송
    public UnityWebRequest CreateRequestWithCookies(string url, string method = "GET")
    {
        var request = new UnityWebRequest(url, method);
        
        // 저장된 쿠키들을 Cookie 헤더에 추가
        if (cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            request.SetRequestHeader("Cookie", cookieHeader);
        }
        
        return request;
    }
    
    // 응답에서 쿠키 추출
    public void ProcessResponseCookies(UnityWebRequest request)
    {
        // Set-Cookie 헤더들 처리
        var setCookieHeaders = request.GetResponseHeader("Set-Cookie");
        if (!string.IsNullOrEmpty(setCookieHeaders))
        {
            ParseSetCookieHeader(setCookieHeaders);
        }
        
        // 여러 Set-Cookie 헤더 처리 (Unity는 하나만 반환하므로 별도 처리 필요)
        var allHeaders = request.GetResponseHeaders();
        if (allHeaders != null)
        {
            foreach (var header in allHeaders)
            {
                if (header.Key.ToLower() == "set-cookie")
                {
                    ParseSetCookieHeader(header.Value);
                }
            }
        }
    }
    
    // Set-Cookie 헤더 파싱
    private void ParseSetCookieHeader(string setCookieHeader)
    {
        // Set-Cookie: name=value; HttpOnly; Secure; SameSite=Strict; Max-Age=3600
        var parts = setCookieHeader.Split(';');
        
        if (parts.Length > 0)
        {
            var nameValue = parts[0].Trim().Split('=');
            if (nameValue.Length == 2)
            {
                var name = nameValue[0].Trim();
                var value = nameValue[1].Trim();
                
                // HttpOnly 쿠키도 저장 (Unity에서는 수동으로 전송)
                cookies[name] = value;
                
                Debug.Log($"Cookie saved: {name}={value}");
            }
        }
    }
    
    // 쿠키 저장 (PlayerPrefs)
    public void SaveCookies()
    {
        var cookieJson = JsonUtility.ToJson(new CookieData { cookies = cookies });
        PlayerPrefs.SetString("saved_cookies", cookieJson);
        PlayerPrefs.Save();
    }
    
    // 쿠키 로드 (PlayerPrefs)
    public void LoadCookies()
    {
        var cookieJson = PlayerPrefs.GetString("saved_cookies", "");
        if (!string.IsNullOrEmpty(cookieJson))
        {
            var cookieData = JsonUtility.FromJson<CookieData>(cookieJson);
            cookies = cookieData.cookies;
        }
    }
    
    // 특정 쿠키 가져오기
    public string GetCookie(string name)
    {
        return cookies.ContainsKey(name) ? cookies[name] : "";
    }
    
    // 쿠키 삭제
    public void DeleteCookie(string name)
    {
        if (cookies.ContainsKey(name))
        {
            cookies.Remove(name);
            SaveCookies();
        }
    }
    
    // 모든 쿠키 삭제
    public void ClearAllCookies()
    {
        cookies.Clear();
        PlayerPrefs.DeleteKey("saved_cookies");
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class CookieData
{
    public Dictionary<string, string> cookies;
}
```

### **방법 2: OAuth2 콜백에서 토큰 직접 전달 (더 간단)**

```csharp
public class OAuth2TokenHandler : MonoBehaviour
{
    // OAuth2 콜백에서 토큰을 URL 파라미터로 직접 받기
    void OnDeepLinkActivated(string url)
    {
        // projectvg://oauth-callback?access_token=xxx&refresh_token=yyy&expires_in=3600
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        
        var accessToken = query["access_token"];
        var refreshToken = query["refresh_token"];
        var expiresIn = query["expires_in"];
        
        // 토큰 저장
        SaveTokens(accessToken, refreshToken, int.Parse(expiresIn));
    }
    
    // API 요청 시 토큰을 Authorization 헤더에 포함
    public async Task<string> MakeAuthenticatedRequest(string url)
    {
        var accessToken = PlayerPrefs.GetString("access_token", "");
        
        using (var request = new UnityWebRequest(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            
            return request.downloadHandler.text;
        }
    }
}
```

## 2. 서버 측 쿠키 설정

### **OAuth2 콜백에서 쿠키 설정**
```csharp
[HttpGet("oauth2/callback")]
public async Task<IActionResult> OAuth2Callback(string code, string state)
{
    // OAuth2 토큰 교환
    var tokenResponse = await _authService.ExchangeOAuth2CodeAsync(code);
    
    if (tokenResponse.IsSuccess)
    {
        // Unity 앱으로 리다이렉트하면서 쿠키도 설정
        var redirectUrl = $"projectvg://oauth-callback?access_token={tokenResponse.Tokens.AccessToken}&refresh_token={tokenResponse.Tokens.RefreshToken}&expires_in={tokenResponse.Tokens.ExpiresIn}";
        
        // 쿠키 설정 (Unity에서 수동으로 처리)
        Response.Cookies.Append("access_token", tokenResponse.Tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(15)
        });
        
        Response.Cookies.Append("refresh_token", tokenResponse.Tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(30)
        });
        
        return Redirect(redirectUrl);
    }
    
    return BadRequest("OAuth2 token exchange failed");
}
```

## 3. 플랫폼별 고려사항

### **Android**
```csharp
#if UNITY_ANDROID
// Android에서는 WebView 쿠키를 사용할 수 있음
public void SetAndroidWebViewCookies(string url, string cookies)
{
    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
    {
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject webView = new AndroidJavaObject("android.webkit.WebView", activity);
        
        // 쿠키 설정
        webView.Call("setCookie", url, cookies);
    }
}
#endif
```

### **iOS**
```csharp
#if UNITY_IOS
// iOS에서는 WKWebView 쿠키를 사용할 수 있음
[System.Runtime.InteropServices.DllImport("__Internal")]
private static extern void SetWebViewCookie(string url, string cookies);
#endif
```

## 4. 권장 접근 방식

### **하이브리드 방식 (가장 실용적)**

```csharp
public class HybridAuthHandler : MonoBehaviour
{
    public async Task<bool> HandleOAuth2Callback(string url)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        
        // 1. URL 파라미터에서 토큰 추출 (주요 방법)
        var accessToken = query["access_token"];
        var refreshToken = query["refresh_token"];
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            SaveTokens(accessToken, refreshToken, int.Parse(query["expires_in"]));
            return true;
        }
        
        // 2. 쿠키에서 토큰 추출 (백업 방법)
        var cookieManager = GetComponent<UnityCookieManager>();
        var cookieAccessToken = cookieManager.GetCookie("access_token");
        var cookieRefreshToken = cookieManager.GetCookie("refresh_token");
        
        if (!string.IsNullOrEmpty(cookieAccessToken))
        {
            SaveTokens(cookieAccessToken, cookieRefreshToken, 3600);
            return true;
        }
        
        return false;
    }
}
```

## 5. 결론

### **권장사항:**

1. **주요 방법**: OAuth2 콜백 URL에 토큰을 파라미터로 포함
   - `projectvg://oauth-callback?access_token=xxx&refresh_token=yyy`
   - Unity에서 URL 파싱하여 토큰 추출

2. **보조 방법**: 쿠키 + 수동 쿠키 관리
   - Unity에서 Set-Cookie 헤더 파싱
   - PlayerPrefs에 쿠키 저장
   - 요청 시 수동으로 Cookie 헤더 설정

3. **보안**: 
   - 토큰은 PlayerPrefs에 암호화 저장
   - HttpOnly 쿠키는 Unity에서 수동 관리

**결론**: Unity에서는 쿠키를 효율적으로 다루기 어려우므로, **URL 파라미터 방식이 더 실용적**입니다! 🚀
