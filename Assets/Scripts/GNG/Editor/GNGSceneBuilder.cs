#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Build;

namespace GNG.EditorTools
{
    public static class GNGSceneBuilder
    {
        static Color CYAN = new Color(0.30f, 0.83f, 0.86f);
        static Color WHITE = new Color(0.95f, 0.97f, 1f);
        static Color GREY = new Color(0.65f, 0.70f, 0.78f);
        static Color DIM = new Color(0.45f, 0.50f, 0.58f);
        static Color CARD = new Color(0.08f, 0.10f, 0.14f);
        static Color INPUT = new Color(0.13f, 0.15f, 0.20f);
        static Color BTN = new Color(0.30f, 0.83f, 0.86f);
        static Color BG = new Color(0.04f, 0.05f, 0.08f);
        static Color GO_C = new Color(0.30f, 0.86f, 0.45f);
        static Color RED_C = new Color(0.94f, 0.36f, 0.36f);
        static Color BORDER = new Color(1, 1, 1, 0.10f);

        static Font Fnt() { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }

        [MenuItem("GNG/Build Scene")]
        public static void Build()
        {
            var ctrlGo = GameObject.Find("GNGBootstrap");
            if (ctrlGo == null) { Debug.LogError("GNGBootstrap not found"); return; }
            var ctrl = ctrlGo.GetComponent<GNGController>();
            if (ctrl == null) { Debug.LogError("GNGController not found"); return; }

            var existing = GameObject.Find("GNG_Canvas");
            if (existing != null) Object.DestroyImmediate(existing);
            var es = Object.FindObjectOfType<EventSystem>();
            if (es != null) Object.DestroyImmediate(es.gameObject);

            var canvasGo = new GameObject("GNG_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;

            var newModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModule != null) { var n = new GameObject("EventSystem", typeof(EventSystem)); n.AddComponent(newModule); }
            else new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var bg = MkRect("Background", canvas.transform);
            FullStretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = BG;

            BuildIntro(canvas.transform, ctrl);
            BuildInstructions(canvas.transform, ctrl);
            BuildPreAttempt(canvas.transform, ctrl);
            BuildTest(canvas.transform, ctrl);
            BuildAttemptDone(canvas.transform, ctrl);
            BuildSummary(canvas.transform, ctrl);

            // initial visibility
            ctrl.introPanel.SetActive(true);
            ctrl.instructionsPanel.SetActive(false);
            ctrl.preAttemptPanel.SetActive(false);
            ctrl.testPanel.SetActive(false);
            ctrl.attemptDonePanel.SetActive(false);
            ctrl.summaryPanel.SetActive(false);

            EditorUtility.SetDirty(ctrl);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ctrlGo.scene);
            Debug.Log("[GNG] Scene built. Save scene to persist.");
        }

[MenuItem("GNG/Build APK")]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Android module missing", "Install Android Build Support via Unity Hub (Add Modules) for this Editor version.", "OK");
                return;
            }
            PlayerSettings.applicationIdentifier = string.IsNullOrEmpty(PlayerSettings.applicationIdentifier) || PlayerSettings.applicationIdentifier.StartsWith("com.DefaultCompany") ? "com.mindtrace.gng" : PlayerSettings.applicationIdentifier;
            PlayerSettings.productName = "GNG";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[GNG] Failed to switch to Android target.");
                    return;
                }
            }
            string outDir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Builds");
            if (!System.IO.Directory.Exists(outDir)) System.IO.Directory.CreateDirectory(outDir);
            string apkPath = System.IO.Path.Combine(outDir, "GNG.apk");
            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[GNG] Build result: " + report.summary.result + " -> " + apkPath);
        }


        // ============ INTRO ============
        static void BuildIntro(Transform parent, GNGController ctrl)
        {
            var p = MkRect("IntroPanel", parent); FullStretch(p);
            ctrl.introPanel = p.gameObject;

            var title = MkText("Title", p, "GO / NO-GO TEST", 96, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(60,-220), new Vector2(-60,-60));

            var sub = MkText("Sub", p, "Response Inhibition Assessment", 32, CYAN, TextAnchor.MiddleCenter);
            Anchor(sub.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(60,-280), new Vector2(-60,-220));

            ctrl.sapInput = MkInput("SapInput", p, "Enter your SAP ID", false);
            Anchor(ctrl.sapInput.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-360,80), new Vector2(360,180));

            ctrl.ageInput = MkInput("AgeInput", p, "Enter Age", true);
            Anchor(ctrl.ageInput.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-360,-40), new Vector2(360,60));

            ctrl.sapErrorText = MkText("SapError", p, "", 26, new Color(1f,0.45f,0.45f), TextAnchor.MiddleCenter);
            Anchor(ctrl.sapErrorText.rectTransform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-500,-100), new Vector2(500,-50));

            ctrl.continueBtn = MkButton("ContinueBtn", p, "Start Test", BTN, Color.black, 38, true);
            Anchor(ctrl.continueBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-200,-260), new Vector2(200,-150));

            ctrl.storeInfoText = MkText("StoreInfo", p, "", 22, DIM, TextAnchor.MiddleCenter);
            Anchor(ctrl.storeInfoText.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,30), new Vector2(0,70));
        }

        // ============ INSTRUCTIONS ============
        static void BuildInstructions(Transform parent, GNGController ctrl)
        {
            var p = MkRect("InstructionsPanel", parent); FullStretch(p);
            ctrl.instructionsPanel = p.gameObject;

            var title = MkText("Title", p, "INSTRUCTIONS", 70, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(60,-160), new Vector2(-60,-60));

            ctrl.instructionsSubText = MkText("Sub", p, "", 28, CYAN, TextAnchor.MiddleCenter);
            Anchor(ctrl.instructionsSubText.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(60,-220), new Vector2(-60,-160));

            // Two stimulus cards
            var goCard = MkCard("GoCard", p);
            Anchor(goCard.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-540,-80), new Vector2(-40,260));
            var goCircle = MkCircle("Circle", goCard, GO_C, 200);
            Anchor(goCircle.rectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(-100,-220), new Vector2(100,-20));
            var goLbl = MkText("Lbl", goCard, "GREEN — TAP", 30, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(goLbl.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,40), new Vector2(0,90));

            var noCard = MkCard("NoGoCard", p);
            Anchor(noCard.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(40,-80), new Vector2(540,260));
            var noCircle = MkCircle("Circle", noCard, RED_C, 200);
            Anchor(noCircle.rectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(-100,-220), new Vector2(100,-20));
            var x = MkText("X", noCircle.transform, "✕", 140, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(x.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var noLbl = MkText("Lbl", noCard, "RED — WITHHOLD", 30, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(noLbl.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,40), new Vector2(0,90));

            // Buttons
            ctrl.practiceBtn = MkButton("PracticeBtn", p, "Practice (10)", INPUT, WHITE, 30, false);
            Anchor(ctrl.practiceBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-440,80), new Vector2(-30,180));

            ctrl.beginBtn = MkButton("BeginBtn", p, "Begin Attempt 1 →", BTN, Color.black, 30, true);
            Anchor(ctrl.beginBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(30,80), new Vector2(440,180));
        }

        // ============ PRE ATTEMPT ============
        static void BuildPreAttempt(Transform parent, GNGController ctrl)
        {
            var p = MkRect("PreAttemptPanel", parent); FullStretch(p);
            ctrl.preAttemptPanel = p.gameObject;

            var hdr = MkText("Header", p, "GET READY", 56, CYAN, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(hdr.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-140), new Vector2(0,-50));

            var dots = MkRect("Dots", p);
            Anchor(dots, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(-140,-200), new Vector2(140,-160));
            var hl = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            ctrl.preAttemptDotsContainer = dots;

            ctrl.preAttemptTitle = MkText("Title", p, "Attempt 1 / 3", 70, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(ctrl.preAttemptTitle.rectTransform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-500,40), new Vector2(500,140));

            ctrl.preAttemptInfo = MkText("Info", p, "", 30, GREY, TextAnchor.MiddleCenter);
            Anchor(ctrl.preAttemptInfo.rectTransform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-500,-80), new Vector2(500,40));

            ctrl.preAttemptBeginBtn = MkButton("BeginBtn", p, "Begin →", BTN, Color.black, 38, true);
            Anchor(ctrl.preAttemptBeginBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-200,-220), new Vector2(200,-110));

            ctrl.preAttemptCancelBtn = MkButton("CancelBtn", p, "Cancel test", new Color(0,0,0,0), DIM, 24, false);
            Anchor(ctrl.preAttemptCancelBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-150,40), new Vector2(150,90));
        }

        // ============ TEST ============
        static void BuildTest(Transform parent, GNGController ctrl)
        {
            var p = MkRect("TestPanel", parent); FullStretch(p);
            ctrl.testPanel = p.gameObject;

            // Top bar
            ctrl.testAttemptText = MkText("Attempt", p, "Attempt: 1/3", 28, CYAN, TextAnchor.MiddleLeft, FontStyle.Bold);
            Anchor(ctrl.testAttemptText.rectTransform, new Vector2(0,1), new Vector2(0,1), new Vector2(60,-80), new Vector2(500,-30));

            ctrl.testTimerText = MkText("Timer", p, "00:00", 32, CYAN, TextAnchor.MiddleRight, FontStyle.Bold);
            Anchor(ctrl.testTimerText.rectTransform, new Vector2(1,1), new Vector2(1,1), new Vector2(-300,-80), new Vector2(-60,-30));

            ctrl.testCountText = MkText("Count", p, "0 / 0", 24, DIM, TextAnchor.MiddleCenter);
            Anchor(ctrl.testCountText.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,30), new Vector2(0,70));

            // Stimulus area (left half)
            var fix = MkText("Fixation", p, "+", 200, DIM, TextAnchor.MiddleCenter);
            Anchor(fix.rectTransform, new Vector2(0,0), new Vector2(0.5f,1), Vector2.zero, Vector2.zero);
            ctrl.testFixation = fix.gameObject;

            var stim = MkCircle("Stimulus", p, GO_C, 380);
            Anchor(stim.rectTransform, new Vector2(0.25f,0.5f), new Vector2(0.25f,0.5f), new Vector2(-190,-190), new Vector2(190,190));
            ctrl.testStimulus = stim;
            stim.gameObject.SetActive(false);

            var stimX = MkText("X", stim.transform, "✕", 220, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(stimX.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.testStimulusX = stimX.gameObject;
            stimX.gameObject.SetActive(false);

            // Feedback box (centered between)
            var fb = MkRect("FeedbackBox", p);
            Anchor(fb, new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-300,140), new Vector2(300,250));
            var fbImg = fb.gameObject.AddComponent<Image>();
            fbImg.color = new Color(0.10f,0.40f,0.20f,0.6f);
            ctrl.testFeedbackBox = fb.gameObject;
            ctrl.testFeedbackImg = fbImg;
            var fbT = MkText("Text", fb, "", 32, new Color(0.55f,1f,0.65f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(fbT.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.testFeedbackText = fbT;
            fb.gameObject.SetActive(false);

            // Tap button (right half)
            var tapGo = new GameObject("TapBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            tapGo.transform.SetParent(p, false);
            Anchor(tapGo.GetComponent<RectTransform>(), new Vector2(0.75f,0.5f), new Vector2(0.75f,0.5f), new Vector2(-220,-220), new Vector2(220,220));
            var tapImg = tapGo.GetComponent<Image>();
            tapImg.color = new Color(1,1,1,0.08f);
            tapImg.sprite = MakeRoundSprite(256);
            ctrl.testTapImg = tapImg;
            ctrl.testTapBtn = tapGo.GetComponent<Button>();
            ctrl.testTapBtn.targetGraphic = tapImg;
            var tapLbl = MkText("Label", tapGo.transform, "TAP", 60, new Color(1,1,1,0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(tapLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ctrl.testTapLabel = tapLbl;
        }

        // ============ ATTEMPT DONE ============
        static void BuildAttemptDone(Transform parent, GNGController ctrl)
        {
            var p = MkRect("AttemptDonePanel", parent); FullStretch(p);
            ctrl.attemptDonePanel = p.gameObject;

            ctrl.attemptDoneTitle = MkText("Title", p, "Attempt Complete", 50, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(ctrl.attemptDoneTitle.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-110), new Vector2(0,-40));

            ctrl.attemptDoneSub = MkText("Sub", p, "", 24, CYAN, TextAnchor.MiddleCenter);
            Anchor(ctrl.attemptDoneSub.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-150), new Vector2(0,-110));

            var dots = MkRect("Dots", p);
            Anchor(dots, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(-140,-200), new Vector2(140,-160));
            var hl = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            ctrl.attemptDoneDotsContainer = dots;

            // Card with two cols
            var card = MkCard("MetricsCard", p);
            Anchor(card.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-700,-180), new Vector2(700,250));

            var col1 = MkRect("Col1", card);
            Anchor(col1, new Vector2(0,0), new Vector2(0.5f,1), new Vector2(40,40), new Vector2(-20,-40));
            var v1 = col1.gameObject.AddComponent<VerticalLayoutGroup>();
            v1.spacing = 6; v1.childAlignment = TextAnchor.UpperLeft;
            v1.childControlWidth = true; v1.childControlHeight = true;
            v1.childForceExpandWidth = true; v1.childForceExpandHeight = false;
            ctrl.attemptDoneCol1 = col1;

            var col2 = MkRect("Col2", card);
            Anchor(col2, new Vector2(0.5f,0), new Vector2(1,1), new Vector2(20,40), new Vector2(-40,-40));
            var v2 = col2.gameObject.AddComponent<VerticalLayoutGroup>();
            v2.spacing = 6; v2.childAlignment = TextAnchor.UpperLeft;
            v2.childControlWidth = true; v2.childControlHeight = true;
            v2.childForceExpandWidth = true; v2.childForceExpandHeight = false;
            ctrl.attemptDoneCol2 = col2;

            // Risk pills
            var adhdPill = MkRect("AdhdPill", p);
            Anchor(adhdPill, new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-470,200), new Vector2(-30,300));
            var adhdImg = adhdPill.gameObject.AddComponent<Image>();
            adhdImg.color = new Color(0.45f,0.30f,0f);
            var adhdHdr = MkText("Hdr", adhdPill, "ADHD STATUS", 18, GREY, TextAnchor.UpperLeft, FontStyle.Bold);
            Anchor(adhdHdr.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-30), new Vector2(-20,-10));
            ctrl.adhdRiskText = MkText("Risk", adhdPill, "Moderate RISK", 28, new Color(1f,0.78f,0.30f), TextAnchor.UpperLeft, FontStyle.Bold);
            Anchor(ctrl.adhdRiskText.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-65), new Vector2(-20,-30));
            ctrl.adhdScoreText = MkText("Score", adhdPill, "score 2/6", 18, GREY, TextAnchor.UpperLeft);
            Anchor(ctrl.adhdScoreText.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(20,15), new Vector2(-20,40));

            var demPill = MkRect("DemPill", p);
            Anchor(demPill, new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(30,200), new Vector2(470,300));
            var demImg = demPill.gameObject.AddComponent<Image>();
            demImg.color = new Color(0.05f,0.30f,0.15f);
            var demHdr = MkText("Hdr", demPill, "COGNITIVE DECLINE", 18, GREY, TextAnchor.UpperLeft, FontStyle.Bold);
            Anchor(demHdr.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-30), new Vector2(-20,-10));
            ctrl.demRiskText = MkText("Risk", demPill, "Low RISK", 28, new Color(0.55f,1f,0.65f), TextAnchor.UpperLeft, FontStyle.Bold);
            Anchor(ctrl.demRiskText.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-65), new Vector2(-20,-30));
            ctrl.demScoreText = MkText("Score", demPill, "score 0/8", 18, GREY, TextAnchor.UpperLeft);
            Anchor(ctrl.demScoreText.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(20,15), new Vector2(-20,40));

            ctrl.attemptDoneNextBtn = MkButton("NextBtn", p, "Continue →", BTN, Color.black, 30, true);
            Anchor(ctrl.attemptDoneNextBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-200,60), new Vector2(200,160));
            ctrl.attemptDoneNextLabel = ctrl.attemptDoneNextBtn.GetComponentInChildren<Text>();
        }

        // ============ SUMMARY ============
        static void BuildSummary(Transform parent, GNGController ctrl)
        {
            var p = MkRect("SummaryPanel", parent); FullStretch(p);
            ctrl.summaryPanel = p.gameObject;

            var title = MkText("Title", p, "TEST COMPLETE", 56, WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-110), new Vector2(0,-40));

            ctrl.summarySubText = MkText("Sub", p, "", 26, CYAN, TextAnchor.MiddleCenter);
            Anchor(ctrl.summarySubText.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-150), new Vector2(0,-110));

            var container = MkRect("Attempts", p);
            Anchor(container, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-700,-200), new Vector2(700,250));
            var v = container.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 16; v.padding = new RectOffset(30,30,20,20);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            ctrl.summaryAttemptsContainer = container;

            ctrl.shareBtn = MkButton("ShareBtn", p, "Share CSV", INPUT, WHITE, 28, false);
            Anchor(ctrl.shareBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(-440,60), new Vector2(-30,160));

            ctrl.newSubjectBtn = MkButton("NewBtn", p, "New Subject", BTN, Color.black, 28, true);
            Anchor(ctrl.newSubjectBtn.GetComponent<RectTransform>(), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(30,60), new Vector2(440,160));
        }

        // ============ HELPERS ============
        static RectTransform MkRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        static Text MkText(string name, Transform parent, string txt, int sz, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = txt; t.fontSize = sz; t.color = c; t.alignment = anchor;
            t.font = Fnt(); t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Text MkText(string name, RectTransform parent, string txt, int sz, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            return MkText(name, (Transform)parent, txt, sz, c, anchor, style);
        }

        static Button MkButton(string name, Transform parent, string label, Color bg, Color fg, int sz, bool primary)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = bg;
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
            var lbl = MkText("Label", go.transform, label, sz, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(lbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        static Button MkButton(string name, RectTransform parent, string label, Color bg, Color fg, int sz, bool primary)
        {
            return MkButton(name, (Transform)parent, label, bg, fg, sz, primary);
        }

        static InputField MkInput(string name, Transform parent, string placeholder, bool numeric)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = INPUT;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<Text>();
            ph.text = placeholder; ph.fontSize = 28; ph.color = DIM; ph.alignment = TextAnchor.MiddleLeft;
            ph.font = Fnt();
            Anchor(phGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));

            var txGo = new GameObject("Text", typeof(RectTransform));
            txGo.transform.SetParent(go.transform, false);
            var tx = txGo.AddComponent<Text>();
            tx.fontSize = 28; tx.color = WHITE; tx.alignment = TextAnchor.MiddleLeft;
            tx.font = Fnt(); tx.supportRichText = false;
            Anchor(txGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));

            var inp = go.GetComponent<InputField>();
            inp.targetGraphic = img;
            inp.textComponent = tx;
            inp.placeholder = ph;
            if (numeric) inp.contentType = InputField.ContentType.IntegerNumber;
            return inp;
        }

        static InputField MkInput(string name, RectTransform parent, string placeholder, bool numeric)
        {
            return MkInput(name, (Transform)parent, placeholder, numeric);
        }

        static Image MkCircle(string name, Transform parent, Color c, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.sprite = MakeRoundSprite(256);
            return img;
        }

        static Image MkCircle(string name, RectTransform parent, Color c, float size) { return MkCircle(name, (Transform)parent, c, size); }

        static RectTransform MkCard(string name, Transform parent)
        {
            var rt = MkRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = CARD;
            return rt;
        }

        static RectTransform MkCard(string name, RectTransform parent) { return MkCard(name, (Transform)parent); }

        static Sprite MakeRoundSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float r = size / 2f;
            float r2 = r * r;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d = dx * dx + dy * dy;
                    pixels[y * size + x] = d <= r2 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(pixels); tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
#endif
