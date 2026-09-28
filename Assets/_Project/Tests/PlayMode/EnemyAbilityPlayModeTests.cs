using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The data-driven enemy abilities in the real game (VS pages cited in EnemyAbilityDefaults): a spawned Stage 1
    /// enemy gets one ability through a runtime copy of its EnemyData (ScriptableObject.Instantiate, so no asset on
    /// disk changes) and must summon (capped), shoot, lunge, shield, self-destruct, lay a hazard zone and halve the
    /// player's health. Pause and freeze stop abilities. Plays on the base class's throw-away save.
    /// </summary>
    public class EnemyAbilityPlayModeTests : GameplayPlayModeTestBase
    {
        private EnemySpawner _spawner;
        private readonly List<Object> _copies = new List<Object>();

        [TearDown]
        public void DestroyCopies()
        {
            foreach (var copy in _copies) if (copy != null) Object.Destroy(copy);
            _copies.Clear();
        }

        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            health.IsInvincible = false;
            health.Initialize(1000);
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        /// <summary>A Stage 1 enemy <paramref name="distance"/> to the right of the player, harmless to touch, with one ability.</summary>
        private EnemyController Spawn(float distance, EnemyAbilitySpec spec, bool hold)
        {
            var player = PlayerController.Instance;
            Vector3 at = player.transform.position + Vector3.right * distance;
            var enemy = Object.Instantiate(_spawner.StageEnemyPrefab, at, Quaternion.identity).GetComponent<EnemyController>();
            _copies.Add(enemy.gameObject);
            enemy.Target = player.transform;
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.DamageAmount = 0;
            Assert.IsNotNull(enemy.Data, "The stage enemy prefab has EnemyData.");
            var data = Object.Instantiate(enemy.Data);
            _copies.Add(data);
            data.Abilities = new List<EnemyAbilitySpec> { spec };
            enemy.UseData(data);
            if (hold) enemy.ChangeState(new EnemyHoldState(enemy));
            Assert.IsNotNull(enemy.Abilities, "The enemy runs its abilities.");
            Assert.That(enemy.Abilities.IsRunning, Is.True);
            return enemy;
        }

        private static int PlayerHp => PlayerController.Instance.GetComponent<HealthController>().CurrentHealth;

        private static float Gap(EnemyController enemy)
        {
            Vector3 d = enemy.transform.position - PlayerController.Instance.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private static IEnumerator Seconds(float s)
        {
            float until = Time.time + s;
            while (Time.time < until) yield return null;
        }

        private IEnumerator Until(System.Func<bool> condition, float seconds, string what)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"Timed out waiting for {what}. " + Errors());
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Summon_CallsEnemiesAroundThePlayer_UpToTheCap()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.Summon) { Count = 6, SpawnBursts = 1, IntervalSeconds = 0.3f, MaxAlive = 8, Radius = 7f };
            var summoner = Spawn(10f, spec, hold: true);
            yield return Seconds(2f);
            var abilities = summoner.Abilities;
            Assert.That(abilities.SummonsAlive, Is.EqualTo(8), "Summoned up to the cap.");
            int others = EnemyController.ActiveEnemies.Count(e => e != null && e != summoner);
            Assert.That(others, Is.EqualTo(8), "Exactly the capped number of minions are alive.");

            // A minion leaves (back to the pool, no gem, so no level-up screen pauses the test): the next summon refills
            // to the cap and never past it.
            var minion = EnemyController.ActiveEnemies.First(e => e != null && e != summoner);
            minion.gameObject.SetActive(false);
            Assert.That(abilities.SummonsAlive, Is.EqualTo(7));
            yield return Seconds(1f);
            Assert.That(abilities.SummonsAlive, Is.EqualTo(8));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Shoot_FiresBulletsThatHurtThePlayer()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.Shoot) { IntervalSeconds = 0.5f, FirstDelaySeconds = 0.2f, Damage = 25, Speed = 7.5f };
            var shooter = Spawn(5f, spec, hold: true);
            int before = PlayerHp;
            yield return Until(() => PlayerHp < before, 4f, "a bullet to hit the player");
            Assert.That(shooter.Abilities.ShotsFired, Is.GreaterThan(0));
            Assert.That(before - PlayerHp, Is.GreaterThanOrEqualTo(20), $"A 25-damage bullet hit ({before} -> {PlayerHp}).");
            Assert.That(shooter.GetComponent<HealthController>().CurrentHealth, Is.EqualTo(shooter.GetComponent<HealthController>().MaxHealth),
                "Its own bullets never hurt the shooter.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Lunge_DashesAtThePlayer_FasterThanItWalks_ThenChasesAgain()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.Lunge) { IntervalSeconds = 1f, FirstDelaySeconds = 0.5f, SpeedMultiplier = 4f, DurationSeconds = 0.5f };
            var enemy = Spawn(16f, spec, hold: false);
            yield return Until(() => enemy.CurrentState is EnemyLungeState, 4f, "the lunge");
            float start = Gap(enemy);
            float t0 = Time.time;
            yield return Seconds(0.25f);
            float moved = start - Gap(enemy);
            float walked = enemy.MoveSpeed * (Time.time - t0);
            Assert.That(moved, Is.GreaterThan(walked * 2f), $"Dashed {moved:0.00} toward the player; walking covers {walked:0.00}.");
            Assert.That(enemy.Abilities.LungesStarted, Is.GreaterThanOrEqualTo(1));
            yield return Until(() => enemy.CurrentState is EnemyChaseState, 2f, "the enemy to chase again");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator InvulnerablePhase_AbsorbsDamage_ThenTakesIt()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.InvulnerablePhase) { DurationSeconds = 1.5f };
            var enemy = Spawn(8f, spec, hold: true);
            var health = enemy.GetComponent<HealthController>();
            health.Initialize(1000);
            health.TakeDamage(new DamagePacket(100, enemy.transform.position, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(1000), "The shield absorbs the hit.");
            Assert.That(enemy.Abilities.IsShielded, Is.True);
            Assert.That(enemy.Abilities.AbsorbedDamage, Is.EqualTo(100));
            yield return Seconds(2f);
            Assert.That(enemy.Abilities.IsShielded, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(900), "VS The Ender: the absorbed damage is dealt when the shield ends.");
            health.TakeDamage(new DamagePacket(50, enemy.transform.position, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(850), "Then hits land as usual.");
            Assert.That(health.DamageInterceptor, Is.Null, "The shield is removed.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SelfDestruct_ArmsNearThePlayer_ExplodesAfterTheFuse()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.SelfDestruct) { ArmDistance = 3f, FuseSeconds = 1f, Damage = 50, Radius = 3f };
            var enemy = Spawn(1.5f, spec, hold: true);
            var abilities = enemy.Abilities;
            int before = PlayerHp;
            yield return Seconds(0.5f);
            Assert.That(abilities.IsArmed, Is.True, "Armed when close.");
            Assert.That(PlayerHp, Is.EqualTo(before), "Nothing before the fuse ends.");
            yield return Seconds(1f);
            Assert.That(abilities.Explosions, Is.EqualTo(1));
            Assert.That(before - PlayerHp, Is.GreaterThanOrEqualTo(45), $"The blast hurt the player ({before} -> {PlayerHp}).");
            Assert.That(enemy.gameObject.activeSelf, Is.False, "It blew itself up.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator HazardZone_LandsOnThePlayer_AndHurtsPeriodically()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.HazardZone)
                { IntervalSeconds = 30f, FirstDelaySeconds = 0.2f, Damage = 12, TickSeconds = 0.6f, DurationSeconds = 3f, Radius = 2f };
            var enemy = Spawn(8f, spec, hold: true);
            yield return Until(() => enemy.Abilities.ZonesCreated >= 1, 2f, "the zone");
            var zone = Object.FindFirstObjectByType<EnemyAbilityZone>();
            Assert.IsNotNull(zone);
            int before = PlayerHp;
            yield return Seconds(2f);
            Assert.That(zone != null ? zone.Ticks : 99, Is.GreaterThanOrEqualTo(2), "It ticked more than once.");
            Assert.That(before - PlayerHp, Is.GreaterThanOrEqualTo(12), $"Its ticks hurt the player ({before} -> {PlayerHp}).");
            yield return Seconds(2f);
            Assert.That(zone == null, Is.True, "The zone is gone after its duration.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator HalvingPulse_HalvesThePlayersHealth_Inside()
        {
            yield return QuietArena();
            var health = PlayerController.Instance.GetComponent<HealthController>();
            health.Initialize(200);
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.HalvingPulse) { IntervalSeconds = 30f, FirstDelaySeconds = 0.3f, Radius = 8f };
            var enemy = Spawn(3f, spec, hold: true);
            yield return Until(() => enemy.Abilities.PulsesCast >= 1, 2f, "the pulse");
            yield return Seconds(0.2f); // the pulse strikes on its first frame
            // 100 exactly; 101 allows one point of the hero's Recovery ticking in between.
            Assert.That(health.CurrentHealth, Is.InRange(100, 101), "VS Cosmic Egg: the pulse halves the player's health.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator PausedOrFrozen_AnEnemyUsesNoAbilities()
        {
            yield return QuietArena();
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.Shoot) { IntervalSeconds = 0.2f, FirstDelaySeconds = 0.1f };
            var enemy = Spawn(6f, spec, hold: true);
            var owner = new object();
            SoulHunter.Core.Services.GameTime.Pause(owner);
            float until = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.That(enemy.Abilities.ShotsFired, Is.EqualTo(0), "Nothing while paused.");
            SoulHunter.Core.Services.GameTime.Resume(owner);

            enemy.Freeze(1f);
            yield return Seconds(0.8f);
            Assert.That(enemy.Abilities.ShotsFired, Is.EqualTo(0), "Nothing while frozen (Orologion / Clock Lancet).");
            yield return Until(() => enemy.Abilities.ShotsFired > 0, 3f, "a shot after the freeze");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Resistances_ClockLancetGarlicAndInstantKill()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(_spawner.StageEnemyPrefab, player.transform.position + Vector3.right * 8f, Quaternion.identity)
                .GetComponent<EnemyController>();
            _copies.Add(enemy.gameObject);
            var data = Object.Instantiate(enemy.Data);
            _copies.Add(data);
            data.FreezeResistance = 1.1f;
            data.InstantKillResistance = true;
            data.DebuffResistance = false;
            enemy.UseData(data);

            Assert.That(enemy.TryWeaponFreeze(ClockLancetWeapon.VsFreezeChance, 1f), Is.False, "Res-freeze 1.1 resists Clock Lancet.");
            Assert.That(enemy.ApplyGarlicDebuff(), Is.True);
            Assert.That(enemy.FreezeResistance, Is.EqualTo(1f).Within(1e-4), "Garlic -0.1.");
            Assert.That(enemy.TryWeaponFreeze(ClockLancetWeapon.VsFreezeChance, 1f), Is.True, "Then Clock Lancet freezes it.");

            data.DebuffResistance = true;
            enemy.UseData(data);
            Assert.That(enemy.ApplyGarlicDebuff(), Is.False, "Debuff resistance blocks Garlic.");
            Assert.That(enemy.FreezeResistance, Is.EqualTo(1.1f).Within(1e-4));

            ScreenWipePickup_Wipe();
            Assert.That(enemy.gameObject.activeSelf, Is.True, "Instant kill resistance: Soul Toll does not kill it.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static void ScreenWipePickup_Wipe() => SoulHunter.Gameplay.Pickups.ScreenWipePickup.Wipe(null);
    }
}
