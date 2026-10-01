using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report 2026-10-01 ("HANG HO GAYA", a screenshot of the chest screen in a wide, short Game view): the chest
    /// canvas scaled by width only, so OPEN (430 below the centre) fell off the bottom of a 21:9-like window and the
    /// paused game could not go on. Every button and card of the chest screen must be inside the window at any size.
    /// </summary>
    public class ChestScreenFitPlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly Vector2[] Windows =
        {
            new Vector2(1366, 575),  // the owner's Game view
            new Vector2(2560, 1080), // 21:9
            new Vector2(3440, 1440), // 21:9
            new Vector2(1920, 1080), // 16:9
            new Vector2(1280, 1024), // 5:4
            new Vector2(1080, 1920), // portrait
        };

        [UnityTest]
        public IEnumerator EveryChestButton_StaysInsideTheWindow_AtAnyAspect()
        {
            yield return BootIntoGameplay(save => { save.ChestsOpened = 20; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var chestUI = GetPrivateField<ChestUI>(PickupPoolManager.Instance, "_chestUI");
            var result = new ChestResult { ItemCount = 3, Gold = 300 };
            for (int i = 0; i < 3; i++) result.Rewards.Add(new ChestReward { Name = "Whip", Level = 2 + i });
            chestUI.OpenChest("You Found a Chest!", result);
            var screen = chestUI.Screen;
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.WaitingToOpen, "the OPEN button");

            var scaler = chestUI.ScreenCanvas().GetComponent<CanvasScaler>();
            var parts = new List<RectTransform> { (RectTransform)screen.OpenButton.transform };
            foreach (var w in Windows)
            {
                // The canvas size this scaler gives a window of size w (reference 1920x1080).
                Vector2 reference = scaler.referenceResolution;
                float scale = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand
                    ? Mathf.Min(w.x / reference.x, w.y / reference.y)
                    : scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink
                        ? Mathf.Max(w.x / reference.x, w.y / reference.y)
                        : Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(w.x / reference.x, 2f), Mathf.Log(w.y / reference.y, 2f), scaler.matchWidthOrHeight));
                Vector2 half = w / scale * 0.5f;
                foreach (var part in parts)
                {
                    // Centre-anchored parts: their box from anchoredPosition and size, in canvas units around the centre.
                    Vector2 p = part.anchoredPosition, s = part.rect.size * 0.5f;
                    Assert.That(Mathf.Abs(p.x) + s.x <= half.x + 0.5f && Mathf.Abs(p.y) + s.y <= half.y + 0.5f, Is.True,
                        $"{part.name} at {p} (size {part.rect.size}) is off a {w.x}x{w.y} window (canvas half {half}).");
                }
            }
            screen.PressOpen();
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.Done, "the reveal");
            parts.Clear();
            parts.Add((RectTransform)screen.DoneButton.transform);
            foreach (var w in Windows)
            {
                Vector2 reference = scaler.referenceResolution;
                float scale = Mathf.Min(w.x / reference.x, w.y / reference.y);
                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand) Assert.Fail("The chest canvas expands to fit the window.");
                Vector2 half = w / scale * 0.5f;
                var done = parts[0];
                Assert.That(Mathf.Abs(done.anchoredPosition.y) + done.rect.height * 0.5f, Is.LessThanOrEqualTo(half.y + 0.5f), $"DONE fits a {w.x}x{w.y} window.");
            }
            screen.PressDone();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "DONE resumes the game.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
