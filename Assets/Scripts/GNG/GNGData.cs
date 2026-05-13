using System;
using System.Collections.Generic;

namespace GNG
{
    public static class GNGConfig
    {
        public const int PRACTICE_TRIALS = 10;
        public const int MAIN_TRIALS = 80;
        public const float GO_PROBABILITY = 0.8f;
        public const float FIXATION_MS = 600f;
        public const float STIMULUS_MS = 1000f;
        public const float FEEDBACK_MS = 650f;
        public const int ATTEMPTS = 3;
    }

    public enum TrialType { Go, NoGo }
    public enum Outcome { Hit, Omission, Commission, CorrectRejection }

    [Serializable]
    public class TrialRecord
    {
        public int trialNum;
        public string type;
        public bool responded;
        public int rtMs;
        public string outcome;
    }

    [Serializable]
    public class AttemptResult
    {
        public int attemptNum;
        public long timestamp;
        public int goT, nogoT;
        public int hits, oms, cms, crj;
        public float hitR, ceR, oeR;
        public int mRT, sdRT, medRT, minRT, maxRT;
        public int fhRT, shRT, rtDec;
        public int adhdScore;
        public string adhdRisk;
        public int demScore;
        public string demRisk;
        public List<TrialRecord> log = new List<TrialRecord>();
    }

    [Serializable]
    public class SessionRecord
    {
        public string sapId;
        public int age;
        public long timestamp;
        public List<AttemptResult> attempts = new List<AttemptResult>();
    }

    [Serializable]
    public class SessionStore
    {
        public List<SessionRecord> sessions = new List<SessionRecord>();
    }
}
