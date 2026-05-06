using UnityEngine;
using System.Collections.Generic;

public class SessionMetrics : MonoBehaviour
{
    public static int quizTotalAttempts = 0;
    public static int quizCorrectSelections = 0;
    public static int quizFalseActivations = 0;

    // Latency
    public static float quizTotalResponseTime = 0f;
    public static float quizTotalActivityTime = 0f; // NEW: Total time spent in the quiz
    public static float quizTimeToFirstFixation = 0f;
    public static List<float> questionTimes = new List<float>(); // Stores time for each question

    // Stability
    public static float quizAvgDwellDuration = 0f;

    public static float adventureTotalDuration = 0f;
    public static float adventureGazeOnTarget = 0f;
    public static float adventureLongestFixation = 0f;
    public static int adventureFocusBreaks = 0;
    public static int adventureOffScreenCount = 0;
    public static float adventureTimeToFirstFixation = 0f; // TTFF: seconds from target spawn to first gaze entry

    public const int MAX_GAZE_POINTS = 1000;
    public static List<Vector2> gazePoints = new List<Vector2>();

    public static void AddGazePoint(Vector2 point)
    {
        if (gazePoints.Count >= MAX_GAZE_POINTS)
            gazePoints.RemoveAt(0);
        gazePoints.Add(point);
    }

    public static void ResetMetrics()
    {
        Debug.Log("Resetting session metrics...");

        // Quiz Metrics
        quizTotalAttempts = 0;
        quizCorrectSelections = 0;
        quizTotalResponseTime = 0f;
        quizFalseActivations = 0;
        quizTimeToFirstFixation = 0f;
        quizAvgDwellDuration = 0f;

        questionTimes.Clear();

        // Adventure Metrics
        adventureTotalDuration = 0f;
        adventureGazeOnTarget = 0f;
        adventureLongestFixation = 0f;
        adventureFocusBreaks = 0;
        adventureOffScreenCount = 0;
        adventureTimeToFirstFixation = 0f;

        // Common Data
        gazePoints.Clear();

        Debug.Log("Session metrics cleared.");
    }
}
