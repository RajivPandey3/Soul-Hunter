using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Release item P2-02 (owner, 2026-10-09): Levels 2-10 spawn their VS source stage's enemy of each minute with its wiki
    /// statbox, on the level's own models tinted per type; the owner's signatures hold (Level 7 spectral and Floaty, Level 8
    /// elite-class waves and elite-only events).
    /// </summary>
    public class StageEnemyTypePlayModeTests : GameplayPlayModeTestBase
    {
        private EnemySpawner _spawner;

        private IEnumerator EnterAt(int level, float minute)
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            LevelProgressionManager.Instance.StartStage(level);
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            LevelProgressionManager.Instance.SkipTo(minute * 60f + 1f);
        }

        private IEnumerator WaitFor(string name, System.Action<EnemyController> found)
        {
            EnemyController enemy = null;
            yield return WaitUntilRealtime(() => (enemy = EnemyController.ActiveEnemies.FirstOrDefault(e => e != null && e.isActiveAndEnabled && e.Data != null && e.Data.EnemyName == name)) != null,
                $"a {name}",
                () => string.Join(", ", EnemyController.ActiveEnemies.Where(e => e != null && e.Data != null).Select(e => e.Data.EnemyName).Distinct()));
            yield return null;
            found(enemy);
        }

        private static float Curse => PlayerController.Instance.GetComponent<PlayerStats>().Curse;
        private static int PlayerLevel => PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;

        private GameObject WavePrefab(int minute, string id)
        {
            var wave = GetPrivateField<WaveData>(_spawner, "_currentWave");
            var prefab = wave.Minutes[minute].EnemyTypes.FirstOrDefault(p => p != null && p.name.Contains("_" + id + "_"));
            Assert.IsNotNull(prefab, $"minute {minute} lists {id}");
            return prefab;
        }

        private GameObject Spawn(GameObject prefab)
        {
            var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
            return (GameObject)spawn.Invoke(_spawner, new object[] { prefab });
        }

        [UnityTest]
        public IEnumerator Level2_At15Minutes_HuntingGloomMothsFloat_WithTheirStatbox_OnTheDarkForestModel()
        {
            yield return EnterAt(2, 15f);
            EnemyController moth = null;
            yield return WaitFor("Hunting Gloom Moth", e => moth = e);
            var type = StageEnemyTables.For(2).Find("HuntingGloomMoth");
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(25, Curse), moth.GetComponent<HealthController>().MaxHealth, "Aggressive Big Sneaky Head: 25 HP x Curse.");
            Assert.AreEqual(1f, moth.GetComponent<TouchDamage>().DamageAmount, "Power 1.");
            Assert.AreEqual(240f * 0.03f, moth.Data.MoveSpeed, 1e-4, "Speed 240.");
            Assert.That(moth.CampaignSpeedMultiplier, Is.GreaterThanOrEqualTo(1.10f), "Level 2 keeps the owner-approved pace 1.10.");
            Assert.AreEqual(EnemyMovement.Floaty, moth.Movement, "Owner signature: Level 2's enemies float.");
            Assert.AreEqual(type.Tint, moth.TypeTint, "Drawn in its own tint.");
            Assert.That(moth.name, Does.StartWith("SH10_L02_HuntingGloomMoth_Gameplay"), "Level 2's own model.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level2_GloomMoths_KeepTheirLine_AndWeaveAlongIt()
        {
            yield return EnterAt(2, 11f);
            var moth = Spawn(WavePrefab(11, "GloomMoth")).GetComponent<EnemyController>();
            Assert.That(moth.IsFlying, "Big Sneaky Head: Fixed Direction, so it flies its line.");
            Vector3 start = moth.transform.position;
            Vector3 line = PlayerController.Instance.transform.position - start;
            line.y = 0f;
            line.Normalize();
            PlayerController.Instance.transform.position += Vector3.Cross(Vector3.up, line) * 5f; // the player steps aside
            float widest = 0f;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                Vector3 offset = moth.transform.position - start;
                offset.y = 0f;
                widest = Mathf.Max(widest, Vector3.Cross(line, offset).magnitude);
            }
            Vector3 travelled = moth.transform.position - start;
            travelled.y = 0f;
            Assert.That(Vector3.Dot(travelled.normalized, line), Is.GreaterThan(0.8f), "It keeps its first line, not the player's new spot.");
            Assert.That(widest, Is.GreaterThan(0.05f), "Floaty: it weaves along the line.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level3_At12Minutes_WellSerpentsStandStill_AndShoot_AndTheMinuteBringsAllThreeTypes()
        {
            yield return EnterAt(3, 12f);
            EnemyController serpent = null;
            yield return WaitFor("Well Serpents", e => serpent = e);
            Assert.AreEqual(0f, serpent.Data.MoveSpeed, "Twin Snakes: speed 0.");
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(25, Curse), serpent.GetComponent<HealthController>().MaxHealth);
            var shoot = serpent.Data.Abilities.Single();
            Assert.AreEqual(EnemyAbilityKind.Shoot, shoot.Kind);
            Assert.AreEqual(2f, shoot.IntervalSeconds, 1e-5, "Shoots a bullet every 2 seconds.");
            Assert.AreEqual(20, shoot.Damage, "Bullet power 20.");
            yield return WaitUntilRealtime(() =>
                    new[] { "Rooftop Reaver", "Well Serpents", "Bell Imps" }.All(n => EnemyController.ActiveEnemies.Any(e => e != null && e.Data != null && e.Data.EnemyName == n)),
                "the 12:00 minimum filled with Rooftop Reavers, Well Serpents and Bell Imps");
            Assert.That(serpent.name, Does.StartWith("SH10_L03_"), "Level 3's own model.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level4_At11Minutes_BoneDrakesHaveFourTimesTheLevel_AndGlidingGargoylesFlyAFixedLine()
        {
            yield return EnterAt(4, 11f);
            EnemyController drake = null;
            yield return WaitFor("Bone Drake", e => drake = e);
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(4 * PlayerLevel, Curse), drake.GetComponent<HealthController>().MaxHealth, "Dragon Shrimp 1: 4 x Level.");
            EnemyController gargoyle = null;
            yield return WaitFor("Gliding Gargoyle", e => gargoyle = e);
            Assert.AreEqual(EnemyMovement.FixedDirection, gargoyle.Movement, "Harzia V: Fixed Direction.");
            Assert.That(gargoyle.IsFlying, "It flies its line.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level4_ThriceRisen_RisesTwice_WithAGemEachTime_ButAnInstantKillEndsIt()
        {
            yield return EnterAt(4, 3f);
            _spawner.enabled = false;
            var prefab = WavePrefab(3, "ThriceRisen");
            var risen = Spawn(prefab).GetComponent<HealthController>();
            var lives = risen.GetComponent<EnemyLives>();
            Assert.IsNotNull(lives, "Scarleton: Has 3 lives.");
            Assert.AreEqual(3, lives.LivesLeft);
            for (int death = 1; death <= 2; death++)
            {
                risen.TakeDamage(new DamagePacket(100000, risen.transform.position, Vector3.zero, DamageType.Normal));
                yield return null;
                Assert.That(risen.gameObject.activeSelf, $"Death {death}: it resurrects.");
                Assert.AreEqual(risen.MaxHealth, risen.CurrentHealth, "Full health again (balance method).");
                Assert.AreEqual(3 - death, lives.LivesLeft);
            }
            risen.TakeDamage(new DamagePacket(100000, risen.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            Assert.That(risen.gameObject.activeSelf, Is.False, "The third death is its last.");

            var second = Spawn(prefab).GetComponent<HealthController>();
            Assert.AreEqual(3, second.GetComponent<EnemyLives>().LivesLeft, "A pooled one starts with all its lives.");
            second.EraseByInstantKill();
            yield return null;
            Assert.That(second.gameObject.activeSelf, Is.False, "VS: Pentagram, Gorgeous Moon and Rosary end it at once.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level7_LakeTypes_AreSpectralFloatyGhosts_ThatNormalDamageCannotHurt()
        {
            yield return EnterAt(7, 3f);
            EnemyController shade = null;
            yield return WaitFor("Mournful Shade", e => shade = e);
            Assert.That(shade.IsSpectral, "Owner signature: every Level 7 enemy is a ghost.");
            Assert.AreEqual(EnemyMovement.Floaty, shade.Movement, "And floats.");
            Assert.AreEqual(0f, shade.Data.Knockback, "VS ghosts take no knockback.");
            var health = shade.GetComponent<HealthController>();
            int before = health.CurrentHealth;
            health.TakeDamage(new DamagePacket(5, shade.transform.position, Vector3.zero, DamageType.Normal));
            Assert.AreEqual(before, health.CurrentHealth, "Normal damage passes through.");
            health.TakeDamage(new DamagePacket(1, shade.transform.position, Vector3.zero, DamageType.Magic));
            Assert.That(health.CurrentHealth, Is.LessThan(before), "Magic hurts it.");
            Assert.That(shade.name, Does.StartWith("SH10_L07_"));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level8_At13Minutes_BrandKnightsComeOnTheEliteModel_AndEventsStayEliteOnly()
        {
            yield return EnterAt(8, 13f);
            EnemyController knight = null;
            yield return WaitFor("Brand Knight", e => knight = e);
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(150, Curse), knight.GetComponent<HealthController>().MaxHealth, "Archon Fiamma: 150 HP.");
            Assert.AreEqual(8f, knight.GetComponent<TouchDamage>().DamageAmount, "Power 8.");
            Assert.AreEqual(120f * 0.03f, knight.Data.MoveSpeed, 1e-4, "Speed 120.");
            Assert.That(knight.name, Does.StartWith("SH10_L08_FlameKnight_Gameplay"));
            var elite = GetPrivateField<GameObject>(_spawner, "_stageElitePrefab");
            var eliteData = elite.GetComponent<EnemyController>().Data;
            var members = _spawner.TriggerStageEvent(StageEventType.Swarm, 6);
            Assert.That(members, Is.Not.Empty);
            Assert.That(members.All(m => m != null && m.Data == eliteData), "Level 8's map events still send only its elites.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Level10_PastThirtyMinutes_HoldsThe29MinuteRow()
        {
            yield return EnterAt(10, 35f);
            Assert.That(LevelProgressionManager.Instance.CurrentStage, Is.EqualTo(10), "The Soul Core has no time limit.");
            EnemyController adept = null;
            yield return WaitFor("Pure Fragment", e => adept = e);
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(80 * PlayerLevel, Curse), adept.GetComponent<HealthController>().MaxHealth, "Reaper Trainee: 80 x Level.");
            Assert.AreEqual(1f, adept.Data.FreezeResistance, 1e-5, "res-freeze 1.");
            Assert.That(adept.name, Does.StartWith("SH10_L10_EliteAcolyte_Gameplay"));
        }
    }
}
