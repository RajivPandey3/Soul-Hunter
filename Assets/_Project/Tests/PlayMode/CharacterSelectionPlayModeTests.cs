using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS-style character select: the main menu shows a card for every character, clicking one shows
    /// its full stat block, and the chosen character's stats and abilities are live when the run starts.
    /// Nothing is written to the save file (Confirm is not pressed; selection is set in memory).
    /// </summary>
    public class CharacterSelectionPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SelectScreen_HasACardPerCharacter_AndClickingShowsItsStats()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return null;

            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNotNull(navigation, "MainMenuNavigation missing. " + Errors());
            navigation.ShowCharacters();
            yield return null;

            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            Assert.IsNotNull(ui, "CharacterSelectionUI missing. " + Errors());
            int expected = GameContentCatalog.Load().Characters.Count(c => c != null);
            Assert.That(ui.Cards.Count, Is.EqualTo(expected), "One card per character. " + Errors());

            int isolde = GameContentCatalog.Load().Characters.Where(c => c != null).ToList().FindIndex(c => c.CharacterName == "Isolde");
            Assert.That(isolde, Is.GreaterThanOrEqualTo(0));
            Click(ui.Cards[isolde], "Isolde card");
            yield return null;

            Assert.That(ui.SelectedCharacter.CharacterName, Is.EqualTo("Isolde"));
            string stats = ui.StatsText.text;
            foreach (var label in new[] { "Max Health", "Recovery", "Armor", "Move Speed", "Might", "Speed", "Duration", "Area",
                                          "Cooldown", "Amount", "Revival", "Magnet", "Luck", "Growth", "Greed", "Curse" })
                StringAssert.Contains(label, stats, $"Stats panel is missing {label}.");
            StringAssert.Contains("-15%", stats, "Isolde's -15% cooldown should show.");
            Assert.That(ui.Cards[isolde].gameObject.activeInHierarchy, Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ChosenCharacter_StatBlockAndAbilities_AreLiveInTheRun()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = "Sylas"; // in memory only, never saved
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                var sylas = GameContentCatalog.FindCharacterExact("Sylas");
                var player = PlayerController.Instance;
                var stats = player.Stats;
                var health = player.GetComponent<HealthController>();

                Assert.That(stats.MoveSpeedMultiplier, Is.EqualTo(sylas.BaseMoveSpeed).Within(0.001f), "Move speed multiplier applied. " + Errors());
                Assert.That(health.IsShielded, Is.True, "Sylas starts shielded. " + Errors());
                int before = health.CurrentHealth;
                health.TakeDamage(new DamagePacket(20, player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
                Assert.That(health.CurrentHealth, Is.EqualTo(before), "Shield blocks the hit. " + Errors());
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                save.CurrentData.SelectedCharacterName = previousSelection;
            }
        }
    }
}
