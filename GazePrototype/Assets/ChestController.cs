using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class ChestController : MonoBehaviour
{
    public Transform targetGoal;
    public float speed = 2.0f;
    private bool hasReachedGoal = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Chest"))
        {
            if (!hasReachedGoal)
            {
                hasReachedGoal = true;

                var gazeSensor = FindObjectOfType<MonsterMove>();
                if (gazeSensor != null) gazeSensor.EndGame();

                Debug.Log("[GazePlayerController] Chest reached! Requesting XAI Heatmap...");
                string currentSessionId = "sess_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                string saveDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../sessions/xai"));
                if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
                string targetFilePath = Path.Combine(saveDir, $"xai_Focus_{currentSessionId}_{timestamp}.png");

                StartCoroutine(CaptureAndTriggerXAI(currentSessionId, targetFilePath));
            }

            Animator chestAnim = other.GetComponent<Animator>();
            if (chestAnim != null)
            {
                chestAnim.SetTrigger("OpenTrigger");
            }
        }
    }

    private IEnumerator WaitForXAIAndSaveToDB(string activityType, string sessionId, string heatmapPath)
    {
        bool fileReady = false;
        float waitTime = 0f;
        float maxWaitTime = 35.0f; // XAI does 196 ONNX passes on CPU — needs up to ~30s

        while (waitTime < maxWaitTime)
        {
            if (File.Exists(heatmapPath))
            {
                yield return new WaitForSecondsRealtime(0.2f);
                fileReady = true;
                Debug.Log($"[GazePlayerController] XAI Heatmap ready after {waitTime:F1}s");
                break;
            }

            yield return new WaitForSecondsRealtime(1.0f);
            waitTime += 1.0f;
            Debug.Log($"[GazePlayerController] Waiting for XAI Heatmap... ({waitTime:F0}s / {maxWaitTime:F0}s)");
        }

        if (!fileReady)
        {
            Debug.LogWarning("[GazePlayerController] XAI not saved after 35s — face may not have been detected during session, or Python server was off. Saving DB without heatmap.");
        }

        DataSaver dbManager = FindObjectOfType<DataSaver>();
        if (dbManager == null)
        {
            Debug.LogWarning("[GazePlayerController] DatabaseManager not found — creating one.");
            dbManager = new GameObject("DataSaver").AddComponent<DataSaver>();
        }

        if (dbManager != null)
        {
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            dbManager.InsertFocusSession(
                sessionId,
                dateStr,
                SessionMetrics.adventureTotalDuration,
                SessionMetrics.adventureGazeOnTarget,
                SessionMetrics.adventureLongestFixation,
                SessionMetrics.adventureFocusBreaks,
                SessionMetrics.adventureOffScreenCount,
                SessionMetrics.adventureTimeToFirstFixation,
                heatmapPath,  // absolute path to FYP_GAZE/sessions/xai/
                "Good focus today!"
            );
        }

        Debug.Log("[GazePlayerController] Focus Session saved to DB gracefully.");

        var endManager = FindObjectOfType<GameOverScreen>();
        if (endManager != null) endManager.NotifySessionSaveComplete();
    }

    private IEnumerator CaptureAndTriggerXAI(string sessionId, string xaiPath)
    {
        // Capture game screen before end popup appears
        yield return new WaitForEndOfFrame();
        string bgPath = xaiPath.Replace(".png", "_bg.png");
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(bgPath, shot.EncodeToPNG());
        Destroy(shot);

        var endManager = FindObjectOfType<GameOverScreen>();
        if (endManager != null) endManager.ShowEndScreen(GameOverScreen.GameType.Adventure);

        UDPReceiver.SendXAICommand("Focus", sessionId, xaiPath, bgPath);
        StartCoroutine(WaitForXAIAndSaveToDB("Focus", sessionId, xaiPath));
    }
}
