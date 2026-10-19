using UnityEngine;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// Writes a pending SaveService.SaveSoon once SaveSoonSeconds have passed since the last save, and on quit
    /// (heavy-kill pass, 2026-09-29). Created on the first pending save; lives for the whole game.
    /// </summary>
    public sealed class SaveFlusher : MonoBehaviour
    {
        private static SaveFlusher _instance;
        private SaveService _save;

        public static void Ensure(SaveService save)
        {
            if (!Application.isPlaying) { save.FlushPendingSave(); return; }
            if (_instance == null)
            {
                var go = new GameObject("SaveFlusher") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SaveFlusher>();
            }
            _instance._save = save;
        }

        private void Update()
        {
            if (_save != null && _save.HasPendingSave && Time.unscaledTime - _save.LastSaveTime >= SaveService.SaveSoonSeconds)
                _save.FlushPendingSaveInBackground(); // the disk write runs off the main thread
        }

        private void OnApplicationQuit() => _save?.FlushPendingSave();

        // Bug MIS ITM-05: a phone sends the app to the background and may kill it without OnApplicationQuit, which lost the
        // pending gold (up to SaveSoonSeconds) and Bestiary kills. Going to the background writes a pending save at once
        // (the app is stopping anyway); losing focus queues it on the worker, so an alt-tab on PC does not hitch.
        private void OnApplicationPause(bool paused)
        {
            if (paused) _save?.FlushPendingSave();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) _save?.FlushPendingSaveInBackground();
        }

        private void OnDestroy()
        {
            _save?.FlushPendingSave();
            if (_instance == this) _instance = null;
        }
    }
}
