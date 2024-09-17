using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static List<string> logEntries = new List<string>();

        private static void LogInfo(string s)
        {
            logger.LogInfo(s);
            logEntries.Add($"[Info] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogMessage(string s)
        {
            logger.LogMessage(s);
            logEntries.Add($"[Message] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogWarning(string s)
        {
            logger.LogWarning(s);
            logEntries.Add($"[Warning] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogError(string s)
        {
            logger.LogError(s);
            logEntries.Add($"[Error] [{DateTime.Now.ToString()}] {s}");
        }

        private void CaptureLog(string logString, string stackTrace, LogType type)
        {
            string entry = $"[{Time.time}] [{type}] {logString}";
            if (type == LogType.Exception)
            {
                entry += $"\n{stackTrace}";
            }
            logEntries.Add(entry);

            // Optionally, you can set a max number of entries to keep in memory
            if (logEntries.Count > 10000)  // For example, keep last 10000 entries
            {
                logEntries.RemoveAt(0);
            }
        }

        private void RestartSendLogTimer()
        {
            instance.SetTimer(30, () =>
            {
                instance.SendLogToBrain();
                instance.RestartSendLogTimer();
            });
        }
    }
}
