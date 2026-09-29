using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-09-29): "VS rule: no enemy drops". VS wiki: Floor Chicken, Gold Coin, Coin Bag, Vacuum and Orologion drop
    /// from light sources; enemies drop Experience Gems. A normal kill used to roll a Magnet (1%), a Time Freeze (1%), a
    /// coin (2%) and a chicken (5%, instead of its gem), and a strong hero left thousands of coins and chickens on the ground.
    /// </summary>
    public class EnemyDropPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator NormalKills_DropTheirGem_AndNoChickenCoinMagnetOrFreeze()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            // Luck 10 would have made every drop far likelier; a VS kill still drops only its gem.
            PlayerController.Instance.Stats.AddLuck(9f);
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var origin = PlayerController.Instance.transform.position + Vector3.forward * 25f;

            int chickens = Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length;
            int coins = Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Length;
            int magnets = Object.FindObjectsByType<MagnetPickup>(FindObjectsSortMode.None).Length;
            int freezes = Object.FindObjectsByType<TimeFreezePickup>(FindObjectsSortMode.None).Length;
            int gems = XPGem.ActiveGems.Count;

            const int Kills = 60;
            for (int i = 0; i < Kills; i++)
            {
                var enemy = Object.Instantiate(prefab, origin + new Vector3(i % 10, 0f, i / 10) * 2f, Quaternion.identity);
                enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, enemy.transform.position, Vector3.zero, DamageType.Normal));
            }
            yield return null;

            Assert.That(XPGem.ActiveGems.Count - gems, Is.EqualTo(Kills), "Every normal kill drops its gem (a chicken used to replace it).");
            Assert.That(Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length, Is.EqualTo(chickens), "No floor chickens from kills.");
            Assert.That(Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Length, Is.EqualTo(coins), "No coins from kills.");
            Assert.That(Object.FindObjectsByType<MagnetPickup>(FindObjectsSortMode.None).Length, Is.EqualTo(magnets), "No magnets from kills.");
            Assert.That(Object.FindObjectsByType<TimeFreezePickup>(FindObjectsSortMode.None).Length, Is.EqualTo(freezes), "No time freezes from kills.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
