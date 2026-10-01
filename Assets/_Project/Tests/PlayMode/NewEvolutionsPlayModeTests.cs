using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 1 plan step 3 (2026-10-01): the VS evolutions Soul Hunter lacked, with the owner's names (EvolutionBuilder,
    /// GameContentCatalog.Evolutions). Each one evolves from its base weapon at level 8 plus its passive (VS recipe from
    /// the base weapon's wiki page), replaces the base weapon and shows its Soul Hunter name.
    /// </summary>
    public class NewEvolutionsPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EachNewEvolution_EvolvesFromItsRecipe_AndReplacesItsBaseWeapon()
        {
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog.Evolutions, Is.Not.Null.And.Not.Empty, "GameContent lists the new evolutions.");
            foreach (var evolution in catalog.Evolutions)
            {
                // A fresh run for each: six evolutions in one run would fill the six weapon slots.
                yield return BootIntoGameplay();
                Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
                var player = PlayerController.Instance;
                player.GetComponent<HealthController>().IsInvincible = true;
                var manager = WeaponManager.Instance;
                var known = GetPrivateField<System.Collections.Generic.List<WeaponEvolutionData>>(manager, "_availableEvolutions");
                Assert.That(known, Does.Contain(evolution), $"{evolution.EvolvedName} is known to the WeaponManager.");
                manager.GiveWeapon(evolution.BaseWeapon, 8);
                manager.GiveWeapon(evolution.RequiredPassive, 1);
                yield return null;
                var baseObject = manager.GetWeaponObject(evolution.BaseWeapon);
                Assert.That(manager.TryEvolveWeapon(out string evolvedName), Is.True, $"{evolution.EvolvedName} evolves.");
                Assert.That(evolvedName, Is.EqualTo(evolution.EvolvedName));
                yield return null;
                var evolved = player.GetComponentsInChildren<AutoAttackWeapon>(false)
                    .FirstOrDefault(w => w.gameObject.name.StartsWith(evolution.EvolvedWeaponPrefab.name));
                Assert.That(evolved, Is.Not.Null, $"{evolution.EvolvedName} is held.");
                Assert.That(evolved.isActiveAndEnabled, Is.True);
                Assert.That(WeaponEvolutionData.DisplayName(evolution.EvolvedName), Does.Not.StartWith("Evo"), "A readable name.");
                Assert.That(manager.GetEquippedWeapons().Any(p => p.Key == baseObject), Is.False, "The base weapon is replaced.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
