using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 7 in the real game (approved decision: ordinary Normal damage does not hurt the Sea of Lost Souls'
    /// ghosts; Holy, Magic or Spectral does). Owner, 2026-09-26: weapons are typed by theme - Holy (Cross, Bible,
    /// Santa Water, Holy Wand and their evolutions), Magic (Magic Wand, Song of Mana, Fire Wand, Lightning Ring,
    /// Runetracer, Peachone; Pentagram erases), Normal (the rest) - and Soul Burst is Spectral.
    /// </summary>
    public class SpectralStagePlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerable<UpgradeType> HurtGhosts() => new[]
        {
            UpgradeType.MagicWand, UpgradeType.SongOfMana, UpgradeType.SantaWater, UpgradeType.Cross, UpgradeType.Bible,
            UpgradeType.FireWand, UpgradeType.LightningRing, UpgradeType.Runetracer, UpgradeType.Peachone, UpgradeType.Pentagram,
        };

        private static IEnumerable<UpgradeType> PassThroughGhosts() => new[]
        {
            UpgradeType.Whip, UpgradeType.Knife, UpgradeType.Axe, UpgradeType.Bone, UpgradeType.Gun, UpgradeType.CherryBomb, UpgradeType.Garlic,
        };

        private List<HealthController> _ghosts;
        private readonly List<Vector3> _spots = new List<Vector3>();

        /// <summary>Keeps the ghosts on their ring: stage 7 ghosts chase (Floaty), and would otherwise walk inside
        /// orbiting weapons such as Peachone's bird.</summary>
        private void PinGhosts()
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                var ghost = _ghosts[i];
                if (ghost == null || !ghost.gameObject.activeInHierarchy) continue;
                ghost.transform.position = _spots[i];
                var body = ghost.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic) body.linearVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
        }

        private IEnumerator SeaOfLostSouls(UpgradeType? only)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            LevelProgressionManager.Instance.StartStage(7);
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;

            var weapons = WeaponManager.Instance;
            string keep = only.HasValue ? weapons.GetWeaponObject(only.Value).name : "(none)";
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(keep)) existing.gameObject.SetActive(false);
            if (only.HasValue) weapons.GiveWeapon(only.Value, 1);

            // A ring of stage 7 ghosts, spawned the stage's way (so they carry the stage's damage rule), 3 units out.
            var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
            var prefab = GetPrivateField<GameObject>(spawner, "_stageEnemyPrefab");
            _ghosts = new List<HealthController>();
            _spots.Clear();
            for (int i = 0; i < 6; i++)
            {
                var ghost = (GameObject)spawn.Invoke(spawner, new object[] { prefab });
                ghost.transform.position = player.transform.position + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 3f;
                var touch = ghost.GetComponent<TouchDamage>();
                if (touch != null) touch.DamageAmount = 0;
                ghost.GetComponent<HealthController>().Initialize(100000);
                Assert.That(ghost.GetComponent<EnemyController>().IsSpectral, Is.True, "Sea of Lost Souls enemies are ghosts.");
                _ghosts.Add(ghost.GetComponent<HealthController>());
                _spots.Add(ghost.transform.position);
            }
            PinGhosts();
        }

        private bool AnyGhostHurt() => _ghosts.Any(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < h.MaxHealth);

        [UnityTest]
        public IEnumerator HolyAndMagicWeapons_HurtGhosts([ValueSource(nameof(HurtGhosts))] UpgradeType type)
        {
            yield return SeaOfLostSouls(type);
            float until = Time.realtimeSinceStartup + 10f;
            while (!AnyGhostHurt() && Time.realtimeSinceStartup < until) { PinGhosts(); yield return null; }
            Assert.That(AnyGhostHurt(), Is.True, $"{type} never hurt a ghost within 10 s. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator NormalWeapons_PassThroughGhosts([ValueSource(nameof(PassThroughGhosts))] UpgradeType type)
        {
            yield return SeaOfLostSouls(type);
            float until = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < until) { PinGhosts(); yield return null; }
            Assert.That(AnyGhostHurt(), Is.False, $"{type} (Normal damage) hurt a ghost.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SoulBurst_ReachesGhosts()
        {
            yield return SeaOfLostSouls(null);
            var burst = Object.FindFirstObjectByType<SoulBurstController>();
            Assert.IsNotNull(burst, "The player has the Soul Burst ultimate.");
            typeof(SoulBurstController).GetMethod("TriggerSoulBurst", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(burst, null);
            yield return null;
            Assert.That(AnyGhostHurt(), Is.True, "Soul Burst (Spectral) hurts ghosts. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheStage7Boss_IsAGhostToo()
        {
            yield return SeaOfLostSouls(null);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var boss = GetPrivateField<GameObject>(spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spawner, new object[] { boss }));
            var controller = EnemyController.ActiveEnemies.Last(e => e != null);
            var health = controller.GetComponent<HealthController>();
            health.Initialize(100000); // boss health is 50 x player level (50 here): one Holy hit would kill it
            int start = health.CurrentHealth;
            health.TakeDamage(new DamagePacket(50, controller.transform.position, Vector3.zero, DamageType.Normal));
            Assert.That(health.CurrentHealth, Is.EqualTo(start), "Normal damage passes through the ghost boss.");
            health.TakeDamage(new DamagePacket(50, controller.transform.position, Vector3.zero, DamageType.Holy));
            Assert.That(health.CurrentHealth, Is.EqualTo(start - 100), "Holy hurts it twice as hard.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
