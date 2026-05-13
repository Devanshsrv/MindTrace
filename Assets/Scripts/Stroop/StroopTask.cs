// StroopTask.cs
// This ONE script handles all 3 task scenes (Word, Color, ColorWord).
// Attach to a GameObject called "TaskManager" in each task scene.
// Change the taskIndex value in the Inspector per scene.
//
// SCENE HIERARCHY NEEDED (same for all 3 task scenes):
//
//   Canvas
//     BG_Panel              <-- Image (background color)
//     Text_TaskTitle        <-- TMP Text (e.g. "TASK 1: WORD READING")
//     Text_Instruction      <-- TMP Text (instruction line)
//     Text_Score            <-- TMP Text (shows "Score: 0")
//     Text_Timer            <-- TMP Text (shows "45")
//     TimerBar              <-- Slider (min=0 max=45 value=45)
//
//     Card_Panel            <-- Image (the stimulus card in the middle)
//       Text_Stimulus       <-- TMP Text (the BIG word shown to player)
//
//     Buttons_Panel
//       Btn_Red             <-- Button
//         Text (child)      <-- TMP Text "RED"
//       Btn_Green           <-- Button
//         Text (child)      <-- TMP Text "GREEN"
//       Btn_Blue            <-- Button
//         Text (child)      <-- TMP Text "BLUE"
//
//     FeedbackText          <-- TMP Text (shows "+1" or "Wrong!", hidden at start)
//     CountdownPanel        <-- Panel (shown during 3-2-1 countdown)
//       Text_Countdown      <-- TMP Text
//     DonePanel             <-- Panel (shown when task ends, hidden at start)
//       Text_Done           <-- TMP Text
//       Btn_Next            <-- Button

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StroopTask : MonoBehaviour
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // INSPECTOR FIELDS — drag objects from your Hierarchy into these slots
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Header("=== SET THIS PER SCENE ===")]
    [Tooltip("0 = Word task, 1 = Color task, 2 = ColorWord task")]
    public int taskIndex = 0;

    [Header("Task Settings")]
    public float taskDuration = 45f;

    [Header("Stimulus Display")]
    public TMP_Text textStimulus;   // The big text in the middle card
    public Image cardPanel;      // The card background image

    [Header("HUD")]
    public TMP_Text textScore;
    public TMP_Text textTimer;
    public Slider timerBar;
    public TMP_Text textTaskTitle;
    public TMP_Text textInstruction;

    [Header("Answer Buttons")]
    public Button btnRed;
    public Button btnGreen;
    public Button btnBlue;

    [Header("Feedback")]
    public TMP_Text feedbackText;   // shows "+1" or "Wrong!"

    [Header("Countdown Panel")]
    public GameObject countdownPanel;
    public TMP_Text textCountdown;

    [Header("Done Panel")]
    public GameObject donePanel;
    public TMP_Text textDone;
    public Button btnNext;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE — do not touch
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // The three color words and their matching colors
    static readonly string[] ColorWords = { "RED", "GREEN", "BLUE" };
    static readonly Color[] InkColors = {
        new Color(0.95f, 0.22f, 0.22f),   // Red
        new Color(0.18f, 0.78f, 0.35f),   // Green
        new Color(0.22f, 0.55f, 1.00f)    // Blue
    };
    static readonly Color BG_CardColor = new Color(0.10f, 0.12f, 0.20f);
    static readonly Color BG_Correct = new Color(0.10f, 0.60f, 0.20f, 0.5f);
    static readonly Color BG_Wrong = new Color(0.80f, 0.10f, 0.10f, 0.5f);

    int currentScore = 0;
    int correctAnswer = 0;   // 0=Red 1=Green 2=Blue
    float timeRemaining;
    bool taskRunning = false;
    float stimulusStartTime;     // when current stimulus appeared

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UNITY START
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void Start()
    {
        timeRemaining = taskDuration;

        // Hook up timer bar
        if (timerBar != null)
        {
            timerBar.minValue = 0f;
            timerBar.maxValue = taskDuration;
            timerBar.value = taskDuration;
        }

        // Set title and instruction text
        SetTitleText();

        // Hide panels that start hidden
        if (donePanel != null) donePanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        // Hook up buttons - each button passes its color index (0,1,2)
        if (btnRed != null) btnRed.onClick.AddListener(() => OnButtonPressed(0));
        if (btnGreen != null) btnGreen.onClick.AddListener(() => OnButtonPressed(1));
        if (btnBlue != null) btnBlue.onClick.AddListener(() => OnButtonPressed(2));

        // Hook up Next button
        if (btnNext != null) btnNext.onClick.AddListener(OnNextButtonClicked);

        // Set button colors
        SetButtonColor(btnRed, InkColors[0]);
        SetButtonColor(btnGreen, InkColors[1]);
        SetButtonColor(btnBlue, InkColors[2]);

        // Disable buttons until countdown finishes
        SetButtonsActive(false);

        // Start 3-2-1 countdown
        StartCoroutine(CountdownRoutine());
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UNITY UPDATE — runs every frame
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void Update()
    {
        if (!taskRunning) return;

        timeRemaining -= Time.deltaTime;

        // Update timer display
        int secs = Mathf.CeilToInt(timeRemaining);
        if (textTimer != null) textTimer.text = secs.ToString();
        if (timerBar != null) timerBar.value = Mathf.Max(0f, timeRemaining);

        // Change timer color when low
        if (textTimer != null)
        {
            if (timeRemaining > taskDuration * 0.4f) textTimer.color = Color.white;
            else if (timeRemaining > taskDuration * 0.2f) textTimer.color = new Color(1f, 0.76f, 0.1f);
            else textTimer.color = new Color(1f, 0.2f, 0.2f);
        }

        // End task when time is up
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndTask();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // COUNTDOWN
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    IEnumerator CountdownRoutine()
    {
        if (countdownPanel != null) countdownPanel.SetActive(true);

        string[] steps = { "3", "2", "1", "GO!" };
        foreach (string s in steps)
        {
            if (textCountdown != null) textCountdown.text = s;
            yield return new WaitForSeconds(0.8f);
        }

        if (countdownPanel != null) countdownPanel.SetActive(false);

        // Task begins!
        taskRunning = true;
        SetButtonsActive(true);
        ShowNextStimulus();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // STIMULUS DISPLAY
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void ShowNextStimulus()
    {
        if (textStimulus == null) return;

        stimulusStartTime = Time.time;

        int wordIndex = Random.Range(0, 3);
        int colorIndex = Random.Range(0, 3);

        switch (taskIndex)
        {
            // ── TASK 0: Word Reading ──────────────────────────────────
            // Show a color word in BLACK. Player taps the word's meaning.
            case 0:
                textStimulus.text = ColorWords[wordIndex];
                textStimulus.color = Color.white;
                correctAnswer = wordIndex;   // answer = what the word says
                break;

            // ── TASK 1: Color Naming ──────────────────────────────────
            // Show "XXXX" in a color. Player taps that ink color.
            case 1:
                textStimulus.text = "XXXX";
                textStimulus.color = InkColors[colorIndex];
                correctAnswer = colorIndex;  // answer = the ink color
                break;

            // ── TASK 2: Color-Word Interference ──────────────────────
            // Show a color word in a DIFFERENT ink color.
            // Player must tap the INK color (ignore the word meaning).
            case 2:
                // Make sure word and ink color are different (the Stroop conflict!)
                int inkIndex = colorIndex;
                while (inkIndex == wordIndex)
                    inkIndex = Random.Range(0, 3);

                textStimulus.text = ColorWords[wordIndex];
                textStimulus.color = InkColors[inkIndex];
                correctAnswer = inkIndex;    // answer = ink color, NOT the word
                break;
        }

        // Reset card background
        if (cardPanel != null) cardPanel.color = BG_CardColor;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // BUTTON PRESSED
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void OnButtonPressed(int colorTapped)
    {
        if (!taskRunning) return;

        // Measure reaction time
        float reactionMs = (Time.time - stimulusStartTime) * 1000f;

        bool isCorrect = (colorTapped == correctAnswer);

        if (isCorrect)
        {
            currentScore++;
            if (textScore != null) textScore.text = "Score: " + currentScore;

            // Log RT
            switch (taskIndex)
            {
                case 0: GameData.RT_Word.Add(reactionMs); break;
                case 1: GameData.RT_Color.Add(reactionMs); break;
                case 2: GameData.RT_ColorWord.Add(reactionMs); break;
            }

            ShowFeedback(true);
        }
        else
        {
            // Log error
            switch (taskIndex)
            {
                case 0: GameData.Errors_Word++; break;
                case 1: GameData.Errors_Color++; break;
                case 2: GameData.Errors_ColorWord++; break;
            }

            ShowFeedback(false);
        }

        // Show next stimulus immediately
        ShowNextStimulus();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // FEEDBACK (green/red flash + text)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void ShowFeedback(bool correct)
    {
        // Flash card background color
        if (cardPanel != null)
            cardPanel.color = correct ? BG_Correct : BG_Wrong;

        // Show "+1" or "Wrong!"
        if (feedbackText != null)
        {
            feedbackText.text = correct ? "+1" : "Wrong!";
            feedbackText.color = correct
                ? new Color(0.2f, 1f, 0.4f)
                : new Color(1f, 0.3f, 0.3f);
            feedbackText.gameObject.SetActive(true);
            StartCoroutine(HideFeedbackAfterDelay(0.4f));
        }
    }

    IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // END TASK
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void EndTask()
    {
        taskRunning = false;
        SetButtonsActive(false);

        // Save this task's score into GameData
        switch (taskIndex)
        {
            case 0: GameData.Score_Word = currentScore; break;
            case 1: GameData.Score_Color = currentScore; break;
            case 2: GameData.Score_ColorWord = currentScore; break;
        }

        // If this is the last task, compute all scores now
        if (taskIndex == 2)
            GameData.ComputeScores();

        // Show done panel
        if (donePanel != null) donePanel.SetActive(true);

        // Score text only
        if (textDone != null)
        {
            textDone.text = $"Task Done!\nScore: {currentScore}";
        }

        // Button text separately
        if (btnNext != null)
        {
            TMP_Text btnLabel = btnNext.GetComponentInChildren<TMP_Text>(true);

            if (btnLabel != null)
                btnLabel.text = taskIndex < 2 ? "Next Task →" : "See Results →";
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // NEXT BUTTON
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void OnNextButtonClicked()
    {
        switch (taskIndex)
        {
            case 0: SceneManager.LoadScene("Stroop_Color"); break;
            case 1: SceneManager.LoadScene("Stroop_ColorWord"); break;
            case 2: SceneManager.LoadScene("Results"); break;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // HELPERS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void SetButtonsActive(bool active)
    {
        if (btnRed != null) btnRed.interactable = active;
        if (btnGreen != null) btnGreen.interactable = active;
        if (btnBlue != null) btnBlue.interactable = active;
    }

    static void SetButtonColor(Button btn, Color c)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    void SetTitleText()
    {
        if (textTaskTitle != null)
        {
            textTaskTitle.text = taskIndex switch
            {
                0 => "TASK 1 — WORD READING",
                1 => "TASK 2 — COLOR NAMING",
                _ => "TASK 3 — INTERFERENCE"
            };
        }

        if (textInstruction != null)
        {
            textInstruction.text = taskIndex switch
            {
                0 => "Tap the button that matches the WORD you read.",
                1 => "Tap the button that matches the COLOR of XXXX.",
                _ => "IGNORE the word — tap the button that matches the INK COLOR."
            };
        }
    }
}
