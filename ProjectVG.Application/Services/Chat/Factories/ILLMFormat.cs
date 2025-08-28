namespace ProjectVG.Application.Services.Chat.Factories
{
    public interface ILLMFormat<TInput, TOutput>
    {
        string GetSystemMessage(TInput? input);
        string GetInstructions(TInput? input);
        string Model { get; }
        float Temperature { get; }
        int MaxTokens { get; }
        /// <summary>
/// LLM 응답 문자열을 입력 정보(TInput)를 참고하여 TOutput 타입으로 변환(파싱)합니다.
/// </summary>
/// <param name="llmResponse">LLM에서 반환한 원본 응답 텍스트.</param>
/// <param name="input">파싱 동작에 필요한 컨텍스트 또는 제약을 담은 입력값.</param>
/// <returns>파싱 결과로 생성된 TOutput 인스턴스.</returns>
TOutput Parse(string llmResponse, TInput input);
        /// <summary>
/// 주어진 프롬프트 토큰 수와 완료(응답) 토큰 수를 기반으로 LLM 호출 비용을 계산합니다.
/// </summary>
/// <param name="promptTokens">프롬프트(입력 및 지시문)에 사용된 토큰 수.</param>
/// <param name="completionTokens">LLM이 생성한 응답에 사용된 토큰 수.</param>
/// <returns>계산된 비용(통화 단위). 구현체는 모델별 가격 정책을 사용하여 두 토큰 수로부터 비용을 산출합니다.</returns>
double CalculateCost(int promptTokens, int completionTokens);
    }
}
