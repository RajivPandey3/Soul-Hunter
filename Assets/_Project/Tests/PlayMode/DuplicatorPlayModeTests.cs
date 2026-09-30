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
                var controller = enemy.GetComponent<EnemyController>();
                if (controller != null) controller.enabled = false;
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
                // King Bible keeps its books orbiting (built in Update), so the live books are counted instead.
                Assert.That(LiveBooks(), Is.EqualTo(booksBefore + 2), $"Two more books orbit ({booksBefore} before).");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
                yield break;
            }

            string detail = $"{type}: {without.Count} shots without, {with.Count} with Duplicator 2. With: " +
                            string.Join("; ", with.Select(s => s.ToString()));
            Debug.Log("[DuplicatorTest] " + detail + " | stacked pairs: " + StackedPairs(with));
            Assert.That(with.Count, Is.GreaterThanOrEqualTo(without.Count + 2), "Two more shots. " + detail);
            Assert.That(StackedPairs(with), Is.EqualTo(0), "Every extra shot is visible on its own, not on top of another. " + detail);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private struct Shot
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Forward;
            public float Time;
            public override string ToString() => $"{Name}@({Position.x:0.0},{Position.z:0.0}) dir({Forward.x:0.0},{Forward.z:0.0}) t{Time:0.00}";
        }

        /// <summary>Two shots that appeared together, at the same spot, flying the same way: they look like one.</summary>
        private static int StackedPairs(List<Shot> shots)
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
        private IEnumerator MeasureOneAttack(AutoAttackWeapon weapon, List<Shot> shots)
        {
            var timer = typeof(AutoAttackWeapon).GetField("_timer", Private);
            timer.SetValue(weapon, 999f);
            var active = new HashSet<GameObject>(LiveShots());
            float start = Time.time;
            typeof(AutoAttackWeapon).GetMethod("Attack", Private).Invoke(weapon, null);
            while (Time.time < start + 1f)
            {
                foreach (var go in LiveShots())
                    if (active.Add(go))
                    {
                        // Physics shots (Bone, Runetracer, Axe) fly by velocity, not by their facing.
                        var body = go.GetComponent<Rigidbody>();
                        Vector3 forward = body != null && !body.isKinematic && body.linearVelocity.sqrMagnitude > 0.01f ? body.linearVelocity.normalized : go.transform.forward;
                        shots.Add(new Shot { Name = go.name.Replace("(Clone)", "").Trim(), Position = go.transform.position, Forward = forward, Time = Time.time - start });
                    }
                active.RemoveWhere(g => g == null || !g.activeInHierarchy);
                timer.SetValue(weapon, 999f);
                yield return null;
            }
        }

        private static int LiveBooks() =>
            Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.gameObject.activeInHierarchy && t.name.StartsWith("SH10_Bible("));

        private static IEnumerable<GameObject> LiveShots()
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
