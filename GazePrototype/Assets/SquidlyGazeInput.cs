using UnityEngine;
using System.Runtime.InteropServices;

// -----------------------------------------------------------------------
// SquidlyGazeInput.cs
// Receives live gaze coordinates from Squidly and translates them into
// hover events on the monster, driving the game mechanic.
//
// ATTACH THIS SCRIPT to a new empty GameObject named "SquidlyGazeInput"
// in the Env1_LVL_1 scene. The GameObject name must match exactly because
// SquidlyGazeBridge.jslib uses SendMessage("SquidlyGazeInput", ...) to
// call back into Unity.
//
// HOW IT WORKS:
//   1. On Start(), calls InitGazeBridge() which registers the Squidly
//      cursor listener in JavaScript (SquidlyGazeBridge.jslib).
//   2. Every time Squidly fires a gaze update, ReceiveGaze() is called
//      with a "x,y" pixel coordinate string.
//   3. We raycast from that screen position into the scene.
//   4. If the ray hits the monster, we call MonsterMove.OnGazeEnter().
//      If it stops hitting, we call MonsterMove.OnGazeExit().
//
// IN THE UNITY EDITOR (non-WebGL):
//   InitGazeBridge() does nothing — mouse hover drives the game instead
//   via OnMouseEnter/OnMouseExit in MonsterMove.cs. No action needed.
// -----------------------------------------------------------------------

public class SquidlyGazeInput : MonoBehaviour
{
    // Reference to the monster — assign in the Inspector
    [Tooltip("Drag the monster GameObject here.")]
    public MonsterMove monsterMove;

    // Tracks whether gaze is currently on the monster
    private bool isGazingAtMonster = false;

    // Import the JS function from SquidlyGazeBridge.jslib
    // This call is ignored silently in the Editor (non-WebGL builds)
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitGazeBridge();
#endif

    void Start()
    {
        if (monsterMove == null)
            Debug.LogWarning("[SquidlyGazeInput] MonsterMove reference not set — drag the monster into the Inspector.");

#if UNITY_WEBGL && !UNITY_EDITOR
        // Register the Squidly cursor listener in JavaScript
        InitGazeBridge();
        Debug.Log("[SquidlyGazeInput] Gaze bridge initialised.");
#else
        Debug.Log("[SquidlyGazeInput] Running outside WebGL — mouse hover active instead (MonsterMove.OnMouseEnter/Exit).");
#endif
    }

    /*
     * ReceiveGaze(string payload)
     * Called by SquidlyGazeBridge.jslib via SendMessage every gaze frame.
     * payload format: "pixelX,pixelY"  e.g. "640.00,360.00"
     */
    public void ReceiveGaze(string payload)
    {
        if (monsterMove == null) return;

        // Parse the "x,y" payload
        var parts = payload.Split(',');
        if (parts.Length != 2) return;

        if (!float.TryParse(parts[0], out float screenX) ||
            !float.TryParse(parts[1], out float screenY)) return;

        // Raycast from the gaze screen position into the 2D scene
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenX, screenY, 0));
        Collider2D hit = Physics2D.OverlapPoint(worldPoint);

        bool gazeOnMonster = hit != null && hit.gameObject == monsterMove.gameObject;

        // Fire enter/exit only on state change (not every frame)
        if (gazeOnMonster && !isGazingAtMonster)
        {
            isGazingAtMonster = true;
            monsterMove.OnGazeEnter();
        }
        else if (!gazeOnMonster && isGazingAtMonster)
        {
            isGazingAtMonster = false;
            monsterMove.OnGazeExit();
        }
    }
}
