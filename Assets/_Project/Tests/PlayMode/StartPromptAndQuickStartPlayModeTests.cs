using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS main menu (vampire.survivors.wiki/w/Main_menu, raw, fetched 2026-10-12) in the real menu, on a throw-away save:
    /// - "Once the player loads the game, a flashing "PRESS TO START" prompt will appear. When clicked, the player will gain
    ///   access to the main menu": Soul Hunter's prompt shows once per launch, hides the menu's buttons, continues on any
    ///   key or a click, pulses only while the Flashing VFX option is on, and shows the game's version;
    /// - "Quick Start: Immediately accesses a random stage with a randomly selected character", unlocked by the five main
    ///   stages: Soul Hunter's is locked (unusable, saying why) until a character has cleared Level 5, then names an owned
    ///   character, changes its pick each time the menu opens, and starts a run with that character.
    /// </summary>
    public class StartPromptAndQuickStartPlayModeTests : GameplayPlayModeTestBase
    {
        private static GameData Save() => GameServices.Instance.Get<SaveService>().CurrentData;

        private IEnumerator BootMenu(System.Action<GameData> configure)
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            configure?.Invoke(save.CurrentData);
            yield return ReloadMenu();
        }

        /// <summary>The menu reads the save (and decides on the prompt) when it is built: load it again.</summary>
        private IEnumerator ReloadMenu()
        {
            var old = Object.FindFirstObjectByType<MainMenuNavigation>();
            SceneManager.LoadSceneAsync("MainMenu");
            yield return WaitUntilRealtime(() =>
            {
                var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
                return nav != null && nav != old && nav.Layout != null;
            }, "the main menu to load again");
            for (int i = 0; i < 5; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
        }

        [TearDown]
        public void PromptAnswered() => StartPrompt.ShownThisLaunch = true;

        [UnityTest]
        public IEnumerator TheStartPrompt_ShowsOncePerLaunch_HidesTheMenu_AndAnyKeyOrClickContinues()
        {
            StartPrompt.ShownThisLaunch = false; // as on a fresh launch
            yield return BootMenu(data => data.FlashingVfx = true);
            StartPrompt.ShownThisLaunch = false; // the first menu load of this test answered nothing; show it on this one
            yield return ReloadMenu();

            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            var prompt = nav.Prompt;
            Assert.That(prompt != null && StartPrompt.Current == prompt, Is.True, "The prompt is up when the menu first loads. " + Errors());
            Assert.That(prompt.PromptText.text, Is.EqualTo(StartPrompt.KeyLine), "Soul Hunter's own words on a PC.");
            Assert.That(prompt.PromptText.gameObject.activeInHierarchy, Is.True);
            Assert.That(prompt.VersionText.text, Is.EqualTo(StartPrompt.VersionPrefix + Application.version), "The game's version, small in a corner.");
            Assert.That(prompt.VersionText.fontSize, Is.LessThan(prompt.PromptText.fontSizeMax), "The version is the small text.");
            foreach (var button in nav.Layout.Buttons.Where(b => b != null))
            {
                Assert.That(button.IsInteractable(), Is.False, $"{button.name} is out of reach while the prompt is up.");
                Assert.That(button.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f), $"{button.name} is hidden.");
            }

            // The prompt sits where the buttons will be, on screen.
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            Rect text = ScreenRect(prompt.PromptText.rectTransform);
            Assert.That(text.xMin >= -0.5f && text.xMax <= screen.width + 0.5f && text.yMin >= -0.5f && text.yMax <= screen.height + 0.5f, Is.True,
                $"The prompt {text} is on screen.");
            Rect first = ScreenRect((RectTransform)nav.Layout.Buttons.First(b => b != null).transform);
            Rect last = ScreenRect((RectTransform)nav.QuitButton.transform);
            Assert.That(text.center.y, Is.LessThanOrEqualTo(first.yMax + 1f).And.GreaterThanOrEqualTo(last.yMin - 1f), "Where the buttons will be.");
            Rect version = ScreenRect(prompt.VersionText.rectTransform);
            Assert.That(version.xMax <= screen.width + 0.5f && version.yMin >= -0.5f && version.center.x > screen.width * 0.5f && version.center.y < screen.height * 0.5f,
                Is.True, $"The version {version} is in the bottom-right corner.");

            // Flashing: a slow pulse, and steady with the Flashing VFX option off.
            Assert.That(StartPrompt.PulseAt(0f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(StartPrompt.PulseAt(StartPrompt.PulseSeconds * 0.5f), Is.EqualTo(StartPrompt.PulseFloor).Within(0.001f));
            Assert.That(1f / StartPrompt.PulseSeconds, Is.LessThan(3f), "Well under three flashes a second.");
            yield return null;
            Assert.That(prompt.IsFlashing, Is.True, "Flashing VFX on: the prompt pulses.");
            Save().FlashingVfx = false;
            yield return null;
            yield return null;
            Assert.That(prompt.IsFlashing, Is.False, "Flashing VFX off: no blinking.");
            Assert.That(prompt.PromptAlpha, Is.EqualTo(1f), "The text is steady at full strength.");

            // Any key continues.
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Assert.That(prompt.Continued, Is.True, "A key press answers the prompt. " + Errors());
                foreach (var button in nav.Layout.Buttons.Where(b => b != null))
                    Assert.That(button.IsInteractable(), Is.False, "The press that continues cannot also press a button: the menu waits a frame.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            yield return null;
            yield return null;
            yield return null;
            Assert.That(prompt == null, Is.True, "The prompt is gone.");
            Assert.That(StartPrompt.ShownThisLaunch, Is.True);
            foreach (var button in nav.Layout.Buttons.Where(b => b != null && b != nav.QuickStartButton))
                Assert.That(button.IsInteractable(), Is.True, $"{button.name} is back.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(nav.StartButton.gameObject), "Start is focused.");

            // A later visit to the menu in the same launch: no prompt.
            yield return ReloadMenu();
            nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.That(nav.Prompt == null && StartPrompt.Current == null, Is.True, "Once per launch.");
            Assert.That(nav.StartButton.IsInteractable(), Is.True);

            // A click (or a tap) anywhere continues as well.
            StartPrompt.ShownThisLaunch = false;
            yield return ReloadMenu();
            nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            prompt = nav.Prompt;
            Assert.IsNotNull(prompt, "A fresh launch shows the prompt again.");
            Rect catcher = ScreenRect((RectTransform)prompt.Catcher.transform);
            Assert.That(catcher.width >= screen.width - 1f && catcher.height >= screen.height - 1f, Is.True, "The whole screen takes the click.");
            Click(prompt.Catcher, "the start prompt");
            yield return null;
            yield return null;
            yield return null;
            Assert.That(prompt == null && nav.StartButton.IsInteractable(), Is.True, "A click answers the prompt.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator QuickStart_IsLockedUntilLevel5IsCleared_ThenNamesAnOwnedCharacter_AndStartsARunWithIt()
        {
            yield return BootMenu(data =>
            {
                data.CharacterStages = new List<CharacterStageRecord>();
                // Two characters to pick from: Kael (free) and Corvan (unlocked and bought).
                data.UnlockedCharacters = new List<string> { "Corvan" };
                data.OwnedCharacters = new List<string> { "Corvan" };
                data.SelectedCharacterName = "Kael";
                foreach (var mode in SoulHunter.Gameplay.Data.RunModeOptions.All)
                    SoulHunter.Gameplay.Data.RunModeOptions.Set(data, mode, false);
            });
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            var quick = nav.QuickStartButton;
            Assert.IsNotNull(quick, "The menu has a Quick Start button. " + Errors());
            Assert.That(nav.Layout.Buttons[1], Is.SameAs(quick), "Right after Start.");

            // Locked: shown, unusable, and it says what opens it in words.
            Assert.That(QuickStart.IsUnlocked(Save()), Is.False);
            Assert.That(quick.gameObject.activeInHierarchy, Is.True, "A locked Quick Start is still shown.");
            Assert.That(quick.interactable, Is.False);
            Assert.That(nav.QuickStartText, Is.EqualTo(QuickStart.LockedLabel));
            StringAssert.Contains("Level 5", nav.QuickStartText);
            Assert.That(nav.QuickStartPick, Is.Null);
            var label = quick.GetComponentInChildren<TMPro.TMP_Text>();
            Assert.That(label.color, Is.EqualTo(UITheme.TextLocked), "The theme's unusable look.");
            nav.StartQuickRun();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "A locked Quick Start starts nothing.");

            // Levels 1-4 are not enough; Level 5 with any character opens it.
            CharacterStages.Record(Save(), "Corvan", 4);
            nav.ShowHome();
            Assert.That(quick.interactable, Is.False, "Level 4 does not open it.");
            CharacterStages.Record(Save(), "Corvan", 5);
            nav.ShowHome();
            yield return null;
            Assert.That(quick.interactable, Is.True, "Cleared Level 5: Quick Start is open.");
            var owned = QuickStart.Owned(Save());
            Assert.That(owned.Select(c => c.CharacterName), Is.EquivalentTo(new[] { "Kael", "Corvan" }));
            Assert.That(owned, Does.Contain(nav.QuickStartPick), "The pick is a character the save owns.");
            Assert.That(nav.QuickStartText, Is.EqualTo(QuickStart.Label + ": " + nav.QuickStartPick.CharacterName), "The button names who it starts with.");
            Assert.That(label.color, Is.Not.EqualTo(UITheme.TextLocked), "The usable look is back.");

            // The pick changes each time the menu opens.
            for (int i = 0; i < 4; i++)
            {
                var before = nav.QuickStartPick;
                nav.ShowHome();
                Assert.That(nav.QuickStartPick, Is.Not.SameAs(before), "A new pick each time the menu opens.");
                Assert.That(owned, Does.Contain(nav.QuickStartPick));
                StringAssert.EndsWith(nav.QuickStartPick.CharacterName, nav.QuickStartText);
            }
            Assert.That(QuickStart.Pick(Save(), 0f).CharacterName, Is.Not.EqualTo(QuickStart.Pick(Save(), 0.99f).CharacterName), "The roll decides.");

            // The button keeps the menu's one size; its label stays inside it.
            yield return null;
            Rect start = ScreenRect((RectTransform)nav.StartButton.transform), q = ScreenRect((RectTransform)quick.transform);
            Assert.That(q.width, Is.EqualTo(start.width).Within(0.5f));
            Assert.That(q.height, Is.EqualTo(start.height).Within(0.5f));
            label.ForceMeshUpdate();
            Assert.That(label.isTextTruncated, Is.False, "The name is not cut short.");

            // It starts the run at once, with the picked character.
            string picked = nav.QuickStartPick.CharacterName;
            Click(quick, "Quick Start");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "Main_Gameplay" && PlayerController.Instance != null,
                "Quick Start to load the run");
            Assert.That(Save().SelectedCharacterName, Is.EqualTo(picked), "The run plays the picked character.");
            Assert.That(CharacterUnlocks.IsOwned(SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacterExact(picked), Save()), Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
