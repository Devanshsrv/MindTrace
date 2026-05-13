using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;

public class CSVLogger : MonoBehaviour
{
    private string filePath;

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    filePath = Path.Combine("/storage/emulated/0/Download", "CorsiResults.csv");
#else
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        filePath = Path.Combine(desktop, "CorsiResults.csv");
#endif

        if (!File.Exists(filePath))
        {
            string header = "Date,Name,Age,Attempt,Span,TotalCorrect,Product,Latency,Hesitation,Accuracy,Status\n";
            File.WriteAllText(filePath, header);
        }
    }

    public void SaveData(string name, int span, int totalCorrect, int product,
        float latency, float hesitation, float accuracy, string status)
    {
        // Normalize name (case-insensitive)
        name = name.ToLower().Trim();
        name = char.ToUpper(name[0]) + name.Substring(1);

        // Get date
        string date = DateTime.Now.ToString("dd-MM-yy");

        // Create row
        string row = date + "," + name + "," + span + "," + totalCorrect + "," +
                     product + "," + latency.ToString("F2") + "," +
                     hesitation.ToString("F2") + "," +
                     accuracy.ToString("F1") + "," + status + "\n";

        // Append to file
        bool saved = false;
        int attempts = 0;

        while (!saved && attempts < 3)
        {
            try
            {
                File.AppendAllText(filePath, row);
                saved = true;
            }
            catch (IOException e)
            {
                attempts++;
                Debug.LogWarning("Retry " + attempts + ": " + e.Message);
                System.Threading.Thread.Sleep(500);
            }
        }

        if (saved)
        {
            Debug.Log("Saved to: " + filePath);
        }
        else
        {
            Debug.LogError("Failed to save. File may be open.");
        }

    }

    public void SaveFinalData(string name, int age, List<string> attempts)
    {
        name = name.ToLower().Trim();
        name = char.ToUpper(name[0]) + name.Substring(1);

        string date = DateTime.Now.ToString("dd-MM-yy");

        for (int i = 0; i < attempts.Count; i++)
        {
            string row = date + "," + name + "," + age + "," + (i + 1) + "," + attempts[i] + "\n";
            File.AppendAllText(filePath, row);
        }
    }
}