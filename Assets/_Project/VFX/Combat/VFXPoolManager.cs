using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Learning Comment:
    /// Gameplay VFX pool (hit sparks, enemy death, pickups, heal, chest, level-up, explosions). No scene contained this
    /// manager, so its effects never played; it now creates itself in the active scene on first use and plays the
    /// particle prefabs of Resources/VfxLibrary (built by VfxBuilder). Each kind has a capped pool, and very frequent
    /// kinds are throttled, so a horde of 300 cannot flood the frame.
    /// </summary>
    public class VFXPoolManager : MonoBehaviour
    {
        public static VFXPoolManager Instance { get; private set; }

        private const int MaxPerKind = 32;
        private static readonly float[] MinInterval = { 0.02f, 0.01f, 0.03f, 0.03f, 0.2f, 0f, 0f, 0f, 0f, 0.01f }; // by VfxKind
        /// <summary>Blood splats stay on the ground for a while, so a horde needs more of them than other kinds.</summary>
        private const int MaxBlood = 64;

        private VfxLibrary _library;
        private readonly Dictionary<VfxKind, List<GameObject>> _pools = new Dictionary<VfxKind, List<GameObject>>();
        private readonly Dictionary<VfxKind, int> _reuseIndex = new Dictionary<VfxKind, int>();
        private readonly float[] _lastPlayed = new float[System.Enum.GetValues(typeof(VfxKind)).Length];

        /// <summary>The manager of the active scene, created on first use.</summary>
        public static VFXPoolManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            if (!Application.isPlaying) return null;
            return new GameObject("VFXPoolManager").AddComponent<VFXPoolManager>();
        }

        /// <summary>Plays a feedback effect at a position (skipped while throttled or when the library is missing).</summary>
        public static void Play(VfxKind kind, Vector3 position)
        {
            var manager = EnsureInstance();
            if (manager != null) manager.PlayInternal(kind, position, null, 1f);
        }

        /// <summary>Plays an oriented, scaled effect (the Whip's slash arc follows its lash and its area).</summary>
        public static void Play(VfxKind kind, Vector3 position, Quaternion rotation, float scale)
        {
            var manager = EnsureInstance();
            if (manager != null) manager.PlayInternal(kind, position, rotation, scale);
        }

        /// <summary>
        /// Blood where an enemy died: a spray from its body and a splat on the ground under it that fades. VS has blood
        /// on kills and an option to switch it off (vampire.survivors.wiki/w/Options, "Disable blood", added in v1.6.0);
        /// the wiki does not describe how it looks, so the look is Soul Hunter's own. Nothing plays with Disable Blood on.
        /// </summary>
        public static void PlayBlood(Vector3 bodyPosition)
        {
            if (SoulHunter.Core.Persistence.GameSettings.DisableBlood) return;
            Play(VfxKind.Blood, new Vector3(bodyPosition.x, GroundHeight(bodyPosition) + 0.03f, bodyPosition.z));
        }

        /// <summary>Top of the stage floor under a point (bodies stand on the player's plane, above the floor's top).</summary>
        public static float GroundHeight(Vector3 point)
        {
            int ignore = LayerMask.GetMask("Enemy", "Player", "Obstacle");
            return UnityEngine.Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out var hit, 12f, ~ignore, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : 0f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _library = Resources.Load<VfxLibrary>(VfxLibrary.ResourcePath);
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = -1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayEnemyDeathVFX(Vector3 position) => PlayInternal(VfxKind.EnemyDeath, position, null, 1f);
        public void PlayChestOpenVFX(Vector3 position) => PlayInternal(VfxKind.ChestOpen, position, null, 1f);

        /// <summary>Number of pooled instances of a kind (tests).</summary>
        public int PooledCount(VfxKind kind) => _pools.TryGetValue(kind, out var pool) ? pool.Count : 0;

        /// <summary>The instances of a kind that are playing now (tests).</summary>
        public List<GameObject> Playing(VfxKind kind) =>
            _pools.TryGetValue(kind, out var pool) ? pool.FindAll(v => v != null && v.activeSelf) : new List<GameObject>();

        private void PlayInternal(VfxKind kind, Vector3 position, Quaternion? rotation, float scale)
        {
            int k = (int)kind;
            if (Time.time - _lastPlayed[k] < MinInterval[k]) return;
            var prefab = _library != null ? _library.Get(kind) : null;
            if (prefab == null) return;
            _lastPlayed[k] = Time.time;

            var vfx = GetOrCreate(kind, prefab);
            if (vfx == null) return;
            vfx.transform.SetPositionAndRotation(position, rotation.HasValue ? rotation.Value * prefab.transform.rotation : prefab.transform.rotation);
            vfx.transform.localScale = prefab.transform.localScale * Mathf.Max(0.05f, scale);
            if (vfx.activeSelf) vfx.SetActive(false); // a recycled one restarts cleanly
            vfx.SetActive(true);
            bool unscaled = PlaysWhilePaused(kind);
            foreach (var particles in vfx.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.useUnscaledTime = unscaled;
                particles.Clear(false);
                particles.Play(false);
            }
        }

        /// <summary>
        /// Effects that start in the same frame the game pauses (a chest or level-up screen opens). Their particles run in
        /// real time; on game time they froze on their first frame and never showed (found 2026-09-28).
        /// </summary>
        public static bool PlaysWhilePaused(VfxKind kind) => kind == VfxKind.ChestOpen || kind == VfxKind.LevelUp;

        private GameObject GetOrCreate(VfxKind kind, GameObject prefab)
        {
            if (!_pools.TryGetValue(kind, out var pool)) _pools[kind] = pool = new List<GameObject>();
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && !pool[i].activeSelf) return pool[i];

            if (pool.Count < (kind == VfxKind.Blood ? MaxBlood : MaxPerKind))
            {
                var created = Instantiate(prefab, transform);
                created.SetActive(false);
                if (created.GetComponent<VFXAutoDisable>() == null) created.AddComponent<VFXAutoDisable>();
                pool.Add(created);
                return created;
            }

            // Full: recycle the oldest so a big wave never allocates more.
            _reuseIndex.TryGetValue(kind, out int index);
            index = (index + 1) % pool.Count;
            _reuseIndex[kind] = index;
            return pool[index];
        }
    }

    /// <summary>Hides a pooled effect once all of its particles have died.</summary>
    public class VFXAutoDisable : MonoBehaviour
    {
        private ParticleSystem[] _systems;
        private float _startedAt;

        private void Awake() { _systems = GetComponentsInChildren<ParticleSystem>(); }
        // Effects without particles (the slash arc) end themselves (VfxQuadFade).
        private void OnEnable() { _startedAt = Time.time; }

        private void Update()
        {
            if (_systems.Length == 0 || Time.time - _startedAt < 0.05f) return;
            for (int i = 0; i < _systems.Length; i++)
                if (_systems[i] != null && _systems[i].IsAlive(false)) return;
            gameObject.SetActive(false);
        }
    }
}
