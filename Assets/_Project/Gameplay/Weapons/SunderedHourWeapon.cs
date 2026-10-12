using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Sundered Hour: Soul Hunter's evolution of the Clock Lancet (own name, 2026-10-12; not yet owner-approved), built
    /// on VS Infinite Corridor (vampire.survivors.wiki/w/Infinite_Corridor, raw, fetched 2026-10-12). Recipe: Clock
    /// Lancet with "Silver Ring and Gold Ring at max level" (wiki Evolution: "In the case of Infinite Corridor ... all
    /// related passive items must also be fully leveled").
    /// Infobox: duration 6.0 s, cooldown 1.0 s, knockback None, "Ignores Might, Speed, Amount, and Area".
    /// "Infinite Corridor fires a white beam around the player in twelve directions clockwise one at a time. The beam
    /// does no damage, but will freeze any enemy it hits. After the freezing beam has completed one full rotation and it
    /// fires upwards in the 12 o'clock direction, it creates a spinning, shining rainbow aura around the player, halving
    /// the health of every enemy on screen." So one beam a second, and with every twelfth-plus-one beam (the one that
    /// begins the next rotation) every enemy on screen loses half its current health. The pulse works on bosses (the
    /// page: "works on the Reaper") and defeats Death's Shadow (The_Stalker; StageHazardTable.KillerOf).
    ///
    /// Owner decision kept (2026-09-24, weapon aiming; the Clock Lancet is on its list): each beam aims at the nearest
    /// enemy and falls back to the clockwise sweep when there is none. The count of twelve beams per pulse is the wiki's.
    /// The freeze chance is the Clock Lancet's (the page gives none). The pulse deals Spectral damage so that Level 7's
    /// ghosts lose their half too (the wiki's "every enemy on screen"). Soul Hunter's own: the 20-unit beam (the Clock
    /// Lancet's, not scaled by Area) and the look (the Clock Lancet's beam and the approved magic circle; not
    /// owner-approved art).
    /// Shadow Kael's copy: the beam slows the player as his Clock Lancet does (owner, 2026-09-30); it never halves the
    /// player's health (VS has no such effect on a player) and never touches his horde.
    /// </summary>
    public class SunderedHourWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        public override float Knockback => 0f; // VS wiki Infinite Corridor: knockback None

        public GameObject FreezeBeamPrefab;

        public const float VsCooldown = 1f;
        public const float VsFreezeDuration = 6f;
        public const int BeamsPerRotation = 12;
        public const float BeamRange = 20f;

        /// <summary>VS wiki (Infinite_Corridor, Limit Break): only "Duration +100ms", up to 10 seconds.</summary>
        public override float LimitBreakBaseDuration => VsFreezeDuration;
        // The Clock Lancet it replaces has 7 levels; the evolution has no levels of its own.
        public override int MaxLevel => 7;
        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        /// <summary>Beams fired, and health-halving pulses sent, since this weapon was made (tests).</summary>
        public int BeamsFired { get; private set; }
        public int Pulses { get; private set; }

        private readonly RaycastHit[] _hits = new RaycastHit[128];
        private static readonly List<EnemyController> PulseBuffer = new List<EnemyController>();

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = 0f;
            AttackCooldown = VsCooldown;
        }

        /// <summary>True when beam number <paramref name="beamIndex"/> (0-based) begins a rotation after a full one.</summary>
        public static bool BeginsNewRotation(int beamIndex) => beamIndex > 0 && beamIndex % BeamsPerRotation == 0;

        /// <summary>Half of an enemy's current health, rounded down (1 health is left alone).</summary>
        public static int HalvingDamage(int currentHealth) => Mathf.Max(0, currentHealth) / 2;

        protected override void Attack()
        {
            int beam = BeamsFired;
            BeamsFired++;
            // Twelve clock positions, clockwise from 12 o'clock (up the screen).
            float angle = (beam % BeamsPerRotation) * (360f / BeamsPerRotation);
            Vector3 direction = AimDirection(Quaternion.Euler(0f, angle, 0f) * Vector3.forward);

            if (FreezeBeamPrefab != null)
            {
                GameObject visual = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction))
                    : Instantiate(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction));
                if (visual != null)
                {
                    visual.transform.localScale = FreezeBeamPrefab.transform.localScale; // ignores Area
                    var lifetime = visual.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(0.5f);
                    else Destroy(visual, 0.5f);
                }
            }

            int count = UnityEngine.Physics.RaycastNonAlloc(transform.position, direction, _hits, BeamRange);
            float freezeSeconds = VsFreezeDuration * DurationMultiplier;

            if (HeldByEnemy)
            {
                for (int i = 0; i < count; i++)
                {
                    var player = _hits[i].collider.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>();
                    if (player == null) continue;
                    player.Slow(SoulHunter.Gameplay.Player.PlayerController.ClockLancetSlowFactor, freezeSeconds);
                    break;
                }
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var enemy = _hits[i].collider.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.TryWeaponFreeze(ClockLancetWeapon.VsFreezeChance, freezeSeconds);
            }

            if (BeginsNewRotation(beam)) HalveEveryEnemyOnScreen();
        }

        /// <summary>The pulse: every enemy on screen takes half its current health as damage (also called by tests).</summary>
        public void HalveEveryEnemyOnScreen()
        {
            if (HeldByEnemy) return;
            Pulses++;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position);
            // A snapshot: defeating Death's Shadow takes it off the live list while it is being read.
            PulseBuffer.Clear();
            var enemies = EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null && enemies[i].isActiveAndEnabled) PulseBuffer.Add(enemies[i]);
            foreach (var enemy in PulseBuffer)
            {
                if (enemy == null || !OnScreen(enemy.transform.position)) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                int half = HalvingDamage(health.CurrentHealth);
                if (half <= 0) continue;
                Hit(health, new DamagePacket(half, enemy.transform.position, Vector3.zero, DamageType.Spectral).WithKnockback(0f));
            }
            PulseBuffer.Clear();
        }

        private static bool OnScreen(Vector3 position)
        {
            var camera = Camera.main;
            if (camera == null) return true;
            Vector3 v = camera.WorldToViewportPoint(position);
            return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }
    }
}
