using UnityEngine;

public class EyeTrackerCursor : MonoBehaviour
{
    [Header("Settings")]
    [Range(1f, 50f)] public float smoothSpeed = 15f;

    [Header("Face Lost Warning")]
    public GameObject faceLostWarning; // Assign a simple UI panel in the Inspector

    private Vector2 targetScreenPos;
    private GazeMapper gazeMapper;
    private bool isFaceLost = false;
    private float nextDriftLogTime = 0f;
    private const float DriftLogCooldown = 1.5f;

    void Start()
    {
        gazeMapper = FindObjectOfType<GazeMapper>();
        targetScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (faceLostWarning != null) faceLostWarning.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[EyeTrackerCursor] C pressed, returning cursor to center.");
            ResetPosition();
        }

        bool trackerConnected = UDPReceiver.IsReceiving;

        if (trackerConnected)
        {
            bool faceFound = UDPReceiver.FaceFound;

            if (!faceFound)
            {
                if (!isFaceLost)
                {
                    isFaceLost = true;
                    Debug.Log("[EyeTrackerCursor] Head not in scene. Cursor frozen until eyes are visible again.");
                    if (faceLostWarning != null) faceLostWarning.SetActive(true);
                }
            }
            else
            {
                // Face recovered
                if (isFaceLost)
                {
                    isFaceLost = false;
                    Debug.Log("[EyeTrackerCursor] Head detected again. Cursor control restored.");
                    if (faceLostWarning != null) faceLostWarning.SetActive(false);
                }

                // Update target from eye tracker
                if (gazeMapper != null)
                {
                    Vector2 raw = new Vector2(UDPReceiver.GazeX, UDPReceiver.GazeY);
                    targetScreenPos = gazeMapper.MapRawGazeToScreen(raw);
                }
                else
                {
                    targetScreenPos = new Vector2(
                        UDPReceiver.GazeX * Screen.width,
                        UDPReceiver.GazeY * Screen.height
                    );
                }

                bool wasOffScreen = (targetScreenPos.x < 0f || targetScreenPos.x > Screen.width ||
                                     targetScreenPos.y < 0f || targetScreenPos.y > Screen.height);
                if (wasOffScreen)
                {
                    targetScreenPos.x = Mathf.Clamp(targetScreenPos.x, 0f, Screen.width);
                    targetScreenPos.y = Mathf.Clamp(targetScreenPos.y, 0f, Screen.height);

                    if (Time.unscaledTime >= nextDriftLogTime)
                    {
                        Debug.LogWarning("[EyeTrackerCursor] Gaze drift detected off-screen. Clamping cursor target to screen bounds.");
                        nextDriftLogTime = Time.unscaledTime + DriftLogCooldown;
                    }
                }
            }
        }
        else
        {
            if (isFaceLost)
            {
                isFaceLost = false;
                if (faceLostWarning != null) faceLostWarning.SetActive(false);
            }
            targetScreenPos = Input.mousePosition;
        }

        if (!isFaceLost)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(targetScreenPos.x, targetScreenPos.y, 10f));
            worldPos.z = 0f;
            transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * smoothSpeed);
        }
    }

    public void ResetPosition()
    {
        targetScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 10f));
        worldPos.z = 0f;
        transform.position = worldPos;
    }
}
