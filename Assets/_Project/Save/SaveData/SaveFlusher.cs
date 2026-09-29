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
                _save.FlushPendingSave();
        }

        private void OnApplicationQuit() => _save?.FlushPendingSave();

        private void OnDestroy()
        {
            _save?.FlushPendingSave();
            if (_instance == this) _instance = null;
        }
    }
}
