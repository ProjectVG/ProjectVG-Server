namespace ProjectVG.Common.Constants
{
    public static class TTSCostInfo
    {
        private const double TTS_CREDITS_PER_DOLLAR = 100_000.0;
        private const double TTS_CREDITS_PER_SECOND = 10.0;
        private const double MILLICENTS_PER_DOLLAR = 100_000.0;
        private const double TTS_COST_PER_SECOND = TTS_CREDITS_PER_SECOND / TTS_CREDITS_PER_DOLLAR * MILLICENTS_PER_DOLLAR;

        public static double GetTTSCostPerSecond()
        {
            return TTS_COST_PER_SECOND;
        }

        public static double CalculateTTSCost(double durationInSeconds)
        {
            var roundedDuration = Math.Ceiling(durationInSeconds * 10) / 10.0;
            return Math.Ceiling(roundedDuration * TTS_COST_PER_SECOND);
        }
    }
}
