using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// CPU audit (2026-10-14): ticks every gem on the ground once a frame (XPGem.Tick), in place of up to 400 separate
    /// Update calls. Created on first use, one per loaded game.
    /// </summary>
    public sealed class GemRunner : MonoBehaviour
    {
        private static GemRunner _instance;

        /// <summary>True once the runner exists (tests).</summary>
        public static bool Exists => _instance != null;

        internal static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("GemRunner") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GemRunner>();
        }

        private void Update()
        {
            var gems = XPGem.ActiveGems;
            // Backwards: a gem collected during its tick is swap-removed, which only moves an already-ticked gem.
            for (int i = gems.Count - 1; i >= 0; i--)
            {
                if (i >= gems.Count) continue;
                var gem = gems[i];
                if (gem == null) // destroyed without OnDisable (never expected)
                {
                    int last = gems.Count - 1;
                    gems[i] = gems[last];
                    if (gems[i] != null) gems[i].ActiveIndex = i;
                    gems.RemoveAt(last);
                    continue;
                }
                try { gem.Tick(); }
                catch (System.Exception e) { Debug.LogException(e, gem); } // one gem's error never stops the rest
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so gems from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            XPGem.ActiveGems.Clear();
            _instance = null;
        }
    }
}
