using UnityEngine;
using System;

// -----------------------------------------------------------------------
// ChestController.cs
// Attached to the monster character.
// Detects when the monster reaches the treasure chest (win condition),
// saves the session metrics, and triggers the game over screen.
// -----------------------------------------------------------------------

public class ChestController : MonoBehaviour
{
    private bool hasReachedGoal = false;

    // Fires when the monster's collider overlaps with the chest's collider
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Chest") || hasReachedGoal) return;

        hasReachedGoal = true;

        // Stop the monster moving and freeze metrics
        var monsterMove = FindObjectOfType<MonsterMove>();
        if (monsterMove != null) monsterMove.EndGame();

        // Play chest open animation
        Animator chestAnim = other.GetComponent<Animator>();
        if (chestAnim != null) chestAnim.SetTrigger("OpenTrigger");

        SaveSessionAndShowEndScreen();
    }

    private void SaveSessionAndShowEndScreen()
    {
        string sessionId = "sess_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        // Save the 6 session metrics to the database
        // FOR SQUIDLY: DataSaver (SQLite) will be replaced with SquidlyFirebaseBridge
        DataSaver dataSaver = FindObjectOfType<DataSaver>();
        if (dataSaver == null)
            dataSaver = new GameObject("DataSaver").AddComponent<DataSaver>();

        dataSaver.InsertFocusSession(
            sessionId,
            dateStr,
            SessionMetrics.adventureTotalDuration,
            SessionMetrics.adventureGazeOnTarget,
            SessionMetrics.adventureLongestFixation,
            SessionMetrics.adventureFocusBreaks,
            SessionMetrics.adventureOffScreenCount,
            SessionMetrics.adventureTimeToFirstFixation
        );

        // Show the end screen
        var endScreen = FindObjectOfType<GameOverScreen>();
        if (endScreen != null) endScreen.ShowEndScreen(GameOverScreen.GameType.Adventure);
    }
}
