using System.Collections.Generic;
using UnityEngine;

namespace GNG
{
    public static class GNGStats
    {
        public static AttemptResult Compute(List<TrialRecord> log, int attemptNum)
        {
            var r = new AttemptResult();
            r.attemptNum = attemptNum;
            r.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            r.log = log;

            var rts = new List<int>();
            foreach (var t in log)
            {
                if (t.type == "go") r.goT++;
                else r.nogoT++;
                if (t.outcome == "hit") { r.hits++; rts.Add(t.rtMs); }
                else if (t.outcome == "omission") r.oms++;
                else if (t.outcome == "commission") r.cms++;
                else if (t.outcome == "correct") r.crj++;
            }

            r.hitR = r.goT > 0 ? Mathf.Round((float)r.hits / r.goT * 1000f) / 10f : 0f;
            r.ceR = r.nogoT > 0 ? Mathf.Round((float)r.cms / r.nogoT * 1000f) / 10f : 0f;
            r.oeR = r.goT > 0 ? Mathf.Round((float)r.oms / r.goT * 1000f) / 10f : 0f;

            if (rts.Count > 0)
            {
                rts.Sort();
                long sum = 0;
                for (int i = 0; i < rts.Count; i++) sum += rts[i];
                float mean = (float)sum / rts.Count;
                r.mRT = Mathf.RoundToInt(mean);
                r.medRT = rts[rts.Count / 2];
                r.minRT = rts[0];
                r.maxRT = rts[rts.Count - 1];

                if (rts.Count > 1)
                {
                    double sq = 0;
                    foreach (var v in rts) sq += (v - mean) * (v - mean);
                    r.sdRT = Mathf.RoundToInt((float)System.Math.Sqrt(sq / (rts.Count - 1)));
                }
            }

            int half = log.Count / 2;
            int fhSum = 0, fhN = 0, shSum = 0, shN = 0;
            foreach (var t in log)
            {
                if (t.outcome != "hit") continue;
                if (t.trialNum < half) { fhSum += t.rtMs; fhN++; }
                else { shSum += t.rtMs; shN++; }
            }
            r.fhRT = fhN > 0 ? Mathf.RoundToInt((float)fhSum / fhN) : r.mRT;
            r.shRT = shN > 0 ? Mathf.RoundToInt((float)shSum / shN) : r.mRT;
            r.rtDec = r.shRT - r.fhRT;

            CalcRisk(r);
            return r;
        }

        public static void CalcRisk(AttemptResult r)
        {
            int adhd = 0;
            if (r.ceR > 35f) adhd += 2; else if (r.ceR > 20f) adhd += 1;
            if (r.sdRT > 150) adhd += 2; else if (r.sdRT > 100) adhd += 1;
            if (r.oeR > 20f) adhd += 2; else if (r.oeR > 10f) adhd += 1;
            r.adhdScore = adhd;
            r.adhdRisk = adhd >= 4 ? "High" : adhd >= 2 ? "Moderate" : "Low";

            int dem = 0;
            if (r.mRT > 650) dem += 3; else if (r.mRT > 500) dem += 2; else if (r.mRT > 380) dem += 1;
            if (r.oeR > 30f) dem += 3; else if (r.oeR > 20f) dem += 2; else if (r.oeR > 10f) dem += 1;
            if (r.rtDec > 100) dem += 2; else if (r.rtDec > 50) dem += 1;
            r.demScore = dem;
            r.demRisk = dem >= 5 ? "High" : dem >= 3 ? "Moderate" : "Low";
        }

        public static List<TrialType> GenerateTrials(int n, float goProb)
        {
            int k = Mathf.RoundToInt(n * goProb);
            var list = new List<TrialType>(n);
            for (int i = 0; i < k; i++) list.Add(TrialType.Go);
            for (int i = k; i < n; i++) list.Add(TrialType.NoGo);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
            return list;
        }
    }
}
