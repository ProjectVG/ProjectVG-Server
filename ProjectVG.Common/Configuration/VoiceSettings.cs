namespace ProjectVG.Common.Configuration
{
    public static class VoiceSettings
    {
        public static class TextToSpeech
        {
            /// <summary>
            /// 기본 보이스 ID
            /// </summary>
            public const string DefaultVoiceId = "f4a2a3f41fc82de8616b84";

            /// <summary>
            /// 기본 언어 설정
            /// </summary>
            public const string DefaultLanguage = "ko";

            /// <summary>
            /// 기본 스타일 설정
            /// </summary>
            public const string DefaultStyle = "neutral";

            /// <summary>
            /// 기본 모델 설정
            /// </summary>
            public const string DefaultModel = "sona_speech_1";

            /// <summary>
            /// 최대 텍스트 길이
            /// </summary>
            public const int MaxTextLength = 300;

            /// <summary>
            /// 기본 피치 시프트
            /// </summary>
            public const int DefaultPitchShift = 0;

            /// <summary>
            /// 기본 피치 분산
            /// </summary>
            public const float DefaultPitchVariance = 1.0f;

            /// <summary>
            /// 기본 속도
            /// </summary>
            public const float DefaultSpeed = 1.0f;

            /// <summary>
            /// 피치 시프트 최소값
            /// </summary>
            public const int MinPitchShift = -12;

            /// <summary>
            /// 피치 시프트 최대값
            /// </summary>
            public const int MaxPitchShift = 12;

            /// <summary>
            /// 피치 분산 최소값
            /// </summary>
            public const float MinPitchVariance = 0.1f;

            /// <summary>
            /// 피치 분산 최대값
            /// </summary>
            public const float MaxPitchVariance = 2.0f;
        }

        public static class SupportedLanguages
        {
            public const string Korean = "ko";
            public const string English = "en";
            public const string Japanese = "ja";
        }

        public static class SupportedStyles
        {
            public const string Neutral = "neutral";
            public const string Happy = "happy";
            public const string Sad = "sad";
            public const string Angry = "angry";
            public const string Excited = "excited";
        }
    }
} 