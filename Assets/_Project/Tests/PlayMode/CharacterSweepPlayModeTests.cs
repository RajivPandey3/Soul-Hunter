using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Plays every character in the content catalog for a few seconds and checks the basics a player
    /// would notice: the model is visible, the starting weapon is equipped, it damages a nearby enemy,
    /// and it never hurts its own holder. Written after the Whip (never hit) and Song of Mana / Bloody
    /// Tear (hit the player) bugs, which only surfaced once characters started with their own weapons.
    /// </summary>
    public class CharacterSweepPlayModeTests : GameplayPlayModeTestBase
    {
        private const float AttackTimeoutSeconds = 12f;

        private static IEnumerable<string> CharacterNames()
        {
            var catalog = GameContentCatalog.Load();
            if (catalog == null || catalog.Characters == null) return Enumerable.Empty<string>();
            return catalog.Characters.Where(c => c != null).Select(c => c.CharacterName).Distinct().ToList();
        }

        [UnityTest]
        public IEnumerator Character_IsVisible_AttacksWithStartingWeapon_AndNeverHurtsItself(
            [ValueSource(nameof(CharacterNames))] string characterName)
        {
            var character = GameContentCatalog.FindCharacterExact(characterName);
            Assert.IsNotNull(character, $"{characterName} is not in the content catalog.");

            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = characterName; // in memory only, never saved
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                // Nothing but the player's own weapon may touch the player during this check.
                var spawner = Object.FindFirstObjectByType<EnemySpawner>();
                Assert.IsNotNull(spawner, "EnemySpawner missing. " + Errors());
                spawner.enabled = false;
                foreach (var stray in EnemyController.ActiveEnemies.ToList())
                    if (stray != null) Object.Destroy(stray.gameObject);
                yield return null;

                var player = PlayerController.Instance;
                var playerHealth = player.GetComponent<HealthController>();
                int startHealth = playerHealth.CurrentHealth;

                // 1. Visible.
                bool visible = player.GetComponentsInChildren<Renderer>().Any(r =>
                    r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.sqrMagnitude > 0.0001f);
                Assert.IsTrue(visible, $"{characterName}: player has no visible renderer. " + Errors());

                // 2. Starting weapon equipped.
                var weapons = Object.FindFirstObjectByType<WeaponManager>();
                Assert.IsNotNull(weapons, "WeaponManager missing. " + Errors());
                Assert.That(weapons.GetWeaponLevel(character.StartingWeapon), Is.GreaterThanOrEqualTo(1),
                    $"{characterName}: starting weapon {character.StartingWeapon} was not equipped. " + Errors());

                // 3. It damages a nearby, harmless enemy.
                var enemyPrefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
                Assert.That(enemyPrefabs, Is.Not.Null.And.Not.Empty);
                var enemy = Object.Instantiate(enemyPrefabs[0], player.transform.position + new Vector3(0f, 0f, 3f), Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var enemyHealth = enemy.GetComponent<HealthController>();
                int enemyStart = enemyHealth.CurrentHealth;

                float deadline = Time.realtimeSinceStartup + AttackTimeoutSeconds;
                bool hit = false;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (enemy == null || !enemy.activeInHierarchy || enemyHealth.CurrentHealth < enemyStart) { hit = true; break; }
                    yield return null;
                }
                Assert.IsTrue(hit, $"{characterName}: starting weapon {character.StartingWeapon} never damaged an enemy 3 units away within {AttackTimeoutSeconds}s. " + Errors());

                // 4. Never hurts its own holder, and nothing threw.
                Assert.That(playerHealth.CurrentHealth, Is.GreaterThanOrEqualTo(startHealth),
                    $"{characterName}: player lost health with only its own weapon active. " + Errors());
                StringAssert.StartsWith("(no errors", Errors(), $"{characterName}: errors were logged.");

                if (enemy != null) Object.Destroy(enemy);
            }
            finally
            {
                if (GameServices.Instance != null)
                    GameServices.Instance.Get<SaveService>().CurrentData.SelectedCharacterName = previousSelection;
            }
        }
    }
}
