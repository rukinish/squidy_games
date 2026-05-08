using UnityEngine;
using System.Collections;

// -----------------------------------------------------------------------
// MonsterMove.cs
// Controls the monster character that moves toward the treasure chest.
//
// HOW IT WORKS:
//   The monster moves ONLY while the player is hovering over it.
//   Currently driven by mouse hover (OnMouseEnter / OnMouseExit).
//
// SLOPE FOLLOWING:
//   A downward raycast runs every frame to find the ground surface.
//   The monster snaps its Y to the surface and tilts to match the slope.
//   This means waypoints only need correct X positions — Y is automatic.
//
// FOR SQUIDLY: OnMouseEnter/OnMouseExit will be replaced by gaze events
//   from SquidlyGazeInput.cs. Call OnGazeEnter() / OnGazeExit() instead.
// -----------------------------------------------------------------------

public class MonsterMove : MonoBehaviour
{
    [Header("Game Settings")]
    public Transform targetGoal;   // Drag the TreasureChest GameObject here
    public float moveSpeed = 2f;

    [Header("Waypoints (optional)")]
    [Tooltip("For sloped levels — place empty GameObjects along the X path and drag them here in order. Y position doesn't matter, ground raycast handles it.")]
    public Transform[] waypoints;

    [Header("Ground Following")]
    [Tooltip("LayerMask for the ground/tilemap. Set this to the layer your Tilemap Collider is on.")]
    public LayerMask groundLayer;

    [Tooltip("How far down to raycast to find the ground surface.")]
    public float groundCheckDistance = 2f;

    [Tooltip("Height offset above the ground surface the monster rides on.")]
    public float groundOffset = 0.5f;

    [Tooltip("Smoothing speed for tilting on slopes. Higher = snappier.")]
    public float tiltSmoothSpeed = 10f;

    [Header("Audio Distraction")]
    public AudioSource distractionAudio;
    public AudioClip distractionClip;

    [Header("Warm-Up")]
    [Tooltip("Seconds before metrics start recording after the game begins.")]
    public float warmUpDuration = 3f;

    [Tooltip("Tick this for levels with no setup popup — game starts immediately on scene load.")]
    public bool autoStart = false;

    // Internal state
    private bool isHovered = false;
    private bool gameStarted = false;
    private Animator anim;
    private SpriteRenderer sr;
    private bool suppressNextBreak = false;
    private int currentWaypoint = 0;

    // ORIGINAL EYE TRACKER: tracked whether the child's face was visible to the eye tracker
    // private bool wasFaceFound = true;

    // Metric tracking
    private float currentFixationTime = 0f;
    private bool ttffRecorded = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();

        if (targetGoal == null)
            Debug.LogWarning("[MonsterMove] Target Goal (chest) is not assigned.");
        else
            FlipToFace(GetCurrentTarget());

        if (autoStart)
            StartGameWithWarmUp();
    }

    public void StartGameWithWarmUp()
    {
        if (distractionAudio == null || distractionClip == null)
            Debug.LogWarning("[MonsterMove] Distraction audio not assigned — distractions will be skipped.");
        StartCoroutine(WarmUpCoroutine());
    }

    private IEnumerator WarmUpCoroutine()
    {
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowManualGameOver();
            return;
        }

        SessionMetrics.adventureTotalDuration += Time.deltaTime;

        // -------------------------------------------------------------------
        // ORIGINAL EYE TRACKER — face detection logic (commented out)
        // FOR SQUIDLY: re-enable if SquidlyAPI exposes face/presence detection.
        // -------------------------------------------------------------------
        // bool trackerConnected = UDPReceiver.IsReceiving;
        // bool isFaceFound = !trackerConnected || UDPReceiver.FaceFound;
        // if (wasFaceFound && !isFaceFound)
        // {
        //     SessionMetrics.adventureOffScreenCount++;
        //     if (isHovered) suppressNextBreak = true;
        // }
        // else if (!wasFaceFound && isFaceFound) { }
        // wasFaceFound = isFaceFound;

        if (isHovered && targetGoal != null)
        {
            // ---------------------------------------------------------------
            // MOVEMENT — move toward current waypoint / chest along X axis,
            // then snap Y to ground surface via raycast (handles slopes).
            // ---------------------------------------------------------------
            Transform target = GetCurrentTarget();
            float step = moveSpeed * Time.deltaTime;

            // Move only in X toward the target — raycast handles Y
            Vector3 targetPos = new Vector3(target.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            // Snap Y to ground and tilt sprite to match slope angle
            FollowGround();

            // Flip sprite to face direction of travel
            FlipToFace(target);

            // Advance waypoint when X is close enough
            if (waypoints != null && currentWaypoint < waypoints.Length &&
                Mathf.Abs(transform.position.x - target.position.x) < 0.15f)
            {
                currentWaypoint++;
                FlipToFace(GetCurrentTarget());
            }

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

    // Casts a ray downward, snaps Y to ground, tilts sprite to match slope normal
    private void FollowGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.up * 0.5f, // cast from slightly above to avoid self-hit
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (hit.collider != null)
        {
            // Snap Y to ground surface + offset
            Vector3 pos = transform.position;
            pos.y = hit.point.y + groundOffset;
            transform.position = pos;

            // Tilt sprite to match slope — angle between world Up and surface normal
            float slopeAngle = Vector2.SignedAngle(Vector2.up, hit.normal);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, -slopeAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * tiltSmoothSpeed);
        }
        else
        {
            // No ground found — reset tilt so monster doesn't stay tilted in the air
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * tiltSmoothSpeed);
        }
    }

    // -----------------------------------------------------------------------
    // HOVER EVENTS — currently mouse-based.
    // FOR SQUIDLY: call OnGazeEnter() / OnGazeExit() from SquidlyGazeInput.cs
    // -----------------------------------------------------------------------

    private void OnMouseEnter()
    {
        if (!gameStarted) return;
        OnGazeEnter();
    }

    private void OnMouseExit()
    {
        if (!gameStarted) return;
        OnGazeExit();
    }

    public void OnGazeEnter()
    {
        isHovered = true;
        suppressNextBreak = false;
        Debug.Log("[MonsterMove] Gaze entered monster.");
        if (!ttffRecorded)
        {
            SessionMetrics.adventureTimeToFirstFixation = SessionMetrics.adventureTotalDuration;
            ttffRecorded = true;
            Debug.Log($"[MonsterMove] Time to first fixation: {SessionMetrics.adventureTimeToFirstFixation:F2}s");
        }
    }

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
            SessionMetrics.adventureFocusBreaks++;
            Debug.Log($"[MonsterMove] Focus break recorded. Total: {SessionMetrics.adventureFocusBreaks}");
        }
        currentFixationTime = 0f;
    }

    private Transform GetCurrentTarget()
    {
        if (waypoints != null && currentWaypoint < waypoints.Length)
            return waypoints[currentWaypoint];
        return targetGoal;
    }

    // Flips sprite horizontally based on direction of travel — no body rotation
    private void FlipToFace(Transform target)
    {
        if (target == null || sr == null) return;
        float dirX = target.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.01f)
            sr.flipX = dirX < 0;
    }

    public void EndGame()
    {
        gameStarted = false;
        isHovered = false;
        StopAllCoroutines();
        if (anim != null) anim.SetBool("isWalking", false);
        // Reset tilt when game ends
        transform.rotation = Quaternion.identity;
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
