using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Learning Comment:
    /// Single owner of Time.timeScale. Pause menu, level-up, chest, merchant and game over
    /// each register their own pause, and gameplay only resumes when all of them have
    /// released it. Before this, each screen wrote Time.timeScale directly, so closing or
    /// toggling one (or Soul Burst's slow motion ending, or the next stage starting)
    /// resumed the game underneath another screen that was still open.
    /// </summary>
    public static class GameTime
    {
        private static readonly List<object> _pausers = new List<object>();
        private static float _slowMotion = 1f;

        public static bool IsPaused
        {
            get { PruneDestroyedOwners(); return _pausers.Count > 0; }
        }

        public static float SlowMotion => _slowMotion;

        /// <summary>Freezes gameplay until <paramref name="owner"/> calls Resume. Repeat calls are ignored.</summary>
        public static void Pause(object owner)
        {
            if (owner != null && !_pausers.Contains(owner)) _pausers.Add(owner);
            Apply();
        }

        /// <summary>Releases <paramref name="owner"/>'s pause; gameplay runs once no pause remains.</summary>
        public static void Resume(object owner)
        {
            _pausers.Remove(owner);
            Apply();
        }

        /// <summary>Hit-stop / slow motion scale (1 = normal). A pause still wins over it.</summary>
        public static void SetSlowMotion(float scale)
        {
            _slowMotion = Mathf.Max(0f, scale);
            Apply();
        }

        /// <summary>Clears every pause and slow motion. Only for run start, restart and scene changes.</summary>
        public static void ResetAll()
        {
            _pausers.Clear();
            _slowMotion = 1f;
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnPlay()
        {
            // Domain reload may be off, so a pause from the last play session must not survive.
            _pausers.Clear();
            _slowMotion = 1f;
        }

        private static void Apply()
        {
            PruneDestroyedOwners();
            Time.timeScale = _pausers.Count > 0 ? 0f : _slowMotion;
        }

        // A screen destroyed without releasing its pause (scene unload) must not freeze the game forever.
        private static void PruneDestroyedOwners()
        {
            _pausers.RemoveAll(owner => owner is Object unityObject && unityObject == null);
        }
    }
}
