using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MonsterMove : MonoBehaviour
{
    [Header("Game Settings")]
    public Transform targetGoal;
    public float moveSpeed = 2f;

    [Header("Audio Distraction")]
    public AudioSource distractionAudio; // Assign the AudioSource with focus_game.mp3
    public AudioClip distractionClip;    // Drag focus_game.mp3 here

    [Header("Warm-Up")]
    [Tooltip("Seconds of grace time after Start button before metrics begin recording.")]
    public float warmUpDuration = 3f;

    // Internal State
    private bool isGazing = false;
    private bool gameStarted = false;   // nothing tracked until StartGameWithWarmUp() fires
    private Animator anim;
    private bool wasFaceFound = true;   // tracks face presence to detect off-screen transitions
    private bool suppressNextBreak = false; // prevents false break when face recovers after being lost on-target

    // Tracking Variables
    private float currentFixationTime = 0f;   // Current continuous fixation streak
    private bool ttffRecorded = false;         // TTFF: only record the very first fixation

    void Start()
    {
        anim = GetComponent<Animator>();

        if (targetGoal == null)
            Debug.LogWarning("GazeSensor: Target Goal is missing!");

        Debug.Log("GazeSensor initialized. Waiting for game start.");
    }

    public void StartGameWithWarmUp()
    {
        if (distractionAudio == null || distractionClip == null)
            Debug.LogWarning("GazeSensor: Distraction AudioSource or clip not assigned — distractions will be skipped.");

        StartCoroutine(WarmUpCoroutine());
    }

    private IEnumerator WarmUpCoroutine()
    {
        Debug.Log($"[GazeSensor] Warm-up started — metrics begin in {warmUpDuration}s.");
        yield return new WaitForSeconds(warmUpDuration);
        gameStarted = true;
        Debug.Log("[GazeSensor] Warm-up complete — metrics now recording.");

        int maxDistractions = PlayerPrefs.GetInt("MaxDistractions", 3);
        StartCoroutine(DistractionRoutine(maxDistractions));
    }

    void Update()
    {
        if (!gameStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[GazeSensor] Esc pressed, game over end panel popped up.");
            ShowManualGameOver();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[GazeSensor] R pressed, reopening calibration. Current focus session will restart.");
            RestartSessionThroughCalibration();
            return;
        }

        // Track session duration
        SessionMetrics.adventureTotalDuration += Time.deltaTime;

        bool trackerConnected = UDPReceiver.IsReceiving;
        bool isFaceFound = !trackerConnected || UDPReceiver.FaceFound;
        if (wasFaceFound && !isFaceFound)
        {
            Debug.Log("[GazeSensor] Head not detected. Pausing gaze-driven interaction and counting off-screen.");
            SessionMetrics.adventureOffScreenCount++;
            if (isGazing) suppressNextBreak = true;
        }
        else if (!wasFaceFound && isFaceFound)
        {
            Debug.Log("[GazeSensor] Head detected again. Resuming gaze interaction.");
        }
        wasFaceFound = isFaceFound;

        if (isGazing && targetGoal != null)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetGoal.position, step);
            if (anim != null) anim.SetBool("isWalking", true);

            SessionMetrics.adventureGazeOnTarget += Time.deltaTime;
            currentFixationTime += Time.deltaTime;
            if (currentFixationTime > SessionMetrics.adventureLongestFixation)
                SessionMetrics.adventureLongestFixation = currentFixationTime;
        }
        else
        {
            if (anim != null) anim.SetBool("isWalking", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameStarted) return;

        if (other.CompareTag("Cursor"))
        {
            isGazing = true;
            suppressNextBreak = false; // cursor is back on target — any future exit is a real break
            Debug.Log("Cursor entered target area.");

            if (!ttffRecorded)
            {
                SessionMetrics.adventureTimeToFirstFixation = SessionMetrics.adventureTotalDuration;
                ttffRecorded = true;
                Debug.Log($"[TTFF] Adventure first fixation at {SessionMetrics.adventureTimeToFirstFixation:F2}s");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!gameStarted) return;

        if (other.CompareTag("Cursor"))
        {
            isGazing = false;
            Debug.Log($"Cursor exited target area. Fixation streak: {currentFixationTime:F2}s");

            if (suppressNextBreak)
            {
                suppressNextBreak = false;
                Debug.Log("[GazeSensor] Break suppressed — caused by face loss recovery, not real distraction.");
            }
            else
            {
                SessionMetrics.adventureFocusBreaks++;
                Debug.Log($"[GazeSensor] Focus break recorded. Total: {SessionMetrics.adventureFocusBreaks}");
            }

            // Reset attention streak on exit
            currentFixationTime = 0f;
        }
    }

    public void EndGame()
    {
        gameStarted = false;
        isGazing = false;
        StopAllCoroutines();
        if (anim != null) anim.SetBool("isWalking", false);
        Debug.Log("[GazeSensor] EndGame called — metrics frozen, coroutines stopped.");
    }

    private void ShowManualGameOver()
    {
        EndGame();
        var endManager = FindObjectOfType<GameOverScreen>();
        if (endManager != null)
        {
            endManager.ShowImmediateEndScreen(GetCurrentFocusGameType(), "Session ended manually");
        }
        else
        {
            Debug.LogError("[GazeSensor] SessionEndManager not found. Cannot open game over panel.");
        }
    }

    private void RestartSessionThroughCalibration()
    {
        EndGame();
        SessionMetrics.ResetMetrics();
        PlayerPrefs.SetString("CalibrationReturnScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        Debug.Log("[GazeSensor] Metrics reset. Loading calibration scene.");
        SceneManager.LoadScene("calibration");
    }

    private GameOverScreen.GameType GetCurrentFocusGameType()
    {
        return SceneManager.GetActiveScene().name == "Focus_Personalized"
            ? GameOverScreen.GameType.Floating
            : GameOverScreen.GameType.Adventure;
    }

    private IEnumerator DistractionRoutine(int maxDistractions)
    {
        Debug.Log($"[GazeSensor] DistractionRoutine started. Max distractions: {maxDistractions}");

        for (int i = 0; i < maxDistractions; i++)
        {
            float waitTime = Random.Range(5f, 10f);
            yield return new WaitForSeconds(waitTime);

            if (distractionAudio != null && distractionClip != null)
            {
                distractionAudio.Stop();
                distractionAudio.clip = distractionClip;
                distractionAudio.Play();
                Debug.Log($"[GazeSensor] Distraction {i + 1}/{maxDistractions} played after {waitTime:F1}s wait.");
            }
            else
            {
                Debug.LogWarning($"[GazeSensor] Distraction {i + 1} skipped — AudioSource or clip not assigned.");
            }
        }

        Debug.Log("[GazeSensor] DistractionRoutine complete.");
    }
}
