using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

// -----------------------------------------------------------------------
// GameSetup.cs
// Shows the pre-game settings panel before the session starts.
// Therapist configures character size, speed, and max distractions,
// then clicks Play Game to begin.
//
// ATTACH TO: _SetupManager GameObject in the scene
// -----------------------------------------------------------------------

public class GameSetup : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject setupPopupPanel;  // The settings popup panel
    public Button startGameButton;      // Play Game button
    public TMP_Dropdown sizeDropdown;   // Small / Normal / Large
    public TMP_Dropdown speedDropdown;  // Slow / Normal / Fast
    public TMP_InputField threshold;    // Max Distractions input field

    [Header("Game References")]
    public ChestController playerController; // Drag Character_0 here
    public Collider2D goalChestCollider;     // Drag Goal_Chest collider here
    public MonsterMove gazeSensor;           // Drag Character_0 here

    [Header("Visuals")]
    public SpriteRenderer playerSpriteRenderer; // Drag Character_0 SpriteRenderer here

#if UNITY_WEBGL && !UNITY_EDITOR
    // Calls CloseGame() in SquidlyFirebaseBridge.jslib to exit back to Squidly
    [DllImport("__Internal")]
    private static extern void CloseGame();
#endif

    void Start()
    {
        // Disable game objects until Play Game is pressed
        if (playerController != null) playerController.enabled = false;
        if (goalChestCollider != null) goalChestCollider.enabled = false;
        if (playerSpriteRenderer != null) playerSpriteRenderer.enabled = true;

        // Show settings panel — Play Game button is always interactable now
        setupPopupPanel.SetActive(true);
        if (startGameButton != null) startGameButton.interactable = true;
    }

    // Called by the Size dropdown OnValueChanged event in the Inspector
    public void OnSizeDropdownChanged()
    {
        if (playerSpriteRenderer == null || sizeDropdown == null) return;
        float[] sizeMultipliers = { 0.7f, 1.0f, 1.5f }; // Small / Normal / Large
        float scale = sizeMultipliers[sizeDropdown.value];
        playerSpriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    // Called by the Speed dropdown OnValueChanged event in the Inspector
    public void OnSpeedDropdownChanged()
    {
        if (gazeSensor == null || speedDropdown == null) return;
        float[] speeds = { 1.2f, 2.2f, 3.5f }; // Slow / Normal / Fast
        gazeSensor.moveSpeed = speeds[speedDropdown.value];
    }

    // Called by the Play Game button OnClick event in the Inspector
    public void OnStartGameClicked()
    {
        SaveMaxDistractions();
        setupPopupPanel.SetActive(false);

        if (playerController != null) playerController.enabled = true;
        if (goalChestCollider != null) goalChestCollider.enabled = true;

        SessionMetrics.ResetMetrics();
        if (gazeSensor != null) gazeSensor.StartGameWithWarmUp();
    }

    // Called by the Home button OnClick event in the Inspector
    public void OnHomeButtonClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Tell Squidly to close the game and return to the platform
        CloseGame();
#else
        Debug.Log("[GameSetup] Home button pressed — CloseGame() only runs in WebGL.");
#endif
    }

    // Reads the Max Distractions input and saves to PlayerPrefs
    private void SaveMaxDistractions()
    {
        int max = 3; // default if field is empty or invalid
        if (threshold != null && int.TryParse(threshold.text, out int parsed))
            max = Mathf.Max(0, parsed);
        PlayerPrefs.SetInt("MaxDistractions", max);
        PlayerPrefs.Save();
    }
}
