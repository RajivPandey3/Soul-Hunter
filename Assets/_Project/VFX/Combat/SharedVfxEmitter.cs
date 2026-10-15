using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Draw calls (2026-10-15): a Level 5 render census counted 10-24 blood neck sprays and 14-18 death-puff particle
    /// systems on screen, each pooled effect its own particle renderer and so its own draw. With this switched on, each
    /// shared kind (BloodSpray, EnemyDeath) plays through ONE world-space copy of each of its particle systems: the copy is
    /// moved to the effect's pose and told to Emit, so shape, speed, lifetime, size, colour, gravity and the stretched look
    /// all come from the very same particle system settings, and the prefab's bursts (times, steps, intervals) are replayed
    /// on game time. 24 blood jets become one draw, all puffs one and all wisps one. The blood budget
    /// (VFXPoolManager.SprayStepCount) is applied per spray as before, from the sprays this emitter has in the air.
    /// Off by default: BloodPlayModeTests read the pooled per-spray GameObjects and their own particle systems, which this
    /// mode does not make (SharedVfxEmitterPlayModeTests checks the same rules here). Kept only if an A/B in one run shows
    /// a gain (gems, 2026-10-15, did not).
    /// </summary>
    public sealed class SharedVfxEmitter : MonoBehaviour
    {
        /// <summary>False plays every effect through VFXPoolManager's own pool (default; A/B measurement).</summary>
        public static bool Enabled = false;

        /// <summary>The kinds played through shared systems: the per-kill ones.</summary>
        public static bool Handles(VfxKind kind) => kind == VfxKind.BloodSpray || kind == VfxKind.EnemyDeath;

        private const int MaxLivePerKind = 32; // as VFXPoolManager's pool
        private const int MaxParticlesCap = 20000;

        private sealed class Channel
        {
            public ParticleSystem System;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public ParticleSystem.Burst[] Bursts;
            public float MaxLifetime;
            public float LastStep;
        }

        private sealed class Kind
        {
            public VfxKind Id;
            public GameObject Prefab;
            public readonly List<Channel> Channels = new List<Channel>();
            public bool Shareable;
            public float Span; // seconds from start to the last particle's death
        }

        private sealed class Spray
        {
            public Kind Kind;
            public Vector3 Position;
            public Quaternion Rotation;
            public float StartedAt, EndsAt;
            public int[][] StepCounts; // [channel][burst] particles per step
            public int[][] StepsDone;
            public bool Emitting;
            public int Scheduled;
        }

        private readonly Dictionary<VfxKind, Kind> _kinds = new Dictionary<VfxKind, Kind>();
        private readonly List<Spray> _sprays = new List<Spray>();

        /// <summary>Particles emitted per kind since the emitter was made (tests).</summary>
        private readonly Dictionary<VfxKind, int> _emitted = new Dictionary<VfxKind, int>();

        /// <summary>The emitter on the active VFXPoolManager, made on first use.</summary>
        internal static SharedVfxEmitter For(VFXPoolManager manager)
        {
            var e = manager.GetComponent<SharedVfxEmitter>();
            return e != null ? e : manager.gameObject.AddComponent<SharedVfxEmitter>();
        }

        /// <summary>The emitter of the active scene, if any (tests and the bot).</summary>
        public static SharedVfxEmitter Current =>
            VFXPoolManager.Instance != null ? VFXPoolManager.Instance.GetComponent<SharedVfxEmitter>() : null;

        /// <summary>
        /// Starts an effect through the shared systems. False when this kind cannot be shared (an unusual prefab, a
        /// scaled play); the caller then uses its pool.
        /// </summary>
        internal bool TryPlay(VfxKind kindId, GameObject prefab, Vector3 position, Quaternion rotation, float scale, System.Func<int, int, int> stepCount)
        {
            if (Mathf.Abs(scale - 1f) > 0.001f) return false;
            var kind = KindFor(kindId, prefab);
            if (kind == null || !kind.Shareable) return false;

            // As the pool: at most 32 of a kind; the oldest stops spraying.
            int live = 0, oldest = -1;
            for (int i = 0; i < _sprays.Count; i++)
                if (_sprays[i].Kind == kind) { live++; if (oldest < 0) oldest = i; }
            if (live >= MaxLivePerKind && oldest >= 0) _sprays.RemoveAt(oldest);

            float now = Time.time;
            var spray = new Spray
            {
                Kind = kind, Position = position, Rotation = rotation, StartedAt = now, EndsAt = now + kind.Span,
                StepCounts = new int[kind.Channels.Count][], StepsDone = new int[kind.Channels.Count][], Emitting = true,
            };
            for (int c = 0; c < kind.Channels.Count; c++)
            {
                var bursts = kind.Channels[c].Bursts;
                spray.StepCounts[c] = new int[bursts.Length];
                spray.StepsDone[c] = new int[bursts.Length];
                for (int b = 0; b < bursts.Length; b++)
                {
                    int count = stepCount != null ? stepCount(c, b) : -1;
                    if (count < 0) count = BurstCount(bursts[b]);
                    spray.StepCounts[c][b] = count;
                    spray.Scheduled += count * Mathf.Max(1, bursts[b].cycleCount);
                }
            }
            _sprays.Add(spray);
            Step(spray, now); // the t = 0 steps show this frame, as a pooled effect's would
            return true;
        }

        private static int BurstCount(ParticleSystem.Burst burst) =>
            burst.count.mode == ParticleSystemCurveMode.TwoConstants
                ? Mathf.RoundToInt(Random.Range(burst.count.constantMin, burst.count.constantMax))
                : Mathf.RoundToInt(burst.count.constant);

        private void Update()
        {
            float now = Time.time;
            for (int i = _sprays.Count - 1; i >= 0; i--)
            {
                var spray = _sprays[i];
                if (!Enabled) spray.Emitting = false; // switched off: no more steps; what is in the air finishes
                if (spray.Emitting) Step(spray, now);
                if (now >= spray.EndsAt) _sprays.RemoveAt(i);
            }
        }

        private void Step(Spray spray, float now)
        {
            float t = now - spray.StartedAt;
            bool pending = false;
            for (int c = 0; c < spray.Kind.Channels.Count; c++)
            {
                var channel = spray.Kind.Channels[c];
                for (int b = 0; b < channel.Bursts.Length; b++)
                {
                    var burst = channel.Bursts[b];
                    int cycles = Mathf.Max(1, burst.cycleCount);
                    int due = 0;
                    if (t >= burst.time)
                        due = burst.repeatInterval > 0f
                            ? Mathf.Min(cycles, Mathf.FloorToInt((t - burst.time) / burst.repeatInterval + 0.0001f) + 1)
                            : cycles;
                    int done = spray.StepsDone[c][b];
                    if (due > done)
                    {
                        int count = (due - done) * spray.StepCounts[c][b];
                        if (count > 0) Emit(spray.Kind.Id, channel, spray.Position, spray.Rotation, count);
                        spray.StepsDone[c][b] = due;
                    }
                    if (spray.StepsDone[c][b] < cycles) pending = true;
                }
            }
            spray.Emitting = pending;
        }

        private void Emit(VfxKind kind, Channel channel, Vector3 position, Quaternion rotation, int count)
        {
            var t = channel.System.transform;
            // The shared system stands where this effect's system would stand, then emits: its shape cone and every
            // start value apply exactly as on the pooled copy. World-space particles stay where they were emitted.
            t.SetPositionAndRotation(position + rotation * channel.LocalPosition, rotation * channel.LocalRotation);
            channel.System.Emit(count);
            _emitted.TryGetValue(kind, out int total);
            _emitted[kind] = total + count;
        }

        private Kind KindFor(VfxKind id, GameObject prefab)
        {
            if (_kinds.TryGetValue(id, out var kind) && kind.Prefab == prefab) return kind;
            if (kind != null) foreach (var ch in kind.Channels) if (ch.System != null) Destroy(ch.System.gameObject);
            kind = Build(id, prefab);
            _kinds[id] = kind;
            return kind;
        }

        private Kind Build(VfxKind id, GameObject prefab)
        {
            var kind = new Kind { Id = id, Prefab = prefab, Shareable = true };
            var systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0) { kind.Shareable = false; return kind; }
            var root = prefab.transform;
            foreach (var source in systems)
            {
                if (!Shareable(source)) { kind.Shareable = false; break; }
                var bursts = new ParticleSystem.Burst[source.emission.burstCount];
                source.emission.GetBursts(bursts);
                float delay = source.main.startDelay.constantMax;
                for (int b = 0; b < bursts.Length; b++) bursts[b].time += delay;

                var copy = Instantiate(source.gameObject, transform);
                copy.name = $"Shared {id} {source.name}";
                foreach (Transform child in copy.transform) Destroy(child.gameObject); // nested systems get their own channel
                foreach (var extra in copy.GetComponents<MonoBehaviour>()) Destroy(extra);
                copy.SetActive(true);
                var ps = copy.GetComponent<ParticleSystem>();
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = false;
                main.startDelay = 0f;
                main.stopAction = ParticleSystemStopAction.None;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.useUnscaledTime = false;
                main.maxParticles = Mathf.Min(MaxParticlesCap, Mathf.Max(1, source.main.maxParticles) * MaxLivePerKind);
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.rateOverDistance = 0f;
                emission.SetBursts(new ParticleSystem.Burst[0]);
                emission.enabled = false; // only Emit() makes particles
                ps.Clear(false);
                ps.Play(false);

                // The source's pose under the prefab root, without the root's own pose (VFXPoolManager applies that).
                var local = root.worldToLocalMatrix * source.transform.localToWorldMatrix;
                var channel = new Channel
                {
                    System = ps,
                    LocalPosition = local.GetColumn(3),
                    LocalRotation = Quaternion.Inverse(root.rotation) * source.transform.rotation,
                    Bursts = bursts,
                    MaxLifetime = source.main.startLifetime.constantMax,
                };
                for (int b = 0; b < bursts.Length; b++)
                    channel.LastStep = Mathf.Max(channel.LastStep, bursts[b].time + Mathf.Max(0, bursts[b].cycleCount - 1) * bursts[b].repeatInterval);
                kind.Span = Mathf.Max(kind.Span, channel.LastStep + channel.MaxLifetime);
                kind.Channels.Add(channel);
            }
            if (!kind.Shareable)
                foreach (var ch in kind.Channels) if (ch.System != null) Destroy(ch.System.gameObject);
            return kind;
        }

        // A system whose look depends on the pose it has after emitting (local simulation of moving parts, local-space
        // velocity or force, sub-emitters, rate emission) is left to the pool.
        private static bool Shareable(ParticleSystem ps)
        {
            if (ps.subEmitters.enabled) return false;
            if (ps.velocityOverLifetime.enabled && ps.velocityOverLifetime.space == ParticleSystemSimulationSpace.Local) return false;
            if (ps.forceOverLifetime.enabled && ps.forceOverLifetime.space == ParticleSystemSimulationSpace.Local) return false;
            var emission = ps.emission;
            if (ps.main.loop || emission.rateOverTime.constantMax > 0f || emission.rateOverDistance.constantMax > 0f) return false;
            return emission.burstCount > 0;
        }

        // ------------------------------------------------------------------ queries (tests, the bot, VFXPoolManager)

        /// <summary>Effects of a kind still in the air (spraying, or their particles not yet dead).</summary>
        public int LiveCount(VfxKind kind)
        {
            int n = 0;
            foreach (var s in _sprays) if (s.Kind.Id == kind) n++;
            return n;
        }

        /// <summary>True once this kind has its shared systems.</summary>
        public bool HasSystems(VfxKind kind) => _kinds.TryGetValue(kind, out var k) && k.Shareable && k.Channels.Count > 0;

        /// <summary>The shared particle systems of a kind (one per system in its prefab).</summary>
        public List<ParticleSystem> Systems(VfxKind kind)
        {
            var list = new List<ParticleSystem>();
            if (_kinds.TryGetValue(kind, out var k)) foreach (var ch in k.Channels) if (ch.System != null) list.Add(ch.System);
            return list;
        }

        /// <summary>Particles the newest effect of a kind will emit in all (its budget).</summary>
        public int LastScheduled(VfxKind kind)
        {
            for (int i = _sprays.Count - 1; i >= 0; i--) if (_sprays[i].Kind.Id == kind) return _sprays[i].Scheduled;
            return 0;
        }

        /// <summary>Pose of the newest effect of a kind.</summary>
        public bool LastPose(VfxKind kind, out Vector3 position, out Quaternion rotation)
        {
            for (int i = _sprays.Count - 1; i >= 0; i--)
                if (_sprays[i].Kind.Id == kind) { position = _sprays[i].Position; rotation = _sprays[i].Rotation; return true; }
            position = default; rotation = Quaternion.identity;
            return false;
        }

        /// <summary>Particles emitted for a kind so far.</summary>
        public int Emitted(VfxKind kind) => _emitted.TryGetValue(kind, out int n) ? n : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = false;
    }
}
