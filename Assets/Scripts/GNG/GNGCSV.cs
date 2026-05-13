using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GNG
{
    public static class GNGCSV
    {
        public static string SummaryCSV(List<SessionRecord> sessions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SAP_ID,Age,Test_Date,Attempt,Total_Trials,Go_Trials,NoGo_Trials,Hits,Omissions,Commissions,Correct_Rejections,Hit_Rate_Pct,Commission_Error_Pct,Omission_Error_Pct,Mean_RT_ms,Median_RT_ms,RT_SD_ms,RT_Min_ms,RT_Max_ms,RT_First_Half_ms,RT_Second_Half_ms,RT_Change_ms,ADHD_Score,ADHD_Risk,Dementia_Score,Dementia_Risk");
            foreach (var s in sessions)
            {
                string d = System.DateTimeOffset.FromUnixTimeMilliseconds(s.timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                foreach (var a in s.attempts)
                {
                    sb.Append(s.sapId).Append(',');
                    sb.Append(s.age > 0 ? s.age.ToString() : "").Append(',');
                    sb.Append(d).Append(',');
                    sb.Append(a.attemptNum).Append(',');
                    sb.Append(a.goT + a.nogoT).Append(',');
                    sb.Append(a.goT).Append(',');
                    sb.Append(a.nogoT).Append(',');
                    sb.Append(a.hits).Append(',');
                    sb.Append(a.oms).Append(',');
                    sb.Append(a.cms).Append(',');
                    sb.Append(a.crj).Append(',');
                    sb.Append(a.hitR).Append(',');
                    sb.Append(a.ceR).Append(',');
                    sb.Append(a.oeR).Append(',');
                    sb.Append(a.mRT).Append(',');
                    sb.Append(a.medRT).Append(',');
                    sb.Append(a.sdRT).Append(',');
                    sb.Append(a.minRT).Append(',');
                    sb.Append(a.maxRT).Append(',');
                    sb.Append(a.fhRT).Append(',');
                    sb.Append(a.shRT).Append(',');
                    sb.Append(a.rtDec).Append(',');
                    sb.Append(a.adhdScore).Append(',');
                    sb.Append(a.adhdRisk).Append(',');
                    sb.Append(a.demScore).Append(',');
                    sb.AppendLine(a.demRisk);
                }
            }
            return sb.ToString();
        }

        public static string TrialCSV(List<SessionRecord> sessions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SAP_ID,Test_Date,Attempt,Trial_Num,Trial_Type,Responded,RT_ms,Outcome");
            foreach (var s in sessions)
            {
                string d = System.DateTimeOffset.FromUnixTimeMilliseconds(s.timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                foreach (var a in s.attempts)
                {
                    foreach (var t in a.log)
                    {
                        sb.Append(s.sapId).Append(',');
                        sb.Append(d).Append(',');
                        sb.Append(a.attemptNum).Append(',');
                        sb.Append(t.trialNum + 1).Append(',');
                        sb.Append(t.type).Append(',');
                        sb.Append(t.responded ? "1" : "0").Append(',');
                        sb.Append(t.responded ? t.rtMs.ToString() : "").Append(',');
                        sb.AppendLine(t.outcome);
                    }
                }
            }
            return sb.ToString();
        }

        public static string WriteFile(string filename, string content)
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

            string path = Path.Combine(dir, "GoNoGoResults.csv");

            File.WriteAllText(path, content);
            return path;
        }

        public static void ShareFiles(string subject, string body, params string[] paths)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = unityActivity.GetStatic<AndroidJavaObject>("currentActivity");
                    string packageName = currentActivity.Call<string>("getPackageName");
                    string authority = packageName + ".fileprovider";

                    using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                    using (var providerClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
                    {
                        AndroidJavaObject intent;
                        if (paths.Length == 1)
                        {
                            intent = new AndroidJavaObject("android.content.Intent");
                            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                            using (var fileObj = new AndroidJavaObject("java.io.File", paths[0]))
                            {
                                var uri = providerClass.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, fileObj);
                                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                            }
                            intent.Call<AndroidJavaObject>("setType", "text/csv");
                        }
                        else
                        {
                            intent = new AndroidJavaObject("android.content.Intent");
                            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND_MULTIPLE"));
                            using (var arrayList = new AndroidJavaObject("java.util.ArrayList"))
                            {
                                foreach (var p in paths)
                                {
                                    using (var fileObj = new AndroidJavaObject("java.io.File", p))
                                    {
                                        var uri = providerClass.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, fileObj);
                                        arrayList.Call<bool>("add", uri);
                                    }
                                }
                                intent.Call<AndroidJavaObject>("putParcelableArrayListExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), arrayList);
                            }
                            intent.Call<AndroidJavaObject>("setType", "text/csv");
                        }

                        intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
                        intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body);
                        intent.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));

                        var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share results");
                        currentActivity.Call("startActivity", chooser);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GNG] Share failed: " + e.Message);
            }
#else
            Debug.Log("[GNG] Share (editor stub). Files: " + string.Join(", ", paths));
#endif
        }
    }
}
