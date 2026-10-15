using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Death's Shadow, Soul Hunter's version of VS "The Stalker XL" (vampire.survivors.wiki/w/The_Stalker, raw, fetched
    /// 2026-10-09; owner decision 2026-10-09: "use the latest VS Stalker"). It is this stage's boss model, drawn 1.5x and
    /// dark, spawned through the enemy pool, with the XL statbox (StageHazardTable): 655,350 x player level health, VS
    /// speed 300 (9 units/s, x Curse and the run modes), 50 contact damage, no knockback, freeze resistance 2 (Clock
    /// Lancet cannot freeze it; Orologion still does: wiki Freeze, "The global freeze from Orologions are unaffected by
    /// freeze resistance"), debuff resistance (no Garlic or Requiem debuffs), no instant-kill resistance, 0 XP.
    /// <para>
    /// Ordinary weapons cannot hurt it: every weapon page calls it "otherwise intangible". Only instant kills defeat it
    /// (The_Stalker: "The Stalker is instantly killed upon taking damage from Pentagram, Gorgeous Moon, ... Crimson
    /// Shroud, ... Rosary, ... or the Dairy Carts"): Soul Hunter's Pentagram (no chest), Eclipse Sigil, Crimson Shroud and
    /// Soul Toll (a high-quality chest) and the Stage 3 rail carts (a lower-quality chest). Which one struck is read from
    /// the hit's <see cref="DamageSource"/>. The stage-clear wipe and anything else leave it standing.
    /// </para>
    /// <para>
    /// It trails the player at a gap without touching, then closes in for a short dash (StageHazardTable, rhythm), until
    /// it is defeated or its time (game time) is up. It is never a stage boss, never a kill and never drops a gem; it
    /// fades back into the pool, and everything it changed on the pooled enemy is put back, so the next enemy from that
    /// pool is ordinary.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class DeathsShadow : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color ShadowTint = new Color(0.16f, 0.1f, 0.22f, 1f);

        /// <summary>Seconds (game time) before it leaves. Tests may shorten it.</summary>
        public float RemainingSeconds { get; set; }
        /// <summary>Seconds (game time) left in the current trail or dash phase. Tests may shorten it.</summary>
        public float PhaseRemainingSeconds { get; set; }
        public bool IsLeaving { get; private set; }
        /// <summary>True while it closes in on the player; false while it trails at <see cref="TrailGap"/>.</summary>
        public bool IsDashing { get; private set; }
        /// <summary>What defeated it (None while it lives, or when it left on its own).</summary>
        public ShadowKiller DefeatedBy { get; private set; }
        /// <summary>The last Death's Shadow's killer and chest, for logs and tests (statics survive the component).</summary>
        public static ShadowKiller LastDefeatedBy { get; private set; }
        public static ChestTiers? LastChest { get; private set; }

        /// <summary>Centre-to-centre distance it keeps while trailing: its body radius + the clearance.</summary>
        public float TrailGap => (_enemy != null ? _enemy.CrowdRadius : 0.5f) + StageHazardTable.ShadowTrailClearance;

        private EnemyController _enemy;
        private HealthController _health;
        private TouchDamage _touch;
        private float _touchDamageBefore;
        private float _knockbackResistanceBefore;
        private float _attackDistanceBefore;
        private System.Func<bool> _preventDeathBefore;
        private System.Func<DamagePacket, bool> _interceptorBefore;
        private MaterialPropertyBlock _block;

        /// <summary>Turns a freshly spawned pooled enemy into Death's Shadow.</summary>
        /// <param name="curseAndModes">The Curse x run-mode speed factor (StageHazardTable.ShadowSpeedMultiplier).</param>
        public static DeathsShadow Begin(GameObject enemyObject, float lifetimeSeconds = StageHazardTable.ShadowLifetimeSeconds,
            int playerLevel = 1, float curseAndModes = 1f)
        {
            if (enemyObject == null) return null;
            var shadow = enemyObject.GetComponent<DeathsShadow>();
            if (shadow == null) shadow = enemyObject.AddComponent<DeathsShadow>();
            shadow.Setup(lifetimeSeconds, playerLevel, curseAndModes);
            return shadow;
        }

        private void Setup(float lifetimeSeconds, int playerLevel, float curseAndModes)
        {
            _enemy = GetComponent<EnemyController>();
            _health = GetComponent<HealthController>();
            _touch = GetComponent<TouchDamage>();
            RemainingSeconds = Mathf.Max(0.1f, lifetimeSeconds);
            IsLeaving = false;
            DefeatedBy = ShadowKiller.None;

            transform.localScale *= StageHazardTable.ShadowScale; // the spawner puts the pool's scale back on reuse
            if (_enemy != null)
            {
                float baseSpeed = _enemy.MoveSpeed / Mathf.Max(0.1f, _enemy.CampaignSpeedMultiplier * _enemy.LingeringSlow);
                _enemy.ApplyCampaignSpeed(StageHazardTable.ShadowSpeedMultiplier(baseSpeed, curseAndModes));
                _enemy.SetCastsShadows(true);
                _enemy.OverrideResistances(StageHazardTable.ShadowFreezeResistance,
                    StageHazardTable.ShadowInstantKillResistance, StageHazardTable.ShadowDebuffResistance);
                if (_enemy.Abilities != null) _enemy.Abilities.Stop(); // the boss model's skills are not the Stalker's
                _attackDistanceBefore = _enemy.AttackDistance;
            }
            if (_touch != null)
            {
                _touchDamageBefore = _touch.DamageAmount;
                // VS damage 50; the Endless cycle's +25% enemy damage applies to it like to every enemy.
                _touch.DamageAmount = Mathf.RoundToInt(StageHazardTable.ShadowContactDamage * SoulHunter.Gameplay.Data.RunModes.EnemyDamageMultiplier);
            }
            if (_health != null)
            {
                _knockbackResistanceBefore = _health.KnockbackResistance;
                _preventDeathBefore = _health.TryPreventDeath;
                _interceptorBefore = _health.DamageInterceptor;
                _health.Initialize(StageHazardTable.ShadowHealth(playerLevel)); // VS HP x Level (the spawner sets it again on reuse)
                _health.KnockbackResistance = 1f; // VS knockback 0
                _health.IsInvincible = false;     // hits must reach Intercept, which sorts killers from ordinary weapons
                _health.DamageInterceptor = Intercept;
                // Instant kills (KillSilently) skip every hit check and end in Die(); refusing the death there keeps it
                // standing without OnDied, so there is no gem and no kill either way.
                _health.TryPreventDeath = OnLethal;
            }
            IsDashing = false;
            PhaseRemainingSeconds = StageHazardTable.ShadowTrailSeconds;
            ApplyRhythm();
            Tint(true);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.BossWarningSound, 0.7f);
        }

        /// <summary>
        /// Every hit that reaches it (TakeDamage): a hit from a killer (Crimson Shroud's pulse, a rail cart) defeats it;
        /// any other hit is absorbed ("otherwise intangible"): no damage, number, flash or knockback.
        /// </summary>
        private bool Intercept(DamagePacket packet)
        {
            var killer = StageHazardTable.KillerOf(DamageSource.Weapon, DamageSource.Name);
            if (killer != ShadowKiller.None) Defeat(killer);
            return true;
        }

        /// <summary>Lethal instant kills (Pentagram, Eclipse Sigil, Soul Toll) and wipes arrive here.</summary>
        private bool OnLethal()
        {
            _health.ResetHealth(); // alive either way: no OnDied
            var killer = StageHazardTable.KillerOf(DamageSource.Weapon, DamageSource.Name);
            if (killer != ShadowKiller.None) Defeat(killer);
            return true;
        }

        /// <summary>Defeated by an instant kill: drops that killer's chest (none for Pentagram), then leaves.</summary>
        public void Defeat(ShadowKiller killer)
        {
            if (IsLeaving || killer == ShadowKiller.None) return;
            DefeatedBy = killer;
            var chest = StageHazardTable.ShadowChest(killer);
            LastDefeatedBy = killer;
            LastChest = chest;
            var pickups = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            if (chest.HasValue && pickups != null)
            {
                // The existing chest path (as EnemyDrop): the chest takes this config and its evolution look.
                var chestObject = pickups.SpawnChest(transform.position);
                var chestPickup = chestObject != null ? chestObject.GetComponent<SoulHunter.Gameplay.Pickups.ChestPickup>() : null;
                if (chestPickup != null) chestPickup.Tiers = chest.Value;
            }
            Debug.Log($"[DeathsShadow] Defeated by {killer}; chest: {(chest.HasValue ? chest.Value.ToString() : "none")}.");
            Leave();
        }

        private void Update()
        {
            if (IsLeaving) return;
            if (_health != null)
            {
                // Kept on every frame, like the old invincibility, in case something else took the hooks.
                _health.IsInvincible = false;
                if (_health.DamageInterceptor != (System.Func<DamagePacket, bool>)Intercept) _health.DamageInterceptor = Intercept;
                if (_health.TryPreventDeath != (System.Func<bool>)OnLethal) _health.TryPreventDeath = OnLethal;
            }
            float dt = Time.deltaTime;                         // game time: stops while paused
            PhaseRemainingSeconds -= dt;
            if (PhaseRemainingSeconds <= 0f)
            {
                IsDashing = !IsDashing;
                PhaseRemainingSeconds = IsDashing ? StageHazardTable.ShadowDashSeconds : StageHazardTable.ShadowTrailSeconds;
                ApplyRhythm();
            }
            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f) Leave();
        }

        /// <summary>
        /// The chase state stops at AttackDistance: while trailing that is the gap (it follows without touching); while
        /// dashing it is the enemy's own, so it runs into the player.
        /// </summary>
        private void ApplyRhythm()
        {
            if (_enemy == null) return;
            _enemy.AttackDistance = IsDashing ? _attackDistanceBefore : TrailGap;
        }

        /// <summary>Fades back into the pool: no drops, no kill.</summary>
        public void Leave()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up);
            gameObject.SetActive(false); // AutoReturnToPool takes it back; OnDisable restores the enemy
        }

        private void OnDisable()
        {
            Restore();
            Destroy(this);
        }

        private void Restore()
        {
            if (_touch != null) _touch.DamageAmount = _touchDamageBefore;
            if (_health != null)
            {
                _health.IsInvincible = false;
                _health.KnockbackResistance = _knockbackResistanceBefore;
                if (_health.TryPreventDeath == (System.Func<bool>)OnLethal) _health.TryPreventDeath = _preventDeathBefore;
                if (_health.DamageInterceptor == (System.Func<DamagePacket, bool>)Intercept) _health.DamageInterceptor = _interceptorBefore;
            }
            if (_enemy != null)
            {
                _enemy.SetCastsShadows(false);
                _enemy.AttackDistance = _attackDistanceBefore;
                _enemy.ClearResistanceOverrides();
            }
            Tint(false);
            _touch = null;
            _health = null;
            _enemy = null;
        }

        private void Tint(bool on)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            // Horde models darken through a shared shaded material (CPU audit, 2026-10-14): a property block would take the
            // body out of the SRP Batcher, and DamageFlash would draw its hit flash in the property block's colour.
            var flash = GetComponent<SoulHunter.Gameplay.VFX.DamageFlash>();
            if (flash != null) flash.SetShade(on ? ShadowTint : (Color?)null);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (flash != null && flash.ColoursItself(r)) continue;
                if (!on) { r.SetPropertyBlock(null); continue; }
                var m = r.sharedMaterial;
                if (m == null) continue;
                r.GetPropertyBlock(_block);
                if (m.HasProperty(BaseColorId)) _block.SetColor(BaseColorId, m.GetColor(BaseColorId) * ShadowTint);
                if (m.HasProperty(ColorId)) _block.SetColor(ColorId, m.GetColor(ColorId) * ShadowTint);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
