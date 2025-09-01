namespace ProjectVG.Common.Constants
{
    public static class CharacterConstants
    {
        public static readonly string[] SupportedEmotions = new[]
        {
            "neutral",       // 기본 중립 표정
            "happy",         // 행복
            "sad",           // 슬픔
            "angry",         // 화남
            "shy",           // 수줍음/부끄러움
            "surprised",     // 놀람
            "embarrassed",   // 당황
            "sleepy",        // 졸림
            "confused"       // 혼란
            // "proud",       // 사용 빈도 낮음
            // "excited",     // 과도한 감정, 잘 안 씀
            // "crying",      // 울음 세부 표현은 필요 시 추가
            // "love",        // 하트눈 필요 시 추가
            // "thinking"     // 필요 시 추가
        };

        public static readonly string[] SupportedActions = new[]
        {
            "nodding",        // 끄덕이기
            "shaking_head",   // 고개 젓기
            "waving",         // 손 흔들기
            "smiling",        // 미소
            "frowning",       // 찡그림
            "looking_away",   // 시선 돌리기
            "tilting_head",   // 고개 갸웃
            "blushing",       // 얼굴 붉히기
            "sighing",        // 한숨
            "pouting",        // 입 삐죽
            "yawning"         // 하품
            // "clapping",     // 구현 난도 높음
            // "pointing",     // 구현 난도 높음
            // "dancing",      // 구현 난도 높음
            // "saluting",     // 구현 난도 높음
            // "crying_action",// 구현 난도 높음
            // "heart_pose"    // 필요 시 추가
        };
    }
}
