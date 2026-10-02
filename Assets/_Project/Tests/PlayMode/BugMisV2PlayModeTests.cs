using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS v2 (Docs/BUG_MIS_Level1_v2.md, the 2026-10-02 Level 1 CTO pass; owner approved fixing all): each fix in the
    /// real game.
    /// </summary>
    public class BugMisV2PlayModeTests : GameplayPlayModeTestBase
    {
        private PlayerController _player;

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            _player = PlayerController.Instance;
            _player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in _player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            yield return null;
        }

        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

        [UnityTest]
        public IEnumerator C02_TheHudTimer_ShowsTheStageClock_NotTheRunTime()
        {
            yield return Boot();
            LevelProgressionManager.Instance.SkipTo(600f);
            yield return null;
            yield return null;
            var timer = Object.FindFirstObjectByType<TimerUI>();
            Assert.IsNotNull(timer);
            Assert.That(timer.ShownSeconds, Is.InRange(599, 602), "The stage clock (10:00), not the few seconds the run has lasted.");
            Assert.That(timer.TimeText.text, Does.Contain("10:0"));
            Assert.That(GameSessionManager.Instance.SurvivalTime, Is.LessThan(120f), "The run itself is much shorter.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C03_HellfireBreath_ShowsOnlyFlames_NoOpaqueCore()
        {
            yield return Boot();
            var template = (GameObject)typeof(FireBreath).GetMethod("FlameTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            Assert.IsNotNull(template);
            var cores = template.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "Core").ToList();
            Assert.That(cores.All(r => !r.enabled), Is.True, "The fireball's opaque core sphere is hidden in the breath.");
            Assert.That(template.GetComponentsInChildren<ParticleSystem>(true).Length, Is.GreaterThan(0), "Its flames stay.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C04_ALightningBolt_KeepsItsWorldSize_AndFades()
        {
            yield return Boot();
            WeaponManager.Instance.GiveWeapon(UpgradeData.UpgradeType.LightningRing, 1);
            yield return null;
            var ring = _player.GetComponentInChildren<LightningRingWeapon>(true);
            Assert.IsNotNull(ring);
            var strike = Object.Instantiate(ring.LightningStrikePrefab, _player.transform.position + Vector3.right * 4f, Quaternion.identity);
            strike.transform.localScale = Vector3.one * 4f; // a big Area
            strike.SetActive(false);
            strike.SetActive(true);
            var bolt = strike.GetComponentInChildren<LightningBolt>();
            var line = bolt.GetComponent<LineRenderer>();
            Vector3 top = line.transform.TransformPoint(line.GetPosition(0)), bottom = line.transform.TransformPoint(line.GetPosition(line.positionCount - 1));
            Assert.That(top.y - bottom.y, Is.EqualTo(bolt.Height).Within(0.5f), "The bolt is as tall as authored, not 4x.");
            Assert.That(line.widthCurve.Evaluate(1f), Is.LessThanOrEqualTo(0.2f), "A thin bolt.");
            Assert.That(bolt.Alpha, Is.GreaterThan(0.8f), "Bright when it strikes.");
            float end = Time.time + bolt.FadeSeconds + 0.1f;
            while (Time.time < end) yield return null;
            Assert.That(bolt.Alpha, Is.LessThan(0.05f), "Faded out.");
            Object.Destroy(strike);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C05_C19_C20_LevelUpCards_AreCentredAndNarrower_AndEachScreenShowsItsOwnLevel()
        {
            yield return Boot();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = _player.GetComponent<PlayerExperience>();
            int start = experience.CurrentLevel;
            // Three level-ups at once (a chain of screens).
            int needed = experience.XPToNextLevel - experience.CurrentXP;
            experience.AddXP(needed);
            for (int i = 0; i < 2; i++) experience.AddXP(experience.XPToNextLevel + 1);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a level-up");
            yield return null;
            yield return null;
            Assert.That(experience.CurrentLevel, Is.GreaterThanOrEqualTo(start + 2), "Several levels are queued.");

            var ui = Object.FindFirstObjectByType<LevelUpUI>();
            var cards = Field<Button[]>(ui, "_upgradeButtons").Where(b => b != null && b.gameObject.activeInHierarchy).ToList();
            Assert.That(cards.Count, Is.GreaterThan(0));
            foreach (var card in cards)
            {
                var rect = (RectTransform)card.transform;
                Assert.That(rect.rect.width, Is.LessThanOrEqualTo(LevelUpUI.CardMaxWidth + 1f), $"{card.name} is {rect.rect.width:0} wide.");
                var canvasRect = (RectTransform)card.GetComponentInParent<Canvas>().rootCanvas.transform;
                float centre = canvasRect.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).x;
                Assert.That(Mathf.Abs(centre), Is.LessThan(10f), $"{card.name} is centred (x {centre:0}).");
            }

            var panel = Field<GameObject>(ui, "_levelUpPanel");
            var veil = panel.GetComponent<Image>();
            if (veil != null)
            {
                Assert.That(veil.sprite, Is.Null, "Bug MIS v2 C08: an even veil, not a textured tile.");
                Assert.That(veil.color, Is.EqualTo(LevelUpUI.DimColor));
            }

            var bar = Object.FindFirstObjectByType<XPBarUI>();
            yield return null;
            var label = bar.LevelLabel;
            Assert.IsNotNull(label);
            Assert.That(label.text, Is.EqualTo(XPBarUI.FormatLevel(start + 1)), "The first screen of the chain is for the next level, not the last.");
            var labelRect = label.rectTransform;
            Assert.That(labelRect.pivot.y, Is.EqualTo(1f), "The label hangs under the bar (Bug MIS v2 C20).");
            Assert.That(labelRect.anchoredPosition.y, Is.LessThan(0f));

            levelUp.SelectUpgrade(levelUp.GetRandomValidUpgrades(1)[0]);
            yield return null;
            yield return null;
            if (levelUp.IsLevelUpActive)
                Assert.That(label.text, Is.EqualTo(XPBarUI.FormatLevel(start + 2)), "The second screen shows the level after it.");
            while (levelUp.IsLevelUpActive)
            {
                levelUp.SelectUpgrade(levelUp.GetRandomValidUpgrades(1)[0]);
                yield return null;
                yield return null;
            }
            yield return null;
            Assert.That(label.text, Is.EqualTo(XPBarUI.FormatLevel(experience.CurrentLevel)), "Back to the player's level after the chain.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C07_TheOpenedChest_DrawsItsLidBothSides_AndDimsTheSeam()
        {
            yield return Boot();
            var pool = PickupPoolManager.Instance;
            var stage = ChestPreviewStage.Create(pool.ChestPrefab, pool.GoldCoinPrefab, false);
            try
            {
                yield return WaitUntilRealtime(() => stage.Landed, "the chest to land");
                Assert.That(stage.SeamGlow, Is.EqualTo(1f), "Closed: the seam glows fully.");
                stage.Open(4);
                Assert.That(stage.SeamGlow, Is.EqualTo(ChestPreviewStage.OpenSeamGlow), "Open: a dim glow, not a flat bright slab.");
                var hinge = stage.GetComponentsInChildren<Transform>().First(t => t.name == "LidHinge");
                var lidMaterials = hinge.GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).Where(m => m != null && m.HasProperty("_Cull")).ToList();
                Assert.That(lidMaterials.Count, Is.GreaterThan(0));
                Assert.That(lidMaterials.All(m => m.GetFloat("_Cull") == 0f), Is.True, "The lid draws its inside faces too.");
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.That(stage.GlowIntensity, Is.LessThanOrEqualTo(ChestPreviewStage.OpenGlowIntensity + 0.05f), "A soft light, not a glare on the planks.");
            }
            finally { Object.Destroy(stage.gameObject); }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C09_C10_ItemSlots_HaveABacking_AndAnEvolutionHasAGoldBorder()
        {
            yield return Boot();
            var manager = WeaponManager.Instance;
            manager.GiveWeapon(UpgradeData.UpgradeType.Whip, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.MaxHealth, 1);
            yield return null;
            Assert.That(manager.TryEvolveWeapon(out _), Is.True, "Whip + Hollow Heart evolves.");
            yield return null;
            yield return null;
            var hud = Object.FindFirstObjectByType<InventoryHud>();
            Assert.IsNotNull(hud);
            for (int i = 0; i < InventoryHud.SlotsPerRow; i++)
                Assert.That(hud.WeaponFrame(i).color.a, Is.GreaterThanOrEqualTo(0.85f), $"Slot {i} hides what is behind it.");
            var borders = Enumerable.Range(0, InventoryHud.SlotsPerRow).Select(i => hud.WeaponFrame(i).GetComponent<Outline>()).ToList();
            Assert.That(borders.All(b => b != null), Is.True);
            Assert.That(borders.Count(b => b.effectColor == InventoryHud.EvolvedBorder), Is.EqualTo(1), "The evolved weapon has the gold border.");
            int evolved = borders.FindIndex(b => b.effectColor == InventoryHud.EvolvedBorder);
            Assert.That(hud.WeaponFrame(evolved).color, Is.EqualTo(InventoryHud.FilledFrame), "Its slot stays dark: no solid yellow square.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C13_C21_TheUnlockNoticeHasABacking_AndTheStoryCardIsClearOfTheSlots()
        {
            yield return Boot();
            var tracker = Object.FindFirstObjectByType<CharacterUnlockTracker>();
            typeof(CharacterUnlockTracker).GetMethod("ShowNotice", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tracker, new object[] { "New character unlocked: Test!" });
            yield return null;
            var backing = tracker.Notice.transform.Find("Backing");
            Assert.IsNotNull(backing, "A dark plate behind the notice.");
            Assert.That(backing.GetComponent<Image>().color.a, Is.GreaterThan(0.8f));

            var card = GameObject.Find("StoryCard");
            if (card != null)
                Assert.That(((RectTransform)card.transform).anchorMin.x, Is.GreaterThanOrEqualTo(0.24f), "The story card starts right of the HUD slots.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator C23_ACoinDroppedInsideAnObstacle_LandsOnOpenGround()
        {
            yield return Boot();
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.layer = LayerMask.NameToLayer("Obstacle");
            block.transform.position = _player.transform.position + new Vector3(12f, 0f, 0f);
            block.transform.localScale = new Vector3(3f, 3f, 3f);
            UnityEngine.Physics.SyncTransforms();
            Assert.That(GroundCheck.IsOpen(block.transform.position, 0.5f), Is.False, "The spot is inside the obstacle.");
            var coin = PickupPoolManager.Instance.SpawnGold(block.transform.position, 1);
            Assert.IsNotNull(coin);
            Assert.That(GroundCheck.IsOpen(coin.transform.position, 0.5f), Is.True, "The coin moved out to open ground.");
            Assert.That(Vector3.Distance(coin.transform.position, block.transform.position), Is.LessThan(10f), "Nearby.");
            var chicken = PickupPoolManager.Instance.SpawnChicken(block.transform.position);
            Assert.That(GroundCheck.IsOpen(chicken.transform.position, 0.5f), Is.True, "The chicken too.");
            Object.Destroy(block);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
