using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end chest flow with the VS chest-opening screen (vampire.survivors.wiki/w/Treasure_Chest): collecting a
    /// chest pauses the game and opens the screen with the "Treasure Found" jingle; OPEN opens the lid, plays the chest-size
    /// jingle, counts the gold up and reveals every item; DONE resumes the game. The first six chests on a save cannot be
    /// skipped; later ones can, and coin-only chests skip themselves once 5-item chests are skippable.
    /// </summary>
    public class ChestFlowPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator CollectingChest_PlaysTheVsOpening_AndDoneResumesGame()
        {
            yield return BootIntoGameplay(save => { save.ChestsOpened = 0; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            // The persistent AudioManager is destroyed between PlayMode tests; the game makes it on its first scene load.
            AudioManager.EnsureInstance();
            yield return null;
            Assert.That(AudioManager.Instance != null, Is.True, "An AudioManager plays the jingles.");
            // Weapons off: a weapon firing in the collection frame would replace Treasure Found as the last sound.
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(true)) weapon.enabled = false;
            var chestUI = LiveChestUI();
            Assert.IsFalse(chestUI.IsOpen, "No chest screen before a chest is collected.");

            // Bug MIS B08: the HUD gold must not jump to the new total before the chest screen has counted it up.
            var hud = Object.FindFirstObjectByType<SoulHunter.Gameplay.UI.GameHUDController>();
            var hudGold = GetPrivateField<TMPro.TextMeshProUGUI>(hud, "_goldText");
            int goldBefore = SoulHunter.Gameplay.Core.RunStatsTracker.Instance.TotalGoldCollected;

            var pool = PickupPoolManager.Instance;
            pool.SpawnChest(PlayerController.Instance.transform.position);
            var chest = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.activeInHierarchy);
            Assert.IsNotNull(chest, "SpawnChest did not produce an active chest.");
            yield return WaitUntilRealtime(() => !chest.gameObject.activeInHierarchy, "the player to collect the chest");
            yield return null;

            Assert.IsTrue(chestUI.IsOpen, "Collecting a chest opens the chest screen. " + Errors());
            Assert.AreEqual(0f, Time.timeScale, "The game pauses under the chest screen.");
            var screen = chestUI.Screen;
            // A new weapon from the chest can fire in the same frame, so the recent clips are checked, not only the last one.
            Assert.That(AudioManager.Instance.RecentClips, Does.Contain(AudioManager.Instance.TreasureFoundSound), "VS: Treasure Found plays first.");
            Assert.That(screen.CanSkip, Is.False, "VS: the first six chests cannot be skipped.");
            Assert.That(screen.TrySkip(), Is.False);
            Assert.That(PauseActionEnabled(), Is.False, "Esc skips chests, so it does not open the pause menu meanwhile.");

            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.WaitingToOpen, "the OPEN button");
            Assert.That(screen.OpenButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(hudGold.text, Is.EqualTo("Gold: " + goldBefore), "Before OPEN the HUD still shows the gold from before the chest.");
            Assert.That(screen.Preview, Is.Not.Null, "The chest is shown in 3D.");
            Assert.That(screen.Preview.Texture, Is.Not.Null);
            Assert.That(screen.Preview.LidOpen, Is.False, "The lid stays shut until OPEN.");

            Click(screen.OpenButton, "Open");
            yield return WaitUntilRealtime(() => screen.Preview != null && screen.Preview.CoinsInFlight > 0, "coins to burst out of the chest");
            Assert.That(AudioManager.Instance.RecentClips, Does.Contain(AudioManager.Instance.TreasureJingle(screen.ItemCount)),
                "The chest-size jingle plays on OPEN.");
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.Done, "every item to be revealed");
            Assert.That(screen.Preview.LidOpen, Is.True, "OPEN opened the lid.");
            var result = Object.FindFirstObjectByType<ChestLogicController>().LastResult;
            Assert.That(screen.RevealedCount, Is.EqualTo(result.Rewards.Count), "Every item is revealed.");
            Assert.That(screen.ShownGold, Is.EqualTo(result.Gold).And.GreaterThan(0), "The gold counts up to the chest's gold.");
            Assert.That(hudGold.text, Is.EqualTo("Gold: " + (goldBefore + result.Gold)), "Once counted, the HUD shows the new total.");
            Assert.That(screen.DoneButton.gameObject.activeInHierarchy, Is.True);
            Assert.AreEqual(0f, Time.timeScale, "Still paused until DONE.");

            Click(screen.DoneButton, "Done");
            yield return null;
            Assert.IsFalse(chestUI.IsOpen, "DONE closes the chest screen.");
            Assert.AreEqual(1f, Time.timeScale, "The game resumes after DONE. " + Errors());
            Assert.That(PauseActionEnabled(), Is.True, "The pause key works again.");
            Assert.That(Save().ChestsOpened, Is.EqualTo(1), "The chest is counted on the save (VS skip rules).");
            Assert.That(GameObject.Find("ChestPreviewStage"), Is.Null, "The 3D preview is cleaned up.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FirstSixChestsOnASave_Give_1_1_3_1_1_5_Items()
        {
            // VS wiki (Treasure_Chest): "The first six chests picked up in a save will always drop items in the set
            // sequence 1-1-3-1-1-5." Six real chests are collected and opened; the seventh rolls again.
            yield return BootIntoGameplay(save => { save.ChestsOpened = 0; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(true)) weapon.enabled = false;
            var chestUI = LiveChestUI();
            var logic = Object.FindFirstObjectByType<ChestLogicController>();
            int[] expected = { 1, 1, 3, 1, 1, 5 };
            var seen = new System.Collections.Generic.List<int>();

            for (int i = 0; i < expected.Length; i++)
            {
                PickupPoolManager.Instance.SpawnChest(PlayerController.Instance.transform.position);
                var chest = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.activeInHierarchy);
                Assert.IsNotNull(chest, $"Chest {i + 1} did not spawn.");
                yield return WaitUntilRealtime(() => !chest.gameObject.activeInHierarchy, $"the player to collect chest {i + 1}");
                yield return null;
                Assert.IsTrue(chestUI.IsOpen, $"Chest {i + 1} opens the screen. " + Errors());
                seen.Add(chestUI.Screen.ItemCount);
                Assert.That(logic.LastResult.ItemCount, Is.EqualTo(expected[i]), $"Chest {i + 1}: seen so far {string.Join("-", seen)}.");
                Assert.That(logic.LastResult.Rewards.Count, Is.EqualTo(expected[i]), $"Chest {i + 1} gives one reward per item.");
                yield return chestUI.OpenAndClose(20f);
                Assert.IsFalse(chestUI.IsOpen, $"Chest {i + 1} closed.");
                Assert.That(Save().ChestsOpened, Is.EqualTo(i + 1));
            }
            Assert.That(Save().FiveItemChestsOpened, Is.EqualTo(1), "The sixth chest was a 5-item chest.");
            Assert.That(ChestLogicController.FirstChestItems(Save().ChestsOpened), Is.EqualTo(0), "From the seventh chest the count is rolled.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheChestScreen_HasItsOwnCanvas_AndSurvivesATemporaryCanvasGoingAway()
        {
            // Found 2026-09-29 (benchmark stall watchdog): the chests' ChestUI (UI_Bindings, no canvas above it) put its
            // screen on FindFirstObjectByType<Canvas>(), which could be a temporary canvas such as the character unlock
            // notice. When that canvas went away, the screen went with it and the game stayed paused for good.
            yield return BootIntoGameplay(save => { save.ChestsOpened = 20; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var chestUI = GetPrivateField<ChestUI>(PickupPoolManager.Instance, "_chestUI");
            Assert.That(chestUI, Is.Not.Null, "The pickup pool hands chests to a ChestUI.");

            // A temporary overlay canvas like the unlock notice, which the old lookup could pick.
            var notice = new GameObject("TemporaryNotice", typeof(RectTransform));
            notice.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var result = new ChestResult { ItemCount = 1, Gold = 150 };
            result.Rewards.Add(new ChestReward { Name = "Whip", Level = 2 });
            chestUI.OpenChest("You Found a Chest!", result);
            var screen = chestUI.Screen;
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.GetComponentInParent<Canvas>().rootCanvas, Is.SameAs(chestUI.ScreenCanvas()),
                "The chest screen lives on the ChestUI's own canvas.");
            Assert.That(chestUI.ScreenCanvas().sortingOrder, Is.EqualTo(ChestUI.ScreenSortingOrder));
            Assert.AreEqual(0f, Time.timeScale);

            Object.Destroy(notice);
            yield return null;
            yield return null;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The screen is still there after the temporary canvas went away.");
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.WaitingToOpen, "the OPEN button");
            screen.PressOpen();
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.Done, "the reveal");
            screen.PressDone();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "DONE resumes the game.");

            // Safety net: a screen that disappears while open releases the pause.
            chestUI.OpenChest("You Found a Chest!", result);
            Assert.AreEqual(0f, Time.timeScale);
            Object.Destroy(chestUI.Screen.gameObject);
            yield return null;
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "A chest screen destroyed while open does not leave the game paused.");
            Assert.That(PauseActionEnabled(), Is.True, "The pause key works again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FiveItemChest_LightsUp_HasFireworks_AndPlaysTheBigJingle()
        {
            yield return BootIntoGameplay(save => { save.ChestsOpened = 20; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            // The persistent AudioManager is destroyed between PlayMode tests; the game makes it on its first scene load.
            AudioManager.EnsureInstance();
            yield return null;
            Assert.That(AudioManager.Instance != null, Is.True, "An AudioManager plays the jingles.");
            var chestUI = LiveChestUI();
            var result = new ChestResult { ItemCount = 5, Gold = 750 };
            for (int i = 0; i < 5; i++) result.Rewards.Add(new ChestReward { Name = "Item " + i, Level = i + 1 });

            chestUI.OpenChest("TREASURE x5!", result);
            var screen = chestUI.Screen;
            Assert.That(screen.CanSkip, Is.False, "VS: 5-item chests need 50 of them (or 500 chests) before they can be skipped.");
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.WaitingToOpen, "the OPEN button");
            screen.PressOpen();
            yield return null;
            Assert.That(AudioManager.Instance.LastClip, Is.SameAs(AudioManager.Instance.TreasureCSound), "The 5-item jingle.");
            yield return WaitUntilRealtime(() => screen.Current == ChestOpeningScreen.Phase.Done, "the reveal");
            Assert.That(screen.BackgroundLit, Is.True, "The background lights up on 3- and 5-item chests.");
            Assert.That(screen.FireworksShown, Is.GreaterThan(0), "Fireworks on a 5-item chest.");
            Assert.That(screen.RevealedCount, Is.EqualTo(5));
            Assert.That(screen.ShownGold, Is.EqualTo(750));
            screen.PressDone();
            yield return null;
            Assert.That(Save().FiveItemChestsOpened, Is.EqualTo(1));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AfterSixChests_AChestCanBeSkipped_AndCoinOnlyChestsSkipThemselves()
        {
            yield return BootIntoGameplay(save => { save.ChestsOpened = 6; save.FiveItemChestsOpened = 0; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var chestUI = LiveChestUI();
            var result = new ChestResult { ItemCount = 1, Gold = 150 };
            result.Rewards.Add(new ChestReward { Name = "Whip", Level = 2 });

            chestUI.OpenChest("You Found a Chest!", result);
            var screen = chestUI.Screen;
            Assert.That(screen.CanSkip, Is.True, "VS: from the seventh chest, 1- and 3-item chests can be skipped.");
            yield return null;
            Assert.That(screen.TrySkip(), Is.True);
            Assert.That(screen.Current, Is.EqualTo(ChestOpeningScreen.Phase.Done), "Skipping jumps to the end.");
            Assert.That(screen.RevealedCount, Is.EqualTo(1));
            Assert.That(screen.ShownGold, Is.EqualTo(150));
            screen.PressDone();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);

            // Once 5-item chests are skippable, a chest of coin bags only plays a quick in-game animation (VS).
            Save().FiveItemChestsOpened = 50;
            var coins = new ChestResult { ItemCount = 3, Gold = 400 };
            for (int i = 0; i < 3; i++) coins.Rewards.Add(new ChestReward { Name = "Coin Bag", IsCoinBag = true });
            chestUI.OpenChest("TREASURE x3!", coins);
            Assert.That(chestUI.IsOpen, Is.False, "No screen for a coin-only chest.");
            Assert.That(chestUI.QuickChests, Is.EqualTo(1));
            Assert.AreEqual(1f, Time.timeScale, "The game never paused.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ChestOpenEffect_KeepsPlaying_WhileTheGameIsPaused()
        {
            // Found 2026-09-28: the chest effect started in the frame the game paused and froze on its first frame.
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var at = PlayerController.Instance.transform.position;
            VFXPoolManager.Play(VfxKind.ChestOpen, at);
            GameTime.Pause(this);
            try
            {
                var effect = VFXPoolManager.Instance.GetComponentsInChildren<ParticleSystem>(true)
                    .FirstOrDefault(p => p.gameObject.activeInHierarchy && p.main.useUnscaledTime);
                Assert.That(effect, Is.Not.Null, "The chest effect runs on real time.");
                float start = effect.time;
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(effect.time, Is.GreaterThan(start), "It keeps playing while paused.");
            }
            finally { GameTime.Resume(this); }
        }

        private static GameData Save() => GameServices.Instance.Get<SaveService>().CurrentData;

        private static bool PauseActionEnabled()
        {
            GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            var pause = input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
            return pause != null && pause.enabled;
        }

        private static ChestUI LiveChestUI()
        {
            var live = Object.FindObjectsByType<ChestUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(ui => ui.isActiveAndEnabled && GetPrivateField<GameObject>(ui, "_chestPanel") != null)
                .ToArray();
            Assert.AreEqual(1, live.Length, "Expected exactly one active, wired ChestUI.");
            return live[0];
        }
    }
}
