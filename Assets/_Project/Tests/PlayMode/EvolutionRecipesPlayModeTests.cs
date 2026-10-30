using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Release items P3-01 and P3-06 (2026-10-09) in the real game, on a throw-away save:
    /// - an evolution from a chest is saved as discovered; one handed out by a debug/bot start is not (wiki Grim_Grimoire:
    ///   "evolutions that have been obtained before");
    /// - level-up cards hint discovered recipes only, on weapon and passive cards (wiki Level_up, 0.3.2 and 1.4.200);
    /// - the pause screen's "Evolutions" list shows discovered recipes, unknown ones as unknown, held items marked in words
    ///   and with the chosen-item frame, the run's evolution fully highlighted, and pages (wiki Grim_Grimoire);
    /// - its layout is on screen and readable (4.5:1) on a PC (16:9, 21:9, 5:4) and on a phone held either way.
    /// </summary>
    public class EvolutionRecipesPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string RealSave => Path.Combine(Application.persistentDataPath, "soulhunter_save.json");

        private static GameData Save() => GameServices.Instance.Get<SaveService>().CurrentData;

        private static UpgradeData Level(UpgradeData.UpgradeType type, int level)
        {
            var definition = WeaponDefinitionLookup.Find(type);
            Assert.IsNotNull(definition, $"{type} has a WeaponDefinition.");
            var upgrade = definition.GetLevel(level);
            Assert.IsNotNull(upgrade, $"{type} has a level {level} upgrade.");
            return upgrade;
        }

        private IEnumerator Boot(params string[] discovered)
        {
            yield return BootIntoGameplay(save =>
            {
                save.DiscoveredEvolutions = new List<string>(discovered);
            });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
        }

        [UnityTest]
        public IEnumerator AChestEvolution_IsSavedAsDiscovered_ButADebugStartEvolutionIsNot()
        {
            byte[] realBefore = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            yield return Boot();
            var manager = WeaponManager.Instance;

            // A debug or bot start (-sh-autoplay-evolve) evolves straight through the WeaponManager: not a discovery.
            manager.GiveWeapon(UpgradeData.UpgradeType.Whip, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.MaxHealth, 1);
            Assert.That(manager.TryEvolveWeapon(out _), Is.True, "Whip 8 + Hollow Heart evolves.");
            Assert.That(manager.LastEvolutionId, Is.EqualTo("Evo_BloodyTear"));
            Assert.That(manager.MadeThisRun("Evo_BloodyTear"), Is.True);
            Assert.That(Save().DiscoveredEvolutions, Does.Not.Contain("Evo_BloodyTear"), "A debug start writes nothing to the save.");

            // A real chest that can evolve: the evolution is now discovered on the save.
            manager.GiveWeapon(UpgradeData.UpgradeType.Garlic, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.Pummarola, 1);
            var logic = Object.FindFirstObjectByType<ChestLogicController>();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            logic.ProcessChest(manager, levelUp, 1, canEvolve: true);
            Assert.That(logic.LastResult.Rewards.Any(r => r.IsEvolution), Is.True, "The chest evolved a weapon. " + Errors());
            Assert.That(manager.LastEvolutionId, Is.EqualTo("Evo_SoulEater"));
            Assert.That(Save().DiscoveredEvolutions, Is.EqualTo(new[] { "Evo_SoulEater" }), "Only the chest's evolution is discovered.");

            // A chest that cannot evolve discovers nothing.
            manager.GiveWeapon(UpgradeData.UpgradeType.MagicWand, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.EmptyTome, 1);
            logic.ProcessChest(manager, levelUp, 1, canEvolve: false);
            Assert.That(Save().DiscoveredEvolutions, Does.Not.Contain("Evo_HolyWand"));

            byte[] realAfter = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            Assert.That(realAfter, Is.EqualTo(realBefore), "The owner's real save is untouched.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelUpCards_HintOnlyDiscoveredRecipes_OnWeaponAndPassiveCards()
        {
            yield return Boot();
            var player = PlayerController.Instance;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a real level-up screen");
            yield return null;

            var ui = Object.FindFirstObjectByType<LevelUpUI>();
            var show = typeof(LevelUpUI).GetMethod("ShowLevelUpScreen", Private);
            var cards = ((Button[])typeof(LevelUpUI).GetField("_upgradeButtons", Private).GetValue(ui)).Where(b => b != null).ToArray();
            var whip = Level(UpgradeData.UpgradeType.Whip, 2);
            var hollowHeart = Level(UpgradeData.UpgradeType.MaxHealth, 1);
            var garlic = Level(UpgradeData.UpgradeType.Garlic, 1);
            var choices = new List<UpgradeData> { whip, hollowHeart, garlic };

            show.Invoke(ui, new object[] { choices });
            Canvas.ForceUpdateCanvases();
            yield return null;
            foreach (var card in cards.Take(3))
                Assert.That(Hint(card) == null || !Hint(card).gameObject.activeSelf, Is.True, $"{card.name}: no hint before anything is discovered.");

            Save().DiscoveredEvolutions.Add("Evo_BloodyTear");
            show.Invoke(ui, new object[] { choices });
            Canvas.ForceUpdateCanvases();
            yield return null;

            var whipHint = Hint(cards[0]);
            Assert.That(whipHint != null && whipHint.gameObject.activeInHierarchy, Is.True, "The Whip card hints Bloody Tear.");
            var whipText = HintText(cards[0]);
            Assert.That(whipText.text, Does.Contain(UpgradeText.NameOf(UpgradeData.UpgradeType.MaxHealth)).And.Contain("Crimson Lash"));
            var counterpart = whipHint.Find(LevelUpUI.EvoHintIconName + "0/Icon").GetComponent<Image>();
            Assert.That(counterpart.sprite, Is.EqualTo(EvolutionRecipes.IconOf(UpgradeData.UpgradeType.MaxHealth)), "The other item's own icon.");
            var result = whipHint.Find(LevelUpUI.EvoHintIconName + "1").GetComponent<Outline>();
            Assert.That(result.enabled && result.effectColor == UITheme.Hud.SlotEvolved, Is.True, "The result wears the HUD's evolved frame.");
            Assert.That(whipHint.Find(LevelUpUI.EvoHintIconName + "2").gameObject.activeSelf, Is.False, "Only the icons the recipe needs.");

            var passiveHint = Hint(cards[1]);
            Assert.That(passiveHint != null && passiveHint.gameObject.activeInHierarchy, Is.True, "The passive card hints it too (1.4.200).");
            Assert.That(HintText(cards[1]).text, Does.Contain(UpgradeText.NameOf(UpgradeData.UpgradeType.Whip)));
            Assert.That(Hint(cards[2]) == null || !Hint(cards[2]).gameObject.activeSelf, Is.True, "Garlic's recipe is not discovered.");

            // Readable: 4.5:1 on the card, inside the card, not cut short; the description makes room for it.
            foreach (var card in cards.Take(2))
            {
                var text = HintText(card);
                text.ForceMeshUpdate();
                Assert.That(UITheme.Contrast(text.color, card.colors.normalColor), Is.GreaterThanOrEqualTo(4.5f), $"{card.name}: the hint reads.");
                Assert.That(text.isTextTruncated, Is.False, $"{card.name}: '{text.text}' fits.");
                Assert.That(Inside(text.rectTransform, (RectTransform)card.transform), Is.True, $"{card.name}: the hint stays on the card.");
                var description = (RectTransform)card.transform.Find(LevelUpUI.DescriptionName);
                Assert.That(Overlap(description, Hint(card)), Is.False, $"{card.name}: the description is clear of the hint.");
            }

            // Banish mode: the cards are being removed, not built on.
            levelUp.AddCharges(0, 0, 1);
            typeof(LevelUpUI).GetMethod("OnBanishClicked", Private).Invoke(ui, null);
            yield return null;
            Assert.That(Hint(cards[0]).gameObject.activeSelf, Is.False, "No hint in Banish mode.");
            typeof(LevelUpUI).GetMethod("OnBanishClicked", Private).Invoke(ui, null);

            yield return CloseOpenLevelUps(); // takes a card the real round offered
            Assert.That(levelUp.IsLevelUpActive, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ThePauseScreen_ListsTheRecipes_WithHeldItemsMarked_AndPages()
        {
            yield return Boot("Evo_BloodyTear");
            var manager = WeaponManager.Instance;
            manager.GiveWeapon(UpgradeData.UpgradeType.MaxHealth, 1); // Kael already holds the Whip
            // A debug start's evolution: shown (and fully highlighted) for this run, still not discovered on the save.
            manager.GiveWeapon(UpgradeData.UpgradeType.Garlic, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.Pummarola, 1);
            Assert.That(manager.TryEvolveWeapon(out _), Is.True);
            Assert.That(manager.LastEvolutionId, Is.EqualTo("Evo_SoulEater"));

            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            Assert.IsNotNull(pause.EvolutionsButton, "The pause menu has an Evolutions button.");
            Assert.That(pause.EvolutionsButton.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Evolutions"));
            Rect evo = ScreenRect((RectTransform)pause.EvolutionsButton.transform), resume = ScreenRect((RectTransform)pause.ResumeButton.transform);
            // Owner-approved responsive pause screen (2026-10-10): one column of buttons, so Evolutions sits in Resume's column, not beside it.
            Assert.That(Mathf.Abs(evo.center.x - resume.center.x), Is.LessThan(2f), "In Resume's column.");
            Assert.That(evo.yMax, Is.LessThanOrEqualTo(resume.yMin + 0.5f), "Below Resume.");
            Assert.That(OnScreen(evo), Is.True, "On screen.");
            foreach (var other in new[] { pause.ResumeButton, pause.OptionsButton, pause.QuitButton })
                Assert.That(evo.Overlaps(ScreenRect((RectTransform)other.transform)), Is.False, $"Clear of {other.name}.");
            var stats = system.PauseMenuPanel.GetComponentInChildren<StatsPanel>(true);
            if (stats != null) Assert.That(evo.Overlaps(ScreenRect((RectTransform)stats.transform)), Is.False, "Clear of the stats.");

            Click(pause.EvolutionsButton, "Evolutions");
            yield return null;
            var screen = pause.RecipesScreen;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The recipes open. " + Errors());
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(screen.BackButton.gameObject), "Back is focused.");
            Assert.AreEqual(0f, Time.timeScale, "Still paused.");
            // Bug MIS UI-06: Esc / B go back to the pause menu (as in Options), so the Pause action is off while open.
            Assert.That(screen.HoldsPauseKey, Is.True, "The list holds the Pause action.");
            Assert.That(PauseActionEnabled(), Is.False, "Esc must not resume the game under the list.");

            int total = EvolutionRecipes.All(manager).Count;
            Assert.That(screen.Recipes.Count, Is.EqualTo(total));
            Assert.That(screen.PageCount, Is.EqualTo((total + EvolutionRecipesScreen.RowsPerPage - 1) / EvolutionRecipesScreen.RowsPerPage));
            Assert.That(screen.Recipes.Take(2).Select(r => r.Id), Is.EquivalentTo(new[] { "Evo_BloodyTear", "Evo_SoulEater" }),
                "Known recipes come first.");

            var bloodyTear = screen.Rows.First(r => r.Recipe.Id == "Evo_BloodyTear");
            Assert.That(bloodyTear.State, Is.EqualTo(EvolutionRecipes.RowState.Open));
            Assert.That(bloodyTear.NameText.text, Does.Contain("Crimson Lash"));
            Assert.That(bloodyTear.HeldItems, Is.EquivalentTo(new[] { UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.MaxHealth }));
            Assert.That(bloodyTear.HeldLabels.Count, Is.EqualTo(2), "Each held item is labelled Held (not by colour alone).");
            Assert.That(bloodyTear.StatusText.text, Does.StartWith("Held: "));
            var edge = UiThemeAsset.Active.Cards.SelectedEdge;
            Assert.That(bloodyTear.Slots[0].GetComponent<Outline>().effectColor, Is.EqualTo(edge), "A held item wears the chosen-item frame.");
            Assert.That(bloodyTear.Slots.Last().transform.Find("Icon").GetComponent<Image>().sprite,
                Is.EqualTo(EvolutionRecipes.IconOf(UpgradeData.UpgradeType.Whip)), "The result shows its base weapon's icon (no new art).");

            var soulEater = screen.Rows.First(r => r.Recipe.Id == "Evo_SoulEater");
            Assert.That(soulEater.State, Is.EqualTo(EvolutionRecipes.RowState.Made));
            Assert.That(soulEater.StatusText.text, Is.EqualTo(EvolutionRecipesScreen.MadeStatus));
            var rowEdge = soulEater.Root.GetComponent<Outline>();
            Assert.That(rowEdge != null && rowEdge.enabled && rowEdge.effectColor == edge, Is.True, "The run's evolution is fully highlighted.");
            Assert.That(Save().DiscoveredEvolutions, Does.Not.Contain("Evo_SoulEater"), "Still not discovered on the save.");

            var unknown = screen.Rows.First(r => r.State == EvolutionRecipes.RowState.Unknown);
            Assert.That(unknown.NameText.text, Does.Contain(EvolutionRecipesScreen.UnknownName), "Undiscovered recipes stay unknown.");
            Assert.That(unknown.Slots.All(s => s.transform.Find("Icon") == null), Is.True, "No item icons on an unknown recipe.");

            // Responsive (owner, 2026-10-10): laid out on a PC (16:9, 21:9, 5:4) and on the owner's phone held either way.
            var canvas = screen.GetComponent<Canvas>();
            var columns = new Dictionary<string, int>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    for (int i = 0; i < 2; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                    Canvas.ForceUpdateCanvases();
                    CheckLayout(screen, d.Name);
                    columns[d.Name] = screen.ShownColumns;
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(columns["PC 1920x1080"], Is.EqualTo(EvolutionRecipesScreen.Columns), "Two columns of four on a PC.");
            Assert.That(columns["Phone portrait"], Is.EqualTo(1), "One column on a phone held upright.");
            yield return ScreenSimulation.AssertFitsEveryDevice(() => new Selectable[] { screen.PreviousButton, screen.BackButton, screen.NextButton }, canvas);
            for (int i = 0; i < 2; i++) { Canvas.ForceUpdateCanvases(); yield return null; }

            // Pages.
            Assert.That(screen.PreviousButton.interactable, Is.False);
            if (screen.PageCount > 1)
            {
                Click(screen.NextButton, "Next");
                yield return null;
                Assert.That(screen.Page, Is.EqualTo(1));
                Assert.That(screen.Rows.Where(r => r.Root.gameObject.activeSelf).All(r => r.State == EvolutionRecipes.RowState.Unknown), Is.True,
                    "Page 2 holds the rest.");
                Click(screen.PreviousButton, "Previous");
                yield return null;
                Assert.That(screen.Page, Is.EqualTo(0));
            }

            Click(screen.BackButton, "Back");
            yield return null;
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(pause.EvolutionsButton.gameObject), "Back returns to Evolutions.");
            Assert.That(PauseActionEnabled(), Is.True, "Back puts the Pause action back.");

            // Resuming from code (the pause key while open is off) closes the recipes with it.
            Click(pause.EvolutionsButton, "Evolutions");
            yield return null;
            Assert.That(PauseActionEnabled(), Is.False);
            system.Resume();
            yield return null;
            Assert.That(screen.IsOpen, Is.False, "Resuming closes the recipes.");
            Assert.That(PauseActionEnabled(), Is.True, "Closed with the pause menu: the Pause action is back.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- layout checks

        /// <summary>
        /// Checks the screen as ScreenSimulation laid it out: the window inside the canvas, the header and the buttons inside
        /// the window and readable, the buttons apart, the rows inside the scroll area's width and apart from each other, and
        /// every row in view readable with its slots inside it and clear of its text.
        /// </summary>
        private static void CheckLayout(EvolutionRecipesScreen screen, string what)
        {
            var root = (RectTransform)screen.transform;
            Rect canvasRect = Local(root, root);
            Rect windowRect = Local(root, screen.Window);
            Assert.That(Contains(canvasRect, windowRect), Is.True, $"{what}: the window {windowRect} is inside the screen {canvasRect}.");
            var panel = screen.Window.GetComponent<Image>().color;
            foreach (var text in new[] { screen.TitleText, screen.NoteText, screen.PageText })
                Readable(text, panel, root, windowRect, $"{what} {text.name}");

            var buttons = new[] { screen.PreviousButton, screen.BackButton, screen.NextButton };
            var buttonRects = buttons.Select(b => Local(root, (RectTransform)b.transform)).ToList();
            for (int i = 0; i < buttons.Length; i++)
            {
                Assert.That(Contains(windowRect, buttonRects[i]), Is.True, $"{what}: {buttons[i].name} {buttonRects[i]} is inside the window.");
                for (int j = i + 1; j < buttons.Length; j++)
                    Assert.That(buttonRects[i].Overlaps(buttonRects[j]), Is.False, $"{what}: {buttons[i].name} and {buttons[j].name} overlap.");
                var label = buttons[i].GetComponentsInChildren<TextMeshProUGUI>().First(t => !string.IsNullOrWhiteSpace(t.text));
                var plate = buttons[i].IsInteractable() ? buttons[i].colors.normalColor : buttons[i].colors.disabledColor;
                float need = buttons[i].IsInteractable() ? 4.5f : 3f; // a locked button reads "not now" (Power Up shop rule)
                Assert.That(UITheme.Contrast(label.color, plate), Is.GreaterThanOrEqualTo(need), $"{what}: '{label.text}' reads on its button.");
            }

            // The rows scroll inside their view, which sits inside the window and clear of the buttons.
            Rect view = Local(root, screen.Scroll.viewport);
            Assert.That(Contains(windowRect, view), Is.True, $"{what}: the rows' view {view} is inside the window.");
            foreach (var b in buttonRects) Assert.That(view.Overlaps(b), Is.False, $"{what}: the rows' view is clear of the buttons.");
            Assert.That(view.Overlaps(Local(root, screen.NoteText.rectTransform)), Is.False, $"{what}: the rows' view is under the note.");

            var rows = screen.Rows.Where(r => r.Root.gameObject.activeSelf).ToList();
            var rowRects = rows.Select(r => Local(root, r.Root)).ToList();
            int shown = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.That(rowRects[i].xMin >= view.xMin - 0.5f && rowRects[i].xMax <= view.xMax + 0.5f, Is.True, $"{what}: row {i} {rowRects[i]} is inside the view's width {view}.");
                for (int j = i + 1; j < rows.Count; j++)
                    Assert.That(rowRects[i].Overlaps(rowRects[j]), Is.False, $"{what}: rows {i} and {j} overlap.");
                if (!Contains(view, rowRects[i])) continue; // scrolled out of view: masked
                shown++;
                var back = rows[i].Background.color;
                Readable(rows[i].NameText, back, root, rowRects[i], $"{what} row {i} name");
                Readable(rows[i].StatusText, back, root, rowRects[i], $"{what} row {i} status");
                foreach (var held in rows[i].HeldLabels) Readable(held, back, root, rowRects[i], $"{what} row {i} Held");
                Rect name = Local(root, rows[i].NameText.rectTransform), status = Local(root, rows[i].StatusText.rectTransform);
                foreach (var slot in rows[i].Slots)
                {
                    Rect s = Local(root, (RectTransform)slot.transform);
                    Assert.That(Contains(rowRects[i], s), Is.True, $"{what}: row {i} slot {slot.name} is inside its row.");
                    Assert.That(s.Overlaps(name) || s.Overlaps(status), Is.False, $"{what}: row {i} slot {slot.name} is clear of the text.");
                }
            }
            Assert.That(shown, Is.GreaterThanOrEqualTo(1), $"{what}: at least one row is in view.");
        }

        private static void Readable(TextMeshProUGUI text, Color back, RectTransform root, Rect area, string what)
        {
            text.ForceMeshUpdate();
            Assert.That(UITheme.Contrast(text.color, back), Is.GreaterThanOrEqualTo(4.5f), $"{what}: '{text.text}' contrast.");
            Assert.That(text.isTextTruncated, Is.False, $"{what}: '{text.text}' is not cut short.");
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(Mathf.Max(14f, UiScreen.MinTextUnits) - 0.01f), $"{what}: '{text.text}' is at least 14 (12 sp on a phone).");
            Assert.That(Contains(area, Local(root, text.rectTransform)), Is.True, $"{what}: '{text.text}' stays inside.");
        }

        private static Rect Local(RectTransform root, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = root.InverseTransformPoint(corners[0]), b = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static bool OnScreen(Rect r) => r.xMin >= -0.5f && r.yMin >= -0.5f && r.xMax <= Screen.width + 0.5f && r.yMax <= Screen.height + 0.5f;

        /// <summary>Bug MIS UI-06: the game's Pause action (Esc / Start) is on.</summary>
        private static bool PauseActionEnabled()
        {
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            var action = input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
            return action != null && action.enabled;
        }

        private static Transform Hint(Button card) => card.transform.Find(LevelUpUI.EvoHintName);

        private static TextMeshProUGUI HintText(Button card) =>
            Hint(card).Find(LevelUpUI.EvoHintTextName).GetComponent<TextMeshProUGUI>();

        private static bool Inside(RectTransform inner, RectTransform outer) => Contains(ScreenRect(outer), ScreenRect(inner));

        private static bool Overlap(RectTransform a, Transform b)
        {
            Rect ra = ScreenRect(a), rb = ScreenRect((RectTransform)b);
            // Touching edges are fine; a real overlap is wider than a pixel.
            return ra.xMin < rb.xMax - 1f && rb.xMin < ra.xMax - 1f && ra.yMin < rb.yMax - 1f && rb.yMin < ra.yMax - 1f;
        }
    }
}
