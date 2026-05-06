/*
 * SquidlyFirebaseBridge.jslib
 * ---------------------------
 * Saves session metrics to Firebase via Squidly's built-in API.
 *
 * HOW IT WORKS:
 *   DataSaver.cs calls SaveSessionToFirebase() with a JSON string
 *   containing all 6 session metrics. This JS function passes that
 *   JSON to SquidlyAPI.firebaseSet, which writes it under:
 *     /sessions/<sessionId>
 *
 * SQUIDLY DOCS:
 *   SquidlyAPI.firebaseSet(path, value)
 *     path  — Firebase database path string
 *     value — JavaScript object to store
 */

mergeInto(LibraryManager.library, {

    /*
     * SaveSessionToFirebase(sessionJsonPtr)
     * Called by DataSaver.cs after the game ends.
     * sessionJsonPtr is a C# string pointer — use UTF8ToString to decode it.
     */
    SaveSessionToFirebase: function (sessionJsonPtr) {

        // Guard: do nothing outside of Squidly
        if (typeof SquidlyAPI === "undefined" || !SquidlyAPI.firebaseSet) {
            console.warn("[SquidlyFirebaseBridge] SquidlyAPI not found — Firebase save skipped.");
            return;
        }

        var json = UTF8ToString(sessionJsonPtr);

        var data;
        try {
            data = JSON.parse(json);
        } catch (e) {
            console.error("[SquidlyFirebaseBridge] Failed to parse session JSON:", e);
            return;
        }

        // Write to Firebase under /sessions/<sessionId>
        var path = "sessions/" + data.sessionId;
        SquidlyAPI.firebaseSet(path, data);

        console.log("[SquidlyFirebaseBridge] Session saved to Firebase at:", path);
    }

});
