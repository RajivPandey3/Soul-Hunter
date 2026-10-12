using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Responsive Results screen (owner, 2026-10-10: the same screens in landscape and portrait). The real screen, with a
    /// full weapon table and an unlock, laid out on a PC (16:9, 21:9, 5:4) and a phone held either way: DONE and the unlocks
    /// button on the canvas, at touch size, pinned at the bottom under the scrolling areas; the four areas in a 2x2 grid
    /// when there is room and in one column in portrait, never overlapping; every weapon number fully visible; no text
    /// under the phone's floor.
    /// </summary>
    public class RunResultsResponsivePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheResultsScreen_FitsEveryScreen_WithItsWeaponTable()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var manager = WeaponManager.Instance;
            foreach (var type in new[] { UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.Knife, UpgradeData.UpgradeType.MagicWand,
                         UpgradeData.UpgradeType.Axe, UpgradeData.UpgradeType.Cross, UpgradeData.UpgradeType.Garlic })
                manager.GiveWeapon(type, 3);
            yield return null;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var stats = RunStatsTracker.Instance;
            foreach (var record in stats.Weapons) { stats.RecordHit(record.Weapon, null, 123456); record.SecondsUsed = 3725f; } // "123k", "1:02:05"
            stats.RecordUnlock("Character: Corvan");

            var owner = new GameObject("ResultsTestOwner");
            var screen = RunResultsScreen.Show(owner.transform, RunResults.Capture(RunResults.EndKind.Quit), null);
            try
            {
                yield return null;
                yield return null;
                var canvas = screen.GetComponent<Canvas>();
                Assert.That(canvas.isRootCanvas, Is.True, "The Results screen is its own root canvas.");
                Assert.That(screen.WeaponRows.Count, Is.GreaterThanOrEqualTo(6), "A full weapon table is checked.");
                Assert.That(screen.UnlocksButton, Is.Not.Null, "The run has an unlock, so the unlocks button shows.");
                Assert.That(screen.DoneButton.transform.parent.GetComponent<ThemedRow>(), Is.Not.Null, "DONE sits in a ThemedRow.");
                Assert.That(screen.UnlocksButton.transform.parent, Is.EqualTo(screen.DoneButton.transform.parent), "Both buttons share the row.");

                var problems = new List<string>();
                try
                {
                    foreach (var d in ScreenSimulation.Devices)
                    {
                        yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                        screen.Relayout();
                        Canvas.ForceUpdateCanvases();
                        problems.AddRange(ScreenSimulation.Problems(d.Name, screen.GetComponentsInChildren<Selectable>()));
                        problems.AddRange(Check(d.Name, d.Pixels, screen, canvas));
                    }
                    // The unlocks panel fits too, and the buttons stay usable over it.
                    foreach (var d in ScreenSimulation.Devices)
                    {
                        yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                        if (!screen.UnlocksPanel.activeSelf) screen.ToggleUnlocks();
                        Canvas.ForceUpdateCanvases();
                        problems.AddRange(ScreenSimulation.Problems(d.Name + " (unlocks shown)", screen.GetComponentsInChildren<Selectable>()));
                        Rect panel = ScreenSimulation.UnitsRect((RectTransform)screen.UnlocksPanel.transform);
                        Rect bar = ScreenSimulation.UnitsRect(screen.ButtonBar);
                        if (panel.yMin < bar.yMax - 1f) problems.Add($"{d.Name}: the unlocks panel {panel} runs into the button row {bar}");
                    }
                    if (screen.UnlocksPanel.activeSelf) screen.ToggleUnlocks();
                }
                finally { ScreenSimulation.Restore(); }
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
            }
            finally
            {
                Object.Destroy(screen.gameObject);
                Object.Destroy(owner);
            }
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static IEnumerable<string> Check(string device, Vector2 pixels, RunResultsScreen screen, Canvas canvas)
        {
            Vector2 size = ((RectTransform)canvas.transform).rect.size;
            bool portrait = pixels.y > pixels.x;

            // The closing button: on the canvas, in the bottom row, which is pinned to the bottom edge, under the areas.
            Rect bar = ScreenSimulation.UnitsRect(screen.ButtonBar);
            Rect done = ScreenSimulation.UnitsRect((RectTransform)screen.DoneButton.transform);
            Rect view = ScreenSimulation.UnitsRect((RectTransform)screen.Scroll.transform);
            if (bar.yMin > 1f) yield return $"{device}: the button row starts at {bar.yMin:0}, not at the bottom edge";
            if (done.yMin < bar.yMin - 1f || done.yMax > bar.yMax + 1f) yield return $"{device}: DONE {done} is outside the button row {bar}";
            if (view.yMin < bar.yMax - 1f) yield return $"{device}: the scrolling areas {view} run into the button row {bar}";
            if (done.yMax > size.y || done.yMin < 0f) yield return $"{device}: DONE {done} is off the {size} canvas";

            // 2x2 when there is room, one column in portrait.
            if (portrait && screen.TwoColumns) yield return $"{device}: two columns on a {size} portrait canvas";
            if (!portrait && !screen.TwoColumns) yield return $"{device}: one column on a {size} landscape canvas";
            var areas = new[] { screen.SummaryArea, screen.WeaponsArea, screen.CharacterArea, screen.ListsArea };
            for (int i = 0; i < areas.Length; i++)
            {
                Rect a = ScreenSimulation.UnitsRect(areas[i]);
                if (a.xMin < view.xMin - 1f || a.xMax > view.xMax + 1f) yield return $"{device}: {areas[i].name} {a} is wider than the view {view}";
                for (int j = i + 1; j < areas.Length; j++)
                {
                    Rect b = ScreenSimulation.UnitsRect(areas[j]);
                    if (Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1f)
                        yield return $"{device}: {areas[i].name} and {areas[j].name} overlap";
                }
            }

            // Every weapon number is shown whole, inside the weapons area.
            Rect table = ScreenSimulation.UnitsRect(screen.WeaponsArea);
            foreach (var row in screen.WeaponRows)
                foreach (var cell in new[] { row.Level, row.Damage, row.Time, row.Dps })
                {
                    cell.ForceMeshUpdate();
                    Rect r = ScreenSimulation.UnitsRect(cell.rectTransform);
                    if (r.xMin < table.xMin - 1f || r.xMax > table.xMax + 1f) yield return $"{device}: {row.Name.text} {cell.name} {r} is outside the table {table}";
                    float need = cell.GetPreferredValues(cell.text).x;
                    if (need > cell.rectTransform.rect.width + 1f) yield return $"{device}: {row.Name.text} {cell.name} '{cell.text}' needs {need:0} units, has {cell.rectTransform.rect.width:0}";
                    var nameRect = ScreenSimulation.UnitsRect(row.Name.rectTransform);
                    if (nameRect.Overlaps(r)) yield return $"{device}: {row.Name.text}'s name covers its {cell.name}";
                }

            // No text under the floor (12 sp on the phone).
            float floor = UiScreen.MinTextUnits;
            foreach (var text in screen.GetComponentsInChildren<TMP_Text>().Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)))
            {
                text.ForceMeshUpdate();
                if (text.fontSize < floor - 0.01f) yield return $"{device}: '{text.name}' text {text.fontSize:0.#} is under {floor:0.#} units";
            }
        }
    }
}
