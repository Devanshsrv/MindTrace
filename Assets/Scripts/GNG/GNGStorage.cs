using System.IO;
using UnityEngine;

namespace GNG
{
    public static class GNGStorage
    {
        const string FILE = "gng_sessions.json";

        public static SessionStore Load()
        {
            string path = Path.Combine(Application.persistentDataPath, FILE);
            if (!File.Exists(path)) return new SessionStore();
            try
            {
                string json = File.ReadAllText(path);
                var s = JsonUtility.FromJson<SessionStore>(json);
                if (s == null || s.sessions == null) return new SessionStore();
                return s;
            }
            catch
            {
                return new SessionStore();
            }
        }

        public static void Save(SessionStore store)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, FILE);
                File.WriteAllText(path, JsonUtility.ToJson(store));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GNG] Save failed: " + e.Message);
            }
        }
    }
}
