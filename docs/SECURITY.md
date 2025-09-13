# Security Guidelines and Best Practices

## 🔐 Critical Security Requirements

### JWT Token Security
- **Key Length**: JWT secret key MUST be at least 32 characters long
- **Key Strength**: Avoid default, fallback, or sample keys in production
- **Key Management**: Store JWT keys in secure environment variables only
- **Token Validation**: All JWT validation failures are logged for security monitoring
- **Token Headers**: Multiple headers supported (Authorization, X-Forwarded-Authorization, etc.)

```bash
# Example: Generate secure JWT key
JWT_SECRET_KEY=$(openssl rand -base64 32)
```

### Database Security
- **Connection Resilience**: Enhanced retry policies with exponential backoff
- **Connection Timeout**: 120-second timeout for database operations
- **Retry Logic**: 5 attempts with up to 30-second delays
- **Error Handling**: Specific SQL error codes handled for Azure SQL and on-premises
- **Connection Pooling**: EF Core manages connection pooling automatically

### Authentication Flow Security
- **OAuth2 PKCE**: Proof Key for Code Exchange mandatory for OAuth2 flows
- **Session Management**: Redis-based session storage with TTL expiration
- **Token Lifecycle**: Short-lived access tokens (15min) + long-lived refresh tokens (30 days)
- **Session Validation**: Real-time session validation for critical operations
- **Logout Security**: Proper token revocation and cleanup

### Input Validation Security
- **Data Annotations**: Comprehensive validation rules on all DTOs
- **Business Logic Validation**: Domain-specific validation in Application layer
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Protection**: JSON serialization prevents script injection
- **CSRF Protection**: OAuth2 state parameter validation

## 🛡️ Production Security Checklist

### Environment Configuration
- [ ] JWT_SECRET_KEY is at least 32 characters and cryptographically random
- [ ] Database connection strings use secure authentication
- [ ] Redis connection secured with authentication if applicable
- [ ] TLS/HTTPS enforced for all external communications
- [ ] OAuth2 client secrets stored securely
- [ ] API keys for external services (TTS, LLM, Memory) secured

### Runtime Security Monitoring
- [ ] JWT validation failures monitored and alerted
- [ ] Database connection failures logged and monitored
- [ ] WebSocket connection anomalies tracked
- [ ] Failed authentication attempts rate-limited
- [ ] Session hijacking patterns detected

### Code Security Standards
- [ ] ConfigureAwait(false) used in all async service methods
- [ ] Exception handling prevents information leakage
- [ ] Logging excludes sensitive information (tokens, passwords, keys)
- [ ] User input sanitized before database operations
- [ ] File uploads (if any) validated for type and size

## 🚨 Security Incident Response

### JWT Compromise Response
1. **Immediate**: Rotate JWT secret key
2. **Revoke**: All existing refresh tokens in Redis
3. **Audit**: Review authentication logs for suspicious activity
4. **Monitor**: Enhanced logging for unusual patterns

### Database Security Incident
1. **Isolate**: Database connections if breach suspected
2. **Audit**: Query logs for unauthorized access patterns
3. **Verify**: Data integrity and unauthorized modifications
4. **Recovery**: Implement additional connection restrictions

### Session Security Incident
1. **Clear**: All Redis sessions for affected users
2. **Force**: Re-authentication for all users
3. **Monitor**: Session creation patterns
4. **Update**: Session validation logic if needed

## 📊 Security Monitoring and Logging

### Critical Security Events to Monitor
- JWT token validation failures
- Database connection retries and failures
- WebSocket connection anomalies
- OAuth2 authentication failures
- Session validation failures
- Credit balance tampering attempts

### Log Levels for Security Events
```csharp
// Security violations - ERROR level
_logger.LogError("Security violation detected: {Details}", details);

// Authentication failures - WARNING level
_logger.LogWarning("Authentication failed: {Reason}", reason);

// Security success events - INFORMATION level
_logger.LogInformation("Secure operation completed: {Operation}", operation);

// Security debugging - DEBUG level (development only)
_logger.LogDebug("Security check passed: {Check}", check);
```

## 🔧 Development Security Guidelines

### Secure Coding Practices
1. **Never hardcode secrets** in source code
2. **Validate all inputs** at API and business logic layers
3. **Use parameterized queries** exclusively (EF Core handles this)
4. **Handle exceptions securely** without exposing system details
5. **Log security events** appropriately for monitoring

### Testing Security Features
1. **Authentication Testing**: Valid/invalid tokens, expired tokens, malformed tokens
2. **Authorization Testing**: Role-based access, resource ownership
3. **Input Validation Testing**: Boundary conditions, malformed data
4. **Session Testing**: Session hijacking prevention, timeout handling
5. **Error Handling Testing**: Information leakage prevention

## 🎯 Recent Security Improvements

### JWT Security Enhancements
- Added JWT key length validation (minimum 32 characters)
- Implemented fallback key detection and prevention
- Enhanced JWT validation exception handling and logging
- Added structured logging for security event tracking

### Database Connection Security
- Increased retry attempts from 3 to 5
- Extended maximum retry delay from 10 to 30 seconds
- Added 10 specific SQL error codes for better failure handling
- Set 120-second command timeout for long operations

### WebSocket Security Improvements
- Enhanced connection lifecycle management
- Added proper resource cleanup on disconnection
- Implemented heartbeat mechanism for connection health
- Added connection timeout handling (30 minutes)

### Session Management Security
- Implemented real-time session validation
- Added session activity tracking
- Graceful handling of session storage failures
- Enhanced session-based authentication for critical operations