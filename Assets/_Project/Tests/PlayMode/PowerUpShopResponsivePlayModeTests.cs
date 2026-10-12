using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
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
    /// The Power Up shop in landscape and portrait (owner, 2026-10-10: the same screens, auto-adjusting). On a PC (16:9,
    /// 21:9, 5:4) and on the owner's phone held either way: every control the player can reach is on the canvas, none
    /// overlaps another, none is under 48 dp on the phone; Back, Buy and Refund All always show; the grid takes the
    /// columns its width allows and scrolls on the phone; the approved arrangement (2026-10-07: Back in the header, the
    /// grid above the info box) holds on every PC and the portrait phone, and only the landscape phone's short body puts
    /// the box beside the grid; text is never under 12 sp on the phone; a rotation keeps the selection.
    /// </summary>
    public class PowerUpShopResponsivePlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator OpenShop()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.Currency = 50000;
            save.CurrentData.PowerUpRanks.Clear();
            save.CurrentData.DisabledPowerUps.Clear();
            save.CurrentData.UnlockedAchievements.Clear(); // locked cells carry their long unlock line
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowPowerUps();
            for (int i = 0; i < 4; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
        }

        [UnityTest]
        public IEnumerator TheShop_FitsEveryScreen()
        {
            yield return OpenShop();
            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            Assert.That(shop, Is.Not.Null, "The Power Up shop.");
            var canvas = shop.GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(canvas.GetComponent<ResponsiveCanvas>(), Is.Not.Null, "The shop takes the game's one UI scale.");
            yield return ScreenSimulation.AssertFitsEveryDevice(() => shop.VisibleControls(), canvas);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator BackBuyAndRefund_AlwaysShow_TheGridScrolls_AndTextStaysReadable()
        {
            yield return OpenShop();
            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            var canvas = shop.GetComponentInParent<Canvas>().rootCanvas;
            shop.Select(PowerUpId.Greed);
            var problems = new List<string>();
            var columns = new Dictionary<string, int>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    columns[d.Name] = shop.Columns;

                    // The price is readable without scrolling: above Buy and Refund, inside the box (phone run, 2026-10-10).
                    Rect price = ScreenSimulation.UnitsRect(shop.PriceText.rectTransform);
                    float buttonsTop = Mathf.Max(ScreenSimulation.UnitsRect((RectTransform)shop.BuyButton.transform).yMax, ScreenSimulation.UnitsRect((RectTransform)shop.RefundButton.transform).yMax);
                    Rect infoBox = ScreenSimulation.UnitsRect(shop.InfoBox);
                    bool beside = ScreenSimulation.UnitsRect((RectTransform)shop.BuyButton.transform).xMin >= price.xMax - 1f;
                    // Inside the words' own window too: the line was half cut at its lower edge while still above Buy (phone run, 2026-10-11).
                    Rect window = ScreenSimulation.UnitsRect(shop.InfoTextWindow);
                    if (price.yMax > infoBox.yMax + 1f || price.yMin < window.yMin - 1f || (!beside && price.yMin < buttonsTop - 1f))
                        problems.Add($"{d.Name}: the price line {price} is hidden (buttons top {buttonsTop:0}, box {infoBox})");

                    // Every cell's words stay inside the cell (owner's phone, 2026-10-12: a LOCKED cell's unlock line ran
                    // 57 px out of it). Every locked cell still says LOCKED.
                    foreach (var cell in shop.VisibleControls().Where(c => c != null && c.name.StartsWith("PowerUp_")))
                    {
                        var label = cell.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        if (label == null) continue;
                        label.ForceMeshUpdate();
                        Rect cellBox = ((RectTransform)cell.transform).rect;
                        Vector2 lo = cell.transform.InverseTransformPoint(label.transform.TransformPoint(label.textBounds.min));
                        Vector2 hi = cell.transform.InverseTransformPoint(label.transform.TransformPoint(label.textBounds.max));
                        float over = Mathf.Max(Mathf.Max(cellBox.xMin - lo.x, hi.x - cellBox.xMax), Mathf.Max(cellBox.yMin - lo.y, hi.y - cellBox.yMax));
                        if (over > 1f) problems.Add($"{d.Name}: {cell.name} '{label.GetParsedText().Replace('\n', ' ')}' runs {over:0.#} units out of its cell {cellBox.size}");
                    }
                    if (!canvas.GetComponentsInChildren<Selectable>(false).Any(c => c.name == "PowerUp_Omni" && c.GetComponentInChildren<TMPro.TextMeshProUGUI>().GetParsedText().Contains("LOCKED")))
                        problems.Add($"{d.Name}: the Omni cell does not say LOCKED");

                    // The closing and buying buttons: shown, wholly on the canvas, the info box's buttons inside the box.
                    Rect box = ScreenSimulation.UnitsRect(shop.InfoBox);
                    foreach (var b in new Selectable[] { shop.BackButton, shop.BuyButton, shop.RefundButton })
                    {
                        if (b == null || !b.isActiveAndEnabled) { problems.Add($"{d.Name}: {(b != null ? b.name : "a button")} is not shown"); continue; }
                        Rect r = ScreenSimulation.UnitsRect((RectTransform)b.transform);
                        if (r.xMin < -1f || r.yMin < -1f || r.xMax > size.x + 1f || r.yMax > size.y + 1f) problems.Add($"{d.Name}: {b.name} {r} is off the {size} canvas");
                        if (b != shop.BackButton && !(r.xMin >= box.xMin - 1f && r.xMax <= box.xMax + 1f && r.yMin >= box.yMin - 1f && r.yMax <= box.yMax + 1f))
                            problems.Add($"{d.Name}: {b.name} {r} is outside the info box {box}");
                    }
                    // Back: in the header above the grid and the box (the approved look), or, only where the header is too
                    // narrow, in a footer under both.
                    Rect back = ScreenSimulation.UnitsRect((RectTransform)shop.BackButton.transform);
                    Rect grid = ScreenSimulation.UnitsRect(shop.GridViewport);
                    if (shop.BackInHeader ? back.yMin < Mathf.Max(grid.yMax, box.yMax) - 1f : back.yMax > Mathf.Min(grid.yMin, box.yMin) + 1f)
                        problems.Add($"{d.Name}: Back {back} is not {(shop.BackInHeader ? "above" : "under")} the grid {grid} and the box {box}");

                    // The approved arrangement (2026-10-07) on every PC and on a portrait phone: Back in the header (PC), the
                    // grid on top, the info box under it. A landscape phone's short body puts the box beside the grid.
                    bool landscapePhone = d.Name == "Phone landscape";
                    if (!d.Touch && !shop.BackInHeader) problems.Add($"{d.Name}: Back left the header on a PC");
                    if (landscapePhone && !shop.BackInHeader) problems.Add($"{d.Name}: Back left the header although it fits there");
                    if (shop.IsStacked == landscapePhone) problems.Add($"{d.Name}: the info box is {(shop.IsStacked ? "under" : "beside")} the grid");
                    if (shop.IsStacked ? box.yMax > grid.yMin + 1f : box.xMin < grid.xMax - 1f)
                        problems.Add($"{d.Name}: the info box {box} is not {(shop.IsStacked ? "under" : "beside")} the grid {grid}");
                    // On a PC every row fits above the info box, as approved: no scrolling there.
                    if (!d.Touch && shop.GridScroll.content.rect.height > shop.GridViewport.rect.height + 1f)
                        problems.Add($"{d.Name}: the grid's rows ({shop.GridScroll.content.rect.height:0}) do not fit its window ({shop.GridViewport.rect.height:0})");

                    // Every cell is at least the touch target, and the grid scrolls when its rows do not fit.
                    foreach (var info in PowerUps.All)
                    {
                        var cell = (RectTransform)shop.CellFor(info.Id).transform;
                        if (Mathf.Min(cell.rect.width, cell.rect.height) < UiScreen.MinTouchUnits - 0.5f) problems.Add($"{d.Name}: {info.Id} cell {cell.rect.size} under the touch target");
                    }
                    float contentHeight = shop.GridScroll.content.rect.height, windowHeight = shop.GridViewport.rect.height;
                    if (contentHeight > windowHeight + 1f && !shop.GridScroll.vertical) problems.Add($"{d.Name}: the grid is taller than its window but does not scroll");
                    if (!shop.VisibleControls().Any(c => c.name.StartsWith("PowerUp_"))) problems.Add($"{d.Name}: no cell shows in the grid's window");

                    // Text on the phone: nothing under 12 sp, and no smaller second line below it.
                    foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(false).Where(t => IsShopText(shop, t)))
                    {
                        if (string.IsNullOrWhiteSpace(text.text)) continue;
                        text.ForceMeshUpdate();
                        if (text.fontSize < UiScreen.MinTextUnits - 0.01f) problems.Add($"{d.Name}: '{text.name}' at {text.fontSize:0.#} units, under {UiScreen.MinTextUnits:0.#}");
                    }
                    if (d.Touch)
                    {
                        string locked = shop.CellFor(PowerUpId.Reroll).GetComponentInChildren<TextMeshProUGUI>().text;
                        if (locked.Contains("<size=")) problems.Add($"{d.Name}: the locked cell shrinks its unlock line under the floor: {locked}");
                    }
                }

                // Rotation keeps the screen's state: the selection stays.
                Assert.That(shop.Selected, Is.EqualTo(PowerUpId.Greed), "Laying out again keeps the selected PowerUp.");

                // A cell reached by gamepad or keyboard scrolls into the window (phone portrait, where the grid scrolls).
                var portrait = ScreenSimulation.Devices.First(d => d.Name == "Phone portrait");
                yield return ScreenSimulation.At(portrait.Pixels, portrait.Dpi, portrait.Touch, canvas);
                var last = shop.CellFor(PowerUps.All.Last().Id);
                EventSystem.current.SetSelectedGameObject(last.gameObject);
                for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                Assert.That(shop.VisibleControls().Contains(last), Is.True, "The focused last cell scrolled into the grid's window.");
            }
            finally { ScreenSimulation.Restore(); }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(columns["Phone portrait"], Is.LessThan(columns["PC 1920x1080"]), "Portrait takes fewer columns than a PC.");
            Assert.That(columns["Phone portrait"], Is.GreaterThanOrEqualTo(2), "Portrait still shows two columns.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>A text of the shop screen (its grid, info box, header or footer).</summary>
        private static bool IsShopText(PowerUpMenuUI shop, TextMeshProUGUI text)
        {
            var panel = shop.GridViewport.GetComponentsInParent<Transform>(true).FirstOrDefault(t => t.name == "VS_PowerUpContent");
            return panel != null && text.transform.IsChildOf(panel);
        }
    }
}
