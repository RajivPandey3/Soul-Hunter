using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS 2026-09-30: B12 (first level-up had larger fonts), B13 (brown focus), B15 (a single card stretched and the
    /// layout jumped), B16 (blue main menu focus) and B17 (low-contrast disabled Reroll/Skip/Banish).
    /// </summary>
    public class LevelUpScreenPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator TheLevelUpScreen_KeepsItsSizes_ColoursAndLayout()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            List<UpgradeData> choices = null;
            levelUp.OnLevelUpUIRound += round => choices = round;
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "the first level-up screen");
            yield return null;
            yield return null;

            var ui = Object.FindFirstObjectByType<LevelUpUI>();
            var cards = ((Button[])typeof(LevelUpUI).GetField("_upgradeButtons", Private).GetValue(ui)).Where(b => b != null).ToArray();
            // B12: the card texts keep their own limits (TMP's default auto-size maximum is 72).
            foreach (var text in cards.SelectMany(c => c.GetComponentsInChildren<TextMeshProUGUI>()).Where(t => t.name.StartsWith("Card_")))
                Assert.That(text.fontSizeMax, Is.LessThan(72f), $"{text.name} keeps its size limit on the first level-up.");
            // B13: focus changes at once (no brown midpoint of a fade).
            foreach (var card in cards) Assert.That(card.colors.fadeDuration, Is.EqualTo(0f), $"{card.name} focuses at once.");
            // B17: a disabled action button is dark, so its white label reads.
            var actions = cards[0].transform.parent.GetComponentsInChildren<Button>().Where(b => !cards.Contains(b) && !b.interactable).ToList();
            foreach (var action in actions)
            {
                Color disabled = action.colors.disabledColor;
                Assert.That(disabled.r + disabled.g + disabled.b, Is.LessThan(0.9f), $"{action.name} is dark when disabled.");
            }

            // B15: a round with one choice keeps the 3-card layout.
            var show = typeof(LevelUpUI).GetMethod("ShowLevelUpScreen", Private);
            var three = choices.Take(3).ToList();
            Assume.That(three.Count, Is.EqualTo(3), "The first level-up offers three cards.");
            show.Invoke(ui, new object[] { three });
            Canvas.ForceUpdateCanvases();
            yield return null;
            float threeHeight = ((RectTransform)cards[0].transform).rect.height;
            float threeTop = cards[0].transform.position.y;
            show.Invoke(ui, new object[] { new List<UpgradeData> { three[0] } });
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.That(((RectTransform)cards[0].transform).rect.height, Is.EqualTo(threeHeight).Within(2f), "One card keeps its size.");
            Assert.That(cards[0].transform.position.y, Is.EqualTo(threeTop).Within(2f), "The card does not move.");
            levelUp.SelectUpgrade(three[0]);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheMainMenu_FocusesInOrange()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuNavigation>();
            Color selected = menu.StartButton.colors.selectedColor;
            // B16 (2026-09-30): focus was Unity's blue. It is now the theme's focus colour (2026-10-04), which must stand
            // apart from the button and must not be blue.
            // 2026-10-08: the Pro look keeps the button's own colour, brighter, when chosen (the ring marks it).
            Assert.AreEqual(SoulHunter.Gameplay.UI.UITheme.SelectedColour(SoulHunter.Gameplay.UI.UITheme.Primary), selected, "The theme's focus colour.");
            Assert.AreNotEqual(menu.StartButton.colors.normalColor, selected, "The chosen button differs from its normal look.");
            Assert.That(selected.b, Is.LessThan(Mathf.Max(selected.r, selected.g)), "Focus is not blue (B16).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
