using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectVG.Api.Filters
{
    public class ModelStateValidationFilter : ActionFilterAttribute
    {
        /// <summary>
        /// 액션 실행 직전에 요청의 모델 상태를 검증합니다. 모델이 유효하지 않으면 파이프라인을 단축하여
        /// BadRequestObjectResult에 ModelState를 담아 응답으로 설정합니다.
        /// </summary>
        /// <param name="context">검증할 모델 상태를 포함한 현재 액션 실행 컨텍스트. 모델이 유효하지 않으면 해당 컨텍스트의 Result가 설정됩니다.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }
}
