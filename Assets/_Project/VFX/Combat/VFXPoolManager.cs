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
        /// Blood where an enemy died. Owner (2026-09-29): "blood effect enemy ke neck se spray particle ki tarah", then
        /// "jameen per blood effect nahi chahiye ... neck ke pass ... 300 particle mein dikhao": a jet of 300 blood particles
        /// spurts from the neck, away from the hero and upwards, in pulses, and nothing is left on the ground. VS has blood
        /// on kills and an option to switch it off (vampire.survivors.wiki/w/Options, "Disable blood", added in v1.6.0);
        /// the wiki does not describe its look. Nothing plays with Disable Blood on.
        /// </summary>
        public static void PlayBlood(Vector3 neckPosition, Vector3 awayFromHero)
        {
            if (SoulHunter.Core.Persistence.GameSettings.DisableBlood) return;
            var manager = EnsureInstance();
            if (manager == null) return;
            Vector3 jet = SprayDirection(awayFromHero, Random.Range(-SprayYawJitter, SprayYawJitter));
            manager.PlayInternal(VfxKind.BloodSpray, neckPosition, Quaternion.FromToRotation(Vector3.up, jet), 1f, manager.SetSprayParticles);
        }

        /// <summary>Tilt of the jet above the ground and the random turn either side (provisional).</summary>
        public const float SprayElevationDegrees = 50f, SprayYawJitter = 25f;

        /// <summary>Blood particles in one neck spray (owner, 2026-09-29).</summary>
        public const int SprayParticles = 300;

        /// <summary>
        /// The three spurts of a spray, like a heartbeat, each weaker than the last: (start seconds, particles per step),
        /// SprayStepsPerPulse steps SprayStepSeconds apart. 140 + 100 + 60 = SprayParticles.
        /// </summary>
        public static readonly (float Time, int Count)[] SprayPulses = { (0f, 14), (0.25f, 10), (0.5f, 6) };
        public const int SprayStepsPerPulse = 10;
        public const float SprayStepSeconds = 0.02f;

        /// <summary>
        /// Owner (2026-09-29, option 2): 300 particles for a normal kill, fewer when many enemies die at once. Up to
        /// FullSprays sprays in the air each get all 300; every spray started while more are playing shares that budget
        /// (300 × FullSprays / sprays playing), never below MinSprayParticles. Values provisional.
        /// </summary>
        public const int FullSprays = 8, MinSprayParticles = 60;

        /// <summary>Particles a new spray gets while <paramref name="playing"/> other sprays are in the air.</summary>
        public static int SprayParticlesFor(int playing)
        {
            int total = 0;
            for (int i = 0; i < SprayPulses.Length; i++) total += SprayStepCount(playing, i) * SprayStepsPerPulse;
            return total;
        }

        /// <summary>Particles per step of pulse <paramref name="pulse"/> while <paramref name="playing"/> sprays are in the air.</summary>
        public static int SprayStepCount(int playing, int pulse)
        {
            float share = playing < FullSprays ? 1f : Mathf.Max((float)MinSprayParticles / SprayParticles, (float)FullSprays / (playing + 1));
            return Mathf.Max(1, Mathf.RoundToInt(SprayPulses[pulse].Count * share));
        }

        // Sets the spray's pulses for how many other sprays are in the air (runs before it is switched on).
        private void SetSprayParticles(GameObject spray)
        {
            int playing = PlayingCount(VfxKind.BloodSpray); // itself is not active yet
            var jet = spray.GetComponentInChildren<ParticleSystem>(true);
            if (jet == null) return;
            var emission = jet.emission;
            for (int i = 0; i < SprayPulses.Length && i < emission.burstCount; i++)
            {
                var burst = emission.GetBurst(i);
                burst.count = new ParticleSystem.MinMaxCurve(SprayStepCount(playing, i));
                emission.SetBurst(i, burst);
            }
        }

        /// <summary>The jet's direction: away from the hero on the ground plane (any way if unknown), tilted upwards.</summary>
        public static Vector3 SprayDirection(Vector3 awayFromHero, float yawDegrees)
        {
            Vector3 flat = new Vector3(awayFromHero.x, 0f, awayFromHero.z);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            flat = Quaternion.Euler(0f, yawDegrees, 0f) * flat.normalized;
            float up = SprayElevationDegrees * Mathf.Deg2Rad;
            return (flat * Mathf.Cos(up) + Vector3.up * Mathf.Sin(up)).normalized;
        }

        /// <summary>The neck of a body: its drawn bounds' centre line at 80% of its height (the models have no neck bone in common).</summary>
        public static Vector3 NeckOf(Bounds body) =>
            new Vector3(body.center.x, body.min.y + body.size.y * NeckHeight, body.center.z);

        public const float NeckHeight = 0.8f;

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
        public int PooledCount(VfxKind kind)
        {
            int count = _pools.TryGetValue(kind, out var pool) ? pool.Count : 0;
            var shared = GetComponent<SharedVfxEmitter>();
            if (shared != null && shared.HasSystems(kind)) count++; // the shared systems show this kind too
            return count;
        }

        /// <summary>The instances of a kind that are playing now (tests).</summary>
        public List<GameObject> Playing(VfxKind kind) =>
            _pools.TryGetValue(kind, out var pool) ? pool.FindAll(v => v != null && v.activeSelf) : new List<GameObject>();

        private readonly Dictionary<VfxKind, GameObject> _last = new Dictionary<VfxKind, GameObject>();

        /// <summary>The instance of a kind started most recently (tests).</summary>
        public GameObject LastStarted(VfxKind kind) => _last.TryGetValue(kind, out var vfx) ? vfx : null;

        /// <summary>How many instances of a kind are playing now.</summary>
        public int PlayingCount(VfxKind kind)
        {
            if (!_pools.TryGetValue(kind, out var pool)) return 0;
            int count = 0;
            for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].activeSelf) count++;
            return count;
        }

        private void PlayInternal(VfxKind kind, Vector3 position, Quaternion? rotation, float scale, System.Action<GameObject> configure = null)
        {
            int k = (int)kind;
            if (Time.time - _lastPlayed[k] < MinInterval[k]) return;
            var prefab = _library != null ? _library.Get(kind) : null;
            if (prefab == null) return;
            _lastPlayed[k] = Time.time;

            // Draw calls (2026-10-15): the per-kill effects can play through one shared system per particle system.
            Quaternion pose = rotation.HasValue ? rotation.Value * prefab.transform.rotation : prefab.transform.rotation;
            if (SharedVfxEmitter.Enabled && SharedVfxEmitter.Handles(kind) && !PlaysWhilePaused(kind))
            {
                var shared = SharedVfxEmitter.For(this);
                System.Func<int, int, int> steps = null;
                if (kind == VfxKind.BloodSpray)
                {
                    int playing = shared.LiveCount(VfxKind.BloodSpray); // the same budget as the pooled sprays
                    steps = (channel, burst) => channel == 0 && burst < SprayPulses.Length ? SprayStepCount(playing, burst) : -1;
                }
                float shownScale = Mathf.Max(0.05f, scale) * prefab.transform.localScale.x;
                if (shared.TryPlay(kind, prefab, position, pose, shownScale, steps)) return;
            }

            var vfx = GetOrCreate(kind, prefab);
            if (vfx == null) return;
            vfx.transform.SetPositionAndRotation(position, pose);
            vfx.transform.localScale = prefab.transform.localScale * Mathf.Max(0.05f, scale);
            if (vfx.activeSelf) vfx.SetActive(false); // a recycled one restarts cleanly
            configure?.Invoke(vfx); // before it starts, so its particle setup is never changed mid-play
            vfx.SetActive(true);
            _last[kind] = vfx;
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

            if (pool.Count < MaxPerKind)
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
