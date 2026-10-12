using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// The pause map and its guide arrows (vampire.survivors.wiki/w/Milky_Way_Map, raw, fetched 2026-10-12) in the real game:
    /// - "The map displays the player's location as well as the locations of pickups, stage items, relics, coffins, and
    ///   Treasure Chests": a mark for the hero (always at the centre), one per stage item at its place relative to the hero,
    ///   the Rune Plates, and anything that implements IMapMarker; a collected item leaves the map; "Items left: n";
    /// - every kind has a shape, a letter and a line of the legend in words;
    /// - the map opens from the pause menu, keeps the game paused, and Back returns to the pause menu with the Map button
    ///   focused; resuming closes it;
    /// - it fits and reads on a PC (1920x1080, 1366x768) and on a phone held either way;
    /// - "Show Guides"/"Hide Guides": with the guides on, arrows point at the nearest stage items that are off screen, at
    ///   most three, never while the game is paused, and none when the guides are off.
    /// </summary>
    public class PauseMapPlayModeTests : GameplayPlayModeTestBase
    {
        /// <summary>A thing another system puts on the map through the interface (a Soul Casket here).</summary>
        private sealed class TestCasket : MonoBehaviour, SoulHunter.Gameplay.IMapMarker
        {
            public bool Visible = true;
            public string MapKind => "Soul Casket";
            public Vector3 MapPosition => transform.position;
            public bool MapVisible => Visible;
        }

        private static readonly (string Name, Vector2 Pixels, float Dpi, bool Touch)[] Devices =
        {
            ("PC 1920x1080", new Vector2(1920, 1080), 96f, false),
            ("PC 1366x768", new Vector2(1366, 768), 96f, false),
            ("Phone portrait", new Vector2(1080, 2400), 480f, true),
            ("Phone landscape", new Vector2(2400, 1080), 480f, true),
        };

        private StageItemSpawner _spawner;

        private IEnumerator Boot()
        {
            MapGuides.Enabled = false;
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var enemy in EnemyController.ActiveEnemies.ToArray()) enemy.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            _spawner = Object.FindFirstObjectByType<StageItemSpawner>();
            Assert.IsNotNull(_spawner, "The stage places its items.");
            yield return WaitUntilRealtime(() => _spawner.Placed.Count(i => i != null && i.activeInHierarchy) >= 6, "the stage items to be placed");
        }

        [TearDown]
        public void GuidesOff() => MapGuides.Enabled = false;

        private static List<GameObject> ItemsOnTheGround(StageItemSpawner spawner) =>
            spawner.Placed.Where(i => i != null && i.activeInHierarchy).ToList();

        private IEnumerator OpenMap(PauseMenuUI pause)
        {
            Click(pause.MapButton, "Map");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMap_OpensFromPause_MarksEveryStageItemAtItsPlace_AndClosesBackToPause()
        {
            yield return Boot();
            var player = PlayerController.Instance;
            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            Assert.IsNotNull(pause.MapButton, "The pause menu has a Map button. " + Errors());
            Assert.That(pause.MapButton.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Map"));
            Assert.That(pause.MapButton.gameObject.activeInHierarchy, Is.True, "Open from the start (no relic gates it yet).");
            // On a PC the column has room: Map sits under Evolutions, in Resume's column, the same size.
            Rect map = ScreenRect((RectTransform)pause.MapButton.transform), evo = ScreenRect((RectTransform)pause.EvolutionsButton.transform);
            Rect resume = ScreenRect((RectTransform)pause.ResumeButton.transform);
            if (!pause.SubScreensSideBySide)
            {
                Assert.That(Mathf.Abs(map.center.x - resume.center.x), Is.LessThan(2f), "In Resume's column.");
                Assert.That(map.yMax, Is.LessThanOrEqualTo(evo.yMin + 0.5f), "Under Evolutions.");
            }
            Assert.That(map.size.x, Is.EqualTo(resume.size.x).Within(0.5f));
            Assert.That(map.size.y, Is.EqualTo(resume.size.y).Within(0.5f));
            foreach (var other in new[] { pause.ResumeButton, pause.OptionsButton, pause.EvolutionsButton, pause.BlindFateButton, pause.QuitButton })
                Assert.That(map.Overlaps(ScreenRect((RectTransform)other.transform)), Is.False, $"Clear of {other.name}.");

            yield return OpenMap(pause);
            var screen = pause.MapScreen;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The map opens. " + Errors());
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(screen.BackButton.gameObject), "Back is focused.");
            Assert.AreEqual(0f, Time.timeScale, "Still paused.");
            Assert.That(screen.TitleText.text, Is.EqualTo(PauseMapScreen.TitleLine));

            // The hero: one mark, at the centre of the map.
            var hero = screen.Markers.Where(m => m.Kind == MapMarkerKind.Hero).ToList();
            Assert.That(hero.Count, Is.EqualTo(1), "One mark for the hero.");
            Assert.That(hero[0].Root.anchoredPosition.magnitude, Is.LessThan(0.5f), "The hero is always at the centre.");
            Assert.That(hero[0].Root.GetSiblingIndex(), Is.EqualTo(hero[0].Root.parent.childCount - 1), "Drawn on top of the other marks.");

            // Every stage item on the ground has its mark, where it lies relative to the hero.
            var items = ItemsOnTheGround(_spawner);
            float side = screen.Field.rect.width;
            Assert.That(side, Is.GreaterThan(100f), "The map has a size.");
            Assert.That(screen.Field.rect.height, Is.EqualTo(side).Within(0.5f), "The map is square.");
            Assert.That(screen.Range, Is.InRange(PauseMapScreen.MinRange, PauseMapScreen.MaxRange));
            foreach (var item in items)
            {
                var mark = screen.Markers.FirstOrDefault(m => ReferenceEquals(m.Source, item));
                Assert.IsNotNull(mark, $"{item.name} has a mark.");
                Assert.That(mark.Kind, Is.EqualTo(MapMarkerKind.StageItem));
                Vector3 offset = item.transform.position - player.transform.position;
                Vector2 expected = PauseMapScreen.FieldPoint(offset, screen.Range, side, out bool border);
                Assert.That(border, Is.False, $"{item.name} is within the ground the map shows ({screen.Range} units).");
                Assert.That(Vector2.Distance(mark.Root.anchoredPosition, expected), Is.LessThan(1f), $"{item.name}'s mark is at its place.");
                // East is right and north (up the screen in play) is up.
                if (Mathf.Abs(offset.x) > 1f) Assert.That(Mathf.Sign(mark.Root.anchoredPosition.x), Is.EqualTo(Mathf.Sign(offset.x)), $"{item.name}: east is right.");
                if (Mathf.Abs(offset.z) > 1f) Assert.That(Mathf.Sign(mark.Root.anchoredPosition.y), Is.EqualTo(Mathf.Sign(offset.z)), $"{item.name}: north is up.");
                if (item.GetComponent<StageItemPickup>() != null && item.GetComponentInChildren<SpriteRenderer>(true).sprite != null)
                    Assert.That(mark.Root.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(item.GetComponentInChildren<SpriteRenderer>(true).sprite),
                        $"{item.name} shows its own icon.");
            }
            Assert.That(screen.Markers.Count(m => m.Kind == MapMarkerKind.StageItem), Is.EqualTo(items.Count), "No stage item is marked twice.");
            foreach (var pickup in screen.Markers.Where(m => m.Kind == MapMarkerKind.Pickup))
                Assert.That(items.Any(i => pickup.Source is Component c && c.gameObject == i), Is.False,
                    "The stage's own chicken and magnet are stage items, not counted again as pickups.");
            Assert.That(screen.ItemsLeft, Is.EqualTo(items.Count));
            Assert.That(screen.ItemsLeftText.text, Is.EqualTo(PauseMapScreen.ItemsLeftPrefix + items.Count), "The map says how many are left.");
            int plates = _spawner.Plates.Count(p => p != null);
            Assert.That(screen.Markers.Count(m => m.Kind == MapMarkerKind.RunePlate), Is.EqualTo(plates), "A mark per Rune Plate.");

            // The legend names every kind on the map in words, with its count and the same mark.
            foreach (var kind in screen.Markers.Select(m => m.Kind).Distinct())
            {
                var row = screen.Legend.FirstOrDefault(r => r.Kind == kind);
                Assert.IsNotNull(row, $"The legend has {kind}.");
                StringAssert.StartsWith(PauseMapScreen.WordsFor(kind), row.Text.text);
                Assert.That(row.Count, Is.EqualTo(screen.Markers.Count(m => m.Kind == kind)));
                if (kind != MapMarkerKind.Hero) StringAssert.Contains("x" + row.Count, row.Text.text);
                Assert.IsNotNull(row.Sample, $"{kind}'s legend line shows its mark.");
            }
            Assert.That(screen.Legend.Select(r => r.Text.text).Distinct().Count(), Is.EqualTo(screen.Legend.Count), "Each kind has its own words.");
            // Shape and letter: no two kinds drawn as letters share a letter.
            var letters = screen.Markers.Where(m => !string.IsNullOrEmpty(m.Letter)).GroupBy(m => m.Letter).ToList();
            foreach (var group in letters)
                Assert.That(group.Select(m => m.Kind).Distinct().Count(), Is.EqualTo(1), $"The letter {group.Key} stands for one kind.");

            // Back: the pause menu again, with Map focused.
            Click(screen.BackButton, "Back");
            yield return null;
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(pause.MapButton.gameObject), "Back returns to the Map button.");
            Assert.That(pause.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f), "The pause menu is back.");
            Assert.AreEqual(0f, Time.timeScale, "Still paused.");

            // A collected item leaves the map, and something that asks for a mark (a Soul Casket) appears on it.
            system.Resume();
            yield return null;
            var taken = items.First(i => i.GetComponent<StageItemPickup>() != null);
            taken.GetComponent<StageItemPickup>().Take();
            var casketGo = new GameObject("TestCasket");
            casketGo.transform.position = player.transform.position + new Vector3(12f, 0f, -18f);
            var casket = casketGo.AddComponent<TestCasket>();
            yield return null;
            yield return CloseOpenLevelUps(); // a stage weapon fires at once and may level the hero
            yield return null;
            if (!system.IsPaused) system.TogglePause();
            yield return null;
            yield return OpenMap(pause);
            Assert.That(screen.IsOpen, Is.True, "The map opens again. " + Errors());
            Assert.That(screen.Markers.Any(m => ReferenceEquals(m.Source, taken)), Is.False, "The collected item is gone from the map.");
            Assert.That(screen.ItemsLeft, Is.EqualTo(items.Count - 1));
            Assert.That(screen.ItemsLeftText.text, Is.EqualTo(PauseMapScreen.ItemsLeftPrefix + (items.Count - 1)));
            var casketMark = screen.Markers.FirstOrDefault(m => ReferenceEquals(m.Source, casket));
            Assert.IsNotNull(casketMark, "A thing that implements IMapMarker is on the map.");
            Assert.That(casketMark.Kind, Is.EqualTo(MapMarkerKind.Casket), "\"Soul Casket\" is the casket kind.");
            Assert.That(casketMark.Letter, Is.EqualTo("?"), "wiki: a question mark marks coffins.");
            Vector2 casketAt = PauseMapScreen.FieldPoint(casket.MapPosition - player.transform.position, screen.Range, screen.Field.rect.width, out _);
            Assert.That(Vector2.Distance(casketMark.Root.anchoredPosition, casketAt), Is.LessThan(1f));
            Assert.That(casketMark.Root.anchoredPosition.x > 0f && casketMark.Root.anchoredPosition.y < 0f, Is.True, "South-east of the hero.");
            StringAssert.StartsWith("Soul Casket", screen.Legend.First(r => r.Kind == MapMarkerKind.Casket).Text.text);

            // Hidden things are not marked.
            casket.Visible = false;
            screen.Refresh();
            Assert.That(screen.Markers.Any(m => ReferenceEquals(m.Source, casket)), Is.False, "MapVisible false: no mark.");

            // Resuming with the pause key closes the map with the pause menu.
            system.Resume();
            yield return null;
            Assert.That(screen.IsOpen, Is.False, "Resuming closes the map.");
            Assert.AreEqual(1f, Time.timeScale);
            Object.Destroy(casketGo);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheMap_FitsAndReads_OnAPc_AndOnAPhoneHeldEitherWay()
        {
            yield return Boot();
            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            yield return OpenMap(pause);
            var screen = pause.MapScreen;
            var canvas = screen.GetComponent<Canvas>();
            var pauseCanvas = pause.GetComponentInParent<Canvas>().rootCanvas;
            var problems = new List<string>();
            try
            {
                foreach (var d in Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas, pauseCanvas);
                    screen.Relayout();
                    for (int f = 0; f < 2; f++) { Canvas.ForceUpdateCanvases(); yield return null; }
                    Canvas.ForceUpdateCanvases();

                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    var whole = new Rect(0f, 0f, size.x, size.y);
                    Rect window = ScreenSimulation.UnitsRect(screen.Window);
                    Rect field = ScreenSimulation.UnitsRect(screen.Field);
                    if (!Inside(whole, window)) problems.Add($"{d.Name}: the window {window} is off the {size} screen");
                    if (!Inside(window, field)) problems.Add($"{d.Name}: the map {field} is outside the window {window}");
                    if (Mathf.Abs(field.width - field.height) > 1f) problems.Add($"{d.Name}: the map {field.size} is not square");
                    if (field.width < 240f) problems.Add($"{d.Name}: the map is only {field.width:0} units wide");
                    bool portraitPhone = d.Touch && d.Pixels.y > d.Pixels.x;
                    if (screen.IsStacked != portraitPhone && d.Touch)
                        problems.Add($"{d.Name}: map and legend {(screen.IsStacked ? "stacked" : "side by side")}");

                    // Back and Guides: on the screen, apart, touch-sized on the phone, their labels inside them.
                    problems.AddRange(ScreenSimulation.Problems(d.Name, new Selectable[] { screen.BackButton, screen.GuidesButton }));
                    foreach (var button in new[] { screen.BackButton, screen.GuidesButton })
                    {
                        Rect b = ScreenSimulation.UnitsRect((RectTransform)button.transform);
                        if (!Inside(window, b)) problems.Add($"{d.Name}: {button.name} {b} is outside the window");
                        if (b.Overlaps(field)) problems.Add($"{d.Name}: {button.name} covers the map");
                    }

                    // Every mark whole inside the map.
                    foreach (var mark in screen.Markers)
                        if (!Inside(field, ScreenSimulation.UnitsRect(mark.Root)))
                            problems.Add($"{d.Name}: the {mark.Kind} mark {ScreenSimulation.UnitsRect(mark.Root)} leaves the map {field}");

                    // The words: inside the window, readable against it, never under the floor, not cut short.
                    Color panel = screen.Window.GetComponent<Image>().color;
                    float floor = Mathf.Max(14f, UiScreen.MinTextUnits) - 0.01f;
                    foreach (var text in new[] { screen.ItemsLeftText, screen.NoteText })
                    {
                        text.ForceMeshUpdate();
                        if (!Inside(window, ScreenSimulation.UnitsRect(text.rectTransform))) problems.Add($"{d.Name}: '{text.text}' leaves the window");
                        if (text.fontSize < floor) problems.Add($"{d.Name}: '{text.text}' is {text.fontSize:0.#}, under {floor:0.#}");
                        if (text.isTextTruncated) problems.Add($"{d.Name}: '{text.text}' is cut short");
                    }
                    if (UITheme.Contrast(screen.NoteText.color, panel) < 4.5f) problems.Add($"{d.Name}: the note reads at {UITheme.Contrast(screen.NoteText.color, panel):0.0}:1");
                    if (UITheme.Contrast(screen.ItemsLeftText.color, panel) < 3f) problems.Add($"{d.Name}: the count reads at {UITheme.Contrast(screen.ItemsLeftText.color, panel):0.0}:1");
                    screen.TitleText.ForceMeshUpdate();
                    if (!Inside(window, ScreenSimulation.UnitsRect(screen.TitleText.rectTransform))) problems.Add($"{d.Name}: the title leaves the window");
                    Rect header = ScreenSimulation.UnitsRect(screen.NoteText.rectTransform);
                    if (header.yMin < field.yMax - 1f && header.Overlaps(field)) problems.Add($"{d.Name}: the note covers the map");

                    // The legend: every line inside the window's width, readable on its row, its mark beside its words.
                    Rect legendView = ScreenSimulation.UnitsRect(screen.LegendScroll.viewport);
                    if (!Inside(window, legendView)) problems.Add($"{d.Name}: the legend {legendView} is outside the window");
                    if (legendView.Overlaps(field)) problems.Add($"{d.Name}: the legend covers the map");
                    if (legendView.width < 160f) problems.Add($"{d.Name}: the legend is only {legendView.width:0} units wide");
                    foreach (var row in screen.Legend)
                    {
                        Rect r = ScreenSimulation.UnitsRect(row.Root);
                        if (r.xMin < legendView.xMin - 1f || r.xMax > legendView.xMax + 1f) problems.Add($"{d.Name}: the {row.Kind} line is wider than the legend");
                        row.Text.ForceMeshUpdate();
                        if (row.Text.fontSize < floor) problems.Add($"{d.Name}: '{row.Text.text}' is {row.Text.fontSize:0.#}, under {floor:0.#}");
                        if (UITheme.Contrast(row.Text.color, UITheme.Row) < 4.5f) problems.Add($"{d.Name}: '{row.Text.text}' reads at {UITheme.Contrast(row.Text.color, UITheme.Row):0.0}:1");
                        Rect sample = ScreenSimulation.UnitsRect(row.Sample), words = ScreenSimulation.UnitsRect(row.Text.rectTransform);
                        if (sample.xMax > words.xMin + 1f) problems.Add($"{d.Name}: the {row.Kind} mark covers its words");
                        if (!Inside(r, sample)) problems.Add($"{d.Name}: the {row.Kind} mark {sample} leaves its line {r}");
                    }
                    for (int i = 1; i < screen.Legend.Count; i++)
                        if (ScreenSimulation.UnitsRect(screen.Legend[i].Root).Overlaps(ScreenSimulation.UnitsRect(screen.Legend[i - 1].Root)))
                            problems.Add($"{d.Name}: legend lines {i - 1} and {i} overlap");
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            system.Resume();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Guides_PointAtTheNearestStageItemsOffScreen_OnlyWhenSwitchedOn_AndNeverWhilePaused()
        {
            yield return Boot();
            var player = PlayerController.Instance;
            var system = GameSystemManager.Instance;
            var guides = MapGuides.Current;
            Assert.IsNotNull(guides, "The run has its guide arrows. " + Errors());
            Assert.That(guides.Arrows.Count, Is.EqualTo(MapGuides.MaxArrows));
            var camera = Camera.main;
            Assert.IsNotNull(camera);

            // Off until the player switches them on (wiki: "when toggled on").
            Assert.That(MapGuides.Enabled, Is.False);
            guides.Refresh(camera);
            Assert.That(guides.ShownCount, Is.EqualTo(0), "Guides off: no arrows.");

            // The switch is the map screen's Guides button, and it says its state in words.
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            yield return OpenMap(pause);
            var screen = pause.MapScreen;
            Assert.That(screen.GuidesText, Is.EqualTo(PauseMapScreen.GuidesOff));
            Click(screen.GuidesButton, "Guides");
            Assert.That(MapGuides.Enabled, Is.True);
            Assert.That(screen.GuidesText, Is.EqualTo(PauseMapScreen.GuidesOn));
            guides.Refresh(camera);
            Assert.That(guides.ShownCount, Is.EqualTo(0), "Never while the game is paused (a menu is open).");
            Click(screen.BackButton, "Back");
            system.Resume();
            yield return null;
            yield return null;

            // In play: the nearest stage items that are off screen, at most three. Relics and caskets get arrows too
            // (they are on the map through IMapMarker); this test is about the stage's items, so those step aside.
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is SoulHunter.Gameplay.IMapMarker) behaviour.gameObject.SetActive(false);
            var items = ItemsOnTheGround(_spawner);
            var offScreen = items.Where(i => !MapGuides.IsOnScreen(camera.WorldToViewportPoint(i.transform.position)))
                .OrderBy(i => Flat(i.transform.position - player.transform.position).sqrMagnitude).ToList();
            Assert.That(offScreen.Count, Is.GreaterThanOrEqualTo(MapGuides.MaxArrows), "The stage's items start far from the hero.");
            guides.Refresh(camera);
            Assert.That(guides.ShownCount, Is.EqualTo(MapGuides.MaxArrows), "At most three arrows, so the HUD stays calm.");
            var shown = guides.Arrows.Where(a => a.Shown).ToList();
            // The three nearest (two of them, the Metaglios, lie equally far: the order among equals is free).
            var nearest = offScreen.Take(MapGuides.MaxArrows).ToList();
            Assert.That(shown.Select(a => nearest.FindIndex(n => Vector3.Distance(a.Target, n.transform.position) < 0.01f)).OrderBy(x => x),
                Is.EqualTo(Enumerable.Range(0, MapGuides.MaxArrows)), "The arrows point at the three nearest items, one each.");
            for (int i = 0; i < shown.Count; i++)
            {
                var item = nearest.First(n => Vector3.Distance(shown[i].Target, n.transform.position) < 0.01f);
                Assert.That(shown[i].Kind, Is.EqualTo(MapMarkerKind.StageItem));
                Assert.That(shown[i].Root.gameObject.activeInHierarchy, Is.True);
                // Near the screen's edge, in the thing's direction, clear of the HUD's top band.
                Vector2 spot = shown[i].Root.anchorMin;
                Assert.That(spot.x, Is.InRange(MapGuides.InsetX - 0.001f, 1f - MapGuides.InsetX + 0.001f));
                Assert.That(spot.y, Is.InRange(MapGuides.InsetBottom - 0.001f, 1f - MapGuides.InsetTop + 0.001f));
                Vector3 hero = camera.WorldToViewportPoint(player.transform.position), thing = camera.WorldToViewportPoint(shown[i].Target);
                Vector2 direction = MapGuides.ScreenDirection(thing, hero);
                Vector2 expected = MapGuides.EdgePoint(direction);
                Assert.That(Vector2.Distance(spot, expected), Is.LessThan(0.001f), $"Arrow {i} sits where its item's direction meets the edge.");
                var sprite = item.GetComponent<StageItemPickup>() != null ? item.GetComponentInChildren<SpriteRenderer>(true).sprite : null;
                if (sprite != null) Assert.That(shown[i].Icon.sprite, Is.SameAs(sprite), "The arrow carries the item's icon (wiki).");
                else Assert.That(shown[i].Letter.text, Is.EqualTo("S"), "An item without a picture shows the map's letter.");
            }

            // An item the player can see gets no arrow.
            var near = offScreen[0];
            Vector3 was = near.transform.position;
            near.transform.position = player.transform.position + new Vector3(2f, 0f, 1f);
            Assert.That(MapGuides.IsOnScreen(camera.WorldToViewportPoint(near.transform.position)), Is.True, "The item is now on screen.");
            guides.Refresh(camera);
            Assert.That(guides.Arrows.Where(a => a.Shown).Any(a => Vector3.Distance(a.Target, near.transform.position) < 0.01f), Is.False,
                "On screen: its arrow is gone.");
            near.transform.position = was;

            // Paused again: hidden. Switched off: hidden.
            system.TogglePause();
            yield return null;
            guides.Refresh(camera);
            Assert.That(guides.ShownCount, Is.EqualTo(0), "Hidden under the pause menu.");
            system.Resume();
            yield return null;
            MapGuides.Enabled = false;
            guides.Refresh(camera);
            Assert.That(guides.ShownCount, Is.EqualTo(0), "Guides off: no arrows.");
            Assert.That(guides.Arrows.All(a => !a.Root.gameObject.activeSelf), Is.True);

            // The same rule in portrait and landscape: the edge point is a share of the screen.
            Assert.That(MapGuides.EdgePoint(Vector2.right).x, Is.EqualTo(1f - MapGuides.InsetX).Within(0.0001f));
            Assert.That(MapGuides.EdgePoint(Vector2.up).y, Is.EqualTo(1f - MapGuides.InsetTop).Within(0.0001f));
            Assert.That(MapGuides.EdgePoint(Vector2.down).y, Is.EqualTo(MapGuides.InsetBottom).Within(0.0001f));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - 1f && inner.xMax <= outer.xMax + 1f && inner.yMin >= outer.yMin - 1f && inner.yMax <= outer.yMax + 1f;

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
