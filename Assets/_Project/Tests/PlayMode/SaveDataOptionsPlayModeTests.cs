using System.Collections;
using System.Collections.Generic;
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
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The main menu Options' Save Data section (VS Account section, vampire.survivors.wiki/w/Options, raw, fetched
    /// 2026-10-12), on a throw-away save in its own folder (the real save is checked untouched):
    /// - "DATA RECOVERY ... Restore": both saves side by side in words, one question, Cancel changes nothing, Restore
    ///   brings the previous save back and says what was restored;
    /// - "Delete Save ... only appears after clicking on the Account section button 6 times after initially selecting it",
    ///   then "confirm through all the pop-ups": hidden until the sixth extra press, two questions, either "Keep Save"
    ///   stops it, and only both confirmations delete (a copy is kept, the options stay);
    /// - a run's Options (opened without the flag) have no Save Data section;
    /// - the questions fit on a PC and on a phone held either way.
    /// </summary>
    public class SaveDataOptionsPlayModeTests : GameplayPlayModeTestBase
    {
        private static string RealSave => Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
        private string _folder;

        private static SaveService Service() => GameServices.Instance.Get<SaveService>();

        [TearDown]
        public void RemoveFolder()
        {
            try { if (!string.IsNullOrEmpty(_folder) && Directory.Exists(_folder)) Directory.Delete(_folder, true); }
            catch (System.Exception) { }
        }

        /// <summary>The menu on a save of its own: 100 gold written first, then 900 gold and a bought character.</summary>
        private IEnumerator BootMenuOnTwoSaves()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu");
            _folder = Path.Combine(Application.temporaryCachePath, "savedata_options_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            var save = Service();
            save.UseFile(Path.Combine(_folder, "save.json"));
            var data = save.CurrentData;
            data.Currency = 100;
            data.OwnedCharacters = new List<string>();
            data.UnlockedCharacters = new List<string>();
            data.PowerUpRanks = new List<PowerUpRank>();
            data.ChestsOpened = 3;
            data.FlashingVfx = false; // an option that must survive Delete Save
            Assert.That(save.SaveGame(), Is.True);
            data.Currency = 900;
            data.OwnedCharacters.Add("Corvan");
            data.UnlockedCharacters.Add("Corvan");
            data.ChestsOpened = 7;
            Assert.That(save.SaveGame(), Is.True);
            Assert.That(File.Exists(save.PreviousPath), Is.True, "The save before the last one is kept.");
            yield return ReloadMenu();
        }

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

        private IEnumerator PressSaveTab(OptionsMenuUI options, int times)
        {
            for (int i = 0; i < times; i++)
            {
                Click(options.TabButtons[options.SaveDataTab], "Save Data tab");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SaveData_RestoresThePreviousSave_AndDeletesOnlyAfterSixPressesAndTwoConfirmations()
        {
            byte[] realBefore = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            yield return BootMenuOnTwoSaves();
            var save = Service();
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();

            // A run's Options (no flag) have no Save Data section.
            var plain = OptionsMenuUI.Open();
            yield return null;
            Assert.That(plain.SaveDataTab, Is.EqualTo(-1));
            Assert.That(plain.TabButtons.Count, Is.EqualTo(5), "Favorites, Display, Sound, Gameplay, Controls.");
            Assert.That(plain.TabButtons.Any(t => t.name.Contains(OptionsMenuUI.SaveDataTabName)), Is.False);
            plain.Close();
            yield return null;
            yield return null;
            Assert.That(save.SaveGame(), Is.True); // closing Options saved once more: put the two test saves back in place
            save.CurrentData.Currency = 100;
            save.CurrentData.OwnedCharacters.Clear();
            save.CurrentData.UnlockedCharacters.Clear();
            save.CurrentData.ChestsOpened = 3;
            Assert.That(save.SaveGame(), Is.True);
            save.CurrentData.Currency = 900;
            save.CurrentData.OwnedCharacters.Add("Corvan");
            save.CurrentData.UnlockedCharacters.Add("Corvan");
            save.CurrentData.ChestsOpened = 7;
            Assert.That(save.SaveGame(), Is.True);

            // The main menu's Options have it, as the last tab.
            Click(nav.OptionsButton, "Options");
            yield return null;
            var options = OptionsMenuUI.Current;
            Assert.IsNotNull(options, "Options opened. " + Errors());
            int tab = options.SaveDataTab;
            Assert.That(tab, Is.EqualTo(options.TabButtons.Count - 1), "Save Data is the last section.");
            Assert.That(options.TabButtons[tab].GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.SaveDataTabName));

            // Selecting the section: both saves side by side in words; Delete Save is not there.
            yield return PressSaveTab(options, 1);
            Assert.That(options.CurrentTab, Is.EqualTo(tab));
            string compare = options.SaveComparisonText;
            StringAssert.Contains("Current save: 900 gold, 1 character bought", compare);
            StringAssert.Contains("7 chests opened", compare);
            StringAssert.Contains("Previous save: 100 gold, 0 characters bought", compare);
            StringAssert.Contains("3 chests opened", compare);
            StringAssert.Contains("Power Up rank", compare);
            Assert.That(options.RestoreButton.gameObject.activeInHierarchy && options.RestoreButton.interactable, Is.True, "There is a save to restore.");
            Assert.That(options.DeleteSaveShown, Is.False, "Delete Save is hidden at first.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.RestoreButton.gameObject), "The section's first control is focused.");

            // wiki: it appears only after 6 more presses of the section's button.
            yield return PressSaveTab(options, OptionsMenuUI.DeletePressesNeeded - 1);
            Assert.That(options.DeleteSaveShown, Is.False, "Five more presses: still hidden.");
            yield return PressSaveTab(options, 1);
            Assert.That(options.DeleteSaveShown, Is.True, "The sixth press shows Delete Save.");
            Assert.That(options.DeleteSaveButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.DeleteLabel));
            // Leaving the section and coming back starts the count again.
            Click(options.TabButtons[0], "Favorites tab");
            yield return null;
            yield return PressSaveTab(options, 1);
            Assert.That(options.DeleteSaveShown, Is.False, "Selecting the section again hides it: the count starts over.");

            // Data Recovery: one question with both saves; Cancel is the chosen answer and changes nothing.
            Click(options.RestoreButton, "Restore");
            yield return null;
            Assert.That(options.DialogOpen, Is.True, "Restore asks first.");
            StringAssert.Contains("900 gold", options.DialogText);
            StringAssert.Contains("100 gold", options.DialogText);
            StringAssert.Contains("Current save:", options.DialogText);
            StringAssert.Contains("Previous save:", options.DialogText);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.DialogCancelButton.gameObject), "The safe answer is the chosen one.");
            Assert.That(options.DialogConfirmButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.RestoreLabel));
            Assert.That(options.DialogCancelButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.CancelLabel));
            yield return DialogFitsEveryScreen(options);
            Click(options.DialogCancelButton, "Cancel");
            yield return null;
            Assert.That(options.DialogOpen, Is.False);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(900), "Cancel restores nothing.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.RestoreButton.gameObject), "Focus returns to Restore.");
            Assert.That(options.SaveReplaced, Is.False);

            Click(options.RestoreButton, "Restore");
            yield return null;
            Click(options.DialogConfirmButton, "Restore (confirm)");
            yield return null;
            Assert.That(options.DialogOpen, Is.False);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(100), "The previous save is the save again. " + Errors());
            Assert.That(save.CurrentData.OwnedCharacters, Is.Empty);
            Assert.That(File.Exists(save.ReplacedBackupPath), Is.True, "The replaced save is kept.");
            StringAssert.StartsWith("Restored the previous save: 100 gold", options.SaveStatusText);
            StringAssert.Contains("Current save: 100 gold", options.SaveComparisonText);
            Assert.That(options.SaveReplaced, Is.True);
            Assert.That(GameSettings.Data, Is.SameAs(save.CurrentData), "The options now read the restored save.");

            // Delete Save: six presses again, then two questions.
            yield return PressSaveTab(options, OptionsMenuUI.DeletePressesNeeded);
            Assert.That(options.DeleteSaveShown, Is.True);
            Click(options.DeleteSaveButton, "Delete Save");
            yield return null;
            Assert.That(options.DialogOpen, Is.True, "Delete Save asks first.");
            StringAssert.StartsWith("Delete your save?", options.DialogText);
            StringAssert.Contains("100 gold", options.DialogText);
            Assert.That(options.DialogConfirmButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.DeleteLabel));
            Assert.That(options.DialogCancelButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(OptionsMenuUI.KeepLabel));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.DialogCancelButton.gameObject), "Keep Save is the chosen answer.");
            Assert.That(options.DialogConfirmButton.colors.normalColor, Is.EqualTo(UITheme.DangerButton), "The deleting answer wears the leaving colour.");
            Click(options.DialogConfirmButton, "Delete Save (first question)");
            yield return null;
            Assert.That(options.DialogOpen, Is.True, "One confirmation is not enough: a second question.");
            StringAssert.Contains("cannot be undone", options.DialogText);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(100), "Nothing is deleted yet.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.DialogCancelButton.gameObject));
            Click(options.DialogCancelButton, "Keep Save");
            yield return null;
            Assert.That(options.DialogOpen, Is.False);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(100), "Keep Save at the second question deletes nothing.");
            Assert.That(File.Exists(save.FilePath), Is.True);
            Assert.That(save.LastDeletedBackupPath, Is.Null);

            Click(options.DeleteSaveButton, "Delete Save");
            yield return null;
            Click(options.DialogConfirmButton, "Delete Save (first question)");
            yield return null;
            Click(options.DialogConfirmButton, "Delete Save (second question)");
            yield return null;
            Assert.That(options.DialogOpen, Is.False);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(0), "Both confirmations: the save is deleted. " + Errors());
            Assert.That(save.CurrentData.ChestsOpened, Is.EqualTo(0));
            Assert.That(save.CurrentData.FlashingVfx, Is.False, "The options are kept.");
            Assert.That(save.LastDeletedBackupPath != null && File.Exists(save.LastDeletedBackupPath), Is.True, "A copy of the deleted save is kept.");
            StringAssert.StartsWith("Save deleted.", options.SaveStatusText);
            StringAssert.Contains(Path.GetFileName(save.LastDeletedBackupPath), options.SaveStatusText);
            Assert.That(options.DeleteSaveShown, Is.False, "Delete Save hides again.");
            Assert.That(options.RestoreButton.interactable, Is.False, "Nothing is left to restore.");
            StringAssert.Contains(OptionsMenuUI.NoPreviousLine, options.SaveComparisonText);
            Assert.That(GameSettings.FlashingVfx, Is.False, "The options read the fresh save.");

            // Closing Options loads the menu again, so no screen shows the save that is gone.
            Click(options.BackButton, "Back");
            Assert.That(OptionsMenuUI.LastClosedReplacedSave, Is.True);
            yield return WaitUntilRealtime(() =>
            {
                var again = Object.FindFirstObjectByType<MainMenuNavigation>();
                return again != null && again != nav && again.Layout != null;
            }, "the main menu to load again after the save was replaced");

            byte[] realAfter = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            Assert.That(realAfter, Is.EqualTo(realBefore), "The owner's real save is untouched.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>The open question: its window on the screen, its two answers inside it, apart, touch-sized on a phone.</summary>
        private static IEnumerator DialogFitsEveryScreen(OptionsMenuUI options)
        {
            var canvas = options.GetComponent<Canvas>();
            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    for (int f = 0; f < 2; f++) { Canvas.ForceUpdateCanvases(); yield return null; }
                    Canvas.ForceUpdateCanvases();
                    var answers = new Selectable[] { options.DialogCancelButton, options.DialogConfirmButton };
                    problems.AddRange(ScreenSimulation.Problems(d.Name, answers));
                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    Rect panel = ScreenSimulation.UnitsRect((RectTransform)options.DialogCancelButton.transform.parent.parent);
                    if (panel.xMin < -1f || panel.yMin < -1f || panel.xMax > size.x + 1f || panel.yMax > size.y + 1f)
                        problems.Add($"{d.Name}: the question {panel} is off the {size} screen");
                    foreach (var answer in answers)
                    {
                        Rect a = ScreenSimulation.UnitsRect((RectTransform)answer.transform);
                        if (a.xMin < panel.xMin - 1f || a.xMax > panel.xMax + 1f || a.yMin < panel.yMin - 1f || a.yMax > panel.yMax + 1f)
                            problems.Add($"{d.Name}: {answer.name} {a} leaves the question {panel}");
                    }
                    var message = options.DialogCancelButton.transform.parent.parent.Find("Message").GetComponent<TMPro.TMP_Text>();
                    message.ForceMeshUpdate();
                    Rect m = ScreenSimulation.UnitsRect(message.rectTransform);
                    if (message.isTextTruncated) problems.Add($"{d.Name}: the question's words are cut short");
                    if (message.fontSize < Mathf.Max(14f, UiScreen.MinTextUnits) - 0.01f) problems.Add($"{d.Name}: the question's words are {message.fontSize:0.#} units");
                    foreach (var answer in answers)
                        if (m.Overlaps(ScreenSimulation.UnitsRect((RectTransform)answer.transform))) problems.Add($"{d.Name}: the words run under {answer.name}");
                }
            }
            finally { ScreenSimulation.Restore(); }
            for (int f = 0; f < 2; f++) { Canvas.ForceUpdateCanvases(); yield return null; }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
