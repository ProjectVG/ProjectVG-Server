# ASP.NET Core 코딩 컨벤션

## 목차
1. [네이밍 컨벤션](#네이밍-컨벤션)
2. [프로젝트 구조](#프로젝트-구조)
3. [클래스 및 인터페이스 설계](#클래스-및-인터페이스-설계)
4. [컨트롤러 설계](#컨트롤러-설계)
5. [DTO 설계](#dto-설계)
6. [비동기 프로그래밍](#비동기-프로그래밍)
7. [로깅 및 예외 처리](#로깅-및-예외-처리)
8. [테스트 코드](#테스트-코드)
9. [커밋 컨벤션](#커밋-컨벤션)

---

## 네이밍 컨벤션

### ASP.NET 네이밍 컨벤션 요약

| 항목 | 컨벤션 | 예시 |
|------|--------|------|
| 클래스명 | PascalCase | HomeController, UserService |
| 인터페이스명 | I + PascalCase | IUserRepository, ILogger |
| 메서드명 | PascalCase | GetUserById(), SendEmail() |
| 변수명 (private) | camelCase (보통 _ prefix 사용) | _userRepository, _logger |
| 변수명 (public/protected) | PascalCase | UserId, Token |
| 속성명 (Property) | PascalCase | Name, EmailAddress |
| 매개변수명 | camelCase | userId, email |
| 이벤트명 | PascalCase + EventSuffix | UserLoggedIn, OnChanged |
| Enum명 | PascalCase, 멤버도 PascalCase | UserStatus, Active, Inactive |
| ViewModel / DTO | PascalCase + ViewModel or Dto | UserViewModel, LoginRequestDto |
| Route/Action 이름 | 명사 또는 동사 중심 (camelCase) | /api/users/{id}, /login, /registerUser |
| 비동기 메서드 | 접미사에 Async를 붙임 | GetUserAsync() |

---

## 프로젝트 구조

### 기본 디렉토리 구조
```
ProjectName/
├── Controllers/           # API 컨트롤러
├── Services/             # 비즈니스 로직 서비스
├── Models/              # 데이터 모델 & DTO
├── Data/               # 데이터 액세스 계층
├── Middlewares/        # 미들웨어
├── Config/             # 설정 클래스
└── Properties/         # 프로젝트 속성
```

### 네임스페이스 규칙
```csharp
namespace ProjectName
namespace ProjectName.Services
namespace ProjectName.Models.DTOs
```

---

## 클래스 및 인터페이스 설계

### 서비스 클래스 구조
```csharp
public interface IUserService
{
    Task<UserDto> GetUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> GetUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user?.ToDto();
    }
}
```

---

## 컨트롤러 설계

### 컨트롤러 구조
```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserAsync(id);
        return Ok(user);
    }
}
```

### API 엔드포인트 설계
```csharp
[HttpGet]                    // 목록 조회
[HttpGet("{id}")]           // 단일 조회
[HttpPost]                  // 생성
[HttpPut("{id}")]          // 전체 수정
[HttpPatch("{id}")]        // 부분 수정
[HttpDelete("{id}")]       // 삭제
```

---

## DTO 설계

### DTO 네이밍 컨벤션

| 용도 | 네이밍 패턴 | 예시 |
|------|-------------|------|
| API 요청 | {Action}Request | CreateUserRequest, UpdateUserRequest |
| API 응답 | {Entity}Response | UserResponse, ChatResponse |
| 내부 서비스 | {Entity}Dto | UserDto, ChatContextDto |
| 외부 서비스 | {Service}{Action} | LLMRequest, MemorySearchRequest |
| 뷰 모델 | {Entity}ViewModel | UserViewModel, ChatViewModel |

### DTO 디렉토리 구조

```
Models/
├── API/                    # API 계층 DTO
│   ├── Request/           # API 요청 DTO
│   └── Response/          # API 응답 DTO
├── Service/               # 서비스 계층 DTO
├── External/              # 외부 서비스 DTO
└── Domain/               # 도메인 모델
```

### DTO 설계 원칙

```csharp
// 기본값 명시적 설정
public string Name { get; set; } = string.Empty;
public List<string> Tags { get; set; } = new();

// JSON 속성명 snake_case 사용
[JsonPropertyName("user_name")]
public string UserName { get; set; }
```

---

## 비동기 프로그래밍

### ConfigureAwait 사용
```csharp
public async Task<UserDto> GetUserAsync(int id)
{
    var user = await _userRepository.GetByIdAsync(id).ConfigureAwait(false);
    return user?.ToDto();
}
```

---

## 로깅 및 예외 처리

### 로깅 규칙
```csharp
public async Task<UserDto> GetUserAsync(int id)
{
    _logger.LogInformation("사용자 조회 시작: {UserId}", id);

    try
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user?.ToDto();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "사용자 조회 실패: {UserId}", id);
        throw;
    }
}
```

### 예외 처리 규칙
```csharp
try
{
    var user = await _userService.GetUserAsync(id);
    return Ok(user);
}
catch (UserNotFoundException)
{
    return NotFound();
}
catch (Exception ex)
{
    return StatusCode(500, "Internal server error");
}
```

---

## 테스트 코드

### 단위 테스트 구조
```csharp
[TestClass]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<ILogger<UserService>> _mockLogger;
    private UserService _userService;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_mockRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetUserAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new User { Id = userId, Name = "Test User" };
        _mockRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Name, result.Name);
    }
}
```

### 통합 테스트 구조
```csharp
[TestClass]
public class UserControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task GetUser_ValidId_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/user/1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## 커밋 컨벤션

### Git 커밋 메시지 규칙
```
feat: 새로운 기능 추가
fix: 버그 수정
docs: 문서 수정
style: 코드 포맷팅
refactor: 코드 리팩토링
test: 테스트 코드 추가/수정
chore: 빌드 프로세스 또는 보조 도구 변경
perf: 성능 개선
ci: CI/CD 관련 변경
build: 빌드 시스템 변경
```

### 커밋 메시지 형식
```
<type>(<scope>): <subject>

<body>

<footer>
```

### 예시
```
feat(user): 사용자 인증 기능 추가

- JWT 토큰 기반 인증 구현
- 로그인/로그아웃 API 추가

Closes #123
```

### 브랜치 명명 규칙
```
feature/user-management
bugfix/login-error
hotfix/security-patch
release/v1.0.0
```

---

## 참고 자료

- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Clean Code by Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350884)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)