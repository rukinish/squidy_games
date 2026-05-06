using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAutoFitter : MonoBehaviour
{
    [Header("Fit Settings")]
    public float maxDimension = 3.0f;

    [Header("API Settings")]
    [Tooltip("Paste your remove.bg API key here")]
    public string removeBgApiKey = "PASTE_YOUR_API_KEY_HERE";

    private SpriteRenderer spriteRenderer;
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg" };

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        FitCurrentSprite();
    }

    public void LoadExternalImageAndFit(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogWarning("[SpriteAutoFitter] Invalid upload: empty file path.");
            return;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        bool isAllowed = false;
        foreach (string ext in AllowedExtensions)
        {
            if (extension == ext)
            {
                isAllowed = true;
                break;
            }
        }

        if (!isAllowed)
        {
            Debug.LogError($"[SpriteAutoFitter] Invalid upload: unsupported file type '{extension}'. Allowed: .png, .jpg, .jpeg");
            return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[SpriteAutoFitter] Invalid upload: file not found at '{filePath}'.");
            return;
        }

        StartCoroutine(RemoveBackgroundRoutine(filePath));
    }

    private IEnumerator RemoveBackgroundRoutine(string filePath)
    {
        byte[] rawImageData;
        try
        {
            rawImageData = File.ReadAllBytes(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SpriteAutoFitter] Invalid upload: failed to read file. {e.Message}");
            yield break;
        }

        if (rawImageData == null || rawImageData.Length == 0)
        {
            Debug.LogError("[SpriteAutoFitter] Invalid upload: file is empty or unreadable.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("image_file", rawImageData, "image.png", "image/png");
        form.AddField("size", "auto");

        using (UnityWebRequest www = UnityWebRequest.Post("https://api.remove.bg/v1.0/removebg", form))
        {
            www.SetRequestHeader("X-Api-Key", removeBgApiKey);

            Debug.Log("[BackgroundRemover] Stripping background...");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[BackgroundRemover] Success! Applying transparent image.");

                byte[] transparentImageData = www.downloadHandler.data;
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(transparentImageData);

                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                spriteRenderer.sprite = newSprite;
                FitCurrentSprite();
                spriteRenderer.enabled = true; // Reveal only after the clean image is ready
            }
            else
            {
                Debug.LogError("[BackgroundRemover] API Failed: " + www.error);
                if (!LoadRawImage(rawImageData))
                {
                    Debug.LogError("[SpriteAutoFitter] Invalid upload: fallback decode failed. Sprite was not updated.");
                }
            }
        }
    }

    private bool LoadRawImage(byte[] fileData)
    {
        Texture2D tex = new Texture2D(2, 2);
        bool loaded = tex.LoadImage(fileData);
        if (!loaded)
        {
            Debug.LogError("[SpriteAutoFitter] Invalid upload: could not decode image bytes.");
            return false;
        }

        if (tex.width <= 2 && tex.height <= 2)
        {
            Debug.LogWarning($"[SpriteAutoFitter] Suspicious upload: decoded tiny image ({tex.width}x{tex.height}).");
        }

        Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        spriteRenderer.sprite = newSprite;
        FitCurrentSprite();
        spriteRenderer.enabled = true; // Reveal even in fallback mode
        Debug.Log($"[SpriteAutoFitter] Raw image fallback applied successfully ({tex.width}x{tex.height}).");
        return true;
    }

    public void FitCurrentSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        transform.localScale = Vector3.one;
        float rawWidth = spriteRenderer.bounds.size.x;
        float rawHeight = spriteRenderer.bounds.size.y;
        float longestSide = Mathf.Max(rawWidth, rawHeight);
        float scaleFactor = maxDimension / longestSide;

        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }
}
