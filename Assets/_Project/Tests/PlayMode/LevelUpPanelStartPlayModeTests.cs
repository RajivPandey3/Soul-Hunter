using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report 2026-09-30 ("kabhi bhi hang ho ja raha hai"): with LevelUp_Panel saved inactive in the scene, its own
    /// LevelUpUI ran Awake only when the first level-up switched it on, and hid it again. The game stayed paused with no
    /// cards to pick.
    /// </summary>
    public class LevelUpPanelStartPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator APanelSavedInactive_StillShowsItsCards_OnTheFirstLevelUp()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;

            // A copy of the panel whose LevelUpUI has never woken, as when the scene saves the panel inactive.
            var field = typeof(LevelUpUI).GetField("_levelUpPanel", Private);
            var uis = Object.FindObjectsByType<LevelUpUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var original = (GameObject)field.GetValue(uis[0]);
            var holder = new GameObject("InactiveHolder");
            holder.SetActive(false);
            var copy = Object.Instantiate(original, holder.transform);
            copy.SetActive(false);
            copy.transform.SetParent(original.transform.parent, false);
            original.SetActive(false);
            foreach (var ui in uis)
                if (ui.gameObject != original) field.SetValue(ui, copy);
            var copyUi = copy.GetComponent<LevelUpUI>();
            if (copyUi != null) field.SetValue(copyUi, copy);
            Object.Destroy(holder);

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "the first level-up");
            yield return null;
            yield return null;

            Assert.That(copy.activeInHierarchy, Is.True, "The level-up panel is shown on the first level-up, not hidden by its own Awake.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
