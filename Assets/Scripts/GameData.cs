// 씬 간 결과 데이터 전달용 정적 클래스
public static class GameData
{
    public static int FinalScore;
    public static int MaxCombo;
    public static int TotalTouches;
    public static int CorrectTouches;
    public static float TotalReactionTime;
    public static int ReactionCount;
    public static float MaxReach;

    public static float Accuracy =>
        TotalTouches == 0 ? 0f : (float)CorrectTouches / TotalTouches * 100f;

    public static float AvgReactionMs =>
        ReactionCount == 0 ? 0f : TotalReactionTime / ReactionCount * 1000f;

    public static void Reset()
    {
        FinalScore = 0;
        MaxCombo = 0;
        TotalTouches = 0;
        CorrectTouches = 0;
        TotalReactionTime = 0f;
        ReactionCount = 0;
        MaxReach = 0f;
    }
}
