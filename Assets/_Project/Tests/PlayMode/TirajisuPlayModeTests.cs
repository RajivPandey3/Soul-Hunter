using System.Collections;
using System.Linq;
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
    /// VS wiki (Tirajisú, fetched 2026-09-28): "Tirajisú will only be removed from the inventory when the last Revive is
    /// used up", after every other Revival source; "you can get it again through a level up".
    /// </summary>
    public class TirajisuPlayModeTests : GameplayPlayModeTestBase
    {
        private UpgradeData _one, _two;
        private Texture2D _iconTexture;

        [TearDown]
        public void DestroyUpgrades()
        {
            if (_one != null) Object.Destroy(_one);
            if (_two != null) Object.Destroy(_two);
            if (_iconTexture != null) Object.Destroy(_iconTexture);
        }

        [UnityTest]
        public IEnumerator Tirajisu_StaysWhileRevivalsRemain_AndLeavesWithTheLast()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            var stats = player.GetComponent<PlayerStats>();
            var health = player.GetComponent<HealthController>();
            var weapons = player.GetComponentInChildren<WeaponManager>() ?? WeaponManager.Instance;
            Assert.That(weapons, Is.Not.Null);

            _iconTexture = new Texture2D(4, 4);
            var icon = Sprite.Create(_iconTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            _one = ScriptableObject.CreateInstance<UpgradeData>();
            _one.Type = UpgradeType.Tiragisu; _one.Level = 1; _one.Icon = icon;
            _two = ScriptableObject.CreateInstance<UpgradeData>();
            _two.Type = UpgradeType.Tiragisu; _two.Level = 2; _two.Icon = icon;

            // Only Tirajisú's Revivals count here, whatever the save or character gives.
            while (stats.UseRevival()) { }
            weapons.ApplyUpgrade(_one);
            weapons.ApplyUpgrade(_two);
            Assert.That(stats.Revivals, Is.EqualTo(2), "Tirajisú level 2: +2 Revival.");

            yield return Kill(health);
            Assert.That(player.gameObject.activeSelf && health.CurrentHealth > 0, Is.True, "The first death is revived.");
            Assert.That(stats.Revivals, Is.EqualTo(1));
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(2), "A Revival is left, so Tirajisú stays.");

            yield return Kill(health);
            Assert.That(player.gameObject.activeSelf && health.CurrentHealth > 0, Is.True, "The second death is revived too.");
            Assert.That(stats.Revivals, Is.EqualTo(0));
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(0), "The last Revival is used: Tirajisú leaves.");
            Assert.That(weapons.HasSlotFor(UpgradeType.Tiragisu), Is.True, "Its slot is free for the next passive.");

            weapons.ApplyUpgrade(_one);
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(1), "A level-up can offer it again from level 1.");
            Assert.That(stats.Revivals, Is.EqualTo(1));
            Assert.That(GameSessionManager.Instance, Is.Not.Null);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private IEnumerator Kill(HealthController health)
        {
            // Wait out the revival's invulnerability, then land a lethal hit.
            yield return WaitUntilRealtime(() => !health.IsInvincible, "revival invulnerability to end");
            health.ShieldedUntil = -1f;
            var packet = new DamagePacket(health.MaxHealth * 10, health.transform.position, Vector3.zero);
            packet.IgnoresHitInvulnerability = true;
            health.TakeDamage(packet);
            yield return null;
        }
    }
}
