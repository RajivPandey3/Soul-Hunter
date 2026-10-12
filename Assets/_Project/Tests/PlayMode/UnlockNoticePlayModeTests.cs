using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.UI;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS B05 (2026-09-30): an unlock during a level-up drew "New character unlocked" on top of "LEVEL UP!". The
    /// notice now waits, hidden, while a menu has the game paused.
    /// </summary>
    public class UnlockNoticePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AnUnlockDuringAPause_WaitsUntilPlayResumes()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var tracker = Object.FindFirstObjectByType<CharacterUnlockTracker>();
            Assert.IsNotNull(tracker, "The run has an unlock tracker.");
            var character = GameContentCatalog.Load().Characters.First(c => c != null && c.CharacterName != "Kael");
            var show = typeof(CharacterUnlockTracker).GetMethod("ShowNotice", BindingFlags.Instance | BindingFlags.NonPublic);

            GameTime.Pause(this);
            try
            {
                show.Invoke(tracker, new object[] { $"New character unlocked: {character.CharacterName}!" });
                yield return null;
                Assert.That(tracker.Notice, Is.Not.Null, "The notice exists.");
                Assert.That(tracker.Notice.activeSelf, Is.False, "Hidden while a menu has the game paused.");
                yield return new WaitForSecondsRealtime(4f); // longer than the notice's 3.5 s
                Assert.That(tracker.Notice, Is.Not.Null, "Its time does not run while paused.");
            }
            finally { GameTime.Resume(this); }
            yield return null;
            yield return null;
            Assert.That(tracker.Notice != null && tracker.Notice.activeSelf, Is.True, "Shown once play resumes.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner (2026-10-10): landscape and portrait from the same screens. On a PC (16:9, 21:9, 5:4) and on the owner's
        /// phone held either way, the unlock notice hangs from the top centre of the screen, stays inside it, is never wider
        /// than the screen minus its margins (nor than its 1000-unit design width), holds its whole line (a narrow screen
        /// wraps it into a taller box), and its text never goes under the touch floor (12 sp).
        /// </summary>
        [UnityTest]
        public IEnumerator TheNotice_FitsEveryScreen()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            yield return CloseOpenLevelUps();
            var tracker = Object.FindFirstObjectByType<CharacterUnlockTracker>();
            var show = typeof(CharacterUnlockTracker).GetMethod("ShowNotice", BindingFlags.Instance | BindingFlags.NonPublic);
            show.Invoke(tracker, new object[] { "PowerUp unlocked: Skip rank 1. New character unlocked: Seraphina, The Radiant!" });
            yield return null;
            Assert.That(tracker.Notice != null && tracker.Notice.activeSelf, Is.True, "The notice shows.");
            var canvas = tracker.Notice.GetComponent<Canvas>();
            var root = (RectTransform)canvas.transform;
            var problems = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    yield return null; // the notice lays itself out again for the new canvas
                    Canvas.ForceUpdateCanvases();
                    string what = $"unlock notice on {d.Name}";
                    Assert.That(tracker.Notice, Is.Not.Null, $"{what}: still shown.");
                    Rect box = ScreenSimulation.UnitsRect(tracker.NoticeBox);
                    Vector2 size = root.rect.size;
                    if (box.xMin < -0.5f || box.yMin < -0.5f || box.xMax > size.x + 0.5f || box.yMax > size.y + 0.5f)
                        problems.Add($"{what}: {box} is off the {size} canvas.");
                    if (box.width > Mathf.Min(CharacterUnlockTracker.NoticeWidth, size.x - 2f * CharacterUnlockTracker.NoticeMargin) + 0.5f)
                        problems.Add($"{what}: {box.width:0} wide, more than the screen minus its margins or the design width.");
                    if (Mathf.Abs(box.center.x - size.x * 0.5f) > 1f) problems.Add($"{what}: not centred ({box.center.x:0} of {size.x:0}).");
                    // A PC: its approved place. A phone: under the HUD strip and the stage name (2026-10-12: it was drawn over them).
                    float top = Mathf.Max(CharacterUnlockTracker.NoticeTop, GameHUDController.PhoneClearTop);
                    if (size.y - box.yMax > top + 1f) problems.Add($"{what}: {size.y - box.yMax:0} units under the top, expected {top:0}.");
                    if (!d.Touch && GameHUDController.PhoneClearTop > 0f) problems.Add($"{what}: a PC has no phone strip to clear ({GameHUDController.PhoneClearTop:0}).");
                    var text = tracker.NoticeText;
                    text.ForceMeshUpdate();
                    if (text.fontSize < UiScreen.MinTextUnits - 0.01f)
                        problems.Add($"{what}: the text is {text.fontSize:0.#} units, under the {UiScreen.MinTextUnits:0.#} floor.");
                    float needed = text.GetPreferredValues(text.text, text.rectTransform.rect.width - text.margin.x - text.margin.z, 0f).y;
                    if (needed > text.rectTransform.rect.height + 1f) problems.Add($"{what}: the line needs {needed:0} units, the box has {text.rectTransform.rect.height:0}.");
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
