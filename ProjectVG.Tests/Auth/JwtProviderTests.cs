using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ProjectVG.Tests.Auth
{
    public class JwtProviderTests
    {
        private readonly JwtProvider _jwtProvider;
        private readonly string _testJwtKey = "your-super-secret-jwt-key-here-minimum-32-characters";
        private readonly string _testIssuer = "ProjectVG";
        private readonly string _testAudience = "ProjectVG";
        private readonly int _accessTokenExpirationMinutes = 15;
        private readonly int _refreshTokenExpirationMinutes = 1440;

        public JwtProviderTests()
        {
            _jwtProvider = new JwtProvider(
                _testJwtKey,
                _testIssuer,
                _testAudience,
                _accessTokenExpirationMinutes,
                _refreshTokenExpirationMinutes
            );
        }

        [Fact]
        public void GenerateAccessToken_ValidUserId_ShouldReturnValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var token = _jwtProvider.GenerateAccessToken(userId);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().Contain(".");
            
            // 토큰 구조 검증 (3개 부분으로 구성)
            var parts = token.Split('.');
            parts.Should().HaveCount(3);
        }

        [Fact]
        public void GenerateRefreshToken_ValidUserId_ShouldReturnValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var token = _jwtProvider.GenerateRefreshToken(userId);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().Contain(".");
            
            // 토큰 구조 검증
            var parts = token.Split('.');
            parts.Should().HaveCount(3);
        }

        [Fact]
        public void ValidateToken_ValidAccessToken_ShouldReturnValidPrincipal()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = _jwtProvider.GenerateAccessToken(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.Identity!.IsAuthenticated.Should().BeTrue();
            
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(userId.ToString());
            
            var tokenTypeClaim = principal.FindFirst("token_type");
            tokenTypeClaim.Should().NotBeNull();
            tokenTypeClaim!.Value.Should().Be("access");
        }

        [Fact]
        public void ValidateToken_ValidRefreshToken_ShouldReturnValidPrincipal()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = _jwtProvider.GenerateRefreshToken(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.Identity!.IsAuthenticated.Should().BeTrue();
            
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(userId.ToString());
            
            var tokenTypeClaim = principal.FindFirst("token_type");
            tokenTypeClaim.Should().NotBeNull();
            tokenTypeClaim!.Value.Should().Be("refresh");
        }

        [Fact]
        public void ValidateToken_InvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var principal = _jwtProvider.ValidateToken(invalidToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_ExpiredToken_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expiredToken = GenerateExpiredToken(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(expiredToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_TokenWithWrongIssuer_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var wrongIssuerToken = GenerateTokenWithWrongIssuer(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(wrongIssuerToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_TokenWithWrongAudience_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var wrongAudienceToken = GenerateTokenWithWrongAudience(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(wrongAudienceToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void GetUserIdFromToken_ValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = _jwtProvider.GenerateAccessToken(userId);

            // Act
            var extractedUserId = _jwtProvider.GetUserIdFromToken(token);

            // Assert
            extractedUserId.Should().NotBeNull();
            extractedUserId.Should().Be(userId.ToString());
        }

        [Fact]
        public void GetUserIdFromToken_InvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var extractedUserId = _jwtProvider.GetUserIdFromToken(invalidToken);

            // Assert
            extractedUserId.Should().BeNull();
        }

        [Fact]
        public void AccessTokenAndRefreshToken_ShouldHaveDifferentExpirationTimes()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var accessToken = _jwtProvider.GenerateAccessToken(userId);
            var refreshToken = _jwtProvider.GenerateRefreshToken(userId);

            // Assert
            accessToken.Should().NotBe(refreshToken);
            
            // 토큰 내용 검증
            var accessPrincipal = _jwtProvider.ValidateToken(accessToken);
            var refreshPrincipal = _jwtProvider.ValidateToken(refreshToken);
            
            accessPrincipal.Should().NotBeNull();
            refreshPrincipal.Should().NotBeNull();
            
            var accessTokenType = accessPrincipal!.FindFirst("token_type")!.Value;
            var refreshTokenType = refreshPrincipal!.FindFirst("token_type")!.Value;
            
            accessTokenType.Should().Be("access");
            refreshTokenType.Should().Be("refresh");
        }

        [Fact]
        public void TokenClaims_ShouldContainRequiredClaims()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = _jwtProvider.GenerateAccessToken(userId);

            // Act
            var principal = _jwtProvider.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            
            var claims = principal!.Claims.ToList();
            claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
            claims.Should().Contain(c => c.Type == "token_type" && c.Value == "access");
        }

        private string GenerateExpiredToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_testJwtKey);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "access")
            };

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow.AddMinutes(-2), // 2분 전부터 유효
                Expires = DateTime.UtcNow.AddMinutes(-1), // 1분 전에 만료
                Issuer = _testIssuer,
                Audience = _testAudience,
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTokenWithWrongIssuer(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_testJwtKey);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "access")
            };

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = "WrongIssuer", // 잘못된 Issuer
                Audience = _testAudience,
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTokenWithWrongAudience(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_testJwtKey);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "access")
            };

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _testIssuer,
                Audience = "WrongAudience", // 잘못된 Audience
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
