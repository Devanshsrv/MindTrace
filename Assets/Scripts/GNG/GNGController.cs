using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GNG
{
    public class GNGController : MonoBehaviour
    {
        enum AppState { Intro, Instructions, PreAttempt, Practice, Test, AttemptDone, Summary }
        enum TrialPhase { Idle, Fixation, Stimulus, Feedback }

        [Header("Panels")]
        public GameObject introPanel;
        public GameObject instructionsPanel;
        public GameObject preAttemptPanel;
        public GameObject testPanel;
        public GameObject attemptDonePanel;
        public GameObject summaryPanel;

        [Header("Intro")]
        public InputField sapInput;
        public InputField ageInput;
        public Button continueBtn;
        public Text sapErrorText;
        public Text storeInfoText;

        [Header("Instructions")]
        public Text instructionsSubText;
        public Button practiceBtn;
        public Button beginBtn;

        [Header("PreAttempt")]
        public Text preAttemptTitle;
        public Text preAttemptInfo;
        public Button preAttemptBeginBtn;
        public Button preAttemptCancelBtn;
        public Transform preAttemptDotsContainer;
        public GameObject dotPrefab;

        [Header("Test")]
        public Text testTimerText;
        public Text testAttemptText;
        public Text testCountText;
        public GameObject testFixation;
        public Image testStimulus;
        public GameObject testStimulusX;
        public GameObject testFeedbackBox;
        public Image testFeedbackImg;
        public Text testFeedbackText;
        public Image testTapImg;
        public Text testTapLabel;
        public Button testTapBtn;

        [Header("AttemptDone")]
        public Text attemptDoneTitle;
        public Text attemptDoneSub;
        public Transform attemptDoneDotsContainer;
        public Transform attemptDoneCol1;
        public Transform attemptDoneCol2;
        public Text adhdRiskText;
        public Text adhdScoreText;
        public Text demRiskText;
        public Text demScoreText;
        public Button attemptDoneNextBtn;
        public Text attemptDoneNextLabel;
        public GameObject metricRowPrefab;

        [Header("Summary")]
        public Text summarySubText;
        public Transform summaryAttemptsContainer;
        public GameObject summaryAttemptBlockPrefab;
        public Button shareBtn;
        public Button newSubjectBtn;

        [Header("Colors")]
        public Color goColor = new Color(0.30f, 0.86f, 0.45f);
        public Color noGoColor = new Color(0.94f, 0.36f, 0.36f);
        public Color tapIdleColor = new Color(1, 1, 1, 0.08f);
        public Color tapIdleLabelColor = new Color(1, 1, 1, 0.35f);
        public Color feedbackOkBg = new Color(0.10f, 0.40f, 0.20f, 0.6f);
        public Color feedbackOkFg = new Color(0.55f, 1f, 0.65f);
        public Color feedbackWarnBg = new Color(0.45f, 0.30f, 0f, 0.6f);
        public Color feedbackWarnFg = new Color(1f, 0.78f, 0.30f);
        public Color feedbackErrBg = new Color(0.45f, 0.10f, 0.10f, 0.6f);
        public Color feedbackErrFg = new Color(1f, 0.45f, 0.45f);
        public Color dotCompleted = new Color(0.30f, 0.83f, 0.86f);
        public Color dotActive = Color.white;
        public Color dotInactive = new Color(1, 1, 1, 0.10f);

        AppState state = AppState.Intro;
        string sapId = "";
        int age = 0;
        int currentAttempt = 1;
        List<AttemptResult> attempts = new List<AttemptResult>();
        SessionStore allSessions;

        List<TrialType> trials;
        int trialIdx;
        TrialPhase phase = TrialPhase.Idle;
        float phaseStartTime;
        float stimulusOnsetTime;
        float testStartTime;
        bool responded;
        bool isPractice;
        string lastFeedback = "";
        List<TrialRecord> trialLog;
        string lastSummaryPath;
        string lastTrialPath;

        void Awake()
        {
            EnsureEventSystem();
            allSessions = GNGStorage.Load();

            if (sapInput != null) sapInput.onValueChanged.AddListener(v => { sapId = v.Trim(); UpdateContinueState(); });
            if (ageInput != null) ageInput.onValueChanged.AddListener(v => { int.TryParse(v, out age); });
            if (continueBtn != null) continueBtn.onClick.AddListener(() =>
            {
                if (sapId.Length < 3) { if (sapErrorText != null) sapErrorText.text = "SAP ID must be at least 3 characters"; return; }
                ShowInstructions();
            });
            if (practiceBtn != null) practiceBtn.onClick.AddListener(() => StartSession(true));
            if (beginBtn != null) beginBtn.onClick.AddListener(() => { currentAttempt = 1; ShowPreAttempt(); });
            if (preAttemptBeginBtn != null) preAttemptBeginBtn.onClick.AddListener(() => StartSession(false));
            if (preAttemptCancelBtn != null) preAttemptCancelBtn.onClick.AddListener(ResetForNewSubject);
            if (testTapBtn != null) testTapBtn.onClick.AddListener(OnTap);
            if (attemptDoneNextBtn != null) attemptDoneNextBtn.onClick.AddListener(OnAttemptDoneNext);
            if (shareBtn != null)
                shareBtn.onClick.AddListener(OpenMainMenu);
            if (newSubjectBtn != null) newSubjectBtn.onClick.AddListener(ResetForNewSubject);

            ShowIntro();
        }

        void OpenMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var newModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModule != null) { var go = new GameObject("EventSystem", typeof(EventSystem)); go.AddComponent(newModule); }
            else new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void HideAll()
        {
            if (introPanel != null) introPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            if (preAttemptPanel != null) preAttemptPanel.SetActive(false);
            if (testPanel != null) testPanel.SetActive(false);
            if (attemptDonePanel != null) attemptDonePanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        void ShowIntro()
        {
            state = AppState.Intro;
            HideAll();
            if (introPanel != null) introPanel.SetActive(true);
            if (sapInput != null) sapInput.text = sapId;
            if (ageInput != null) ageInput.text = age > 0 ? age.ToString() : "";
            if (sapErrorText != null) sapErrorText.text = "";
            UpdateContinueState();
            if (storeInfoText != null && allSessions != null)
            {
                int totalAttempts = 0;
                foreach (var s in allSessions.sessions) totalAttempts += s.attempts.Count;
                storeInfoText.text = allSessions.sessions.Count + " stored sessions · " + totalAttempts + " attempts";
            }
        }

        void UpdateContinueState()
        {
            if (continueBtn != null)
            {
                bool ok = sapId != null && sapId.Length >= 4;

                continueBtn.interactable = ok;

                if (sapErrorText != null)
                {
                    if (!ok)
                        sapErrorText.text = "SAP ID must be at least 4 characters";
                    else
                        sapErrorText.text = "";
                }
            }
        }

        void ShowInstructions()
        {
            state = AppState.Instructions;
            HideAll();
            if (instructionsPanel != null) instructionsPanel.SetActive(true);
            if (instructionsSubText != null) instructionsSubText.text = "SAP " + sapId + "  ·  " + GNGConfig.ATTEMPTS + " attempts × " + GNGConfig.MAIN_TRIALS + " trials";
        }

        void ShowPreAttempt()
        {
            state = AppState.PreAttempt;
            HideAll();
            if (preAttemptPanel != null) preAttemptPanel.SetActive(true);
            if (preAttemptTitle != null) preAttemptTitle.text = "Attempt " + currentAttempt + " / " + GNGConfig.ATTEMPTS;
            if (preAttemptInfo != null) preAttemptInfo.text = GNGConfig.MAIN_TRIALS + " trials, ~2 minutes\nNo feedback shown";
            BuildDots(preAttemptDotsContainer, currentAttempt - 1, currentAttempt);
        }

        void BuildDots(Transform container, int completed, int active)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--) Destroy(container.GetChild(i).gameObject);
            for (int i = 1; i <= GNGConfig.ATTEMPTS; i++)
            {
                GameObject dot = dotPrefab != null ? Instantiate(dotPrefab, container) : new GameObject("Dot" + i, typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(container, false);
                var img = dot.GetComponent<Image>();
                if (img == null) img = dot.AddComponent<Image>();
                if (i <= completed) img.color = dotCompleted;
                else if (i == active) img.color = dotActive;
                else img.color = dotInactive;
            }
        }

        void StartSession(bool practice)
        {
            isPractice = practice;
            int n = practice ? GNGConfig.PRACTICE_TRIALS : GNGConfig.MAIN_TRIALS;
            trials = GNGStats.GenerateTrials(n, GNGConfig.GO_PROBABILITY);
            trialIdx = 0;
            trialLog = new List<TrialRecord>();
            testStartTime = Time.realtimeSinceStartup;
            state = practice ? AppState.Practice : AppState.Test;
            HideAll();
            if (testPanel != null) testPanel.SetActive(true);
            if (testAttemptText != null) testAttemptText.text = practice ? "Practice" : ("Attempt: " + currentAttempt + "/" + GNGConfig.ATTEMPTS);
            EnterFixation();
        }

        void EnterFixation()
        {
            phase = TrialPhase.Fixation;
            phaseStartTime = Time.realtimeSinceStartup;
            responded = false;
            if (testFixation != null) testFixation.SetActive(true);
            if (testStimulus != null) testStimulus.gameObject.SetActive(false);
            if (testStimulusX != null) testStimulusX.SetActive(false);
            if (testFeedbackBox != null) testFeedbackBox.SetActive(false);
            UpdateTestHud();
        }

        void EnterStimulus()
        {
            phase = TrialPhase.Stimulus;
            phaseStartTime = Time.realtimeSinceStartup;
            stimulusOnsetTime = phaseStartTime;
            responded = false;
            var t = trials[trialIdx];
            bool isGo = t == TrialType.Go;
            if (testFixation != null) testFixation.SetActive(false);
            if (testStimulus != null)
            {
                testStimulus.gameObject.SetActive(true);
                testStimulus.color = isGo ? goColor : noGoColor;
            }
            if (testStimulusX != null) testStimulusX.SetActive(!isGo);
            if (testTapImg != null)
            {
                testTapImg.color = tapIdleColor;

                if (testTapLabel != null)
                    testTapLabel.color = tapIdleLabelColor;
            }
        }

        void EnterFeedback()
        {
            phase = TrialPhase.Feedback;
            phaseStartTime = Time.realtimeSinceStartup;
            if (testStimulus != null) testStimulus.gameObject.SetActive(false);
            if (testStimulusX != null) testStimulusX.SetActive(false);
            if (testTapImg != null)
            {
                testTapImg.color = tapIdleColor;
                if (testTapLabel != null) testTapLabel.color = tapIdleLabelColor;
            }
            if (isPractice && !string.IsNullOrEmpty(lastFeedback) && testFeedbackBox != null)
            {
                testFeedbackBox.SetActive(true);
                if (lastFeedback == "hit" || lastFeedback == "correct")
                {
                    if (testFeedbackImg != null) testFeedbackImg.color = feedbackOkBg;
                    if (testFeedbackText != null) { testFeedbackText.color = feedbackOkFg; testFeedbackText.text = lastFeedback == "hit" ? "Correct ✓" : "Good ✓"; }
                }
                else if (lastFeedback == "omission")
                {
                    if (testFeedbackImg != null) testFeedbackImg.color = feedbackWarnBg;
                    if (testFeedbackText != null) { testFeedbackText.color = feedbackWarnFg; testFeedbackText.text = "Too slow"; }
                }
                else
                {
                    if (testFeedbackImg != null) testFeedbackImg.color = feedbackErrBg;
                    if (testFeedbackText != null) { testFeedbackText.color = feedbackErrFg; testFeedbackText.text = "Should not press"; }
                }
            }
        }

        void OnTap()
        {
            if (phase != TrialPhase.Stimulus || responded) return;
            int rt = Mathf.RoundToInt((Time.realtimeSinceStartup - stimulusOnsetTime) * 1000f);
            responded = true;
            var t = trials[trialIdx];
            string outcome = t == TrialType.Go ? "hit" : "commission";
            trialLog.Add(new TrialRecord
            {
                trialNum = trialIdx,
                type = t == TrialType.Go ? "go" : "nogo",
                responded = true,
                rtMs = rt,
                outcome = outcome
            });
            lastFeedback = outcome;
            EnterFeedback();
        }

        void Update()
        {
            if (state != AppState.Practice && state != AppState.Test) return;

            if (testTimerText != null)
            {
                int elapsed = (int)(Time.realtimeSinceStartup - testStartTime);
                int mm = elapsed / 60, ss = elapsed % 60;
                testTimerText.text = (mm < 10 ? "0" : "") + mm + ":" + (ss < 10 ? "0" : "") + ss;
            }

            float pe = Time.realtimeSinceStartup - phaseStartTime;
            if (phase == TrialPhase.Fixation && pe * 1000f >= GNGConfig.FIXATION_MS)
            {
                EnterStimulus();
            }
            else if (phase == TrialPhase.Stimulus && pe * 1000f >= GNGConfig.STIMULUS_MS && !responded)
            {
                var t = trials[trialIdx];
                string outcome = t == TrialType.Go ? "omission" : "correct";
                trialLog.Add(new TrialRecord
                {
                    trialNum = trialIdx,
                    type = t == TrialType.Go ? "go" : "nogo",
                    responded = false,
                    rtMs = 0,
                    outcome = outcome
                });
                lastFeedback = outcome;
                responded = true;
                EnterFeedback();
            }
            else if (phase == TrialPhase.Feedback && pe * 1000f >= GNGConfig.FEEDBACK_MS)
            {
                trialIdx++;
                UpdateTestHud();
                if (trialIdx >= trials.Count)
                {
                    phase = TrialPhase.Idle;
                    if (isPractice) ShowPreAttempt();
                    else FinishAttempt();
                }
                else EnterFixation();
            }
        }

        void UpdateTestHud()
        {
            if (testCountText != null) testCountText.text = (trialIdx + 1) + " / " + trials.Count;
        }

        void FinishAttempt()
        {
            var result = GNGStats.Compute(trialLog, currentAttempt);
            attempts.Add(result);
            ShowAttemptDone(result);
        }

        void ShowAttemptDone(AttemptResult r)
        {
            state = AppState.AttemptDone;
            HideAll();
            if (attemptDonePanel != null) attemptDonePanel.SetActive(true);

            if (attemptDoneTitle != null) attemptDoneTitle.text = "Attempt " + r.attemptNum + " Complete";
            if (attemptDoneSub != null) attemptDoneSub.text = "SAP " + sapId;
            BuildDots(attemptDoneDotsContainer, attempts.Count, attempts.Count);

            ClearChildren(attemptDoneCol1);
            AddMetric(attemptDoneCol1, "Hit Rate", r.hitR + "%");
            AddMetric(attemptDoneCol1, "Total Correct", (r.hits + r.crj).ToString());
            AddMetric(attemptDoneCol1, "Commission Errors", r.cms + " / " + r.nogoT);
            AddMetric(attemptDoneCol1, "Omission Errors", r.oms + " / " + r.goT);
            AddMetric(attemptDoneCol1, "CE Rate", r.ceR + "%");
            AddMetric(attemptDoneCol1, "OE Rate", r.oeR + "%");

            ClearChildren(attemptDoneCol2);
            AddMetric(attemptDoneCol2, "Mean RT", r.mRT + " ms");
            AddMetric(attemptDoneCol2, "Median RT", r.medRT + " ms");
            AddMetric(attemptDoneCol2, "RT Variability", r.sdRT + " ms");
            AddMetric(attemptDoneCol2, "Min / Max RT", r.minRT + " / " + r.maxRT);
            AddMetric(attemptDoneCol2, "RT Change", (r.rtDec > 0 ? "+" : "") + r.rtDec + " ms");

            if (adhdRiskText != null) adhdRiskText.text = r.adhdRisk + " RISK";
            if (adhdScoreText != null) adhdScoreText.text = "score " + r.adhdScore + "/6";
            if (demRiskText != null) demRiskText.text = r.demRisk + " RISK";
            if (demScoreText != null) demScoreText.text = "score " + r.demScore + "/8";

            bool isLast = currentAttempt >= GNGConfig.ATTEMPTS;
            if (attemptDoneNextLabel != null) attemptDoneNextLabel.text = isLast ? "View Summary →" : "Continue →";
        }

        void OnAttemptDoneNext()
        {
            bool isLast = currentAttempt >= GNGConfig.ATTEMPTS;
            if (isLast) ShowSummary();
            else { currentAttempt++; ShowPreAttempt(); }
        }

        void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        void AddMetric(Transform parent, string label, string value)
        {
            if (parent == null) return;

            GameObject row = Instantiate(metricRowPrefab, parent);

            var legacy = row.GetComponentsInChildren<Text>(true);
            if (legacy.Length >= 2)
            {
                legacy[0].text = label;
                legacy[1].text = value;
                return;
            }

            var tmp = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (tmp.Length >= 2)
            {
                tmp[0].text = label;
                tmp[1].text = value;
            }
        }

        void ShowSummary()
        {
            state = AppState.Summary;
            HideAll();
            if (summaryPanel != null) summaryPanel.SetActive(true);

            var session = new SessionRecord
            {
                sapId = sapId,
                age = age,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                attempts = new List<AttemptResult>(attempts)
            };
            allSessions.sessions.Add(session);
            GNGStorage.Save(allSessions);

            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string sessSummaryFn = "gng_" + sapId + "_summary_" + stamp + ".csv";
            string sessTrialFn = "gng_" + sapId + "_trials_" + stamp + ".csv";
            lastSummaryPath = GNGCSV.WriteFile(sessSummaryFn, GNGCSV.SummaryCSV(new List<SessionRecord> { session }));
            lastTrialPath = GNGCSV.WriteFile(sessTrialFn, GNGCSV.TrialCSV(new List<SessionRecord> { session }));
            GNGCSV.WriteFile("gng_all_summary.csv", GNGCSV.SummaryCSV(allSessions.sessions));
            GNGCSV.WriteFile("gng_all_trials.csv", GNGCSV.TrialCSV(allSessions.sessions));

            if (summarySubText != null) summarySubText.text = "SAP " + sapId + (age > 0 ? "  ·  Age " + age : "");

            ClearChildren(summaryAttemptsContainer);

            for (int i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];

                GameObject blk;

                if (summaryAttemptBlockPrefab != null)
                    blk = Instantiate(summaryAttemptBlockPrefab, summaryAttemptsContainer);
                else
                {
                    blk = new GameObject("Attempt" + a.attemptNum, typeof(RectTransform));
                    blk.transform.SetParent(summaryAttemptsContainer, false);
                    blk.AddComponent<Text>();
                }

                string body =
                    "Attempt " + a.attemptNum + "\n" +
                    "Hit: " + a.hitR + "%   CE: " + a.ceR + "%   OE: " + a.oeR + "%\n" +
                    "Mean RT: " + a.mRT + "ms   SD: " + a.sdRT + "ms   Δ: " +
                    (a.rtDec > 0 ? "+" : "") + a.rtDec + "ms\n" +
                    "ADHD: " + a.adhdRisk + "   Cognitive: " + a.demRisk;

                var txt = blk.GetComponentInChildren<Text>(true);
                if (txt != null)
                {
                    txt.text = body;
                    continue;
                }

                var tmp = blk.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (tmp != null)
                    tmp.text = body;
            }
            GNGCSV.ShareFiles("GNG results " + sapId, "Go/No-Go results for SAP " + sapId, lastSummaryPath, lastTrialPath);
        }

        void ResetForNewSubject()
        {
            sapId = ""; age = 0; currentAttempt = 1;
            attempts = new List<AttemptResult>();
            phase = TrialPhase.Idle;
            ShowIntro();
        }
    }
}
