using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Eclipse Sigil: Soul Hunter's evolution of the Pentagram (owner's name, 2026-10-01), built on VS Gorgeous Moon
    /// (vampire.survivors.wiki/w/Gorgeous_Moon, fetched 2026-10-01): Pentagram at max level + Crown. 60 s cooldown,
    /// "Ignores everything but Cooldown", and it "no longer destroys pickups". Three phases after each cooldown:
    /// 1. Generation, 3 s in: 20 beams at the closest enemies (8 at 0.75 s, 4 at 0.375 s, 8 at 0.2 s apart), each
    ///    erasing its target and leaving an extra 1-XP gem next to its usual drop.
    /// 2. Erasure, 12.1 s in: every enemy on screen is erased like Pentagram (stage bosses and instant-kill-resistant
    ///    enemies are not), with 0% chance of destroying items, and each drops an extra gem.
    /// 3. Vacuum, right after: every gem on the level flies to the player (like the Vacuum).
    /// The glyph is the Pentagram's approved effect; the beams use the hit spark. A boss-held copy only shows its glyph.
    /// </summary>
    public class EclipseSigilWeapon : AutoAttackWeapon
    {
        protected override bool ShowsAttackPose => false;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;

        public GameObject GlyphPrefab;

        public const float VsCooldown = 60f;
        public const float GenerationDelay = 3f;
        public const float ErasureAt = 12.1f;
        public const int ExtraGemXp = 1;
        /// <summary>The beams' gaps (8 x 0.75 s, 4 x 0.375 s, 8 x 0.2 s: 20 beams).</summary>
        public static readonly float[] BeamGaps = BuildGaps();

        private static float[] BuildGaps()
        {
            var gaps = new List<float>();
            for (int i = 0; i < 8; i++) gaps.Add(0.75f);
            for (int i = 0; i < 4; i++) gaps.Add(0.375f);
            for (int i = 0; i < 8; i++) gaps.Add(0.2f);
            return gaps.ToArray();
        }

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        /// <summary>Phase counters (tests).</summary>
        public int BeamsFired { get; private set; }
        public int Erasures { get; private set; }
        public int Vacuums { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            StopAllCoroutines();
            StartCoroutine(Cycle());
        }

        private IEnumerator Cycle()
        {
            yield return new WaitForSeconds(GenerationDelay);
            ShowGlyph(ErasureAt - GenerationDelay);
            if (HeldByEnemy) yield break; // Shadow Kael's copy: its glyph only (owner decision on mirrored Pentagram)
            float elapsed = GenerationDelay;
            foreach (float gap in BeamGaps)
            {
                Beam();
                yield return new WaitForSeconds(gap);
                elapsed += gap;
            }
            if (elapsed < ErasureAt) yield return new WaitForSeconds(ErasureAt - elapsed);
            Erase();
            yield return null;
            Vacuum();
        }

        private void ShowGlyph(float seconds)
        {
            if (GlyphPrefab == null) return;
            GameObject glyph = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(GlyphPrefab, transform.position, Quaternion.identity)
                : Instantiate(GlyphPrefab, transform.position, Quaternion.identity);
            if (glyph == null) return;
            var lifetime = glyph.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(seconds);
            else Destroy(glyph, seconds);
        }

        private void Beam()
        {
            BeamsFired++;
            EnemyController target = null;
            float best = float.MaxValue;
            foreach (var enemy in EnemyController.ActiveEnemies)
            {
                if (!Erasable(enemy)) continue;
                float d = (enemy.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; target = enemy; }
            }
            if (target == null) return;
            Vector3 at = target.transform.position;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, at);
            EraseOne(target);
        }

        private void Erase()
        {
            Erasures++;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var enemy = enemies[i];
                if (Erasable(enemy) && OnScreen(enemy.transform.position)) EraseOne(enemy);
            }
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, transform.position);
        }

        private void Vacuum()
        {
            Vacuums++;
            var gems = SoulHunter.Gameplay.Pickups.XPGem.ActiveGems;
            var holder = SoulHunter.Gameplay.Player.PlayerController.Instance;
            if (holder == null) return;
            var player = holder.transform;
            for (int i = gems.Count - 1; i >= 0; i--)
                if (gems[i] != null && gems[i].gameObject.activeInHierarchy) gems[i].Magnetize(player);
        }

        private static bool Erasable(EnemyController enemy) =>
            enemy != null && enemy.isActiveAndEnabled && !enemy.IsStageBoss && !enemy.ResistsInstantKill;

        private static bool OnScreen(Vector3 position)
        {
            var camera = Camera.main;
            if (camera == null) return true;
            Vector3 v = camera.WorldToViewportPoint(position);
            return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }

        /// <summary>Erases an enemy (its usual drop stays) and leaves an extra 1-XP gem next to it.</summary>
        private static void EraseOne(EnemyController enemy)
        {
            Vector3 at = enemy.transform.position;
            var health = enemy.GetComponent<HealthController>();
            if (health != null) health.KillSilently();
            var pickups = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            if (pickups != null) pickups.SpawnGem(at + new Vector3(0.4f, 0f, 0.4f), ExtraGemXp);
        }
    }
}
