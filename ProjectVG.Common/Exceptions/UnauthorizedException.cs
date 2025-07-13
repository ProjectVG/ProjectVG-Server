namespace ProjectVG.Common.Exceptions
{
    public class UnauthorizedException : ProjectVGException
    {
        public UnauthorizedException(string message = "인증되지 않은 접근입니다", string errorCode = "인증_실패") 
            : base(message, errorCode, 401)
        {
        }
    }
} 