using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Every weapon in the real game (owner request 2026-09-25: proper projectiles and VFX). Almost every weapon used to
    /// fire or place the same orange sphere (Bullet.prefab); area weapons flew away with it and physics weapons were
    /// held by its Projectile script. Each weapon must now show its own visual, never the Bullet, and hurt (or, for
    /// Clock Lancet, freeze) enemies around the player. Laurel is a shield and only has to appear.
    /// </summary>
    public class WeaponSweepPlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerable<UpgradeType> Weapons() => new[]
        {
            UpgradeType.MagicWand, UpgradeType.Whip, UpgradeType.Garlic, UpgradeType.Axe, UpgradeType.Bible, UpgradeType.Cross,
            UpgradeType.LightningRing, UpgradeType.FireWand, UpgradeType.SantaWater, UpgradeType.Knife, UpgradeType.Runetracer,
            UpgradeType.Pentagram, UpgradeType.ClockLancet, UpgradeType.Laurel, UpgradeType.Bone, UpgradeType.Peachone,
            UpgradeType.SongOfMana, UpgradeType.CherryBomb, UpgradeType.Gun,
        };

        [UnityTest]
        public IEnumerator Weapon_ShowsItsOwnVisual_AndAffectsEnemies([ValueSource(nameof(Weapons))] UpgradeType type)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            var weapons = WeaponManager.Instance;
            var weaponPrefab = weapons.GetWeaponObject(type);
            Assert.IsNotNull(weaponPrefab, $"{type} has no weapon prefab.");
            // Only the weapon under test fights: the character's starting weapon is switched off.
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(weaponPrefab.name)) existing.gameObject.SetActive(false);
            yield return null;

            weapons.GiveWeapon(type, 1);
            yield return null;
            var weapon = player.GetComponentsInChildren<AutoAttackWeapon>().FirstOrDefault(w => w.gameObject.name.StartsWith(weaponPrefab.name));
            Assert.IsNotNull(weapon, $"{type} was not equipped on the player. " + Errors());

            // A ring of harmless enemies around the player.
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemies = new List<HealthController>();
            var start = new Dictionary<HealthController, int>();
            for (int i = 0; i < 8; i++)
            {
                var offset = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 3f;
                var enemy = Object.Instantiate(prefab, player.transform.position + offset, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var health = enemy.GetComponent<HealthController>();
                enemies.Add(health);
                start[health] = health.CurrentHealth;
            }

            bool affected = false, sawBullet = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!affected && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                sawBullet |= Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Any(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("Bullet") || t.name.StartsWith("Axe_Model") || t.name.StartsWith("Cross_Model")));
                affected = type == UpgradeType.Laurel ||
                           (type == UpgradeType.ClockLancet
                               ? enemies.Any(h => h != null && h.GetComponent<EnemyController>().IsFrozen)
                               : enemies.Any(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < start[h]));
            }
            Assert.IsFalse(sawBullet, $"{type} still spawns the placeholder Bullet/Axe_Model/Cross_Model. " + Errors());
            Assert.IsTrue(affected, $"{type} never {(type == UpgradeType.ClockLancet ? "froze" : "hurt")} an enemy 3 units away within 10 s. " + Errors());
            foreach (var h in enemies) if (h != null) Object.Destroy(h.gameObject);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Kills_Pickups_AndLevelUps_PlayTheirEffects()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 6f, Quaternion.identity);
            var health = enemy.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            var vfx = VFXPoolManager.Instance;
            Assert.IsNotNull(vfx, "VFXPoolManager was not created on first use. " + Errors());
            Assert.That(vfx.PooledCount(VfxKind.HitSpark), Is.GreaterThan(0), "Hitting an enemy shows a hit spark.");
            health.TakeDamage(new DamagePacket(100000, enemy.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            Assert.That(vfx.PooledCount(VfxKind.EnemyDeath), Is.GreaterThan(0), "A killed enemy shows a death effect.");

            // Kael starts with the Whip, which had no visual: each lash now shows the slash arc.
            float lashDeadline = Time.realtimeSinceStartup + 5f;
            while (vfx.PooledCount(VfxKind.Slash) == 0 && Time.realtimeSinceStartup < lashDeadline) yield return null;
            Assert.That(vfx.PooledCount(VfxKind.Slash), Is.GreaterThan(0), "The Whip shows its slash arc.");

            player.GetComponent<PlayerExperience>().AddXP(100000);
            yield return null;
            Assert.That(vfx.PooledCount(VfxKind.LevelUp), Is.GreaterThan(0), "Levelling up shows its effect.");

            // VS-style level-up cards: icon, readable name, NEW!/Level tag and a real description.
            float cardDeadline = Time.realtimeSinceStartup + 3f;
            SoulHunter.Gameplay.UI.LevelUpUI ui = null;
            while (Time.realtimeSinceStartup < cardDeadline)
            {
                ui = Object.FindObjectsByType<SoulHunter.Gameplay.UI.LevelUpUI>(FindObjectsSortMode.None).FirstOrDefault(u => u.isActiveAndEnabled);
                if (ui != null && ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.name == SoulHunter.Gameplay.UI.LevelUpUI.TitleName)) break;
                yield return null;
            }
            var titles = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)
                .Where(t => t.isActiveAndEnabled && t.name == SoulHunter.Gameplay.UI.LevelUpUI.TitleName).ToList();
            Assert.That(titles, Is.Not.Empty, "The level-up screen shows styled cards. " + Errors());
            foreach (var title in titles)
            {
                var card = title.transform.parent;
                Assert.That(title.text, Does.Not.Contain("Lv"), "Readable name, not the internal \"X Lv.N\".");
                var tag = card.Find(SoulHunter.Gameplay.UI.LevelUpUI.TagName).GetComponent<TMPro.TextMeshProUGUI>().text;
                Assert.That(tag == "NEW!" || tag.StartsWith("Level "), $"Tag was '{tag}'.");
                var description = card.Find(SoulHunter.Gameplay.UI.LevelUpUI.DescriptionName).GetComponent<TMPro.TextMeshProUGUI>().text;
                Assert.That(description, Does.Not.Match(@"upgrade level \d"), "A real description, not the placeholder.");
                Assert.That(card.Find(SoulHunter.Gameplay.UI.LevelUpUI.IconName).GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null, "Card icon.");
                // Only the card's own texts show (old TMP and legacy Text labels showed through the first version).
                var stray = card.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Where(t => !t.name.StartsWith("Card_")).Select(t => t.text)
                    .Concat(card.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t => t.text)).ToList();
                Assert.That(stray, Is.Empty, "Stray labels on the card: " + string.Join(", ", stray));
            }
            // Reroll / Skip / Banish: white labels (their label is a legacy Text, which stayed red).
            foreach (var actionName in new[] { "Reroll_Button", "Skip_Button", "Banish_Button" })
            {
                var action = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.isActiveAndEnabled && b.name == actionName);
                Assert.That(action, Is.Not.Null, actionName);
                var colours = action.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t => t.color)
                    .Concat(action.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Where(t => !t.name.StartsWith("Card_")).Select(t => t.color)).ToList();
                Assert.That(colours, Is.Not.Empty, actionName + " has a visible label.");
                Assert.That(colours.All(c => c == Color.white), $"{actionName} label colours: {string.Join(", ", colours)}");
            }
            // The "LEVEL UP!" heading sits above the cards and below the HUD stage name (it was drawn over it).
            var heading = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.isActiveAndEnabled && t.text == SoulHunter.Gameplay.UI.LevelUpUI.HeadingText);
            Assert.That(heading, Is.Not.Null, "Level-up heading.");
            heading.ForceMeshUpdate();
            float headingTop = heading.transform.TransformPoint(heading.textBounds.max).y;
            float cardsTop = titles.Max(t => { var c = new Vector3[4]; ((RectTransform)t.transform.parent).GetWorldCorners(c); return c[1].y; });
            Assert.That(headingTop, Is.GreaterThan(cardsTop), "The heading sits above the cards.");
            var stage = Object.FindFirstObjectByType<SoulHunter.Gameplay.UI.HudReadability>()?.StageLabel;
            if (stage != null && stage.isActiveAndEnabled)
            {
                var corners = new Vector3[4];
                stage.rectTransform.GetWorldCorners(corners);
                Assert.That(headingTop, Is.LessThan(corners[0].y), "The heading does not overlap the stage name.");
            }
            var effects = vfx.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(effects, Is.Not.Empty);
            Assert.That(effects.All(p => p.GetComponent<ParticleSystemRenderer>().sharedMaterial != null), "Every effect has its material.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    
        [UnityTest]
        public IEnumerator Gems_AndCoins_ShowTheirTierByValue()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var pools = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            var at = PlayerController.Instance.transform.position + new Vector3(8f, 0f, 8f);
            foreach (var (value, tier) in new[] { (10, 0), (60, 1), (300, 2) })
            {
                pools.SpawnGem(at, value);
                var gem = SoulHunter.Gameplay.Pickups.XPGem.ActiveGems.Last();
                Assert.That(gem.XPValue, Is.EqualTo(value));
                var visual = gem.GetComponent<SoulHunter.Gameplay.Pickups.TieredVisual>();
                Assert.That(visual.CurrentTier, Is.EqualTo(tier), $"Gem worth {value} XP.");
                Assert.That(visual.Tiers[tier].activeSelf, Is.True);
                at += Vector3.right * 2f;
            }
            foreach (var (value, tier) in new[] { (1, 0), (10, 1), (50, 2) })
            {
                pools.SpawnGold(at, value);
                yield return null;
                var coin = Object.FindObjectsByType<SoulHunter.Gameplay.Pickups.GoldPickup>(FindObjectsSortMode.None).First(c => c.Value == value);
                Assert.That(coin.GetComponent<SoulHunter.Gameplay.Pickups.TieredVisual>().CurrentTier, Is.EqualTo(tier), $"Coin worth {value} gold.");
                at += Vector3.right * 2f;
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
}
}
