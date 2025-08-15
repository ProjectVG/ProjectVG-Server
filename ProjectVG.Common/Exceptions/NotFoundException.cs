namespace ProjectVG.Common.Exceptions
{
    public class NotFoundException : ProjectVGException
    {
        public NotFoundException(ErrorCode errorCode = ErrorCode.NOT_FOUND) 
            : base(errorCode, 404)
        {
        }

        public NotFoundException(ErrorCode errorCode, string customMessage) 
            : base(errorCode, customMessage, 404)
        {
        }

        public NotFoundException(ErrorCode errorCode, object id) 
            : base(errorCode, $"{errorCode.GetMessage()}: {id}", 404)
        {
        }

        public NotFoundException(ErrorCode errorCode, string resourceName, object id) 
            : base(errorCode, $"{resourceName} (ID: {id})를 찾을 수 없습니다", 404)
        {
        }
    }
} 