using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    public enum GameType { Adventure, Floating, Quiz }

    [Header("UI Elements")]
    public GameObject endPopupPanel;
    public TMP_Text summaryText;
    public Button retryButton;
    public Button homeButton;
    public Button dashboardButton;
    public TMP_Text saveStatusText;

    private bool isSaveComplete = false;

    void Start()
    {
        if (endPopupPanel != null) endPopupPanel.SetActive(false);
    }

    public void ShowEndScreen(GameType gameType)
    {
        Debug.Log($"[SessionEndManager] ShowEndScreen called for {gameType}.");
        Debug.Log($"[SessionEndManager] endPopupPanel assigned: {endPopupPanel != null}, summaryText assigned: {summaryText != null}, saveStatusText assigned: {saveStatusText != null}");

        Time.timeScale = 0f;  // freeze everything while summary is visible
        var cursor = FindObjectOfType<EyeTrackerCursor>();
        if (cursor != null) cursor.gameObject.SetActive(false);
        var gazeCursor = FindObjectOfType<GazeCursor>();
        if (gazeCursor != null) gazeCursor.gameObject.SetActive(false);

        isSaveComplete = false;
        SetPopupButtonsInteractable(false);
        if (summaryText != null)
        {
            summaryText.text = BuildSummary(gameType);
        }
        else
        {
            Debug.LogError("[SessionEndManager] summaryText is not assigned. Popup summary cannot be rendered.");
        }
        UpdateSaveStatusLabel("Saving session...");
        if (endPopupPanel != null)
        {
            endPopupPanel.SetActive(true);
            EnsurePopupVisible();
            Debug.Log($"[SessionEndManager] endPopupPanel activeSelf={endPopupPanel.activeSelf}, activeInHierarchy={endPopupPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[SessionEndManager] endPopupPanel is not assigned. Popup cannot be shown.");
        }
    }

    public void ShowImmediateEndScreen(GameType gameType, string statusMessage)
    {
        ShowEndScreen(gameType);
        isSaveComplete = true;
        SetPopupButtonsInteractable(true);
        UpdateSaveStatusLabel(statusMessage);
        Debug.Log($"[SessionEndManager] Immediate end screen shown for {gameType}. Status: {statusMessage}");
    }

    private string BuildSummary(GameType gameType)
    {
        switch (gameType)
        {
            case GameType.Adventure:
            case GameType.Floating:
                float gazePercent = SessionMetrics.adventureTotalDuration > 0
                    ? (SessionMetrics.adventureGazeOnTarget / SessionMetrics.adventureTotalDuration) * 100f
                    : 0f;
                return
                    $"Total Time:           {SessionMetrics.adventureTotalDuration:F1}s\n" +
                    $"Gaze on Target:       {SessionMetrics.adventureGazeOnTarget:F1}s  ({gazePercent:F0}%)\n" +
                    $"Longest Fixation:     {SessionMetrics.adventureLongestFixation:F1}s\n" +
                    $"Focus Breaks:         {SessionMetrics.adventureFocusBreaks}\n" +
                    $"Off-Screen Count:     {SessionMetrics.adventureOffScreenCount}\n" +
                    $"Time to First Look:   {SessionMetrics.adventureTimeToFirstFixation:F1}s";

            case GameType.Quiz:
                float avgDwell = SessionMetrics.quizCorrectSelections > 0
                    ? SessionMetrics.quizTotalResponseTime / SessionMetrics.quizCorrectSelections
                    : 0f;
                return
                    $"Total Time:           {SessionMetrics.quizTotalActivityTime:F1}s\n" +
                    $"Total Attempts:       {SessionMetrics.quizTotalAttempts}\n" +
                    $"Correct Selections:   {SessionMetrics.quizCorrectSelections}\n" +
                    $"False Activations:    {SessionMetrics.quizFalseActivations}\n" +
                    $"Avg Response Time:    {avgDwell:F1}s\n" +
                    $"Time to First Look:   {SessionMetrics.quizTimeToFirstFixation:F1}s";

            default:
                return "No metrics available.";
        }
    }

    public void OnRetryClicked()
    {
        if (!isSaveComplete) return;
        SessionMetrics.ResetMetrics();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeClicked()
    {
        if (!isSaveComplete) return;
        SessionMetrics.ResetMetrics();
        Time.timeScale = 1f;
        SceneManager.LoadScene("activity screen");
    }

    public void OnDashboardClicked()
    {
        if (!isSaveComplete) return;
        SessionMetrics.ResetMetrics();
        Time.timeScale = 1f;
        SceneManager.LoadScene("sessions ui");
    }

    public void NotifySessionSaveComplete()
    {
        Debug.Log("[SessionEndManager] NotifySessionSaveComplete called.");
        isSaveComplete = true;
        SetPopupButtonsInteractable(true);
        UpdateSaveStatusLabel("Session saved");
    }

    private void SetPopupButtonsInteractable(bool interactable)
    {
        if (retryButton != null) retryButton.interactable = interactable;
        if (homeButton != null) homeButton.interactable = interactable;
        if (dashboardButton != null) dashboardButton.interactable = interactable;
    }

    private void UpdateSaveStatusLabel(string message)
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = message;
            saveStatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }

    private void EnsurePopupVisible()
    {
        if (endPopupPanel == null) return;

        Transform current = endPopupPanel.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }

        endPopupPanel.transform.SetAsLastSibling();

        RectTransform rect = endPopupPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 s = rect.localScale;
            if (Mathf.Approximately(s.x, 0f) || Mathf.Approximately(s.y, 0f) || Mathf.Approximately(s.z, 0f))
            {
                rect.localScale = Vector3.one;
            }
        }

        RectTransform[] allRects = endPopupPanel.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform childRect in allRects)
        {
            if (childRect == null) continue;
            if (!childRect.gameObject.activeSelf)
                childRect.gameObject.SetActive(true);

            Vector3 childScale = childRect.localScale;
            if (Mathf.Approximately(childScale.x, 0f) || Mathf.Approximately(childScale.y, 0f) || Mathf.Approximately(childScale.z, 0f))
            {
                childRect.localScale = Vector3.one;
            }
        }

        Graphic[] graphics = endPopupPanel.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null) continue;
            graphic.enabled = true;
            Color color = graphic.color;
            if (color.a < 1f)
            {
                color.a = 1f;
                graphic.color = color;
            }

            if (graphic is Image image)
            {
                image.material = null;
            }
        }

        TMP_Text[] texts = endPopupPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;
            text.enabled = true;
            Color color = text.color;
            if (color.a < 1f)
            {
                color.a = 1f;
                text.color = color;
            }
        }

        CanvasGroup canvasGroup = endPopupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Canvas panelCanvas = endPopupPanel.GetComponent<Canvas>();
        if (panelCanvas != null)
        {
            panelCanvas.enabled = true;
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 1000;
        }

        Canvas parentCanvas = endPopupPanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.enabled = true;
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 1000;
        }
    }
}
