using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 3 in the real game, VS Merchant rule: the merchant waits near the stage start on open ground,
    /// touching it opens the shop, weapons and Soul Eggs are paid with gold, leaving sends it away, and
    /// Soul Eggs raise the character's stats from the next run. Saves go to a throw-away file.
    /// </summary>
    public class WanderingMerchantPlayModeTests : GameplayPlayModeTestBase
    {
        private static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        [UnityTest]
        public IEnumerator ForgottenVillage_Merchant_SellsForGold_OnceNearTheStart_AndEggsCountNextRun()
        {
            string realSave = Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            byte[] realBefore = File.Exists(realSave) ? File.ReadAllBytes(realSave) : null;

            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string temp = Path.Combine(Application.temporaryCachePath, "merchant_test_save.json");
            foreach (var file in new[] { temp, temp + ".prev", temp + ".tmp" }) if (File.Exists(file)) File.Delete(file);
            save.UseFile(temp); // every save of this test goes to the throw-away file
            save.CurrentData.SelectedCharacterName = "Kael";
            save.CurrentData.Currency = 11500;
            save.CurrentData.SoulEggs.RemoveAll(e => e.Character == "Kael");

            GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var player = PlayerController.Instance;
            float firstRunMight = player.Stats.Might;
            int firstRunRerolls = Object.FindFirstObjectByType<LevelUpManager>().RerollsLeft;

            // Stage 3: the merchant waits close to the start, on open ground.
            LevelProgressionManager.Instance.StartStage(3);
            yield return null;
            yield return null;
            var merchant = Object.FindFirstObjectByType<WanderingMerchant>();
            Assert.IsNotNull(merchant, "Level 3 spawns the Wandering Merchant. " + Errors());
            Vector3 offset = merchant.transform.position - player.transform.position;
            offset.y = 0f;
            Assert.That(offset.magnitude, Is.InRange(5f, 9f), "The merchant waits near the stage start.");
            Assert.That(UnityEngine.Physics.CheckSphere(merchant.transform.position + Vector3.up, 1.2f, LayerMask.GetMask("Obstacle"), QueryTriggerInteraction.Collide),
                Is.False, "No stage obstacle covers the merchant.");

            // The owner-approved model (2026-09-26), not a placeholder: 1.11x the hero, on the ground, facing the player.
            var model = merchant.transform.Find("SH10_NPC_WanderingMerchant");
            Assert.IsNotNull(model, "The merchant uses its approved model.");
            Assert.That(merchant.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh != null && f.sharedMesh.name == "Capsule"), Is.False, "No capsule placeholder.");
            var hero = player.GetComponentsInChildren<Transform>().First(t => t.name.StartsWith("SH10_Hero_"));
            float heroHeight = Bounds(hero.gameObject).size.y, merchantHeight = Bounds(model.gameObject).size.y;
            Assert.That(merchantHeight / heroHeight, Is.InRange(0.95f, 1.3f), $"Merchant {merchantHeight:0.00} vs hero {heroHeight:0.00}.");
            Assert.That(Bounds(model.gameObject).min.y, Is.EqualTo(Bounds(hero.gameObject).min.y).Within(0.35f), "Feet on the ground like the hero's.");
            Vector3 front = -merchant.transform.forward;
            Assert.That(Vector3.Dot(front, (-offset).normalized), Is.GreaterThan(0.9f), "The merchant faces the player (model front is -Z).");

            // Walking into it opens the shop and pauses the game.
            yield return WaitUntilRealtime(() => !GameTime.IsPaused, "the stage to run");
            player.Rigidbody.position = merchant.transform.position;
            player.transform.position = merchant.transform.position;
            yield return WaitUntilRealtime(() => merchant.IsShopOpen, "the shop to open when the player touches the merchant");
            Assert.That(GameTime.IsPaused, Is.True);
            Assert.That(merchant.ShopUI.GetComponentsInChildren<Button>().Length, Is.EqualTo(4), "Bone, Cherry Bomb, Soul Egg and Leave.");
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(merchant.ShopUI.transform), Is.True, "A shop button is focused.");

            // Weapons: 1,000 gold, level 1, not twice.
            var economy = GameServices.Instance.Get<EconomyService>();
            var weapons = WeaponManager.Instance;
            Assert.That(merchant.BuyWeapon(UpgradeData.UpgradeType.Bone), Is.True, "Bone for 1,000 gold. " + Errors());
            Assert.That(economy.CurrentGold, Is.EqualTo(10500));
            Assert.That(weapons.GetWeaponLevel(UpgradeData.UpgradeType.Bone), Is.EqualTo(1));
            Assert.That(weapons.GetActiveWeaponTypes(), Does.Contain(UpgradeData.UpgradeType.Bone));
            Assert.That(merchant.BuyWeapon(UpgradeData.UpgradeType.Bone), Is.False, "Already owned.");
            Assert.That(economy.CurrentGold, Is.EqualTo(10500));

            // Soul Egg: 10,000 gold, stored for Kael and saved.
            Assert.That(merchant.BuySoulEgg(), Is.True, "Soul Egg for 10,000 gold. " + Errors());
            Assert.That(economy.CurrentGold, Is.EqualTo(500));
            Assert.That(SoulEggs.For(save.CurrentData, "Kael").Eggs, Is.EqualTo(1));
            Assert.That(File.Exists(temp), Is.True, "The purchase is saved (to the throw-away file).");
            var saved = JsonUtility.FromJson<GameData>(File.ReadAllText(temp));
            Assert.That(SoulEggs.For(saved, "Kael")?.Eggs, Is.EqualTo(1), "The egg is in the saved file.");
            Assert.That(merchant.BuyWeapon(UpgradeData.UpgradeType.CherryBomb), Is.False, "500 gold is not enough.");
            var labels = merchant.ShopUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Select(t => t.text).ToList();
            Assert.That(labels.Any(t => t.Contains("OWNED")), Is.True, "The shop shows Bone as owned.");
            Assert.That(labels.Any(t => t.StartsWith("Soul Egg: ")), Is.True, "The shop says what the egg raised.");
            var owned = merchant.ShopUI.GetComponentsInChildren<Button>()
                .First(b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>().text.Contains("OWNED"));
            Assert.That(owned.interactable, Is.False, "An owned weapon cannot be bought again.");
            yield return null; // the theme restyles an unusable ware on its next frame
            var ownedText = owned.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            float contrast = SoulHunter.Gameplay.UI.UITheme.Contrast(ownedText.color, owned.colors.disabledColor);
            Assert.That(contrast, Is.GreaterThanOrEqualTo(3f), $"A sold-out ware's text stays readable on its plate ({contrast:0.0}:1).");

            // Leaving sends the merchant away and resumes the game.
            merchant.Leave();
            yield return null;
            Assert.That(merchant == null, Is.True, "VS: the merchant disappears after the shop is closed.");
            Assert.That(GameTime.IsPaused, Is.False);
            Assert.That(Object.FindFirstObjectByType<WanderingMerchant>(), Is.Null);

            // Soul Eggs raise the character's stats from the next run.
            var eggs = SoulEggs.For(save.CurrentData, "Kael");
            eggs.Might += 0.5f;
            eggs.Reroll += 1f;
            float expectedMight = firstRunMight + eggs.Might;
            int expectedRerolls = firstRunRerolls + SoulEggs.Whole(eggs.Reroll);
            GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(player);
            Assert.That(PlayerController.Instance.Stats.Might, Is.EqualTo(expectedMight).Within(1e-3f), "Egg Might applies next run. " + Errors());
            Assert.That(Object.FindFirstObjectByType<LevelUpManager>().RerollsLeft, Is.EqualTo(expectedRerolls), "Egg Rerolls apply next run.");

            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            Assert.That(File.Exists(realSave) ? File.ReadAllBytes(realSave) : null, Is.EqualTo(realBefore), "The real save is never changed.");
        }
    }
}
