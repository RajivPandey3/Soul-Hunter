using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;
using SoulHunter.Gameplay.Weapons;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report (2026-09-30): "Duplicator kaam nahi kar raha". vampire.survivors.wiki/w/Duplicator: +1 Amount per
    /// level, 2 levels. Every weapon the wiki marks "Affected by Amount" must fire that many more, and the extra shots
    /// must be visible as separate shots.
    /// </summary>
    public class DuplicatorPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        // Wiki infobox "Affected by Amount" (Garlic, Song of Mana and Clock Lancet ignore Amount; Pentagram and Laurel
        // only with an Arcana).
        private static IEnumerable<UpgradeType> AmountWeapons() => new[]
        {
            UpgradeType.Whip, UpgradeType.MagicWand, UpgradeType.Knife, UpgradeType.Axe, UpgradeType.Cross, UpgradeType.Bible,
            UpgradeType.FireWand, UpgradeType.SantaWater, UpgradeType.Runetracer, UpgradeType.LightningRing, UpgradeType.Peachone,
            UpgradeType.Bone, UpgradeType.CherryBomb, UpgradeType.Gun,
        };

        [UnityTest]
        public IEnumerator DuplicatorLevel2_AddsTwoVisibleShots([ValueSource(nameof(AmountWeapons))] UpgradeType type)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>()) existing.gameObject.SetActive(false);
            // Level 1 opens with 1-5 HP bats (VS Mad Forest, 2026-10-03): killing them would level the hero, and the paused
            // level-up screen would stop the game-time waits below. Only the unkillable targets stay.
            foreach (var other in EnemyController.ActiveEnemies.ToList()) if (other != null) other.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<SoulHunter.Gameplay.Pickups.XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            yield return null;

            // Targets all round, too tough to die, harmless.
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            for (int i = 0; i < 12; i++)
            {
                var enemy = Object.Instantiate(enemyPrefab, player.transform.position + Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward * 4f, Quaternion.identity);
                enemy.GetComponent<HealthController>().Initialize(1000000);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var body = enemy.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                // The controller stays on: Lightning Ring and Fire Wand pick their targets from the live enemy list.
            }

            var weapons = WeaponManager.Instance;
            weapons.GiveWeapon(type, 1);
            yield return null;
            // Kael already holds a Whip (switched off above): giving the Whip levels that one, so switch it back on.
            string weaponName = weapons.GetWeaponObject(type).name;
            var weapon = player.GetComponentsInChildren<AutoAttackWeapon>(true).FirstOrDefault(w => w.gameObject.name.StartsWith(weaponName));
            if (weapon != null) weapon.gameObject.SetActive(true);
            Assert.IsNotNull(weapon, $"{type} is equipped.");
            yield return new WaitForSeconds(1.5f); // the shot fired on equip dies down

            // King Bible rests between cycles (VS, 2026-10-01): bring its books out before counting them.
            if (type == UpgradeType.Bible) { weapon.ResetCooldownAndFire(); yield return null; yield return null; }
            int booksBefore = LiveBooks();
            var without = new List<Shot>();
            yield return MeasureOneAttack(weapon, without);

            var dup = ScriptableObject.CreateInstance<UpgradeData>();
            dup.Type = UpgradeType.Duplicator;
            try
            {
                dup.Level = 1; weapons.ApplyUpgrade(dup);
                dup.Level = 2; weapons.ApplyUpgrade(dup);
            }
            finally { Object.Destroy(dup); }
            Assert.That(player.GetComponent<PlayerStats>().Amount, Is.EqualTo(2), "Duplicator level 2 gives +2 Amount.");
            yield return new WaitForSeconds(1.5f);

            var with = new List<Shot>();
            yield return MeasureOneAttack(weapon, with);

            if (type == UpgradeType.Bible)
            {
                weapon.ResetCooldownAndFire();
                yield return null;
                yield return null;
                // King Bible keeps its books orbiting (built in Update), so the live books are counted instead.
                Assert.That(LiveBooks(), Is.EqualTo(booksBefore + 2), $"Two more books orbit ({booksBefore} before).");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
                yield break;
            }

            if (type == UpgradeType.CherryBomb)
            {
                // Since 2026-10-01 a bomb explodes only by chance (VS): explosions of earlier bombs can fall in the window,
                // so only the thrown bombs are counted.
                without.RemoveAll(shot => !shot.Name.Contains("CherryBomb"));
                with.RemoveAll(shot => !shot.Name.Contains("CherryBomb"));
            }
            string detail = $"{type}: {without.Count} shots without, {with.Count} with Duplicator 2. With: " +
                            string.Join("; ", with.Select(s => s.ToString()));
            Debug.Log("[DuplicatorTest] " + detail + " | stacked pairs: " + StackedPairs(with));
            Assert.That(with.Count, Is.GreaterThanOrEqualTo(without.Count + 2), "Two more shots. " + detail);
            Assert.That(StackedPairs(with), Is.EqualTo(0), "Every extra shot is visible on its own, not on top of another. " + detail);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        internal struct Shot
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Forward;
            public float Time;
            public override string ToString() => $"{Name}@({Position.x:0.0},{Position.z:0.0}) dir({Forward.x:0.0},{Forward.z:0.0}) t{Time:0.00}";
        }

        /// <summary>Two shots that appeared together, at the same spot, flying the same way: they look like one.</summary>
        internal static int StackedPairs(List<Shot> shots)
        {
            int pairs = 0;
            for (int i = 0; i < shots.Count; i++)
                for (int j = i + 1; j < shots.Count; j++)
                    if (Mathf.Abs(shots[i].Time - shots[j].Time) < 0.02f && (shots[i].Position - shots[j].Position).sqrMagnitude < 0.01f &&
                        Vector3.Angle(shots[i].Forward, shots[j].Forward) < 2f && shots[i].Name == shots[j].Name)
                        pairs++;
            return pairs;
        }

        /// <summary>Fires one attack by hand (the weapon's own timer held back) and records every weapon object that appears in the next second.</summary>
        internal static IEnumerator MeasureOneAttack(AutoAttackWeapon weapon, List<Shot> shots, float window = 1f)
        {
            var timer = typeof(AutoAttackWeapon).GetField("_timer", Private);
            timer.SetValue(weapon, 999f);
            // A natural volley may still be firing its shots one after another (FireInSuccession): let it finish first,
            // or its last shots fall into this window (Thousand Edge counted 10 for one 6-knife volley, 2026-10-04).
            float settle = Time.time + 0.6f;
            while (Time.time < settle) { timer.SetValue(weapon, 999f); yield return null; }
            var active = new HashSet<GameObject>(LiveShots());
            // A pooled object reused in the same frame it ends never looks inactive (Bloody Tear's 0.3 s lash is reused by
            // the lash fired 0.3 s later): an object that jumps to a new spot in one frame counts as a new shot.
            var lastSeen = new Dictionary<GameObject, Vector3>();
            foreach (var go in active) lastSeen[go] = go.transform.position;
            float start = Time.time;
            if (weapon is MagicWandWeapon)
            {
                // The Magic Wand runs its own volley cycle in Update (1.2 s cooldown, shots 0.1 s apart) rather than
                // Attack(): start a volley now through it, so the window always holds exactly one volley.
                typeof(MagicWandWeapon).GetField("_remainingShots", Private).SetValue(weapon, 0);
                typeof(MagicWandWeapon).GetField("_cooldownTimer", Private).SetValue(weapon, 0f);
            }
            else if (weapon is PeachoneWeapon)
            {
                // Peachone bombards in sets over its 4 s Duration (VS, 2026-10-01): stop the running activation and fire
                // exactly one set, so the window holds one set.
                weapon.StopAllCoroutines();
                weapon.StartCoroutine((IEnumerator)typeof(PeachoneWeapon).GetMethod("BombingRun", Private).Invoke(weapon, null));
            }
            else typeof(AutoAttackWeapon).GetMethod("Attack", Private).Invoke(weapon, null);
            while (Time.time < start + window)
            {
                foreach (var go in LiveShots())
                {
                    // A reused pooled shot jumps back toward the holder. A fast shot in a long frame also moves over 1.5
                    // units, but away from the holder, and was counted again (Thousand Edge 6 counted as 18, 2026-10-06).
                    Vector3 origin = weapon.transform.position;
                    bool jumped = lastSeen.TryGetValue(go, out var was) && (go.transform.position - was).sqrMagnitude > 1.5f * 1.5f
                        && (go.transform.position - origin).sqrMagnitude < (was - origin).sqrMagnitude;
                    lastSeen[go] = go.transform.position;
                    if (active.Add(go) || jumped)
                    {
                        // Physics shots (Bone, Runetracer, Axe) fly by velocity, not by their facing.
                        var body = go.GetComponent<Rigidbody>();
                        Vector3 forward = body != null && !body.isKinematic && body.linearVelocity.sqrMagnitude > 0.01f ? body.linearVelocity.normalized : go.transform.forward;
                        shots.Add(new Shot { Name = go.name.Replace("(Clone)", "").Trim(), Position = go.transform.position, Forward = forward, Time = Time.time - start });
                    }
                }
                active.RemoveWhere(g => g == null || !g.activeInHierarchy);
                timer.SetValue(weapon, 999f);
                if (weapon is MagicWandWeapon && Time.time > start + 0.5f)
                    typeof(MagicWandWeapon).GetField("_cooldownTimer", Private).SetValue(weapon, 999f); // no second volley in the window
                yield return null;
            }
        }

        internal static int LiveBooks() =>
            Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.gameObject.activeInHierarchy && t.name.StartsWith("SH10_Bible("));

        internal static IEnumerable<GameObject> LiveShots()
        {
            var found = new HashSet<GameObject>();
            void Add(Component c)
            {
                if (c == null || !c.gameObject.activeInHierarchy || c.GetComponentInParent<EnemyController>() != null) return;
                if (c.GetComponentInParent<PlayerController>() != null && c.GetComponent<AutoAttackWeapon>() != null) return;
                found.Add(c.gameObject);
            }
            foreach (var c in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Add(c);
            foreach (var c in Object.FindObjectsByType<ProjectileDamage>(FindObjectsSortMode.None)) Add(c);
            foreach (var c in Object.FindObjectsByType<TouchDamage>(FindObjectsSortMode.None)) Add(c);
            if (VFXPoolManager.Instance != null)
                foreach (var go in VFXPoolManager.Instance.Playing(VfxKind.Slash)) if (go != null && go.activeInHierarchy) found.Add(go);
            return found;
        }
    }
}
