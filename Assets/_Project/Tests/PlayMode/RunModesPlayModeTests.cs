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
    /// Hurry runs the stage clock twice as fast, Hyper gives the player the stage's Hyper move speed (Level 1: x2 instead
    /// of x1.1, VS Mad Forest), and in Arcana mode the run starts with
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
            // VS Mad Forest infobox (Level 1): player-speed x1.1 normal, player-speed2 = 2 in Hyper.
            Assert.That(CampaignLevelCatalog.CurrentStage, Is.EqualTo(1), "The run starts on Level 1.");
            Assert.That(hyperSpeed / normalSpeed, Is.EqualTo(2.0f / 1.1f).Within(0.01f),
                "Hyper on Level 1 (VS Mad Forest): move speed x2 instead of x1.1.");

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
            // VS (wiki Arcanas, P2-01): the run opens the start choice; the player picks a card.
            var screen = ArcanaChoiceScreen.Current;
            Assert.IsNotNull(screen, "Arcana mode opens the start choice. " + Errors());
            if (screen.IsOpen) Click(screen.TakeButton, "Take this card");
            yield return null;
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(1), "VS Arcana mode: the run starts with one Arcana. " + Errors());
            Assert.That(arcanas.StartingArcana, Is.SameAs(arcanas.ActiveArcanas[0]));

            // An Arcana chest, as the 11:00 / 21:00 chest elites drop it, right on the player: it opens the Arcana choice.
            var chestObject = PickupPoolManager.Instance.SpawnChest(PlayerController.Instance.transform.position);
            Assert.IsNotNull(chestObject, "SpawnChest produced nothing.");
            var chest = chestObject.GetComponent<ChestPickup>();
            chest.IsArcana = true;

            yield return WaitUntilRealtime(() => !chestObject.activeInHierarchy, "the player to collect the Arcana chest");
            yield return null;

            Assert.IsTrue(screen.IsOpen, "The Arcana chest opens the Arcana choice. " + Errors());
            Assert.That(screen.Cards.Count, Is.EqualTo(ArcanaDraft.ChestChoices), "VS: four choices.");
            var picked = screen.Cards[1].Arcana;
            Click(screen.Cards[1].Button, "the second card");
            Click(screen.TakeButton, "Take this card");
            yield return null;
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(2), "The Arcana chest gave a second Arcana.");
            Assert.That(arcanas.ActiveArcanas[1], Is.SameAs(picked), "The card the player chose.");
            Assert.That(arcanas.ActiveArcanas[1], Is.Not.SameAs(arcanas.ActiveArcanas[0]), "Never one already held.");
            Assert.That(SoulHunter.Core.Services.GameTime.IsPaused, Is.False, "Taking the card resumes the game. " + Errors());
        }
    }
}
