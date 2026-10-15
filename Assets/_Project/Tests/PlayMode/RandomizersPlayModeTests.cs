using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Environment;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Release item P3-05 in the real game (owner 2026-10-09): Blind Fate (VS Random LevelUp), Omens (VS Random Events),
    /// the Inverse map look and layout with "Visually Invert Stages", and the Music screen (VS Music Selection). Every test
    /// plays on a throw-away save (GameplayPlayModeTestBase; the menu test redirects it itself).
    /// </summary>
    public class RandomizersPlayModeTests : GameplayPlayModeTestBase
    {
        private static int StageItemsOf(UpgradeData.UpgradeType type, StageItemSpawner items, out Vector3 at)
        {
            at = default;
            int found = 0;
            foreach (var placed in items.Placed)
            {
                var item = placed != null ? placed.GetComponent<StageItemPickup>() : null;
                if (item == null || item.Upgrade != type) continue;
                at = item.transform.position;
                found++;
            }
            return found;
        }

        // ---------------------------------------------------------------- Blind Fate

        [UnityTest]
        public IEnumerator BlindFate_ALevelUpNeverPausesNorOpensTheScreen_AndRaisesOneItem()
        {
            yield return BootIntoGameplay(save => save.ModeRandomLevelUp = true);
            Assert.That(RunModes.RandomLevelUp, Is.True, "The saved mode reaches the run. " + Errors());

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = PlayerController.Instance.GetComponent<PlayerExperience>();
            var weapons = WeaponManager.Instance;
            int screens = 0;
            var picks = new List<UpgradeData>();
            levelUp.OnLevelUpUIRound += _ => screens++;
            levelUp.OnRandomPick += pick => picks.Add(pick);

            int level = experience.CurrentLevel;
            experience.AddXP(experience.XPToNextLevel);
            yield return null;

            Assert.That(experience.CurrentLevel, Is.GreaterThan(level), "The hero levelled up.");
            Assert.That(screens, Is.Zero, "Blind Fate opens no level-up screen.");
            Assert.That(levelUp.IsLevelUpActive, Is.False, "The level-up is over at once.");
            Assert.That(GameTime.IsPaused, Is.False, "The game never paused.");
            Assert.That(picks.Count, Is.GreaterThanOrEqualTo(1), "A random pick was made.");
            var last = picks[picks.Count - 1];
            if (!MaxedRewards.IsCard(last) && !LimitBreak.IsCard(last))
                Assert.That(weapons.GetWeaponLevel(last.Type), Is.GreaterThanOrEqualTo(last.Level), $"{last.UpgradeName} was applied.");
            var popup = Object.FindFirstObjectByType<BlindFatePopup>();
            Assert.IsNotNull(popup, "The HUD shows Blind Fate's pick.");
            Assert.That(popup.IsShowing, Is.True, "The pick shows over the hero.");
            Assert.That(popup.Text, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator BlindFate_TheSwitchInThePauseMenu_BringsTheChoiceBack()
        {
            yield return BootIntoGameplay(save => save.ModeRandomLevelUp = true);
            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            Assert.That(pause.BlindFateText, Is.EqualTo("Blind Fate: ON"), "The switch says its state in words.");
            Click(pause.BlindFateButton, "Blind Fate");
            Assert.That(RunModes.RandomLevelUp, Is.False, "Switched off for this run (VS: \"can now be toggled during a run\").");
            Assert.That(GameSettings.Data.ModeRandomLevelUp, Is.False, "And for the next runs.");
            Assert.That(pause.BlindFateText, Is.EqualTo("Blind Fate: OFF"));
            system.Resume();
            yield return null;

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = PlayerController.Instance.GetComponent<PlayerExperience>();
            List<UpgradeData> offered = null;
            levelUp.OnLevelUpUIRound += choices => offered = choices;
            experience.AddXP(experience.XPToNextLevel);
            yield return null;
            Assert.IsNotNull(offered, "Without Blind Fate the level-up screen opens again.");
            Assert.That(GameTime.IsPaused, Is.True, "And the game pauses for the choice.");
            levelUp.SelectUpgrade(offered[0]);
            yield return CloseOpenLevelUps();
        }

        // ---------------------------------------------------------------- Omens

        [UnityTest]
        public IEnumerator Omens_TheWheelSpinsAtXX30_AndTheEventFiresAtXX00()
        {
            yield return BootIntoGameplay(save => { save.ModeRandomEvents = true; save.FlashingVfx = true; });
            // The hero's weapons off: no kill can level the hero and pause the clock (or the wheel) mid-test.
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            var director = RandomEventDirector.Instance;
            Assert.IsNotNull(director, "GameSessionManager adds the Omens director. " + Errors());
            director.ForcedPick = () => OmenEvent.FortunesFavour;
            var progression = LevelProgressionManager.Instance;
            var stats = PlayerController.Instance.Stats;
            var wheel = Object.FindFirstObjectByType<OmenRouletteUI>();
            Assert.IsNotNull(wheel, "The HUD has the Omens wheel.");

            progression.SkipTo(29.4f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= 30.2f, "0:30");
            Assert.That(director.Pending, Is.EqualTo(OmenEvent.FortunesFavour), "0:30 picks an event.");
            Assert.That(director.PendingFiresAt, Is.EqualTo(60f));
            yield return null;
            Assert.That(wheel.IsShowing, Is.True, "The wheel shows while Omens is on.");
            Assert.That(wheel.IsSpinning, Is.True, "Its rings spin after the pick.");
            float angle = wheel.RingAngle(0);
            yield return null;
            yield return null;
            Assert.That(wheel.RingAngle(0), Is.Not.EqualTo(angle), "The rings turn.");
            // The wheel reads: every line 4.5:1 on its plate (ui-ux-pro-max).
            foreach (var text in wheel.Texts)
                Assert.That(UITheme.Contrast(text.color, wheel.Plate.color), Is.GreaterThanOrEqualTo(4.5f), $"'{text.text}' reads on the wheel's plate.");
            Rect plate = ScreenRect(wheel.Root);
            Assert.That(plate.xMax <= Screen.width + 1f && plate.yMin >= -1f, Is.True, $"The wheel {plate} sits inside the screen, bottom right.");

            float luck = stats.Luck;
            progression.SkipTo(59.4f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= 60.2f, "1:00");
            Assert.That(director.LastFired, Is.EqualTo(OmenEvent.FortunesFavour), "1:00 fires the picked event.");
            Assert.That(director.Pending, Is.Null);
            Assert.That(stats.Luck - luck, Is.EqualTo(OmenTable.FortuneLuck).Within(1e-3f), "Fortune's Favour: Luck +50% (VS Luck Boost).");
            yield return null;
            Assert.That(wheel.EventText, Is.EqualTo(OmenTable.Name(OmenEvent.FortunesFavour)));
            Assert.That(wheel.WhenText, Is.EqualTo("Now"));
        }

        [UnityTest]
        public IEnumerator Omens_Off_PicksNothing_AndTheWheelIsHidden()
        {
            yield return BootIntoGameplay();
            var director = RandomEventDirector.Instance;
            var progression = LevelProgressionManager.Instance;
            progression.SkipTo(29.4f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= 30.2f, "0:30");
            yield return null;
            Assert.That(director.Pending, Is.Null, "Without Omens nothing is picked.");
            var wheel = Object.FindFirstObjectByType<OmenRouletteUI>();
            Assert.That(wheel == null || !wheel.IsShowing, Is.True, "And no wheel shows.");
        }

        [UnityTest]
        public IEnumerator Omens_EveryEventOfLevel1_Happens_AndDeathsShadowOnlyOnItsStages()
        {
            yield return BootIntoGameplay(save => save.ModeRandomEvents = true);
            var director = RandomEventDirector.Instance;
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();

            // The rings: their heals on the ground.
            int before = Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Count(h => h.isActiveAndEnabled);
            Assert.That(director.Fire(OmenEvent.FeastRing), Is.True, "Feast Ring. " + Errors());
            var all = Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Where(h => h.isActiveAndEnabled).ToList();
            Assert.That(all.Count - before, Is.EqualTo(OmenTable.FeastCount), "A ring of chickens.");
            Assert.That(all.Count(h => h.HealAmount == OmenTable.FeastHeal), Is.GreaterThanOrEqualTo(OmenTable.FeastCount), "Each heals 30.");
            Assert.That(director.Fire(OmenEvent.MendingRing), Is.True, "Mending Ring.");
            Assert.That(Object.FindObjectsByType<MendingHeal>(FindObjectsSortMode.None).Length, Is.EqualTo(OmenTable.MendingCount));
            Assert.That(Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Count(h => h.isActiveAndEnabled && h.HealAmount == OmenTable.MendingHealAmount),
                Is.EqualTo(OmenTable.MendingCount), "Each small heal gives 2.");

            Assert.That(director.Fire(OmenEvent.GildedFever), Is.True);
            Assert.That(GoldFever.Running, Is.True, "Gilded Fever starts a Gold Fever.");
            Assert.That(director.Fire(OmenEvent.SwellingHorde), Is.True);
            Assert.That(RunModes.EventFrequencyBoost, Is.EqualTo(OmenTable.SwellingFactor), "Swelling Horde raises the spawn rate.");
            Assert.That(director.Fire(OmenEvent.HellfireBreath), Is.True);
            Assert.IsNotNull(player.GetComponentInChildren<FireBreath>(), "Hellfire Breath on the hero.");
            int relics = Object.FindObjectsByType<StageItemPickup>(FindObjectsSortMode.None).Length;
            Assert.That(director.Fire(OmenEvent.WanderingRelic), Is.True);
            Assert.That(Object.FindObjectsByType<StageItemPickup>(FindObjectsSortMode.None).Length, Is.EqualTo(relics + 1), "A passive lies near the hero.");
            Assert.That(director.Fire(OmenEvent.LanternBloom), Is.True, "Lantern Bloom.");
            Assert.That(director.Fire(OmenEvent.SoulMeteors), Is.True, "Soul Meteors.");
            Assert.That(director.Fire(OmenEvent.GallowsRing), Is.True, "Gallows Ring.");
            foreach (var formation in new[] { OmenEvent.ClosingRing, OmenEvent.HordeSwarm, OmenEvent.MarchingLine, OmenEvent.SwarmRush })
                Assert.That(director.Fire(formation), Is.True, $"{OmenTable.Name(formation)} sends a formation.");
            Assert.That(director.Fire(OmenEvent.TollOfSouls), Is.True, "Toll of Souls.");
            Assert.That(EnemySpawnerVisibleNonBosses(), Is.Zero, "Toll of Souls clears the screen (VS Rosary).");
            // 2026-10-13: as an Omen the Shadow comes on every level (its own timetable stays on Levels 5 and 9).
            Assert.That(director.Fire(OmenEvent.DeathsShadow), Is.True, "Death's Shadow comes as an Omen on Level 1 too.");
            foreach (var shadow in SoulHunter.Gameplay.Hazards.StageHazards.Instance.Shadows.ToList()) if (shadow != null) shadow.Leave();
            Assert.That(health.CurrentHealth, Is.GreaterThan(0));
        }

        private static bool Near(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        private static int EnemySpawnerVisibleNonBosses()
        {
            int count = 0;
            foreach (var enemy in SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies)
                if (enemy != null && enemy.isActiveAndEnabled && !enemy.IsStageBoss && !enemy.ResistsInstantKill
                    && ScreenWipePickup.IsOnScreen(Camera.main, enemy.transform.position))
                    count++;
            return count;
        }

        // ---------------------------------------------------------------- Inverse map

        [UnityTest]
        public IEnumerator Inverse_TurnsTheStageItems_AndDarkensTheFloor_UntilTheOptionGoesOff()
        {
            yield return BootIntoGameplay(save => { save.ModeInverse = true; save.VisuallyInvertStages = true; });
            yield return null;
            yield return null;
            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            var origin = PlayerController.Instance.transform.position;
            foreach (var entry in StageItems.Plan(1).Where(e => e.Kind == StageItems.Kind.Passive || e.Kind == StageItems.Kind.Weapon))
            {
                Assert.That(StageItemsOf(entry.Upgrade, items, out var at), Is.EqualTo(1), $"{entry.Upgrade} is placed.");
                Vector3 toItem = at - origin;
                toItem.y = 0f;
                Assert.That(Vector3.Dot(toItem, entry.Offset), Is.LessThan(0f),
                    $"{entry.Upgrade} lies on the turned side (VS: the layout \"rotated by 180°\"): planned {entry.Offset}, placed {toItem}.");
            }

            var look = InverseStageLook.Instance;
            Assert.IsNotNull(look, "GameSessionManager adds the Inverse look.");
            var map = Object.FindFirstObjectByType<InfiniteMap>();
            var renderer = map.ChunkAt(InfiniteMap.GridSize / 2, InfiniteMap.GridSize / 2).GetComponentsInChildren<Renderer>()
                .First(r => r.sharedMaterial != null);
            Assert.That(look.IsGraded(renderer), Is.True, "The floor carries the inverted grade.");
            Assert.That(FloorMergedLook.IsMerged(renderer), Is.True, "The floor is one merged mesh (FloorMeshMerger).");
            Assert.That(Shader.GetGlobalFloat("_SH_FloorInverse"), Is.EqualTo(1f), "The merged floor draws its baked Inverse tint.");
            // The baked Inverse tint is InverseMap.Grade of the part's own tint (FloorMergeTests checks every part).
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var tints = mesh.colors;
            var inverse = new System.Collections.Generic.List<Vector3>();
            mesh.GetUVs(2, inverse);
            Assert.That(inverse.Count, Is.EqualTo(tints.Length));
            Color first = tints[0].gamma, graded = InverseMap.Grade(new Color(first.r, first.g, first.b, 1f)).linear;
            Assert.That(Near(new Color(inverse[0].x, inverse[0].y, inverse[0].z, 1f), new Color(graded.r, graded.g, graded.b, 1f)), Is.True,
                $"Darker and turned in hue (InverseMap.Grade): baked {inverse[0]}, graded {graded}.");
            Color.RGBToHSV(graded.gamma, out _, out _, out float v);
            Color.RGBToHSV(first, out _, out _, out float v0);
            Assert.That(v, Is.GreaterThanOrEqualTo(Mathf.Min(v0, InverseMap.ComfortMinTint) - 1e-2f), "Never below the ground comfort band.");

            // "Visually Invert Stages" off: the floor looks as it always does.
            GameSettings.Data.VisuallyInvertStages = false;
            GameSettings.NotifyChanged();
            yield return null;
            yield return null;
            Assert.That(look.IsGraded(renderer), Is.False, "Option off: no inverted grade.");
            Assert.That(Shader.GetGlobalFloat("_SH_FloorInverse"), Is.EqualTo(0f));
            Assert.That(InverseMap.Active, Is.False);
            Assert.That(RunModes.Inverse, Is.True, "The Inverse numbers stay; only the look goes.");
        }

        [UnityTest]
        public IEnumerator WithoutInverse_TheStageItemsLieWhereThePlanSays_AndTheFloorIsUntouched()
        {
            yield return BootIntoGameplay();
            yield return null;
            yield return null;
            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            var origin = PlayerController.Instance.transform.position;
            foreach (var entry in StageItems.Plan(1).Where(e => e.Kind == StageItems.Kind.Passive || e.Kind == StageItems.Kind.Weapon))
            {
                Assert.That(StageItemsOf(entry.Upgrade, items, out var at), Is.EqualTo(1));
                Vector3 toItem = at - origin;
                toItem.y = 0f;
                Assert.That(Vector3.Dot(toItem, entry.Offset), Is.GreaterThan(0f), $"{entry.Upgrade} on its planned side.");
            }
            var map = Object.FindFirstObjectByType<InfiniteMap>();
            var renderer = map.ChunkAt(0, 0).GetComponentInChildren<Renderer>();
            Assert.That(InverseStageLook.Instance.IsGraded(renderer), Is.False);
        }

        // ---------------------------------------------------------------- Music

        [UnityTest]
        public IEnumerator Music_TheChosenTrackPlays_AtItsSpeed_ForOneRun()
        {
            AudioManager.EnsureInstance(); // as AudioPlayModeTests: the game makes it at startup, a test boot may not
            yield return BootIntoGameplay(save => { save.MusicChoice = MusicSelection.FirstLevelTrack + 2; save.MusicTempo = MusicSelection.Slower; });
            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager exists. " + Errors());
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[2]), "Level 1 plays the chosen Level 3 track.");
            Assert.That(audio.MusicPitch, Is.EqualTo(MusicSelection.PitchFor(MusicSelection.Slower)).Within(1e-3f), "At the slower speed.");
            Assert.That(GameSettings.Data.MusicChoice, Is.EqualTo(MusicSelection.LevelsOwn), "Not kept: the next run is back to each level's own track.");
        }

        [UnityTest]
        public IEnumerator Music_AKeptChoice_PersistsBetweenRuns()
        {
            AudioManager.EnsureInstance(); // as AudioPlayModeTests: the game makes it at startup, a test boot may not
            yield return BootIntoGameplay(save => { save.MusicChoice = MusicSelection.MainTheme; save.MusicTempo = MusicSelection.Faster; save.MusicLocked = true; });
            var audio = AudioManager.Instance;
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.MenuMusic), "The main theme can be a run's music.");
            Assert.That(audio.MusicPitch, Is.EqualTo(MusicSelection.PitchFor(MusicSelection.Faster)).Within(1e-3f));
            Assert.That(GameSettings.Data.MusicChoice, Is.EqualTo(MusicSelection.MainTheme), "Kept: the choice persists between runs.");
            Assert.That(GameSettings.Data.MusicTempo, Is.EqualTo(MusicSelection.Faster));
        }

        [UnityTest]
        public IEnumerator Music_ByDefault_EachLevelPlaysItsOwnTrack()
        {
            AudioManager.EnsureInstance(); // as AudioPlayModeTests: the game makes it at startup, a test boot may not
            yield return BootIntoGameplay();
            var audio = AudioManager.Instance;
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[0]), "By default Level 1 plays its own track.");
            Assert.That(audio.MusicPitch, Is.EqualTo(1f).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator MusicScreen_PicksPreviewsAndKeeps_FitsAndReads()
        {
            AudioManager.EnsureInstance(); // as AudioPlayModeTests: the game makes it at startup, a test boot may not
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json")); // never the real save
            save.CurrentData.MusicChoice = 0;
            save.CurrentData.MusicTempo = 0;
            save.CurrentData.MusicLocked = false;
            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            navigation.ShowCharacters();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            Assert.IsNotNull(ui.MusicButton, "The Music card sits beside the modes.");
            Rect card = ScreenRect((RectTransform)ui.MusicButton.transform);
            foreach (var mode in ui.ModeToggles)
                Assert.That(ScreenRect((RectTransform)mode.transform).Overlaps(card), Is.False, $"{mode.name} is clear of the Music card.");

            Click(ui.MusicButton, "Music");
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            var screen = ui.MusicScreen;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The Music screen opens.");
            Assert.That(screen.Tracks.Count, Is.EqualTo(MusicSelection.ChoiceCount), "Every Soul Hunter track, and each level's own.");
            Assert.That(screen.IsFramed(MusicSelection.LevelsOwn), Is.True, "The default is framed.");

            var audio = AudioManager.Instance;
            Click(screen.Tracks[MusicSelection.FirstLevelTrack + 4], "Level 5 track");
            Assert.That(save.CurrentData.MusicChoice, Is.EqualTo(MusicSelection.FirstLevelTrack + 4));
            Assert.That(screen.IsFramed(MusicSelection.FirstLevelTrack + 4) && !screen.IsFramed(MusicSelection.LevelsOwn), Is.True, "Only the chosen track is framed.");
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[4]), "The pick plays as a preview.");
            Click(screen.Tempos[MusicSelection.Faster], "Faster");
            Assert.That(save.CurrentData.MusicTempo, Is.EqualTo(MusicSelection.Faster));
            Assert.That(audio.MusicPitch, Is.EqualTo(MusicSelection.PitchFor(MusicSelection.Faster)).Within(1e-3f), "Previewed at its speed.");
            Assert.That(screen.LockText, Is.EqualTo("Keep for next runs: OFF"));
            Click(screen.LockButton, "Keep");
            Assert.That(save.CurrentData.MusicLocked, Is.True);
            Assert.That(screen.LockText, Is.EqualTo("Keep for next runs: ON"), "The lock says its state in words.");

            // Fits the window and reads (ui-ux-pro-max: 4.5:1; one size per kind of control).
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            Rect window = ScreenRect(screen.Window);
            Assert.That(window.xMin >= -1f && window.yMin >= -1f && window.xMax <= Screen.width + 1f && window.yMax <= Screen.height + 1f, Is.True,
                $"The window {window} is on screen ({Screen.width}x{Screen.height}).");
            var controls = screen.Tracks.Concat(screen.Tempos).Concat(new[] { screen.LockButton, screen.DoneButton }).ToList();
            foreach (var control in controls)
            {
                Rect r = ScreenRect((RectTransform)control.transform);
                Assert.That(r.xMin >= window.xMin - 1f && r.xMax <= window.xMax + 1f && r.yMin >= window.yMin - 1f && r.yMax <= window.yMax + 1f, Is.True,
                    $"{control.name} {r} is inside the window {window}.");
            }
            for (int i = 0; i < controls.Count; i++)
                for (int j = i + 1; j < controls.Count; j++)
                    Assert.That(ScreenRect((RectTransform)controls[i].transform).Overlaps(ScreenRect((RectTransform)controls[j].transform)), Is.False,
                        $"{controls[i].name} and {controls[j].name} overlap.");
            var theme = UiThemeAsset.Active;
            foreach (var tab in screen.Tempos)
            {
                Vector2 size = ((RectTransform)tab.transform).rect.size;
                Assert.That(Mathf.Abs(size.x - theme.Layouts.TabWidth) < 0.6f && Mathf.Abs(size.y - theme.Layouts.TabHeight) < 0.6f, Is.True,
                    $"{tab.name} is {size}, the theme's tab size is {theme.Layouts.TabWidth}x{theme.Layouts.TabHeight}.");
            }
            foreach (var (text, surface) in screen.ReadableTexts())
                Assert.That(UITheme.Contrast(text.color, surface), Is.GreaterThanOrEqualTo(4.5f), $"'{text.text}' reads at 4.5:1.");

            Click(screen.DoneButton, "Done");
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.MenuMusic), "Done returns the menu theme.");
            Assert.That(audio.MusicPitch, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(ui.MusicCardText, Does.Contain("Level 5").And.Contain("Faster").And.Contain("kept"), "The card names the choice.");
        }

        // ---------------------------------------------------------------- Options

        [UnityTest]
        public IEnumerator Options_Gameplay_HasVisuallyInvertStages_AndSavesIt()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json")); // never the real save
            save.CurrentData.VisuallyInvertStages = true;
            yield return null;
            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            Click(navigation.OptionsButton, "Options");
            yield return null;
            var options = OptionsMenuUI.Current;
            Assert.IsNotNull(options, "Options opened. " + Errors());
            options.ShowTab(3); // Gameplay
            yield return null;
            var gameplay = options.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Gameplay" && r.parent != null && r.parent.name == "Panel");
            var rows = gameplay.GetComponentsInChildren<Selectable>(true).Select(x => x.transform.parent.name).ToList();
            Assert.That(rows.IndexOf("Visually Invert Stages"), Is.EqualTo(rows.IndexOf("Show Stage Progression") + 1),
                "VS Options, Gameplay: \"Visually Invert Stages\" right after \"Show stage progression\".");
            var row = gameplay.GetComponentsInChildren<Button>(true).First(b => b.name == "Visually Invert Stages");
            Assert.That(row.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("ON"), "On by default.");
            Click(row, "Visually Invert Stages");
            Assert.That(save.CurrentData.VisuallyInvertStages, Is.False, "The switch writes the save's option.");
            Assert.That(row.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("OFF"));
            Object.Destroy(options.gameObject);
        }

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            var canvas = t.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
