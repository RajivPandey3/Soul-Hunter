using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Runs an enemy's VS skills (EnemyData.Abilities). EnemyController starts it whenever the enemy appears (OnEnable,
    /// so pooled reuse starts fresh) and stops it when the enemy leaves; an enemy without abilities never gets one.
    /// Rules: game time only (a pause holds everything; never writes Time.timeScale); a frozen enemy does nothing (its
    /// timers hold, a shield keeps absorbing); a dead enemy does nothing. Each kind cites its VS page in
    /// EnemyAbilityDefaults and below.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyAbilities : MonoBehaviour
    {
        private static readonly Color ZoneTint = new Color(1f, 0.2f, 0.25f, 1f);
        private static readonly Color PulseTint = new Color(0.55f, 0.85f, 1f, 1f);
        private static readonly Color FuseTint = new Color(1f, 0.55f, 0.1f, 1f);
        private static EnemySpawner _spawner;

        private sealed class Runner
        {
            public EnemyAbilitySpec Spec; // resolved: no zeros left
            public float Timer;           // seconds to the next use
            public int BurstsOnSpawn;
            public bool Armed;
            public float Fuse;
            public float PhaseLeft;
            public EnemyAbilityZone Marker;
            public readonly List<EnemySummonTag> Summons = new List<EnemySummonTag>();
        }

        private readonly List<Runner> _runners = new List<Runner>();
        private EnemyController _enemy;
        private HealthController _health;
        private System.Func<DamagePacket, bool> _previousInterceptor;
        private bool _interceptorInstalled;

        // ---- Read by tests and tools ----
        public int ShotsFired { get; private set; }
        public int LungesStarted { get; private set; }
        public int ZonesCreated { get; private set; }
        public int PulsesCast { get; private set; }
        public int Explosions { get; private set; }
        public int SummonsCreated { get; private set; }
        public int AbsorbedDamage { get; private set; }
        public bool IsRunning => _runners.Count > 0;
        public bool IsShielded { get { foreach (var r in _runners) if (r.PhaseLeft > 0f) return true; return false; } }
        public bool IsArmed { get { foreach (var r in _runners) if (r.Armed) return true; return false; } }
        public int SummonsAlive { get { int n = 0; foreach (var r in _runners) n += r.Summons.Count; return n; } }

        /// <summary>Starts (or restarts) the given skills on this enemy. Counters and timers start from zero.</summary>
        public void Begin(EnemyController enemy, IReadOnlyList<EnemyAbilitySpec> specs)
        {
            Stop();
            _enemy = enemy != null ? enemy : GetComponent<EnemyController>();
            _health = GetComponent<HealthController>();
            ShotsFired = LungesStarted = ZonesCreated = PulsesCast = Explosions = SummonsCreated = AbsorbedDamage = 0;
            if (specs != null)
            {
                foreach (var spec in specs)
                {
                    if (spec == null) continue;
                    var resolved = spec.Resolved();
                    var runner = new Runner { Spec = resolved };
                    runner.Timer = resolved.FirstDelaySeconds >= 0f ? resolved.FirstDelaySeconds
                        : EnemyAbilitySpec.NextInterval(resolved, 1f, Random.value);
                    if (resolved.Kind == EnemyAbilityKind.Summon) runner.BurstsOnSpawn = resolved.SpawnBursts;
                    if (resolved.Kind == EnemyAbilityKind.InvulnerablePhase) runner.PhaseLeft = resolved.DurationSeconds;
                    _runners.Add(runner);
                }
            }
            if (IsShielded) InstallShield();
            enabled = _runners.Count > 0;
        }

        /// <summary>Ends every skill: markers go, the shield is removed (absorbed damage is not dealt), summons are released.</summary>
        public void Stop()
        {
            foreach (var r in _runners)
            {
                if (r.Marker != null) Destroy(r.Marker.gameObject);
                r.Marker = null;
                foreach (var tag in r.Summons) if (tag != null) tag.Owner = null;
                r.Summons.Clear();
            }
            _runners.Clear();
            RemoveShield();
            // No Update cost while idle; left alone while the object is being switched off (it restarts on OnEnable).
            if (enabled && gameObject.activeInHierarchy) enabled = false;
        }

        private void OnDisable() => Stop();

        private void Update()
        {
            if (_enemy == null || _runners.Count == 0) return;
            if (SoulHunter.Core.Services.GameTime.IsPaused) return;
            if (_enemy.IsDefanged) return; // VS Defang: it cannot deal damage, so its attacks do not run
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_health != null && _health.CurrentHealth <= 0) return;

            // The shield is passive: it keeps counting down (and absorbing) while the enemy is frozen.
            UpdateShields(dt);
            if (_enemy.IsFrozen) return;

            for (int i = 0; i < _runners.Count; i++)
            {
                var r = _runners[i];
                switch (r.Spec.Kind)
                {
                    case EnemyAbilityKind.Summon: UpdateSummon(r, dt); break;
                    case EnemyAbilityKind.SelfDestruct: UpdateSelfDestruct(r, dt); break;
                    case EnemyAbilityKind.InvulnerablePhase: break;
                    default:
                        r.Timer -= dt;
                        if (r.Timer > 0f) break;
                        r.Timer = EnemyAbilitySpec.NextInterval(r.Spec, HealthFraction(), Random.value);
                        Use(r);
                        break;
                }
                if (_runners.Count == 0 || !isActiveAndEnabled) return; // the enemy left during its skill
            }
        }

        private float HealthFraction() =>
            _health != null && _health.MaxHealth > 0 ? Mathf.Clamp01((float)_health.CurrentHealth / _health.MaxHealth) : 1f;

        private Transform Player() => PlayerController.Instance != null ? PlayerController.Instance.transform : null;

        private void Use(Runner r)
        {
            switch (r.Spec.Kind)
            {
                case EnemyAbilityKind.Shoot: Shoot(r.Spec); break;
                case EnemyAbilityKind.Lunge: Lunge(r.Spec); break;
                case EnemyAbilityKind.HazardZone: LayZones(r.Spec); break;
                case EnemyAbilityKind.HalvingPulse: Pulse(r.Spec); break;
            }
        }

        // ------------------------------------------------------------------ Shoot
        // VS Twin Skulls (vampire.survivors.wiki/w/Twin_Skulls), Twin Snakes (/w/Twin_Snakes), The Ender's Scythe Bullets
        // (/w/The_Ender). Enemy bullets fly in a fixed direction (vampire.survivors.wiki/w/Enemy_projectiles): each is
        // aimed at where the player is when fired. Reuses the stage-hazard enemy bullet (HazardBullets: a Projectile with
        // TargetsPlayer, so it hurts only the player).
        private void Shoot(EnemyAbilitySpec spec)
        {
            var player = Player();
            if (player == null) return;
            Vector3 aim = player.position - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = transform.forward;
            aim.Normalize();
            Vector3 from = transform.position + Vector3.up * 0.5f; // inside the player's capsule height
            int count = Mathf.Max(1, spec.Count);
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * 0.5f) * spec.SpreadDegrees;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * aim;
                var bullet = SoulHunter.Gameplay.Hazards.HazardBullets.Fire(from, direction, spec.Speed, spec.Damage, spec.DurationSeconds);
                if (bullet != null) ShotsFired++;
            }
        }

        // ------------------------------------------------------------------ Lunge
        // VS Orochimario (vampire.survivors.wiki/w/Orochimario): "The heads can lunge at the player to attack them every
        // 5 to 10 seconds". Only a chasing enemy lunges (not a stage-event flier, a held Pile shooter or a frozen one).
        private void Lunge(EnemyAbilitySpec spec)
        {
            var player = Player();
            if (player == null || !(_enemy.CurrentState is EnemyChaseState)) return;
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            _enemy.ChangeState(new EnemyLungeState(_enemy, direction, _enemy.MoveSpeed * spec.SpeedMultiplier, spec.DurationSeconds));
            LungesStarted++;
        }

        // ------------------------------------------------------------------ Hazard zone
        // VS The Ender (vampire.survivors.wiki/w/The_Ender): zones that "periodically deal 12 damage in the area and last
        // 10 seconds"; The Drowner (/w/The_Drowner): "20 damage per tick". Soul Hunter lays them on (or near) the player.
        private void LayZones(EnemyAbilitySpec spec)
        {
            var player = Player();
            if (player == null) return;
            for (int i = 0; i < Mathf.Max(1, spec.Count); i++)
            {
                Vector2 offset = spec.PlacementSpread > 0f ? Random.insideUnitCircle * spec.PlacementSpread : Vector2.zero;
                Vector3 at = player.position + new Vector3(offset.x, 0f, offset.y);
                // The first tick waits one tick, so the player sees the zone before it hurts.
                EnemyAbilityZone.CreateStriking(at, spec.Radius, ZoneTint, spec.Damage, false,
                    spec.TickSeconds, spec.TickSeconds, Mathf.Max(0f, spec.DurationSeconds - spec.TickSeconds));
                ZonesCreated++;
            }
        }

        // ------------------------------------------------------------------ Halving pulse
        // VS Cosmic Egg (vampire.survivors.wiki/w/Cosmic_Egg): a large circular pulse around the enemy that halves the
        // player's health if they are inside. Interval and radius are Soul Hunter's own (provisional).
        private void Pulse(EnemyAbilitySpec spec)
        {
            EnemyAbilityZone.CreateStriking(transform.position, spec.Radius, PulseTint, 0, true, 0f, 1f, 0f);
            PulsesCast++;
        }

        // ------------------------------------------------------------------ Summon
        // VS The Trickster (vampire.survivors.wiki/w/The_Trickster): rings of 48 around the player, two on spawn and one
        // every 30 s. The Directer (/w/The_Directer) summons a group every 5 s. Summons come from EnemySpawner's pools
        // (SpawnHazardEnemy) and never exceed MaxAlive.
        private void UpdateSummon(Runner r, float dt)
        {
            r.Summons.RemoveAll(tag => tag == null || tag.Owner != this);
            if (r.BurstsOnSpawn > 0)
            {
                r.BurstsOnSpawn--;
                SummonRing(r);
                return;
            }
            r.Timer -= dt;
            if (r.Timer > 0f) return;
            r.Timer = EnemyAbilitySpec.NextInterval(r.Spec, HealthFraction(), Random.value);
            SummonRing(r);
        }

        private void SummonRing(Runner r)
        {
            var player = Player();
            if (player == null) return;
            if (_spawner == null) _spawner = FindFirstObjectByType<EnemySpawner>();
            if (_spawner == null) return;
            var prefab = r.Spec.Prefab != null ? r.Spec.Prefab : _spawner.StageEnemyPrefab;
            if (prefab == null) return;
            int room = Mathf.Max(0, r.Spec.MaxAlive - r.Summons.Count);
            int count = Mathf.Min(Mathf.Max(1, r.Spec.Count), room);
            float phase = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * Mathf.PI * 2f / Mathf.Max(1, r.Spec.Count);
                // VS rings are ellipses, wider than tall like the screen (the Pile Assault proportion).
                Vector3 at = player.position + new Vector3(Mathf.Cos(angle) * r.Spec.Radius, 0f, Mathf.Sin(angle) * r.Spec.Radius * 0.7f);
                var summoned = _spawner.SpawnHazardEnemy(prefab, at);
                if (summoned == null || summoned == gameObject) continue;
                var tag = summoned.GetComponent<EnemySummonTag>();
                if (tag == null) tag = summoned.AddComponent<EnemySummonTag>();
                tag.Owner = this;
                tag.Runner = r;
                r.Summons.Add(tag);
                SummonsCreated++;
            }
        }

        internal void OnSummonGone(EnemySummonTag tag, object runner)
        {
            if (runner is Runner r) r.Summons.Remove(tag);
        }

        // ------------------------------------------------------------------ Self-destruct
        // VS Sig.ra Rossi (vampire.survivors.wiki/w/Sig.ra_Rossi): "upon getting close enough it triggers its
        // self-destruct, exploding 2 seconds later for 50 damage". It keeps moving while armed; a frozen one holds its
        // fuse. The exploded enemy leaves without drops and is not a kill. A stage boss explodes but stays (removing it
        // would leave the stage without its boss kill) and re-arms after Interval.
        private void UpdateSelfDestruct(Runner r, float dt)
        {
            var player = Player();
            if (!r.Armed)
            {
                if (r.Timer > 0f) { r.Timer -= dt; return; } // a boss re-arming
                if (player == null) return;
                Vector3 d = player.position - transform.position;
                d.y = 0f;
                if (d.magnitude > r.Spec.ArmDistance) return;
                r.Armed = true;
                r.Fuse = r.Spec.FuseSeconds;
                r.Marker = EnemyAbilityZone.CreateMarker(transform.position, r.Spec.Radius, FuseTint, transform);
                return;
            }
            r.Fuse -= dt;
            if (r.Fuse > 0f) return;
            r.Armed = false;
            if (r.Marker != null) Destroy(r.Marker.gameObject);
            r.Marker = null;
            Explosions++;
            EnemyAbilityZone.DamagePlayerInside(transform.position, r.Spec.Radius, r.Spec.Damage, false);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, transform.position + Vector3.up * 0.3f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.ExplosionSound, 0.08f, 0.6f);
            if (_enemy.IsStageBoss) { r.Timer = r.Spec.IntervalSeconds; return; }
            gameObject.SetActive(false); // back to the pool: no drops, no kill (OnDisable stops these skills)
        }

        // ------------------------------------------------------------------ Invulnerable phase
        // VS The Ender (vampire.survivors.wiki/w/The_Ender): the shield "absorbs all incoming damage" for the first 90 s,
        // then "all of the damage absorbed will be instantly dealt".
        private void UpdateShields(float dt)
        {
            bool wasShielded = false, ended = false;
            foreach (var r in _runners)
            {
                if (r.PhaseLeft <= 0f) continue;
                wasShielded = true;
                r.PhaseLeft -= dt;
                if (r.PhaseLeft <= 0f) { r.PhaseLeft = 0f; ended = true; }
            }
            if (!wasShielded || !ended || IsShielded) return;
            bool discard = false;
            foreach (var r in _runners) if (r.Spec.Kind == EnemyAbilityKind.InvulnerablePhase && r.Spec.DiscardAbsorbedDamage) discard = true;
            int absorbed = AbsorbedDamage;
            RemoveShield();
            if (discard || absorbed <= 0 || _health == null) return;
            // Already multiplied by the enemy's damage rules when absorbed; Spectral passes the Level 7 ghost rule at 1x.
            var packet = new DamagePacket(absorbed, transform.position, Vector3.zero, DamageType.Spectral) { IgnoresHitInvulnerability = true };
            _health.TakeDamage(packet);
        }

        private void InstallShield()
        {
            if (_interceptorInstalled || _health == null) return;
            _previousInterceptor = _health.DamageInterceptor;
            _health.DamageInterceptor = Absorb;
            _interceptorInstalled = true;
        }

        private void RemoveShield()
        {
            if (!_interceptorInstalled) return;
            _interceptorInstalled = false;
            if (_health != null && _health.DamageInterceptor == Absorb) _health.DamageInterceptor = _previousInterceptor;
            _previousInterceptor = null;
        }

        private bool Absorb(DamagePacket packet)
        {
            if (_previousInterceptor != null && _previousInterceptor(packet)) return true;
            if (!IsShielded) return false;
            // A hit the enemy would ignore anyway (Level 7: Normal damage on a ghost) is not stored.
            if (_enemy != null && _enemy.IsSpectral &&
                packet.Type != DamageType.Holy && packet.Type != DamageType.Magic && packet.Type != DamageType.Spectral)
                return false;
            float multiplier = _enemy != null ? _enemy.GetDamageMultiplier(packet.Type) : 1f;
            AbsorbedDamage += Mathf.Max(0, Mathf.RoundToInt(packet.Amount * multiplier));
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, transform.position + Vector3.up * 1f);
            return true;
        }
    }
}
