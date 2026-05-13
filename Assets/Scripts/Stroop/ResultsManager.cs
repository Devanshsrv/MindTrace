// ResultsManager.cs
// Attach this to a GameObject called "ResultsManager" in the Results scene.
//
// SCENE HIERARCHY NEEDED:
//   Canvas
//     Text_PlayerName      <-- TMP Text
//     Text_WordScore       <-- TMP Text
//     Text_ColorScore      <-- TMP Text
//     Text_CWScore         <-- TMP Text
//     Text_Interference    <-- TMP Text
//     Text_RT              <-- TMP Text (reaction times)
//     Text_Percentile_W    <-- TMP Text
//     Text_Percentile_C    <-- TMP Text
//     Text_Percentile_CW   <-- TMP Text
//     Text_RiskBand        <-- TMP Text (colored by performance)
//     Text_Disclaimer      <-- TMP Text
//     Btn_PlayAgain        <-- Button
//     Text_SaveStatus      <-- TMP Text (shows "Saved!" or error)

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsManager : MonoBehaviour
{
    public TMP_Text textPlayerName;
    public TMP_Text textWordScore;
    public TMP_Text textColorScore;
    public TMP_Text textCWScore;
    public TMP_Text textInterference;
    public TMP_Text textRT;
    public TMP_Text textPercentileW;
    public TMP_Text textPercentileC;
    public TMP_Text textPercentileCW;
    public TMP_Text textRiskBand;
    public TMP_Text textDisclaimer;
    public TMP_Text textSaveStatus;

    public Button btnPlayAgain;
    public Button btnMainMenu;

    void Start()
    {
        DisplayAllResults();

        if (btnPlayAgain != null) btnPlayAgain.onClick.AddListener(PlayAgain);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OpenMainMenu);

        SaveToCSV();

        if (textDisclaimer != null)
            textDisclaimer.text =
                "MindTrace is a screening support tool - not a medical diagnosis.\n" +
                "Consult a healthcare professional for any concerns.";
    }

    void DisplayAllResults()
    {
        Set(textPlayerName, "Player: " + GameData.PlayerName);

        Set(textWordScore, $"Word Score:        {GameData.Score_Word}   items");
        Set(textColorScore, $"Color Score:       {GameData.Score_Color}  items");
        Set(textCWScore, $"Color-Word Score:  {GameData.Score_ColorWord} items");

        Set(textInterference, $"Interference:  {GameData.InterferenceScore:F2}");

        float avgW = GameData.AvgRT(GameData.RT_Word);
        float avgC = GameData.AvgRT(GameData.RT_Color);
        float avgCW = GameData.AvgRT(GameData.RT_ColorWord);

        Set(textRT,
            $"Avg Reaction Time\n" +
            $"  Word:        {avgW:F1} ms\n" +
            $"  Color:       {avgC:F1} ms\n" +
            $"  Color-Word:  {avgCW:F1} ms");

        Set(textPercentileW,
            $"Word Percentile:        {GameData.Percentile_Word:F1}th   (Z = {GameData.Z_Word:+0.00;-0.00})");

        Set(textPercentileC,
            $"Color Percentile:       {GameData.Percentile_Color:F1}th   (Z = {GameData.Z_Color:+0.00;-0.00})");

        Set(textPercentileCW,
            $"Color-Word Percentile:  {GameData.Percentile_CW:F1}th   (Z = {GameData.Z_ColorWord:+0.00;-0.00})");


        float pct = Mathf.Min(
            GameData.Percentile_Word,
            GameData.Percentile_Color,
            GameData.Percentile_CW
        );

        string band;
        Color bandColor;

        if (pct >= 84f) { band = "Strong Performance"; bandColor = new Color(0.18f, 0.85f, 0.40f); }
        else if (pct >= 31f) { band = "Typical Range"; bandColor = new Color(0.25f, 0.60f, 1.00f); }
        else if (pct >= 16f) { band = "Monitor Over Time"; bandColor = new Color(1.00f, 0.76f, 0.15f); }
        else { band = "Consider Evaluation"; bandColor = new Color(1.00f, 0.28f, 0.28f); }

        if (textRiskBand != null)
        {
            textRiskBand.text = band;
            textRiskBand.color = bandColor;
        }
    }

    void SaveToCSV()
    {
        string header =
            "Timestamp,PlayerName,Age,EducationYears," +
            "Score_Word,Score_Color,Score_ColorWord," +
            "Errors_Word,Errors_Color,Errors_CW," +
            "AvgRT_Word,AvgRT_Color,AvgRT_CW," +
            "InterferenceScore," +
            "Z_Word,Z_Color,Z_ColorWord," +
            "Percentile_Word,Percentile_Color,Percentile_CW," +
            "RiskBand";

        float pct = Mathf.Min(
            GameData.Percentile_Word,
            GameData.Percentile_Color,
            GameData.Percentile_CW
        );

        string risk =
            pct >= 84f ? "Strong" :
            pct >= 31f ? "Typical" :
            pct >= 16f ? "Monitor" : "Evaluate";

        string content = header + "\n" + string.Join(",",
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Sanitize(GameData.PlayerName),
            GameData.PlayerAge.ToString("F0"),
            GameData.PlayerEduYears.ToString("F0"),
            GameData.Score_Word,
            GameData.Score_Color,
            GameData.Score_ColorWord,
            GameData.Errors_Word,
            GameData.Errors_Color,
            GameData.Errors_ColorWord,
            GameData.AvgRT(GameData.RT_Word).ToString("F1"),
            GameData.AvgRT(GameData.RT_Color).ToString("F1"),
            GameData.AvgRT(GameData.RT_ColorWord).ToString("F1"),
            GameData.InterferenceScore.ToString("F3"),
            GameData.Z_Word.ToString("F3"),
            GameData.Z_Color.ToString("F3"),
            GameData.Z_ColorWord.ToString("F3"),
            GameData.Percentile_Word.ToString("F1"),
            GameData.Percentile_Color.ToString("F1"),
            GameData.Percentile_CW.ToString("F1"),
            risk
        );

        try
        {
            string path = WriteFile("StroopTestResults.csv", content);

            if (textSaveStatus != null)
            {
                textSaveStatus.gameObject.SetActive(true);
                textSaveStatus.text = "✓ Saved to:\n" + path;
                textSaveStatus.color = new Color(0.2f, 1f, 0.4f);
            }
        }
        catch (IOException ex)
        {
            if (textSaveStatus != null)
            {
                textSaveStatus.gameObject.SetActive(true);
                textSaveStatus.text = "❌ Save failed:\nFile may be open in another program.\nClose it and try again.";
                textSaveStatus.color = Color.red;
            }
            Debug.LogError("Failed to save CSV: " + ex.Message);
        }
    }

    static string WriteFile(string filename, string content)
    {
        string dir = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        dir = "/storage/emulated/0/Download";
#else
        dir = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.UserProfile) + "/Downloads";
#endif

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, filename);
        File.WriteAllText(path, content);
        return path;
    }

    static string Sanitize(string s) => s.Replace(",", " ").Replace("\n", " ");

    void PlayAgain()
    {
        GameData.Reset();
        SceneManager.LoadScene("MainMenuStroop");
    }

    void OpenMainMenu()
    {
        GameData.Reset();
        SceneManager.LoadScene("MainMenu");
    }

    static void Set(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }
}