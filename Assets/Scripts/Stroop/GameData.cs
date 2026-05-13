// GameData.cs
// A simple static class that holds ALL game data.
// Because it's static, it works across all scenes without any setup.
// No MonoBehaviour, no singleton pattern - just attach nothing, works everywhere.

public static class GameData
{
    // ── Player info (filled in from the menu) ────────────────────────────
    public static string PlayerName    = "Player";
    public static float  PlayerAge     = 25f;
    public static float  PlayerEduYears = 15f;   // years of formal education

    // ── Raw scores: how many correct items in 45 seconds ─────────────────
    public static int Score_Word      = 0;   // Task 1
    public static int Score_Color     = 0;   // Task 2
    public static int Score_ColorWord = 0;   // Task 3

    // ── Reaction times: list of milliseconds per correct answer ──────────
    public static System.Collections.Generic.List<float> RT_Word
        = new System.Collections.Generic.List<float>();
    public static System.Collections.Generic.List<float> RT_Color
        = new System.Collections.Generic.List<float>();
    public static System.Collections.Generic.List<float> RT_ColorWord
        = new System.Collections.Generic.List<float>();

    // ── Error counts ──────────────────────────────────────────────────────
    public static int Errors_Word      = 0;
    public static int Errors_Color     = 0;
    public static int Errors_ColorWord = 0;

    // ── Computed results (filled in after Task 3 finishes) ────────────────
    public static float InterferenceScore = 0f;
    public static float Z_Word            = 0f;
    public static float Z_Color           = 0f;
    public static float Z_ColorWord       = 0f;
    public static float Percentile_Word   = 0f;
    public static float Percentile_Color  = 0f;
    public static float Percentile_CW     = 0f;

    // ── Call this before starting a new test session ──────────────────────
    public static void Reset()
    {
        Score_Word = Score_Color = Score_ColorWord = 0;
        Errors_Word = Errors_Color = Errors_ColorWord = 0;
        RT_Word.Clear();
        RT_Color.Clear();
        RT_ColorWord.Clear();
        InterferenceScore = Z_Word = Z_Color = Z_ColorWord = 0f;
        Percentile_Word = Percentile_Color = Percentile_CW = 0f;
    }

    // ── Average reaction time helper ──────────────────────────────────────
    public static float AvgRT(System.Collections.Generic.List<float> list)
    {
        if (list == null || list.Count == 0) return 0f;
        float sum = 0f;
        foreach (float v in list) sum += v;
        return sum / list.Count;
    }

    // ── Compute all scores - call this after Task 3 ends ─────────────────
    public static void ComputeScores()
    {
        // Interference formula from Golden (2002): SCW - (SW*SC)/(SW+SC)
        float den = Score_Word + Score_Color;
        InterferenceScore = den > 0
            ? Score_ColorWord - (Score_Word * (float)Score_Color) / den
            : 0f;

        // Z-scores from Ktaiche et al. (2021) regression formulas
        // Centered: age at 38, education at 15 years
        float ac  = PlayerAge - 38f;
        float ac2 = ac * ac;
        float ec  = PlayerEduYears - 15f;
        float ec2 = ec * ec;

        // Stroop Word Z-score  (SDe = 12.1)
        float pred_W = 100.7f + (-0.02f * ac2) + (0.70f * ec) + (-0.12f * ac);
        Z_Word = (Score_Word - pred_W) / 12.1f;

        // Stroop Color Z-score  (SDe = 11.7)
        float pred_C = 80.8f + (-0.02f * ac) + (-0.06f * ec2) + (0.39f * ec);
        Z_Color = (Score_Color - pred_C) / 11.7f;

        // Stroop Color-Word Z-score  (SDe = 9.7)
        float pred_CW = 45.3f + (-0.21f * ac) + (0.80f * ec) + (-0.01f * ac2);
        Z_ColorWord = (Score_ColorWord - pred_CW) / 9.7f;

        // Convert Z to percentile
        Percentile_Word  = ZToPercentile(Z_Word);
        Percentile_Color = ZToPercentile(Z_Color);
        Percentile_CW    = ZToPercentile(Z_ColorWord);
    }

    // Converts a Z-score to a percentile (0-100)
    // Uses Abramowitz & Stegun approximation
    public static float ZToPercentile(float z)
    {
        float absZ = z < 0 ? -z : z;
        float t    = 1f / (1f + 0.2316419f * absZ);
        float poly = t * (0.319381530f
                   + t * (-0.356563782f
                   + t * ( 1.781477937f
                   + t * (-1.821255978f
                   + t *   1.330274429f))));
        float pdf  = UnityEngine.Mathf.Exp(-0.5f * z * z)
                   / UnityEngine.Mathf.Sqrt(2f * UnityEngine.Mathf.PI);
        float phi  = 1f - pdf * poly;
        float pct  = (z >= 0f ? phi : 1f - phi) * 100f;
        return UnityEngine.Mathf.Clamp(pct, 0.1f, 99.9f);
    }
}
