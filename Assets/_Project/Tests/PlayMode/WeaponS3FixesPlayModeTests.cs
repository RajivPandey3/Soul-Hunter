using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Full bug MIS, S3 weapon rows (2026-10-19). Each test fails on the code before its fix.
    /// </summary>
    public class WeaponS3FixesPlayModeTests : GameplayPlayModeTestBase
    {
        // ------------------------------------------------------------------ CMB-06 Axe lifetime

        [UnityTest]
        public IEnumerator AnAxeInFlight_ReturnsToThePool_WhenTheAxeWeaponIsSwitchedOff()
        {
            yield return Prepare();
            var axeWeapon = Give<AxeWeapon>(UpgradeType.Axe);
            yield return null;
            var thrown = LiveAxes();
            Assert.That(thrown.Count, Is.EqualTo(1), "The Axe throws one axe when gained.");
            // As when the Axe evolves into Death Spiral: the weapon's object is switched off with an axe still flying.
            axeWeapon.gameObject.SetActive(false);
            yield return new WaitForSeconds(AxeWeapon.AxeLifetimeSeconds + 0.5f);
            Assert.IsFalse(thrown[0].gameObject.activeSelf, "The axe went back to the pool after its 5 s, weapon off or not.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARecycledAxe_KeepsItsFullLife_WhateverItsEarlierThrowLeftRunning()
        {
            yield return Prepare();
            var axeWeapon = Give<AxeWeapon>(UpgradeType.Axe);
            yield return null;
            axeWeapon.enabled = false; // no throws of its own; the test throws
            var first = LiveAxes().Single();
            yield return new WaitForSeconds(0.4f);
            first.gameObject.SetActive(false); // used up by its pierce
            yield return new WaitForSeconds(3.6f);

            axeWeapon.enabled = true;
            axeWeapon.ResetCooldownAndFire();
            yield return null;
            axeWeapon.enabled = false;
            var second = LiveAxes().Single();
            Assert.AreSame(first, second, "The pool hands back the same axe.");
            float thrownAt = Time.time;

            yield return new WaitForSeconds(1.5f); // 5.5 s after the first throw, 1.5 s into this one
            Assert.IsTrue(second.gameObject.activeSelf, "The first throw's 5 s does not switch the re-thrown axe off.");
            while (Time.time < thrownAt + AxeWeapon.AxeLifetimeSeconds + 0.3f) yield return null;
            Assert.IsFalse(second.gameObject.activeSelf, "The re-thrown axe goes back after its own 5 s.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static List<AxePierce> LiveAxes() =>
            Object.FindObjectsByType<AxePierce>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();

        // ------------------------------------------------------------------ CMB-07 crowd buffers

        // More than the old buffers held (Garlic 200, Requiem 128), all inside the aura.
        private const int CrowdSize = 260;

        [UnityTest]
        public IEnumerator Garlic_HitsEveryEnemyOfABigCrowd()
        {
            yield return Prepare();
            var targets = SpawnTargets(CrowdSize, 2f);
            yield return null;
            var garlic = Give<GarlicWeapon>(UpgradeType.Garlic);
            garlic.ResetCooldownAndFire();
            yield return null;
            yield return null;
            int hit = targets.Count(t => t.GetComponent<HealthController>().CurrentHealth < 1000000);
            Assert.That(hit, Is.EqualTo(CrowdSize), "One Garlic pulse reaches the whole crowd inside its radius.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RequiemOfSouls_HitsEveryEnemyOfABigCrowd()
        {
            yield return Prepare();
            var targets = SpawnTargets(CrowdSize, 4.5f);
            yield return null;
            RequiemOfSoulsWeapon requiem = null;
            yield return Evolve("Evo_RequiemOfSouls", w => requiem = w as RequiemOfSoulsWeapon);
            Assert.IsNotNull(requiem, "Requiem of Souls is equipped.");
            requiem.ResetCooldownAndFire();
            yield return new WaitForSeconds(0.5f); // several 0.1 s pulses; each enemy's first hit lands at once
            int hit = targets.Count(t => t.GetComponent<HealthController>().CurrentHealth < 1000000);
            Assert.That(hit, Is.EqualTo(CrowdSize), "The wave reaches the whole crowd inside its radius.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ CMB-08 Bloody Tear heal

        // VS wiki Bloody_Tear: "Restores 8 HP/attack if it hits any enemies with a crit"; "a one second delay".
        private const float Crit = 0f, NoCrit = 0.99f; // fixed crit rolls (chance 10%)

        [UnityTest]
        public IEnumerator BloodyTear_DoesNotHeal_WhenItsCritLashesPassThroughGhosts()
        {
            yield return BloodyTearAmong(ghosts: true, Crit, (healed, damaged) =>
            {
                Assert.That(damaged, Is.EqualTo(0), "Normal damage passes through Level 7 ghosts.");
                Assert.That(healed, Is.EqualTo(0), "A crit that damages nothing heals nothing.");
            });
        }

        [UnityTest]
        public IEnumerator BloodyTear_DoesNotHeal_OnANonCritHit()
        {
            yield return BloodyTearAmong(ghosts: false, NoCrit, (healed, damaged) =>
            {
                Assert.That(damaged, Is.GreaterThan(0), "The lashes hit the enemies.");
                Assert.That(healed, Is.EqualTo(0), "Only a critical hit heals.");
            });
        }

        [UnityTest]
        public IEnumerator BloodyTear_HealsEightOnACrit_OnceASecond()
        {
            yield return BloodyTearAmong(ghosts: false, Crit, (healed, damaged) =>
            {
                Assert.That(damaged, Is.GreaterThan(0), "The lashes hit the enemies.");
                Assert.That(healed, Is.EqualTo(8), "Two crit lashes 0.1 s apart heal 8 once (the 1 s delay).");
            }, extraAttacks: true);
        }

        /// <param name="extraAttacks">Then a second attack 0.5 s after the first (inside the delay: no heal) and a third
        /// past the delay (8 more), checked after the first attack's check.</param>
        private IEnumerator BloodyTearAmong(bool ghosts, float critRoll, System.Action<int, int> check, bool extraAttacks = false)
        {
            yield return Prepare();
            var targets = SpawnTargets(12, 2.5f);
            if (ghosts) foreach (var t in targets) t.ConfigureDamageVulnerability(DamageType.Holy, 2f, true);
            yield return null;
            AutoAttackWeapon tear = null;
            yield return Evolve("Evo_BloodyTear", w => tear = w);
            Assert.IsInstanceOf<BloodyTearWeapon>(tear, "Bloody Tear is equipped.");
            tear.CritRollForTests = NoCrit;
            yield return new WaitForSeconds(1.2f); // the first attack's lashes and any heal delay are over
            tear.enabled = false;                  // before its next attack at 1.35 s
            var health = PlayerController.Instance.GetComponent<HealthController>();
            typeof(HealthController).GetField("_currentHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(health, 50);
            var before = targets.Select(t => t.GetComponent<HealthController>().CurrentHealth).ToList();
            tear.CritRollForTests = critRoll;
            tear.enabled = true;
            tear.ResetCooldownAndFire();
            yield return null;
            // One attack: two lashes 0.1 s apart, 0.3 s each; the next natural attack is 1.35 s away.
            yield return new WaitForSeconds(0.5f);
            int damaged = targets.Where((t, i) => t.GetComponent<HealthController>().CurrentHealth < before[i]).Count();
            check(health.CurrentHealth - 50, damaged);
            if (extraAttacks)
            {
                tear.ResetCooldownAndFire(); // about 0.5 s after the heal
                yield return new WaitForSeconds(0.45f);
                Assert.That(health.CurrentHealth - 50, Is.EqualTo(8), "A crit inside the 1 s delay does not heal again.");
                yield return new WaitForSeconds(0.3f);
                tear.ResetCooldownAndFire(); // past the delay
                yield return new WaitForSeconds(0.5f);
                Assert.That(health.CurrentHealth - 50, Is.EqualTo(16), "Past the delay a crit heals 8 again.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ CMB-09 Peachone restart

        [UnityTest]
        public IEnumerator Peachone_UpgradedDuringAnActivation_RestartsIt_InsteadOfRunningTwo()
        {
            yield return Prepare();
            var peachone = Give<PeachoneWeapon>(UpgradeType.Peachone);
            yield return new WaitForSeconds(1.5f); // the gain's activation is half way through its 4 s
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(UpgradeType.Peachone).GetLevel(2));
            yield return null;
            yield return null; // the upgrade fires it at once: a new activation, its first set now
            int sets = peachone.SetsStarted;
            yield return new WaitForSeconds(2.2f);
            Assert.That(peachone.SetsStarted - sets, Is.EqualTo(2),
                "One activation runs: a set every second (two activations made four).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ CMB-11 Holy Wand

        [UnityTest]
        public IEnumerator HolyWand_FiresItsVsVolley_FourBolts0Point1SecondsApart_At30Damage()
        {
            yield return Prepare();
            FarTargets();
            AutoAttackWeapon wand = null;
            yield return Evolve("Evo_HolyWand", w => wand = w);
            Assert.IsInstanceOf<HolyWandWeapon>(wand, "Holy Wand is equipped.");
            Assert.That(wand.DamageAmount, Is.EqualTo(30f), "wiki Holy_Wand: damage 30 (DamageAmount, read by the Arcana zones).");
            Assert.That(wand.AttackCooldown, Is.EqualTo(0.5f), "wiki Holy_Wand: cooldown 0.5 s.");
            var shots = new List<DuplicatorPlayModeTests.Shot>();
            yield return DuplicatorPlayModeTests.MeasureOneAttack(wand, shots, 0.45f);
            string detail = string.Join("; ", shots.Select(s => s.ToString()));
            Assert.That(shots.Count, Is.EqualTo(HolyWandWeapon.VsAmount), "Four bolts (wiki amount 4). " + detail);
            for (int i = 1; i < shots.Count; i++)
                Assert.That(shots[i].Time - shots[i - 1].Time, Is.GreaterThan(0.08f), "One after another, 0.1 s apart. " + detail);
            // Owner 2026-10-19 "SH = VS": infobox "pierce = 2", Tips "even with the extra pierce".
            var hits = typeof(Projectile).GetField("_remainingHits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var bolts = Projectile.ActiveShots.Where(p => p != null && p.SourceWeapon == wand).ToList();
            Assert.That(bolts, Is.Not.Empty, "Bolts still in flight.");
            Assert.That(bolts.All(p => (int)hits.GetValue(p) == HolyWandWeapon.VsPierce), Is.True, "Each bolt pierces 2 enemies.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>Three targets 25 units out: aimed weapons have something to fire at, and no shot hits within a second.</summary>
        private static void FarTargets()
        {
            foreach (var target in SpawnTargets(3, 1f)) target.transform.position += Vector3.right * 25f;
        }

        // ------------------------------------------------------------------ CMB-12 duplicate WeaponManager

        [UnityTest]
        public IEnumerator ASecondWeaponManager_IsDestroyed_AndTheFirstStays()
        {
            yield return Prepare();
            var first = WeaponManager.Instance;
            Assert.IsNotNull(first);
            var duplicate = new GameObject("DuplicateWeaponManager");
            duplicate.AddComponent<WeaponManager>();
            yield return null;
            yield return null;
            Assert.IsTrue(duplicate == null, "The duplicate manager's object is destroyed.");
            Assert.AreSame(first, WeaponManager.Instance, "The first manager stays the instance.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ CMB-13 FireInSuccession without allocations

        [UnityTest]
        public IEnumerator FireInSuccession_KeepsItsTiming_FirstShotNow_ThenOneEveryInterval_HeldByAPause()
        {
            var probe = new GameObject("SuccessionProbe").AddComponent<SuccessionProbeWeapon>();
            yield return null;
            probe.Volley(5, 0.1f, new Vector3(1f, 0f, 2f));
            Assert.That(probe.Indices, Is.EqualTo(new[] { 0 }), "Shot 0 leaves at once.");
            float start = Time.time;
            while (probe.Indices.Count < 3 && Time.time < start + 2f) yield return null;
            // A pause holds the rest, as WaitForSeconds did.
            var owner = new object();
            SoulHunter.Core.Services.GameTime.Pause(owner);
            int held = probe.Indices.Count;
            float until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.That(probe.Indices.Count, Is.EqualTo(held), "No shot while the game is paused.");
            SoulHunter.Core.Services.GameTime.Resume(owner);
            while (probe.Indices.Count < 5 && Time.time < start + 3f) yield return null;
            yield return new WaitForSeconds(0.3f);

            Assert.That(probe.Indices, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }), "Every shot once, in order.");
            for (int i = 1; i < probe.Times.Count; i++)
                Assert.That(probe.Times[i] - probe.Times[i - 1], Is.InRange(0.0999f, 0.35f), $"Shot {i}: one interval of game time after the last.");
            Assert.That(probe.Args.All(a => a == new Vector3(1f, 0f, 2f)), "Each shot gets the volley's argument.");
            Assert.That(probe.Counts.All(c => c == 5), "Each shot gets the volley's count.");
            Assert.That(SuccessionRunner.Pending, Is.EqualTo(0), "The finished volley is gone.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FireInSuccession_StopsWhenTheWeaponIsSwitchedOff_AndOverlappingVolleysBothFinish()
        {
            var probe = new GameObject("SuccessionProbe").AddComponent<SuccessionProbeWeapon>();
            yield return null;
            probe.Volley(3, 0.1f, Vector3.zero);
            yield return null;
            probe.Volley(3, 0.1f, Vector3.one); // a new volley before the last one ended (VS Holy Wand tips)
            yield return new WaitForSeconds(0.5f);
            Assert.That(probe.Indices.Count, Is.EqualTo(6), "Two overlapping volleys of three both finish.");

            probe.Clear();
            probe.Volley(4, 0.1f, Vector3.zero);
            probe.enabled = false;
            yield return new WaitForSeconds(0.5f);
            Assert.That(probe.Indices, Is.EqualTo(new[] { 0 }), "A switched-off weapon fires no more of its volley.");
            Assert.That(SuccessionRunner.Pending, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FireInSuccession_AllocatesNothing_OnAnAttack()
        {
            var probe = new GameObject("SuccessionProbe").AddComponent<SuccessionProbeWeapon>();
            yield return null;
            probe.Volley(4, 0.05f, Vector3.zero); // warm up: the runner and its list are made once
            yield return new WaitForSeconds(0.4f);
            probe.Clear();
            Assert.That(() => probe.Volley(4, 0.05f, Vector3.forward), UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(UnityEngine.TestTools.Constraints.Is.Not),
                "An attack of four shots allocated (a closure, an iterator and a WaitForSeconds per shot before CMB-13).");
            yield return new WaitForSeconds(0.4f);
            Assert.That(probe.Indices.Count, Is.EqualTo(4));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Boots a quiet run: no spawner, an invincible hero, no weapons, no enemies and no gems (no level-ups).</summary>
        private IEnumerator Prepare()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>()) existing.gameObject.SetActive(false);
            foreach (var other in EnemyController.ActiveEnemies.ToList()) if (other != null) other.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
        }

        private static T Give<T>(UpgradeType type) where T : AutoAttackWeapon
        {
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(type).GetLevel(1));
            var weapon = PlayerController.Instance.GetComponentsInChildren<T>().FirstOrDefault();
            Assert.IsNotNull(weapon, $"{typeof(T).Name} is equipped.");
            return weapon;
        }

        private IEnumerator Evolve(string evolutionAsset, System.Action<AutoAttackWeapon> found)
        {
            var weapons = WeaponManager.Instance;
            var evolution = GetPrivateField<List<WeaponEvolutionData>>(weapons, "_availableEvolutions")
                .FirstOrDefault(e => e != null && e.name == evolutionAsset);
            Assert.IsNotNull(evolution, $"{evolutionAsset} is one of WeaponManager's evolutions.");
            weapons.GiveWeapon(evolution.BaseWeapon, evolution.BaseWeaponMaxLevel);
            weapons.GiveWeapon(evolution.RequiredPassive, Mathf.Max(1, evolution.RequiredPassiveMaxLevel));
            yield return null;
            Assert.IsTrue(weapons.TryEvolveWeapon(out string evolved), $"{evolutionAsset} evolves.");
            yield return null;
            var weapon = weapons.GetEquippedWeapons().Select(p => p.Value).LastOrDefault();
            foreach (var other in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>())
                if (other != weapon) other.gameObject.SetActive(false);
            found(weapon);
        }

        /// <summary>Unkillable, still targets of this stage's enemy type around the hero, within <paramref name="radius"/>.</summary>
        private static List<EnemyController> SpawnTargets(int count, float radius, int health = 1000000)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var centre = PlayerController.Instance.transform.position;
            var list = new List<EnemyController>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.5f; // golden angle: an even disc
                float distance = radius * Mathf.Sqrt((i + 0.5f) / count);
                var enemy = Object.Instantiate(enemyPrefab, centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance, Quaternion.identity);
                enemy.GetComponent<HealthController>().Initialize(health);
                var controller = enemy.GetComponent<EnemyController>();
                controller.IsStageBoss = true; // immune to instant kills, so nothing erases them
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var body = enemy.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                list.Add(controller);
            }
            return list;
        }
    }

    /// <summary>A weapon that only records the shots FireInSuccession gives it (CMB-13 tests).</summary>
    public class SuccessionProbeWeapon : AutoAttackWeapon
    {
        public readonly List<int> Indices = new List<int>(32);
        public readonly List<int> Counts = new List<int>(32);
        public readonly List<float> Times = new List<float>(32);
        public readonly List<Vector3> Args = new List<Vector3>(32);
        private SuccessionShot _shot;

        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.None;
        protected override void Attack() { }

        public void Volley(int count, float interval, Vector3 arg) => FireInSuccession(count, interval, _shot ??= Shot, arg);

        public void Clear() { Indices.Clear(); Counts.Clear(); Times.Clear(); Args.Clear(); }

        private void Shot(int index, int count, Vector3 arg)
        {
            Indices.Add(index);
            Counts.Add(count);
            Times.Add(Time.time);
            Args.Add(arg);
        }
    }
}
