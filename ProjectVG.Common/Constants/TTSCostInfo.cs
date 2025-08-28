namespace ProjectVG.Common.Constants
{
    public static class TTSCostInfo
    {
        private const double TTS_CREDITS_PER_DOLLAR = 100_000.0;
        private const double TTS_CREDITS_PER_SECOND = 10.0;
        private const double MILLICENTS_PER_DOLLAR = 100_000.0;
        private const double TTS_COST_PER_SECOND = TTS_CREDITS_PER_SECOND / TTS_CREDITS_PER_DOLLAR * MILLICENTS_PER_DOLLAR;

        /// <summary>
        /// TTS(텍스트 음성 변환) 1초당 비용을 밀리센트 단위로 반환합니다.
        /// </summary>
        /// <returns>클래스 내부 상수로 미리 계산된 1초당 비용(밀리센트 단위).</returns>
        public static double GetTTSCostPerSecond()
        {
            return TTS_COST_PER_SECOND;
        }

        /// <summary>
        /// 주어진 길이(초)에 대한 TTS 비용을 계산합니다.
        /// </summary>
        /// <remarks>
        /// 입력된 재생 시간은 소수점 첫째 자리(0.1초)에서 올림되어 반올림되지 않고 항상 올림됩니다(예: 1.01s → 1.1s).
        /// 해당 반올림된 지속시간에 내부 정의된 초당 TTS 비용을 곱한 뒤 결과를 올림하여 반환합니다.
        /// 반환값은 millicents(1 달러 = 100,000 millicents) 단위의 비용입니다.
        /// </remarks>
        /// <param name="durationInSeconds">비용을 계산할 재생 시간(초).</param>
        /// <returns>계산된 총 비용(밀리센트 단위)의 올림값을 나타내는 double.</returns>
        public static double CalculateTTSCost(double durationInSeconds)
        {
            var roundedDuration = Math.Ceiling(durationInSeconds * 10) / 10.0;
            return Math.Ceiling(roundedDuration * TTS_COST_PER_SECOND);
        }
    }
}
