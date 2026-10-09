using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Data;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS character select, Filtering/sorting (vampire.survivors.wiki/w/Characters, fetched 2026-10-09): the filter keeps
    /// "at least one group of characters visible"; the grid sorts by "the default character order, alphabetically, or
    /// through the number of complete stages per character ... ascending or descending"; "favorited characters will show
    /// up first, regardless of how the grid is sorted, but not if it is filtered" (favourites unlock through Mindbender).
    /// Saves go to a throw-away file; save data changed here is put back afterwards.
    /// </summary>
    public class CharacterSortPlayModeTests : GameplayPlayModeTestBase
    {
        private CharacterSelectionUI _ui;
        private GameData _data;
        private List<string> _names;

        private IEnumerator OpenSelect()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            _data = save.CurrentData;
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowCharacters();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            _ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            _names = GameContentCatalog.Load().Characters.Where(c => c != null).Select(c => c.CharacterName).ToList();
        }

        [UnityTest]
        public IEnumerator TheFilter_KeepsAtLeastOneGroupOfCharactersVisible()
        {
            yield return OpenSelect();
            _ui.SetFilter(true);
            for (int level = 1; level <= 10; level++) _ui.SetStageShown(level, false);
            Assert.That(_ui.StageToggles.Count(t => t.isOn), Is.GreaterThanOrEqualTo(1), "A level stays ticked.");
            Assert.That(_ui.VisibleNames(), Is.Not.Empty, "Its characters stay on the grid.");
            for (int i = 0; i < _names.Count; i++)
            {
                int origin = Mathf.Clamp(GameContentCatalog.FindCharacterExact(_names[i]).OriginStage, 1, 10);
                Assert.That(_ui.Cards[i].gameObject.activeSelf, Is.EqualTo(_ui.StageToggles[origin - 1].isOn), $"{_names[i]} follows its level's box.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheGrid_SortsAToZ_AndByLevelsCompleted_AscendingOrDescending()
        {
            yield return OpenSelect();
            var stagesBefore = _data.CharacterStages;
            try
            {
                _ui.SelectCharacter(_names.IndexOf("Gideon"));
                Assert.That(_ui.SortText, Is.EqualTo("SORT: DEFAULT"));
                Assert.That(_ui.OrderText, Is.EqualTo("ASCENDING"));
                Assert.That(_ui.VisibleNames(), Is.EqualTo(_names), "Default: the catalog order.");

                Click(SortButton("Sort"), "the sort tab");
                Assert.That(_ui.Sort, Is.EqualTo(CharacterSelectionUI.CharacterSort.Alphabetical));
                Assert.That(_ui.SortText, Is.EqualTo("SORT: A-Z"));
                var az = _names.OrderBy(n => n, System.StringComparer.OrdinalIgnoreCase).ToList();
                Assert.That(_ui.VisibleNames(), Is.EqualTo(az));
                Assert.That(GridOrder(), Is.EqualTo(az), "The cards are laid out in that order.");
                Click(SortButton("Order"), "the order tab");
                Assert.That(_ui.OrderText, Is.EqualTo("DESCENDING"));
                Assert.That(_ui.VisibleNames(), Is.EqualTo(Enumerable.Reverse(az).ToList()));
                Assert.That(_ui.SelectedCharacter.CharacterName, Is.EqualTo("Gideon"), "Sorting keeps the chosen character.");

                _data.CharacterStages = new List<CharacterStageRecord>();
                foreach (int level in new[] { 1, 2, 3 }) CharacterStages.Record(_data, "Aria", level);
                foreach (int level in new[] { 1, 2 }) CharacterStages.Record(_data, "Kael", level);
                CharacterStages.Record(_data, "Gideon", 4);
                _ui.SetSort(CharacterSelectionUI.CharacterSort.StagesCompleted);
                Assert.That(_ui.SortText, Is.EqualTo("SORT: LEVELS"));
                Assert.That(_ui.VisibleNames().Take(3), Is.EqualTo(new[] { "Aria", "Kael", "Gideon" }), "Most levels completed first.");
                _ui.SetSortDescending(false);
                Assert.That(_ui.VisibleNames().Skip(_names.Count - 3), Is.EqualTo(new[] { "Gideon", "Kael", "Aria" }), "Ascending: most levels last.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                _data.CharacterStages = stagesBefore;
            }
        }

        [UnityTest]
        public IEnumerator Favourites_AreLockedUntilMindbender_ThenComeFirst_ButStayFiltered()
        {
            yield return OpenSelect();
            var achievementsBefore = new List<string>(_data.UnlockedAchievements);
            var favouritesBefore = new List<string>(_data.FavoriteCharacters);
            try
            {
                _data.UnlockedAchievements.Remove(CharacterFavourites.UnlockId);
                _data.FavoriteCharacters.Clear();
                _ui.SelectCharacter(_names.IndexOf("Gideon"));
                Assert.That(_ui.FavouriteText, Is.EqualTo("FAVOURITE: LOCKED"), "VS: favourites come with Mindbender.");
                Assert.That(_ui.FavouriteButton.interactable, Is.False);
                _ui.SetFavourite(true);
                Assert.That(_data.FavoriteCharacters, Is.Empty);

                CharacterFavourites.Unlock(_data);
                _ui.Refresh();
                Assert.That(_ui.FavouriteText, Is.EqualTo("FAVOURITE: OFF"));
                Click(_ui.FavouriteButton, "the favourite button");
                Assert.That(_ui.FavouriteText, Is.EqualTo("FAVOURITE: ON"));
                Assert.That(CharacterFavourites.IsFavourite(_data, "Gideon"), Is.True);

                foreach (var sort in new[] { CharacterSelectionUI.CharacterSort.Default, CharacterSelectionUI.CharacterSort.Alphabetical, CharacterSelectionUI.CharacterSort.StagesCompleted })
                    foreach (bool descending in new[] { false, true })
                    {
                        _ui.SetSort(sort);
                        _ui.SetSortDescending(descending);
                        Assert.That(_ui.VisibleNames()[0], Is.EqualTo("Gideon"), $"A favourite comes first ({sort}, descending {descending}).");
                    }

                // "but not if it is filtered": hiding the favourite's level hides it.
                int origin = GameContentCatalog.FindCharacterExact("Gideon").OriginStage;
                _ui.SetFilter(true);
                _ui.SetStageShown(origin, false);
                Assert.That(_ui.VisibleNames(), Does.Not.Contain("Gideon"));
                _ui.SetStageShown(origin, true);
                _ui.SetFilter(false);
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                _data.UnlockedAchievements = achievementsBefore;
                _data.FavoriteCharacters = favouritesBefore;
            }
        }

        private UnityEngine.UI.Button SortButton(string name) =>
            _ui.transform.root.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == name + "Button");

        /// <summary>The shown cards by their place in the grid (sibling order).</summary>
        private List<string> GridOrder() =>
            _ui.Cards.Where(c => c.gameObject.activeSelf).OrderBy(c => c.transform.GetSiblingIndex())
                .Select(c => _names[_ui.Cards.ToList().IndexOf(c)]).ToList();
    }
}
