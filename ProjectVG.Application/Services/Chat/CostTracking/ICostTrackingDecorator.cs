using ProjectVG.Application.Models.Chat;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public interface ICostTrackingDecorator<T> where T : class
    {
        T Service { get; }
        /// <summary>
/// 채팅 처리 컨텍스트에 대해 비용 추적 로직을 비동기적으로 실행합니다.
/// </summary>
/// <param name="context">사용자 입력, 대화 이력 및 관련 메타데이터를 포함하는 처리 컨텍스트.</param>
/// <returns>처리가 완료될 때까지 대기할 수 있는 비동기 작업을 반환합니다.</returns>
Task ProcessAsync(ChatProcessContext context);
        /// <summary>
/// 주어진 사용자 입력과 대화 기록을 바탕으로 입력을 분석하여 비용 추적에 필요한 정보를 생성합니다.
/// </summary>
/// <param name="userInput">분석할 사용자의 원문 입력.</param>
/// <param name="conversationHistory">분석 시 참조할 이전 대화 기록들의 열거(최근 대화부터 필요한 범위로 제공).</param>
/// <returns>
/// 분석 결과를 담은 <see cref="UserInputAnalysis"/>를 비동기적으로 반환합니다.
/// </returns>
Task<UserInputAnalysis> ProcessAsync(string userInput, IEnumerable<ConversationHistory> conversationHistory);
    }
}
