namespace ProjectVG.Application.Services.Chat.Factories
{
    public interface ILLMFormat<TInput, TOutput>
    {
        /// <summary>
/// 주어진 입력 컨텍스트를 기반으로 LLM(시스템)에게 전달할 시스템 메시지(시스템 프롬프트)를 생성합니다.
/// </summary>
/// <param name="input">시스템 메시지 생성을 위해 사용되는 입력 컨텍스트(문맥, 설정 또는 메타데이터 전달용).</param>
/// <returns>LLM 호출 시 최상위 시스템 역할로 사용될 텍스트 메시지.</returns>
string GetSystemMessage(TInput input);
        /// <summary>
/// 주어진 입력 컨텍스트를 바탕으로 LLM에게 전달할 지침(instructions) 문자열을 생성합니다.
/// </summary>
/// <param name="input">지침 생성을 위한 컨텍스트 또는 파라미터를 담은 입력 값.</param>
/// <returns>LLM 프롬프트 내에 포함될 지침 텍스트.</returns>
string GetInstructions(TInput input);
        string Model { get; }
        float Temperature { get; }
        int MaxTokens { get; }
        /// <summary>
/// LLM으로부터 받은 원시 응답 문자열을 입력 컨텍스트를 사용해 TOutput 타입으로 변환(파싱)합니다.
/// </summary>
/// <param name="llmResponse">LLM이 반환한 원시 응답 문자열(포맷된 텍스트 또는 JSON 등).</param>
/// <param name="input">파싱에 필요한 컨텍스트를 담은 입력값(TInput).</param>
/// <returns>파싱된 결과를 나타내는 TOutput 값.</returns>
TOutput Parse(string llmResponse, TInput input);
        /// <summary>
/// 주어진 토큰 수에 따른 LLM 호출 비용을 계산합니다.
/// </summary>
/// <param name="tokensUsed">사용된 토큰 수(요청 및 응답에 소비된 총 토큰 수).</param>
/// <returns>계산된 비용(서비스가 사용하는 통화 단위로 반환).</returns>
double CalculateCost(int tokensUsed);
    }
}
