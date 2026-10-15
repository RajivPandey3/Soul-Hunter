using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// CPU audit (2026-10-14): ticks only the bodies that are flashing now (a hit flash lasts 0.07 s), in place of an
    /// Update on every DamageFlash (about 300 calls a frame late in a level). Created on first use, one per loaded game.
    /// </summary>
    public sealed class DamageFlashRunner : MonoBehaviour
    {
        private static readonly List<DamageFlash> Flashing = new List<DamageFlash>();
        private static DamageFlashRunner _instance;

        /// <summary>Bodies flashing now (tests).</summary>
        public static int Count => Flashing.Count;

        internal static void Add(DamageFlash flash)
        {
            if (!Flashing.Contains(flash)) Flashing.Add(flash); // a handful at a time, so a scan is cheap
            if (_instance == null && Application.isPlaying)
            {
                var go = new GameObject("DamageFlashRunner") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<DamageFlashRunner>();
            }
        }

        internal static void Remove(DamageFlash flash)
        {
            int i = Flashing.IndexOf(flash);
            if (i < 0) return;
            int last = Flashing.Count - 1;
            Flashing[i] = Flashing[last];
            Flashing.RemoveAt(last);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // Backwards: a flash that ends removes itself by swapping in the last one, which is already ticked.
            for (int i = Flashing.Count - 1; i >= 0; i--)
            {
                if (i >= Flashing.Count) continue;
                var flash = Flashing[i];
                if (flash == null) { Flashing[i] = Flashing[Flashing.Count - 1]; Flashing.RemoveAt(Flashing.Count - 1); continue; }
                flash.TickFlash(dt);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so flashes from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Flashing.Clear();
            _instance = null;
        }
    }
}
