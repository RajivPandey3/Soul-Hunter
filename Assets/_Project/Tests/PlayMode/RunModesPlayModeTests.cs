using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS run modes in the real game (owner 2026-09-27): modes saved on the character select screen reach the run.
    /// Hurry runs the stage clock twice as fast, Hyper makes the player 65% faster, and in Arcana mode the run starts with
    /// an Arcana and an Arcana chest gives another. Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class RunModesPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator HurryAndHyper_ClockRunsTwiceAsFast_AndThePlayerIsFaster()
        {
            yield return BootIntoGameplay(save => { save.ModeHurry = true; save.ModeHyper = true; });

            Assert.That(RunModes.Hurry && RunModes.Hyper, Is.True, "The saved modes are copied into the run. " + Errors());
            Assert.That(RunModes.Inverse || RunModes.Endless || RunModes.Arcana || RunModes.LimitBreak, Is.False);

            var progression = LevelProgressionManager.Instance;
            float clockStart = progression.StageElapsedTime;
            float gameStart = Time.time;
            yield return WaitUntilRealtime(() => Time.time - gameStart >= 1.5f, "1.5 s of game time");
            float ratio = (progression.StageElapsedTime - clockStart) / (Time.time - gameStart);
            Assert.That(ratio, Is.EqualTo(2f).Within(0.15f), $"Hurry: the stage clock runs 2x game time (ratio {ratio:0.00}). " + Errors());

            var player = PlayerController.Instance;
            float hyperSpeed = player.MoveSpeed;
            RunModes.Hyper = false;
            float normalSpeed = player.MoveSpeed;
            RunModes.Hyper = true;
            Assert.That(hyperSpeed / normalSpeed, Is.EqualTo(1.65f).Within(0.01f), "Hyper: the player moves 65% faster.");

            var arcanas = ArcanaManager.Instance;
            if (arcanas != null) Assert.That(arcanas.ActiveArcanas, Is.Empty, "Without Arcana mode no Arcana is given.");
        }

        [UnityTest]
        public IEnumerator ArcanaMode_StartsWithAnArcana_AndAnArcanaChestGivesAnother()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);

            var arcanas = ArcanaManager.Instance;
            Assert.IsNotNull(arcanas, "No ArcanaManager in Main_Gameplay.");
            Assert.That(RunModes.Arcana, Is.True);
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(1), "VS Arcana mode: the run starts with one Arcana. " + Errors());
            Assert.That(arcanas.StartingArcana, Is.SameAs(arcanas.ActiveArcanas[0]));

            var chestUI = Object.FindObjectsByType<ChestUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(ui => ui.isActiveAndEnabled && GetPrivateField<GameObject>(ui, "_chestPanel") != null);
            var panel = GetPrivateField<GameObject>(chestUI, "_chestPanel");

            // An Arcana chest, as the 11:00 / 21:00 chest elites drop it, right on the player.
            var chestObject = PickupPoolManager.Instance.SpawnChest(PlayerController.Instance.transform.position);
            Assert.IsNotNull(chestObject, "SpawnChest produced nothing.");
            var chest = chestObject.GetComponent<ChestPickup>();
            chest.IsArcana = true;

            yield return WaitUntilRealtime(() => !chestObject.activeInHierarchy, "the player to collect the Arcana chest");
            yield return null;

            Assert.IsTrue(panel.activeInHierarchy, "The chest screen opens. " + Errors());
            var rewardText = GetPrivateField<TextMeshProUGUI>(chestUI, "_rewardText");
            Assert.That(rewardText.text, Does.StartWith("ARCANA CHEST!"), $"Reward text: '{rewardText.text}'. " + Errors());
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(2), "The Arcana chest gave a second Arcana.");
            Assert.That(arcanas.ActiveArcanas[1], Is.Not.SameAs(arcanas.ActiveArcanas[0]), "Never one already held.");
            Assert.That(rewardText.text, Does.Contain(arcanas.ActiveArcanas[1].CardName));

            Click(GetPrivateField<Button>(chestUI, "_claimButton"), "Claim");
            yield return null;
            Assert.IsFalse(panel.activeInHierarchy, "Claim closes the chest screen.");
        }
    }
}
