/*
 * SquidlyGazeBridge.jslib
 * -----------------------
 * Bridges Squidly's eye-gaze cursor into Unity.
 *
 * HOW IT WORKS:
 *   1. On game start, SquidlyGazeInput.cs calls InitGazeBridge() from C#.
 *   2. This JS registers a listener on SquidlyAPI.addCursorListener.
 *   3. Every frame Squidly fires a gaze update with (x, y) in 0–1 normalised
 *      screen coordinates (0,0 = top-left, 1,1 = bottom-right).
 *   4. We convert those to Unity pixel coordinates and fire SendMessage
 *      back into the Unity scene so SquidlyGazeInput.cs can act on them.
 *
 * UNITY SIDE:
 *   The GameObject named "SquidlyGazeInput" must exist in the scene.
 *   SquidlyGazeInput.cs handles the incoming "ReceiveGaze" message.
 */

mergeInto(LibraryManager.library, {

    /*
     * InitGazeBridge()
     * Called once by SquidlyGazeInput.cs on Start().
     * Registers the Squidly cursor listener and starts forwarding gaze
     * coordinates to Unity every time Squidly fires an update.
     */
    InitGazeBridge: function () {

        // Guard: do nothing if SquidlyAPI is not present (e.g. running in
        // the Unity Editor or a plain browser outside Squidly).
        if (typeof SquidlyAPI === "undefined" || !SquidlyAPI.addCursorListener) {
            console.warn("[SquidlyGazeBridge] SquidlyAPI not found — gaze bridge inactive.");
            return;
        }

        SquidlyAPI.addCursorListener(function (x, y) {
            /*
             * x, y are normalised (0–1). Convert to Unity pixel coordinates
             * so SquidlyGazeInput.cs can use Screen.width / Screen.height
             * to map them onto world space.
             *
             * We pack both values into a single "x,y" string because
             * Unity's SendMessage only accepts one string argument.
             */
            var pixelX = x * screen.width;
            var pixelY = (1 - y) * screen.height; // flip Y: Squidly 0=top, Unity 0=bottom

            var payload = pixelX.toFixed(2) + "," + pixelY.toFixed(2);

            // Send the gaze coordinate to the Unity GameObject "SquidlyGazeInput"
            SendMessage("SquidlyGazeInput", "ReceiveGaze", payload);
        });

        console.log("[SquidlyGazeBridge] Gaze listener registered.");
    }

});
