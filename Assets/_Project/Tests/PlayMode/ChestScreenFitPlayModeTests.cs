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
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The chest screen on every screen (owner 2026-10-01 "HANG HO GAYA": OPEN fell off a wide, short window and the paused
    /// game could not go on; owner 2026-10-10: the same screens in landscape and portrait). The real screen is laid out on a
    /// PC (16:9, 21:9, 5:4) and a phone held either way, waiting to open and at the end: OPEN and DONE stay on the canvas at
    /// a 48 dp touch size, pinned to the bottom, the five reward cards wrap and stay readable, nothing overlaps, and the
    /// backdrop covers the whole screen at its full strength.
    /// </summary>
    public class ChestScreenFitPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheChestScreen_FitsEveryDevice_WaitingToOpen_AndDone()
        {
            // 60 five-item chests opened: this 5-item chest can be skipped, so the skip hint is laid out too.
            yield return BootIntoGameplay(save => { save.ChestsOpened = 80; save.FiveItemChestsOpened = 60; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var chestUI = GetPrivateField<ChestUI>(PickupPoolManager.Instance, "_chestUI");
            var result = new ChestResult { ItemCount = 5, Gold = 750 };
            string[] names = { "Whip", "Thousand Edge", "Hollow Heart", "Santa Water", "Bloody Tear" };
            for (int i = 0; i < 5; i++) result.Rewards.Add(new ChestReward { Name = names[i], Level = 2 + i, IsEvolution = i == 4 });
            chestUI.OpenChest("You Found a Chest!", result);
            var screen = chestUI.Screen;
            var canvas = chestUI.ScreenCanvas();
            Assert.That(canvas.GetComponent<ResponsiveCanvas>(), Is.Not.Null, "The chest canvas takes the game's one UI scale.");
            Assert.That(screen.CanSkip, Is.True);

            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.WaitingToOpen && Settled(screen.OpenButton), "the OPEN button");
            var backdrop = screen.GetComponent<Image>();
            Assert.That(backdrop.color.a, Is.EqualTo(ChestOpeningScreen.BackdropAlpha).Within(0.001f),
                "The backdrop ends its fade at full strength (it stopped a step short, and on a slow phone frame nearly clear).");
            yield return ScreenSimulation.AssertFitsEveryDevice(() => new Selectable[] { screen.OpenButton }, canvas);
            yield return CheckEveryDevice(screen, canvas, screen.OpenButton);

            screen.PressOpen();
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.Done && Settled(screen.DoneButton), "the reveal");
            Assert.That(screen.Cards.Count, Is.EqualTo(5));
            yield return ScreenSimulation.AssertFitsEveryDevice(() => new Selectable[] { screen.DoneButton }, canvas);
            yield return CheckEveryDevice(screen, canvas, screen.DoneButton);

            screen.PressDone();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "DONE resumes the game.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static bool Settled(Button button) =>
            button.gameObject.activeInHierarchy && Mathf.Abs(button.transform.localScale.x - 1f) < 0.001f && button.transform.localRotation == Quaternion.identity;

        /// <summary>Lays the screen out on each device and checks what AssertFitsEveryDevice does not: cards, texts, backdrop.</summary>
        private static IEnumerator CheckEveryDevice(ChestOpeningScreen screen, Canvas canvas, Button closing)
        {
            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    problems.AddRange(Check(d.Name, screen, canvas, closing));
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static IEnumerable<string> Check(string device, ChestOpeningScreen screen, Canvas canvas, Button closing)
        {
            Vector2 size = ((RectTransform)canvas.transform).rect.size;
            float floor = UiScreen.MinTextUnits;

            // The backdrop covers the whole screen, opaque as before; the content sits inside it.
            Rect backdrop = ScreenSimulation.UnitsRect((RectTransform)screen.transform);
            if (backdrop.width < size.x - 1f || backdrop.height < size.y - 1f) yield return $"{device}: the backdrop {backdrop} does not cover the {size} canvas";

            // The closing button is pinned to the bottom edge of the safe area.
            Rect button = ScreenSimulation.UnitsRect((RectTransform)closing.transform);
            if (button.yMin > 60f) yield return $"{device}: {closing.name} {button} is not pinned to the bottom";

            var boxes = new List<(string Name, Rect Box)> { (closing.name, button) };
            // Button labels are sized by the shared theme (ThemedElement), not by this screen, so they are left to its tests.
            foreach (var label in screen.Content.GetComponentsInChildren<TextMeshProUGUI>()
                         .Where(l => l.gameObject.activeInHierarchy && !string.IsNullOrEmpty(l.text) && l.GetComponentInParent<Selectable>() == null))
            {
                float min = label.enableAutoSizing ? label.fontSizeMin : label.fontSize;
                if (min < floor - 0.01f) yield return $"{device}: '{label.text}' can shrink to {min:0.#}, under the {floor:0.#}-unit text floor";
                if (label.transform.parent == screen.Content) boxes.Add(("'" + label.text + "'", ScreenSimulation.UnitsRect(label.rectTransform)));
            }
            var chest = screen.Content.Find("Chest") as RectTransform;
            boxes.Add(("the chest", ScreenSimulation.UnitsRect(chest)));
            if (chest.rect.width < ChestOpeningScreen.MinChestSize * 0.5f) yield return $"{device}: the chest is only {chest.rect.width:0} units";

            var cardRows = new HashSet<int>();
            for (int i = 0; i < screen.Cards.Count; i++)
            {
                var card = screen.Cards[i];
                Rect box = ScreenSimulation.UnitsRect(card);
                boxes.Add((card.name, box));
                cardRows.Add(Mathf.RoundToInt(box.center.y));
                if (card.rect.width < ChestOpeningScreen.MinCardSize - 0.5f) yield return $"{device}: {card.name} is {card.rect.width:0} units, under {ChestOpeningScreen.MinCardSize}";
                foreach (var label in card.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    Rect inner = ScreenSimulation.UnitsRect(label.rectTransform);
                    if (inner.xMin < box.xMin - 1f || inner.xMax > box.xMax + 1f || inner.yMin < box.yMin - 1f || inner.yMax > box.yMax + 1f)
                        yield return $"{device}: {card.name}'s '{label.text}' {inner} runs outside its card {box}";
                }
            }

            foreach (var (name, box) in boxes)
                if (box.xMin < -1f || box.yMin < -1f || box.xMax > size.x + 1f || box.yMax > size.y + 1f)
                    yield return $"{device}: {name} {box} is off the {size} canvas";
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Rect a = boxes[i].Box, b = boxes[j].Box;
                    if (Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1f)
                        yield return $"{device}: {boxes[i].Name} and {boxes[j].Name} overlap";
                }

            if (screen.Cards.Count == 5)
            {
                if (device == "PC 1920x1080" && (screen.SideBySide || cardRows.Count != 1 || !Mathf.Approximately(screen.CardSize, ChestOpeningScreen.RoomyCardSize)))
                    yield return $"{device}: the approved 1080p layout is one row of 190-unit cards under the chest (rows {cardRows.Count}, size {screen.CardSize}, side {screen.SideBySide})";
                if (device == "Phone portrait" && cardRows.Count < 2) yield return $"{device}: five cards should wrap onto more rows";
            }
            if (device == "Phone landscape" && !screen.SideBySide) yield return $"{device}: a short screen puts the chest beside the rewards";
            if (device == "Phone portrait" && screen.SideBySide) yield return $"{device}: a tall screen stacks the chest over the rewards";
        }
    }
}
