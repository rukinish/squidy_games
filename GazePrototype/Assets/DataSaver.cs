using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

// -----------------------------------------------------------------------
// DataSaver.cs
// Saves the 6 Focus Game session metrics after the game ends.
//
// IN WEBGL (Squidly):
//   Calls SaveSessionToFirebase() from SquidlyFirebaseBridge.jslib,
//   which writes the session data to Firebase via SquidlyAPI.firebaseSet.
//
// IN THE UNITY EDITOR / non-WebGL:
//   Falls back to writing a JSON file at:
//   Application.persistentDataPath/sessions/session_<id>.json
//   This is temporary — only for local testing. No UI reads this file.
//
// CALLED BY:
//   ChestController.cs → InsertFocusSession() when the monster reaches
//   the chest and the game ends.
// -----------------------------------------------------------------------

public class DataSaver : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Import the Firebase save function from SquidlyFirebaseBridge.jslib
    [DllImport("__Internal")]
    private static extern void SaveSessionToFirebase(string sessionJson);
#endif

    /*
     * InsertFocusSession()
     * The single entry point for saving a completed session.
     * Called by ChestController.cs with all 6 metrics once the game ends.
     *
     * Parameters map to the 6 tracked metrics:
     *   sessionId           — unique ID for this session (timestamp-based)
     *   date                — human-readable date string
     *   duration            — total session time in seconds
     *   gazeOnTarget        — total time gaze/hover was on the monster (seconds)
     *   longestFixation     — longest single unbroken gaze streak (seconds)
     *   focusBreaks         — how many times gaze left the monster
     *   offScreenCount      — how many times the child looked away from screen
     *   timeToFirstFixation — seconds from game start to first gaze on monster
     */
    public void InsertFocusSession(
        string sessionId,
        string date,
        float  duration,
        float  gazeOnTarget,
        float  longestFixation,
        int    focusBreaks,
        int    offScreenCount,
        float  timeToFirstFixation,
        string teacherNotes = "")
    {
        var data = new SessionData
        {
            sessionId           = sessionId,
            date                = date,
            duration            = duration,
            gazeOnTarget        = gazeOnTarget,
            longestFixation     = longestFixation,
            focusBreaks         = focusBreaks,
            offScreenCount      = offScreenCount,
            timeToFirstFixation = timeToFirstFixation,
            teacherNotes        = teacherNotes
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Running on Squidly — save to Firebase
        SaveSessionToFirebase(json);
        Debug.Log($"[DataSaver] Session sent to Firebase: {sessionId}");
#else
        // Running in Unity Editor — save to a local JSON file for testing
        string folder = Path.Combine(Application.persistentDataPath, "sessions");
        Directory.CreateDirectory(folder);
        string filePath = Path.Combine(folder, $"session_{sessionId}.json");
        File.WriteAllText(filePath, json);
        Debug.Log($"[DataSaver] Session saved locally (Editor only): {filePath}");
#endif
    }

    // Matches the 6 Focus Game metrics — serialised to JSON
    [Serializable]
    private class SessionData
    {
        public string sessionId;
        public string date;
        public float  duration;            // Total session time (seconds)
        public float  gazeOnTarget;        // Time gaze was on the monster (seconds)
        public float  longestFixation;     // Longest single fixation streak (seconds)
        public int    focusBreaks;         // Times gaze left the monster
        public int    offScreenCount;      // Times child looked away from screen
        public float  timeToFirstFixation; // Seconds until first gaze on monster
        public string teacherNotes;
    }
}
