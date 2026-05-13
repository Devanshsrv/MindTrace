using System.Collections;               // needed for coroutines
using System.Collections.Generic;       // needed for lists
using UnityEngine;                      // Unity base library
using System.Diagnostics;               // used for time measurement
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*For Result Panel*/
using TMPro;   // required for TextMeshPro UI

public class CorsiGameManager : MonoBehaviour
{
    public List<CorsiBlock> blocks;     // list of all 9 blocks in the scene

    public float highlightTime = 0.7f;  // how long a block stays highlighted

    private List<int> sequence = new List<int>();      // correct sequence
    private List<int> playerInput = new List<int>();   // player taps

    private int sequenceLength = 2;     // starting sequence length
    private int trialNumber = 0;        // trial count within a level
    private int correctTrials = 0;      // number of correct trials at this level

    private int blockSpan = 0;          // longest successful level
    private int totalCorrect = 0;       // total correct sequences

    private bool playerTurn = false;    // determines if player can click

    private float stimulusEndTime;      // time when sequence playback ends
    private float firstTapTime;         // time of first player tap

    private List<float> interTapTimes = new List<float>();  // hesitation intervals

    /*For Results Panel*/
    public GameObject resultPanel;   // reference to result screen
    public TMP_Text spanText;        // UI text for span
    public TMP_Text correctText;     // UI text for total correct
    public TMP_Text scoreText;       // UI text for product score
    public TMP_Text reactionText;    // UI text for reaction time
    public TMP_Text hesitationText;   // UI for hesitation metric
    public TMP_Text accuracyText;     // UI for accuracy
    public TMP_Text statusText;  // UI element showing cognitive risk status

    // ----- Behavioral Metrics Tracking -----

    private List<float> responseLatencies = new List<float>();  // stores time from stimulus end to first tap
    private List<float> interTapIntervals = new List<float>();  // stores hesitation times between taps

    private int totalTrials = 0;   // total number of trials played
    private int totalErrors = 0;   // total incorrect trials

    // ----- For Timer -----

    public TMP_Text timerText;  // UI timer display
    private float startTime;    // test start time
    private bool timerRunning;  // controls timer

    // ----- For Bottom Bar Text -----

    public TMP_Text levelText;  // bottom bar sequence length

    // ----- For Border Colour Glow -----
    public Outline innerOutline;
    public Outline outerOutline;
    public Color recallColor = Color.cyan;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;


    // ----- For Attempts Logging -----
    private int currentAttempt = 1;
    private List<string> attemptData = new List<string>();

    // ----- For Dynamically changing the button -----
    public TMP_Text continueButtonText;

    // ----- For CSV Logging -----
    public CSVLogger logger;

    void Start()
    {
        timerRunning = false; // don't start timer yet
    }

    public void BeginGame()
    {
        attemptData.Clear();
        currentAttempt = 1;
        startTime = Time.time;
        timerRunning = true;

        StartCoroutine(StartTrial());
    }


    void Update()
    {
        if (timerRunning)
        {
            float elapsed = Time.time - startTime;  // calculate elapsed time

            int minutes = Mathf.FloorToInt(elapsed / 60);
            int seconds = Mathf.FloorToInt(elapsed % 60);

            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    IEnumerator StartTrial()
    {
        levelText.text = "Attempt: " + currentAttempt + "/3 | Sequence Length: " + sequenceLength +
                 " | Trial: " + (trialNumber + 1) + "/2";
        yield return new WaitForSeconds(1f);   // small delay before showing sequence

        playerTurn = false;                   // disable player clicks

        sequence.Clear();                     // clear previous sequence
        playerInput.Clear();                  // clear player input
        interTapTimes.Clear();                // clear hesitation tracking

        int lastBlock = -1;                   // prevent consecutive duplicates

        for (int i = 0; i < sequenceLength; i++)   // generate sequence of current length
        {
            int randomBlock;

            do
            {
                randomBlock = Random.Range(0, blocks.Count);  // pick random block
            }
            while (randomBlock == lastBlock);  // avoid repeating same block consecutively

            lastBlock = randomBlock;

            sequence.Add(randomBlock + 1);    // store block ID

            blocks[randomBlock].Highlight();  // highlight the block

            yield return new WaitForSeconds(highlightTime);

            blocks[randomBlock].ResetColor(); // return to default

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.25f);

        // start of recall phase
        stimulusEndTime = Time.time;

        SetBorderGlow(recallColor);

        playerTurn = true;
    }

    public void PlayerClicked(int blockID, CorsiBlock clickedBlock)
    {
        if (!playerTurn) return;

        // ----- Timing logic (UNCHANGED) -----
        if (playerInput.Count == 0)
        {
            firstTapTime = Time.time;
            float latency = firstTapTime - stimulusEndTime;
            responseLatencies.Add(latency);
        }
        else
        {
            float interval = Time.time - firstTapTime;
            interTapIntervals.Add(interval);
            firstTapTime = Time.time;
        }

        playerInput.Add(blockID);
        int index = playerInput.Count - 1;

        // ❌ WRONG CLICK
        if (playerInput[index] != sequence[index])
        {
            // 🔴 block red
            StartCoroutine(clickedBlock.ClickFlash(wrongColor));

            // 🔴 border red
            SetBorderGlow(wrongColor);

            // 🚫 stop input immediately
            playerTurn = false;

            // ⏳ delayed transition
            StartCoroutine(HandleEndTrial(false));
            return;
        }

        // ✅ CORRECT CLICK
        StartCoroutine(clickedBlock.ClickFlash(correctColor));

        // ✅ SEQUENCE COMPLETED
        if (playerInput.Count == sequence.Count)
        {
            // 🟢 border green
            SetBorderGlow(correctColor);

            playerTurn = false;

            StartCoroutine(HandleEndTrial(true));
        }
    }

    void EndTrial(bool success)
    {
        totalTrials++;   // increase trial count
        playerTurn = false;   // stop player input

        if (success)
        {
            totalCorrect++;     // increment correct sequences
            correctTrials++;    // increment correct trials at this level
        }
        else
        {
            totalErrors++;      // count errors
        }

        trialNumber++;        // increase trial count

        if (trialNumber >= 2)  // two trials completed
        {
            if (correctTrials > 0)   // at least one success
            {
                blockSpan = sequenceLength;  // update span
                sequenceLength++;            // increase difficulty
            }
            else
            {
                EndTest();  // both trials failed → stop test
                return;
            }

            trialNumber = 0;
            correctTrials = 0;
        }

        StartCoroutine(StartTrial());  // start next trial
    }

    public bool IsPlayerTurn()
    {
        return playerTurn; // returns true only when player input is allowed
    }


    IEnumerator HandleEndTrial(bool success)
    {
        playerTurn = false;

        // wait before moving to next trial (brain reset)
        yield return new WaitForSeconds(1.2f);

        // reset border
        SetBorderGlow(new Color(0.176f, 0.216f, 0.282f, 1f)); // default border color

        EndTrial(success);
    }

    void SetBorderGlow(Color color)
    {
        // inner sharp outline
        innerOutline.effectColor = color;

        // outer soft glow (same color, lower alpha)
        outerOutline.effectColor = new Color(color.r, color.g, color.b, 0.3f);
    }

    void EndTest()
    {
        UnityEngine.Debug.Log("TEST COMPLETE");

        int corsiProduct = blockSpan * totalCorrect;  // calculate product score
        float avgLatency = 0f;   // average response latency
        float avgHesitation = 0f; // average inter-tap interval

        if (responseLatencies.Count > 0)
        {
            foreach (float value in responseLatencies)   // sum all latency values
            {
                avgLatency += value;
            }

            avgLatency /= responseLatencies.Count;  // compute mean
        }

        if (interTapIntervals.Count > 0)
        {
            foreach (float value in interTapIntervals)   // sum hesitation intervals
            {
                avgHesitation += value;
            }

            avgHesitation /= interTapIntervals.Count;   // compute mean
        }

        float accuracy = 0f;   // accuracy percentage

        if (totalTrials > 0)
        {
            accuracy = ((float)totalCorrect / totalTrials) * 100f;  // calculate accuracy
        }
        // ----- Determine Cognitive Status -----

        string status = "Normal";  // default status

        Color statusColor = new Color(0.13f, 0.77f, 0.37f);
        // green (#22C55E) for normal cognitive performance

        // check for mild risk conditions
        if (blockSpan <= 4 || avgLatency > 2.5f)
        {
            status = "Risk";
            statusColor = new Color(0.96f, 0.62f, 0.04f);
            // orange (#F59E0B) for moderate risk
        }

        // check for higher risk conditions
        if (blockSpan <= 3 || avgLatency > 4f)
        {
            status = "High Risk";
            statusColor = new Color(0.94f, 0.27f, 0.27f);
            // red (#EF4444) for high risk
        }

        // update UI text
        //statusText.text = "Cognitive Status: " + status;

        // apply the corresponding color
        statusText.color = statusColor;

        timerRunning = false;  // stop timer

        spanText.text = "Block Span: " + blockSpan;  // display span

        correctText.text = "Total Correct: " + totalCorrect;  // display correct sequences

        scoreText.text = "Corsi Product Score: " + corsiProduct;  // display product score

        reactionText.text = "Avg Response Latency: " + avgLatency.ToString("F2") + " s";

        hesitationText.text = "Avg Hesitation: " + avgHesitation.ToString("F2") + " s";

        accuracyText.text = "Accuracy: " + accuracy.ToString("F1") + "%";

        statusText.text = "Cognitive Status: " + status.ToUpper();

        // Log results to CSV
        // Store this attempt's data
        string attempt = blockSpan + "," + totalCorrect + "," + corsiProduct + "," +
                         avgLatency.ToString("F2") + "," +
                         avgHesitation.ToString("F2") + "," +
                         accuracy.ToString("F1") + "," + status;

        if (attemptData.Count < currentAttempt)
        {
            attemptData.Add(attempt);
        }

        // Show result screen
        resultPanel.SetActive(true);

        if (currentAttempt < 3)
        {
            continueButtonText.text = "Continue";
        }
        else
        {
            continueButtonText.text = "Finish";
        }
    }

    public void RestartTest()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // reload scene
    }

    void ResetGame()
    {
        sequenceLength = 2;
        trialNumber = 0;
        correctTrials = 0;

        blockSpan = 0;
        totalCorrect = 0;

        totalTrials = 0;
        totalErrors = 0;

        responseLatencies.Clear();
        interTapIntervals.Clear();
    }

    public void ContinueToNextAttempt()
    {
        resultPanel.SetActive(false);

        if (currentAttempt < 3)
        {
            currentAttempt++;

            ResetGame();

            startTime = Time.time;
            timerRunning = true;

            StartCoroutine(StartTrial());
        }
        else
        {
            // Save after final attempt
            logger.SaveFinalData(
                StartScreenManager.playerName,
                StartScreenManager.playerAge,
                attemptData
            );

            // Go back to start screen
            SceneManager.LoadScene(0);
        }
    }
}