# GitHub Secrets 설정 가이드

이 문서는 ProjectVG CI/CD 파이프라인을 위한 GitHub Secrets 설정 방법을 안내합니다.

## GitHub Secrets 설정 방법

1. GitHub 저장소로 이동
2. `Settings` 탭 클릭
3. 좌측 메뉴에서 `Secrets and variables` → `Actions` 선택
4. `New repository secret` 버튼 클릭

## 필수 Secrets 목록

### 🐳 Docker Hub 관련
```
DOCKER_USERNAME
- 설명: Docker Hub 사용자명
- 예시: your-dockerhub-username

DOCKER_PASSWORD
- 설명: Docker Hub 액세스 토큰 또는 비밀번호
- 참고: Docker Hub → Account Settings → Security → New Access Token
```

### ☁️ AWS 배포 관련
```
AWS_ACCESS_KEY_ID
- 설명: AWS IAM 사용자의 액세스 키 ID
- 예시: AKIA1234567890EXAMPLE

AWS_SECRET_ACCESS_KEY
- 설명: AWS IAM 사용자의 시크릿 액세스 키
- 예시: wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

AWS_REGION
- 설명: AWS 리전
- 예시: ap-northeast-2
```

### 🔐 애플리케이션 환경 변수 (선택사항)
운영 환경에서 다른 값이 필요한 경우에만 설정:

```
JWT_SECRET_KEY
- 설명: JWT 토큰 서명용 시크릿 키 (최소 32자)
- 예시: your-super-secret-jwt-key-here-minimum-32-characters

GOOGLE_OAUTH_CLIENT_ID
- 설명: Google OAuth2 클라이언트 ID
- 예시: 1234567890-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com

GOOGLE_OAUTH_CLIENT_SECRET
- 설명: Google OAuth2 클라이언트 시크릿
- 예시: GOCSPX-abcdefghijklmnopqrstuvwxyz123456

DB_CONNECTION_STRING
- 설명: 운영 데이터베이스 연결 문자열
- 예시: Server=prod-db.amazonaws.com,1433;Database=ProjectVG;User Id=admin;Password=SecurePassword123!;TrustServerCertificate=true;

REDIS_CONNECTION_STRING
- 설명: Redis 연결 문자열
- 예시: prod-redis.amazonaws.com:6379

LLM_BASE_URL
- 설명: LLM 서비스 기본 URL
- 예시: https://api.llm-service.com

MEMORY_BASE_URL
- 설명: Memory 서비스 기본 URL
- 예시: https://api.memory-service.com

TTS_API_KEY
- 설명: TTS 서비스 API 키
- 예시: sk-1234567890abcdefghijklmnopqrstuvwxyz
```

## AWS IAM 권한 설정

AWS 배포를 위한 IAM 사용자에게 다음 권한이 필요합니다:

### ECS 배포 시 필요 권한
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ecs:UpdateService",
                "ecs:DescribeServices",
                "ecs:DescribeTasks",
                "ecs:ListTasks",
                "ecs:RegisterTaskDefinition",
                "ecs:DescribeTaskDefinition"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "iam:PassRole"
            ],
            "Resource": "arn:aws:iam::*:role/ecsTaskExecutionRole"
        }
    ]
}
```

### EC2/EKS 배포 시 필요 권한 (해당하는 경우)
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ec2:DescribeInstances",
                "eks:DescribeCluster",
                "eks:UpdateClusterConfig"
            ],
            "Resource": "*"
        }
    ]
}
```

## 보안 주의사항

1. **최소 권한 원칙**: 필요한 최소한의 권한만 부여
2. **액세스 키 로테이션**: 정기적으로 AWS 액세스 키 교체
3. **Docker 토큰**: Docker Hub 비밀번호 대신 액세스 토큰 사용 권장
4. **환경별 분리**: 개발/스테이징/운영 환경별로 다른 값 사용

## 설정 확인 방법

1. Pull Request를 `develop` 브랜치로 생성하여 CI 동작 확인
2. Pull Request를 `release` 브랜치로 생성하여 CI/CD 전체 과정 확인
3. GitHub Actions 탭에서 워크플로우 실행 결과 확인

## 문제 해결

### 자주 발생하는 오류

1. **Docker login 실패**
   - `DOCKER_USERNAME`, `DOCKER_PASSWORD` 값 확인
   - Docker Hub 액세스 토큰 권한 확인

2. **AWS 배포 실패**
   - AWS 자격 증명 확인
   - IAM 권한 확인
   - 리소스명(클러스터, 서비스) 확인

3. **테스트 실패**
   - 테스트 코드 자체 문제일 수 있음
   - 로컬에서 `dotnet test` 실행하여 확인

## 추가 정보

- [GitHub Secrets 공식 문서](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Docker Hub 액세스 토큰 생성](https://docs.docker.com/docker-hub/access-tokens/)
- [AWS IAM 권한 설정](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html)