using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Plays as a character whose starting weapon is the Whip (Antonio) and checks what the player sees:
    /// the character model is visible, the starting weapon is the Whip, and the Whip damages an enemy
    /// on the side it swings. Guards the Whip prefab's empty enemy mask (it never hit anything).
    /// </summary>
    public class PlayerLoadoutPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Antonio_IsVisible_StartsWithWhip_AndWhipDamagesEnemies()
        {
            // Boot to the menu, pick Antonio in memory only (never written to the save file), then play.
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = "Antonio";
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                var player = PlayerController.Instance;

                // 1. The character model is visible.
                var visible = new List<string>();
                foreach (var renderer in player.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.bounds.size.sqrMagnitude > 0.0001f)
                        visible.Add(renderer.name);
                Assert.That(visible, Is.Not.Empty, "Player has no enabled, non-empty renderer, so it is invisible. " + Errors());

                // 2. Antonio starts with the Whip.
                var whip = player.GetComponentInChildren<WhipWeapon>();
                Assert.IsNotNull(whip, "Antonio's starting Whip was not spawned. " + Errors());
                Assert.That(whip.TargetLayer.value, Is.EqualTo(LayerMask.GetMask("Enemy")), "Whip must target the Enemy layer.");

                // 3. The Whip damages an enemy on the side it swings (first swing faces the player's facing).
                var spawner = Object.FindFirstObjectByType<EnemySpawner>();
                var enemyPrefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
                Assert.That(enemyPrefabs, Is.Not.Null.And.Not.Empty);
                float facing = Mathf.Sign(player.transform.root.localScale.x);
                var enemy = Object.Instantiate(enemyPrefabs[0], player.transform.position + new Vector3(facing * 2f, 0f, 0f), Quaternion.identity);
                var enemyHealth = enemy.GetComponent<HealthController>();
                int startHealth = enemyHealth.CurrentHealth;

                yield return WaitUntilRealtime(() => enemyHealth == null || enemyHealth.CurrentHealth < startHealth,
                    "the Whip to damage an enemy next to the player");
                if (enemy != null) Object.Destroy(enemy);
            }
            finally
            {
                if (GameServices.Instance != null)
                    GameServices.Instance.Get<SaveService>().CurrentData.SelectedCharacterName = previousSelection;
            }
        }

        [UnityTest]
        public IEnumerator Aria_SongOfManaBeam_DoesNotHurtThePlayer()
        {
            // Aria starts with Song of Mana, whose beam used TouchDamage's default "Player" target and
            // killed its own holder within seconds.
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = "Aria";
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                var player = PlayerController.Instance;
                var health = player.GetComponent<HealthController>();
                Assert.IsNotNull(player.GetComponentInChildren<SongOfManaWeapon>(), "Aria's Song of Mana was not spawned. " + Errors());

                TouchDamage beam = null;
                yield return WaitUntilRealtime(() => (beam = player.GetComponentInChildren<TouchDamage>()) != null, "the Song of Mana beam");
                Assert.That(beam.TargetTag, Is.EqualTo("Enemy"), "The beam must hit enemies, not the player.");

                // Enemies spawn 15+ units away, so nothing else can reach the player this quickly.
                int startHealth = health.CurrentHealth;
                float until = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < until) yield return null;
                Assert.That(health.CurrentHealth, Is.EqualTo(startHealth), "The player took damage while only its own beam was active. " + Errors());
            }
            finally
            {
                if (GameServices.Instance != null)
                    GameServices.Instance.Get<SaveService>().CurrentData.SelectedCharacterName = previousSelection;
            }
        }
    }
}
