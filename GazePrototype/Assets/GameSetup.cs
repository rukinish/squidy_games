using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameSetup : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject setupPopupPanel;
    public Button startGameButton;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown speedDropdown;
    public TMP_InputField threshold; // "Set Max Distractions" input

    [Header("Game References")]
    public ChestController playerController;
    public Collider2D goalChestCollider;
    public MonsterMove gazeSensor;

    [Header("Visuals")]
    public SpriteRenderer playerSpriteRenderer;

    private bool isCalibrated = false;

    void Start()
    {
        if (playerController != null) playerController.enabled = false;
        if (goalChestCollider != null) goalChestCollider.enabled = false;
        if (playerSpriteRenderer != null) playerSpriteRenderer.enabled = true;

        if (PlayerPrefs.GetInt("StartGameAfterCalibration", 0) == 1)
        {
            PlayerPrefs.SetInt("StartGameAfterCalibration", 0);
            PlayerPrefs.Save();
            isCalibrated = true;
            setupPopupPanel.SetActive(false);
            OnStartGameClicked();
            return;
        }

        setupPopupPanel.SetActive(true);
        if (startGameButton != null) startGameButton.interactable = isCalibrated;
    }

    public void OnSizeDropdownChanged()
    {
        if (playerSpriteRenderer != null && sizeDropdown != null)
        {
            float[] sizeMultipliers = { 0.7f, 1.0f, 1.5f };  // small / normal / large
            float scale = sizeMultipliers[sizeDropdown.value];
            playerSpriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void OnSpeedDropdownChanged()
    {
        if (gazeSensor != null && speedDropdown != null)
        {
            float[] speeds = { 1.2f, 2.2f, 3.5f };  // slow / normal / fast — kept low for therapy use
            gazeSensor.moveSpeed = speeds[speedDropdown.value];
        }
    }

    public void OnCalibrateClicked()
    {
        SaveMaxDistractions();
        PlayerPrefs.SetString("CalibrationReturnScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        Debug.Log("[GameSetupManager_Default] Saved return scene: " + SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("calibration");
    }

    private void SaveMaxDistractions()
    {
        int max = 3; // default if field is empty or invalid
        if (threshold != null && int.TryParse(threshold.text, out int parsed))
            max = Mathf.Max(0, parsed);
        PlayerPrefs.SetInt("MaxDistractions", max);
        PlayerPrefs.Save();
        Debug.Log("[GameSetupManager_Default] MaxDistractions saved: " + max);
    }

    public void OnHomeButtonClicked()
    {
        SceneManager.LoadScene("focus screen");
    }

    public void OnStartGameClicked()
    {
        if (!isCalibrated) return;

        SaveMaxDistractions();
        setupPopupPanel.SetActive(false);
        if (playerController != null) playerController.enabled = true;
        if (goalChestCollider != null) goalChestCollider.enabled = true;

        SessionMetrics.ResetMetrics();
        if (gazeSensor != null) gazeSensor.StartGameWithWarmUp();
    }
}
