using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report 2026-10-01 ("level up panel ke neeche skip etc 3 button kaam nahi karte"). VS (wiki Level_up):
    /// Reroll, Skip and Banish "become available in this screen if the player invests in these PowerUps"; they start at
    /// 0. With none, the buttons are hidden; with uses, a real click on each does its job.
    /// </summary>
    public class LevelUpActionButtonsPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator WithoutUses_TheButtonsAreHidden_WithUses_EachClickWorks()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            var ui = Object.FindFirstObjectByType<LevelUpUI>();

            // No PowerUps: 0 uses, no buttons on the screen.
            Assume.That(levelUp.RerollsLeft + levelUp.SkipsLeft + levelUp.BanishesLeft, Is.EqualTo(0));
            yield return OpenLevelUp(levelUp, experience);
            foreach (var name in new[] { "_rerollButton", "_skipButton", "_banishButton" })
            {
                var button = Action(ui, name);
                Assert.That(button == null || !button.gameObject.activeInHierarchy, Is.True, $"{name} is hidden with 0 uses.");
            }
            levelUp.SelectUpgrade(Choices(ui)[0]);
            yield return null;

            // With uses: each button shows and a real click (through the UI raycast) does its job.
            levelUp.AddCharges(1, 1, 1);
            yield return OpenLevelUp(levelUp, experience);
            var reroll = Action(ui, "_rerollButton");
            var skip = Action(ui, "_skipButton");
            var banish = Action(ui, "_banishButton");
            foreach (var button in new[] { reroll, skip, banish })
                Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True, $"{button.name} shows with a use left.");

            var before = Choices(ui).ToList();
            yield return Click(reroll);
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(0), "Reroll used.");
            Assert.That(levelUp.IsLevelUpActive, Is.True, "Still choosing after a reroll.");
            Assert.That(reroll.gameObject.activeInHierarchy, Is.False, "Reroll hides once used up.");

            yield return Click(banish);
            Assert.That((bool)typeof(LevelUpUI).GetField("_banishMode", Private).GetValue(ui), Is.True, "Banish is armed.");
            yield return Click(((Button[])typeof(LevelUpUI).GetField("_upgradeButtons", Private).GetValue(ui))[0]);
            Assert.That(levelUp.BanishesLeft, Is.EqualTo(0), "Banish used on the clicked card.");

            if (!levelUp.IsLevelUpActive) yield return OpenLevelUp(levelUp, experience);
            skip = Action(ui, "_skipButton");
            float xp = experience.CurrentXP;
            yield return Click(skip);
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(0), "Skip used.");
            Assert.That(levelUp.IsLevelUpActive, Is.False, "Skip closes the level-up.");
            Assert.That(experience.CurrentXP, Is.GreaterThan(xp), "Skip gives experience (wiki Skip).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static IEnumerator OpenLevelUp(LevelUpManager levelUp, PlayerExperience experience)
        {
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            float until = Time.realtimeSinceStartup + 5f;
            while (!levelUp.IsLevelUpActive && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(levelUp.IsLevelUpActive, Is.True, "A level-up opened.");
            yield return null;
            yield return null;
        }

        private static Button Action(LevelUpUI ui, string field) => (Button)typeof(LevelUpUI).GetField(field, Private).GetValue(ui);

        private static List<UpgradeData> Choices(LevelUpUI ui) => (List<UpgradeData>)typeof(LevelUpUI).GetField("_currentChoices", Private).GetValue(ui);

        /// <summary>Clicks where a mouse would: the top UI object under the button's centre must be the button (or its child).</summary>
        private static IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current) { position = centre, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), $"Something is under {button.name} at {centre}.");
            var top = hits[0].gameObject;
            Assert.That(top.transform.IsChildOf(button.transform), Is.True, $"{button.name} is on top at its centre, not {top.name}.");
            data.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
            yield return null;
            yield return null;
        }
    }
}
