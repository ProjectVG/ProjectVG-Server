namespace ProjectVG.Common.Exceptions
{
    public class NotFoundException : ProjectVGException
    {
        public NotFoundException(string message, string errorCode = "일반_찾을_수_없음") 
            : base(message, errorCode, 404)
        {
        }

        public NotFoundException(string resourceName, object id, string errorCode = "일반_찾을_수_없음") 
            : base($"{resourceName} (ID: {id})를 찾을 수 없습니다", errorCode, 404)
        {
        }
    }
} 