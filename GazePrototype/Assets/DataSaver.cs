using UnityEngine;
using System;
using System.IO;

// -----------------------------------------------------------------------
// DataSaver.cs — TEMPORARY placeholder
// Saves session metrics as a JSON file to the device's persistent storage.
// No UI, no database, no table — just a file dump so data is not lost.
//
// FOR SQUIDLY INTEGRATION:
// Replace this entire file with SquidlyFirebaseBridge calls.
// The InsertFocusSession() signature stays the same — only the body changes.
// Save path (for testing): Application.persistentDataPath/sessions/
// -----------------------------------------------------------------------

public class DataSaver : MonoBehaviour
{
    public void InsertFocusSession(
        string sessionId,
        string date,
        float duration,
        float gazeOnTarget,
        float longestFixation,
        int focusBreaks,
        int offScreenCount,
        float timeToFirstFixation,
        string teacherNotes = "")
    {
        var data = new SessionData
        {
            sessionId          = sessionId,
            date               = date,
            duration           = duration,
            gazeOnTarget       = gazeOnTarget,
            longestFixation    = longestFixation,
            focusBreaks        = focusBreaks,
            offScreenCount     = offScreenCount,
            timeToFirstFixation = timeToFirstFixation,
            teacherNotes       = teacherNotes
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);

        string folder = Path.Combine(Application.persistentDataPath, "sessions");
        Directory.CreateDirectory(folder);

        string fileName = $"session_{sessionId}.json";
        string filePath = Path.Combine(folder, fileName);

        File.WriteAllText(filePath, json);
        Debug.Log($"[DataSaver] Session saved to: {filePath}");
    }

    // Simple serialisable struct matching the 6 Focus Game metrics
    [Serializable]
    private class SessionData
    {
        public string sessionId;
        public string date;
        public float  duration;           // Total session time (seconds)
        public float  gazeOnTarget;       // Time gaze/hover was on the monster (seconds)
        public float  longestFixation;    // Longest single continuous fixation (seconds)
        public int    focusBreaks;        // Number of times gaze left the monster
        public int    offScreenCount;     // Number of off-screen / face-lost events
        public float  timeToFirstFixation;// Time from game start to first fixation (seconds)
        public string teacherNotes;
    }
}
