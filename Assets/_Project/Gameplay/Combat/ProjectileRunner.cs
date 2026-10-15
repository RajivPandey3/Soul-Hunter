using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// 60 FPS pass (2026-10-15): moves every flying shot from one Update (Projectile.Tick), in place of up to 560 separate
    /// Update calls (Level 5 with Knife, Fire Wand and Song of Mana: 0.54 ms a frame in calls alone). Created on first
    /// use, one per loaded game.
    /// </summary>
    public sealed class ProjectileRunner : MonoBehaviour
    {
        private static ProjectileRunner _instance;
        private Camera _camera;

        /// <summary>True once the runner exists (tests).</summary>
        public static bool Exists => _instance != null;

        internal static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("ProjectileRunner") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ProjectileRunner>();
        }

        private void Update()
        {
            var shots = Projectile.ActiveShots;
            float dt = Time.deltaTime;
            // Each shot checks whether it is on screen every fourth frame, a quarter of them each frame.
            if (_camera == null) _camera = Camera.main;
            int phase = Time.frameCount & 3;
            // Backwards: a shot that ends during its tick is swap-removed, which only moves an already-ticked shot.
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (i >= shots.Count) continue;
                var shot = shots[i];
                if (shot == null) // destroyed without OnDisable (never expected)
                {
                    int last = shots.Count - 1;
                    shots[i] = shots[last];
                    if (shots[i] != null) shots[i].ActiveIndex = i;
                    shots.RemoveAt(last);
                    continue;
                }
                shot.Tick(dt);
                if ((i & 3) == phase && i < shots.Count && shots[i] == shot) shot.UpdateFx(_camera);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so shots from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Projectile.ActiveShots.Clear();
            _instance = null;
        }
    }
}
