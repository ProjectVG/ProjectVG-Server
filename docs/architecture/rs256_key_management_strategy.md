# RS256 키 관리 및 키 롤테이션 전략

## 개요
JWT 서명을 위한 RS256 비대칭 키 관리 및 롤테이션 전략 문서

## 키 관리 전략

### 1. 키 생성 및 저장
- **알고리즘**: RSA-256 (RS256)
- **키 크기**: 2048 비트 (최소), 4096 비트 (권장)
- **키 형식**: PEM 형식 (PKCS#8)

### 2. 환경별 키 저장 방식
- **개발 환경**: 파일 시스템 (./keys/ 폴더)
- **스테이징/운영**: Azure Key Vault 또는 환경변수
- **컨테이너**: Docker Secrets 또는 Kubernetes Secrets

### 3. 키 식별자 (KID) 체계
- 형식: `{environment}-{timestamp}-{uuid}`
- 예시: `dev-20250101-a1b2c3d4-e5f6-7890-abcd-ef1234567890`

### 4. 키 롤테이션 정책
- **롤테이션 주기**: 90일 (권장)
- **겹침 기간**: 30일 (이전 키로 서명된 토큰 검증 허용)
- **비상 롤테이션**: 키 유출 시 즉시 수행

## 구현 계획

### 1. 키 저장소 인터페이스
```csharp
public interface IKeyStore
{
    Task<RsaSecurityKey> GetCurrentSigningKeyAsync();
    Task<IEnumerable<RsaSecurityKey>> GetValidationKeysAsync();
    Task<string> RotateKeyAsync();
    Task RevokeKeyAsync(string keyId);
}
```

### 2. 키 관리 서비스
```csharp
public interface IKeyManagementService
{
    Task<string> GenerateNewKeyPairAsync();
    Task<bool> IsKeyRotationNeededAsync();
    Task PerformKeyRotationAsync();
    Task<IEnumerable<string>> GetActiveKeyIdsAsync();
}
```

### 3. 파일 구조
```
keys/
├── current/
│   ├── private-{kid}.pem
│   └── public-{kid}.pem
├── previous/
│   ├── private-{kid}.pem
│   └── public-{kid}.pem
└── metadata.json
```

### 4. 메타데이터 스키마
```json
{
  "currentKeyId": "dev-20250101-a1b2c3d4",
  "keys": [
    {
      "keyId": "dev-20250101-a1b2c3d4",
      "createdAt": "2025-01-01T00:00:00Z",
      "expiresAt": "2025-04-01T00:00:00Z",
      "status": "active",
      "algorithm": "RS256"
    }
  ]
}
```

## 보안 고려사항

### 1. 키 보호
- 개인키는 절대 로그에 기록하지 않음
- 메모리에서 사용 후 즉시 정리
- 파일 권한: 600 (소유자만 읽기/쓰기)

### 2. 키 백업
- 안전한 오프라인 저장소에 백업
- 암호화된 형태로 보관
- 접근 로그 및 감사 추적

### 3. 모니터링
- 키 사용 횟수 및 빈도 모니터링
- 비정상적인 키 접근 패턴 감지
- 키 만료 전 알림 시스템

## 환경별 설정

### 개발 환경
```json
{
  "Authentication": {
    "Jwt": {
      "KeyStore": "FileSystem",
      "KeysDirectory": "./keys",
      "RotationIntervalDays": 30
    }
  }
}
```

### 운영 환경
```json
{
  "Authentication": {
    "Jwt": {
      "KeyStore": "AzureKeyVault",
      "KeyVaultUrl": "https://projectvg-kv.vault.azure.net/",
      "RotationIntervalDays": 90
    }
  }
}
```

## 구현 우선순위
1. 파일 시스템 기반 키 저장소 구현
2. 키 메타데이터 관리 시스템
3. 자동 키 롤테이션 스케줄러
4. Azure Key Vault 통합
5. 키 성능 모니터링 및 알림
