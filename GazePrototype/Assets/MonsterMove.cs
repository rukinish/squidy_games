using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

// -----------------------------------------------------------------------
// MonsterMove.cs
// Controls the monster character that moves toward the treasure chest.
//
// HOW IT WORKS:
//   The monster moves ONLY while the player is hovering over it.
//   Currently driven by mouse hover (OnMouseEnter / OnMouseExit).
//
// FOR SQUIDLY INTEGRATION:
//   Replace OnMouseEnter / OnMouseExit with Squidly's eye gaze cursor.
//   Squidly provides real-time gaze coordinates via SquidlyAPI.addCursorListener.
//   Wire those coordinates into a cursor GameObject and call
//   OnGazeEnter() / OnGazeExit() below instead of the mouse events,
//   OR move the cursor GameObject over the monster collider to keep
//   the same trigger-based flow.
// -----------------------------------------------------------------------

public class MonsterMove : MonoBehaviour
{
    [Header("Game Settings")]
    public Transform targetGoal;       // Drag the TreasureChest GameObject here
    public float moveSpeed = 2f;

    [Header("Audio Distraction")]
    public AudioSource distractionAudio; // AudioSource component on this GameObject
    public AudioClip distractionClip;    // The distraction sound clip

    [Header("Warm-Up")]
    [Tooltip("Seconds before metrics start recording after the game begins.")]
    public float warmUpDuration = 3f;

    // Internal state
    private bool isHovered = false;      // true while cursor/gaze is on the monster
    private bool gameStarted = false;    // false until StartGameWithWarmUp() fires
    private Animator anim;
    private bool suppressNextBreak = false; // suppresses a false focus-break on re-entry

    // Metric tracking
    private float currentFixationTime = 0f; // running streak of continuous hover time
    private bool ttffRecorded = false;      // ensures Time-To-First-Fixation is only set once

    void Start()
    {
        anim = GetComponent<Animator>();

        if (targetGoal == null)
            Debug.LogWarning("[MonsterMove] Target Goal (chest) is not assigned.");
    }

    // Called by GameSetup.cs once the therapist hits Start
    public void StartGameWithWarmUp()
    {
        if (distractionAudio == null || distractionClip == null)
            Debug.LogWarning("[MonsterMove] Distraction audio not assigned — distractions will be skipped.");

        StartCoroutine(WarmUpCoroutine());
    }

    private IEnumerator WarmUpCoroutine()
    {
        // Grace period — monster is visible but metrics are not yet recorded
        Debug.Log($"[MonsterMove] Warm-up started — metrics begin in {warmUpDuration}s.");
        yield return new WaitForSeconds(warmUpDuration);
        gameStarted = true;
        Debug.Log("[MonsterMove] Warm-up complete — metrics now recording.");

        int maxDistractions = PlayerPrefs.GetInt("MaxDistractions", 3);
        StartCoroutine(DistractionRoutine(maxDistractions));
    }

    void Update()
    {
        if (!gameStarted) return;

        // Escape key — manual early exit (useful for therapist to end session)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowManualGameOver();
            return;
        }

        // Always tick total session duration
        SessionMetrics.adventureTotalDuration += Time.deltaTime;

        // -------------------------------------------------------------------
        // MOVEMENT + METRIC TRACKING
        // Monster moves toward the chest only while the cursor/gaze is on it.
        //
        // FOR SQUIDLY: isHovered is set by OnMouseEnter/OnMouseExit below.
        // Replace those with gaze enter/exit events from SquidlyGazeInput.cs.
        // -------------------------------------------------------------------
        if (isHovered && targetGoal != null)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetGoal.position, step);
            if (anim != null) anim.SetBool("isWalking", true);

            // Metric: total time player looked at (hovered over) the monster
            SessionMetrics.adventureGazeOnTarget += Time.deltaTime;

            // Metric: longest single continuous fixation/hover streak
            currentFixationTime += Time.deltaTime;
            if (currentFixationTime > SessionMetrics.adventureLongestFixation)
                SessionMetrics.adventureLongestFixation = currentFixationTime;
        }
        else
        {
            if (anim != null) anim.SetBool("isWalking", false);
        }
    }

    // -----------------------------------------------------------------------
    // HOVER EVENTS — currently mouse-based.
    // FOR SQUIDLY: replace with gaze cursor enter/exit from SquidlyGazeInput.cs
    // -----------------------------------------------------------------------

    // Fires when the mouse cursor enters the monster's collider
    private void OnMouseEnter()
    {
        if (!gameStarted) return;
        OnGazeEnter();
    }

    // Fires when the mouse cursor leaves the monster's collider
    private void OnMouseExit()
    {
        if (!gameStarted) return;
        OnGazeExit();
    }

    // Called on gaze/hover enter — shared logic for both mouse and Squidly gaze
    public void OnGazeEnter()
    {
        isHovered = true;
        suppressNextBreak = false;
        Debug.Log("[MonsterMove] Gaze entered monster.");

        // Metric: record the first time the player ever looks at the monster
        if (!ttffRecorded)
        {
            SessionMetrics.adventureTimeToFirstFixation = SessionMetrics.adventureTotalDuration;
            ttffRecorded = true;
            Debug.Log($"[MonsterMove] Time to first fixation: {SessionMetrics.adventureTimeToFirstFixation:F2}s");
        }
    }

    // Called on gaze/hover exit — shared logic for both mouse and Squidly gaze
    public void OnGazeExit()
    {
        isHovered = false;
        Debug.Log($"[MonsterMove] Gaze exited. Fixation streak: {currentFixationTime:F2}s");

        if (suppressNextBreak)
        {
            suppressNextBreak = false;
        }
        else
        {
            // Metric: count how many times the player looked away
            SessionMetrics.adventureFocusBreaks++;
            Debug.Log($"[MonsterMove] Focus break recorded. Total: {SessionMetrics.adventureFocusBreaks}");
        }

        // Reset the streak timer on every exit
        currentFixationTime = 0f;
    }

    // Called by ChestController when the monster reaches the chest
    public void EndGame()
    {
        gameStarted = false;
        isHovered = false;
        StopAllCoroutines();
        if (anim != null) anim.SetBool("isWalking", false);
        Debug.Log("[MonsterMove] EndGame — metrics frozen.");
    }

    private void ShowManualGameOver()
    {
        EndGame();
        var endManager = FindObjectOfType<GameOverScreen>();
        if (endManager != null)
            endManager.ShowImmediateEndScreen(GameOverScreen.GameType.Adventure, "Session ended manually");
        else
            Debug.LogError("[MonsterMove] GameOverScreen not found.");
    }

    // Plays a distraction sound at random intervals to test the child's focus
    private IEnumerator DistractionRoutine(int maxDistractions)
    {
        for (int i = 0; i < maxDistractions; i++)
        {
            float waitTime = Random.Range(5f, 10f);
            yield return new WaitForSeconds(waitTime);

            if (distractionAudio != null && distractionClip != null)
            {
                distractionAudio.Stop();
                distractionAudio.clip = distractionClip;
                distractionAudio.Play();
                Debug.Log($"[MonsterMove] Distraction {i + 1}/{maxDistractions} played.");
            }
        }
    }
}
