using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public interface IChatMetricsService
    {
        /// <summary>
/// 지정된 세션, 사용자 및 캐릭터에 대한 채팅 비용/지표 수집을 초기화합니다.
/// </summary>
/// <param name="sessionId">추적할 채팅 세션의 고유 식별자.</param>
/// <param name="userId">채팅을 수행하는 사용자의 고유 식별자.</param>
/// <param name="characterId">해당 세션에서 사용되는 캐릭터(또는 페르소나)의 식별자.</param>
void StartChatMetrics(string sessionId, string userId, string characterId);
        /// <summary>
/// 지정한 이름의 하위 프로세스에 대한 비용/시간 수집을 시작합니다.
/// </summary>
/// <param name="processName">측정할 하위 프로세스의 식별용 이름(예: "ModelInference", "PromptBuild").</param>
void StartProcessMetrics(string processName);
        /// <summary>
/// 지정한 프로세스의 메트릭 수집을 종료하고 해당 데이터(비용, 오류, 추가 정보)를 기록합니다.
/// </summary>
/// <param name="processName">종료할 프로세스의 이름(식별자).</param>
/// <param name="cost">해당 프로세스에 귀속되는 비용(기본값 0).</param>
/// <param name="errorMessage">프로세스 종료 시 발생한 오류 메시지(없으면 null).</param>
/// <param name="additionalData">프로세스와 관련된 추가 키-값 정보(없으면 null).</param>
/// <remarks>
/// 이 호출은 현재 활성화된 채팅 메트릭 세션에 대해 지정된 프로세스의 메트릭을 마무리하고, 제공된 비용·오류·추가 데이터를 연관시킵니다.
/// </remarks>
void EndProcessMetrics(string processName, decimal cost = 0, string? errorMessage = null, Dictionary<string, object>? additionalData = null);
        /// <summary>
/// 현재 활성화된 채팅 메트릭 세션을 종료하고 해당 세션의 메트릭 수집을 마무리합니다.
/// </summary>
/// <remarks>
/// 이 메서드를 호출하면 진행 중인 프로세스 메트릭이 종료되고(있을 경우) 이후 <see cref="GetCurrentChatMetrics"/>는 null을 반환할 수 있습니다.
/// </remarks>
void EndChatMetrics();
        /// <summary>
/// 현재 활성화된 채팅 메트릭스를 반환합니다.
/// </summary>
/// <returns>활성화된 ChatMetrics 인스턴스 또는 활성화된 메트릭이 없으면 null.</returns>
ChatMetrics? GetCurrentChatMetrics();
        /// <summary>
/// 현재 수집된 채팅 메트릭(ChatMetrics)을 기록(로그 또는 외부 수집기로 전송)합니다.
/// </summary>
/// <remarks>
/// 현재 활성화된 메트릭이 없으면 아무 작업도 수행하지 않습니다.
/// 호출은 메트릭의 상태를 외부로 내보내는 부작용을 발생시킵니다.
/// </remarks>
void LogChatMetrics();
    }
}
