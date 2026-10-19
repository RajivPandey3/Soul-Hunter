using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The Collection and Bestiary are filled by real runs (release item P3-02, 2026-10-09; wiki Collection: entries "used by
    /// the player at least once in their save file"; wiki Ars_Gouda: "the enemies they have killed on their save file"):
    /// a weapon gained, a chicken eaten and an enemy killed in the real game reach the (throw-away) save, the Bestiary opens
    /// when the run reaches Level 3, and the real save file is never written.
    /// </summary>
    public class CollectionDiscoveryPlayModeTests : GameplayPlayModeTestBase
    {
        private static string RealSave => Path.Combine(Application.persistentDataPath, "soulhunter_save.json");

        [UnityTest]
        public IEnumerator AWeapon_AChicken_AndAKill_AreRecordedOnTheSave()
        {
            string before = File.Exists(RealSave) ? File.ReadAllText(RealSave) : null;
            yield return BootIntoGameplay(data =>
            {
                data.CollectionFound = new List<string>(CollectionBook.FoundFromTheStart);
                data.BestiaryKills = new List<EnemyKillRecord>();
                data.UnlockedAchievements.Remove(Bestiary.UnlockId);
            });
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var save = GameServices.Instance.Get<SaveService>();
            var data = save.CurrentData;
            Assert.IsNotNull(Object.FindFirstObjectByType<CollectionTracker>(), "Every run has a CollectionTracker.");
            yield return null;
            Assert.That(CollectionBook.IsFound(data, CollectionCatalog.ItemId(UpgradeData.UpgradeType.Whip)), Is.True, "Kael's starting Whip is used.");

            // A weapon gained.
            WeaponManager.Instance.GiveWeapon(UpgradeData.UpgradeType.Knife, 1);
            yield return null;
            Assert.That(CollectionBook.IsFound(data, CollectionCatalog.ItemId(UpgradeData.UpgradeType.Knife)), Is.True, "The Knife is found once held.");

            // A chicken eaten.
            var chicken = PickupPoolManager.Instance.SpawnChicken(player.transform.position + Vector3.forward * 1.5f);
            yield return WaitUntilRealtime(() => !chicken.activeInHierarchy, "the chicken next to the hero to be collected");
            Assert.That(CollectionBook.IsFound(data, CollectionCatalog.PickupId(PickupNames.Chicken)), Is.True, "The Roast Chicken is found once eaten.");

            // An enemy killed, twice.
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var type = prefab.GetComponent<EnemyController>().Data;
            Assert.IsNotNull(type, "The stage enemy has its EnemyData.");
            int kills = Bestiary.Kills(data, type.name);
            for (int i = 0; i < 2; i++)
            {
                var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * (25f + i * 3f), Quaternion.identity);
                enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, enemy.transform.position, Vector3.zero, DamageType.Normal));
            }
            yield return null;
            Assert.That(Bestiary.Kills(data, type.name), Is.EqualTo(kills + 2), "Each defeat of the type is counted.");
            Assert.That(data.BestiaryKills.FindAll(r => r.Enemy == type.name).Count, Is.EqualTo(1), "One record per type.");

            // Bug MIS ITM-09: the tracker now keeps each type's record; a save whose list is replaced (Delete Save, a load)
            // must still get the next kill on its own list.
            data.BestiaryKills = new List<EnemyKillRecord>();
            var again = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 31f, Quaternion.identity);
            again.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, again.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            Assert.That(Bestiary.Kills(data, type.name), Is.EqualTo(1), "The kill is counted on the new list.");
            Assert.That(Bestiary.IsUnlocked(data), Is.False, "Level 1 does not open the Bestiary.");

            // Reaching Level 3 opens the Bestiary (balance method for VS's Ars Gouda in Dairy Plant).
            LevelProgressionManager.Instance.StartStage(3);
            yield return WaitUntilRealtime(() => Bestiary.IsUnlocked(data), "the Bestiary to unlock at Level 3");

            // The throw-away save holds it all; the real save was not written.
            save.SaveGame();
            var written = new GameData();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(save.FilePath), written);
            Assert.That(written.CollectionFound, Does.Contain(CollectionCatalog.ItemId(UpgradeData.UpgradeType.Knife)));
            Assert.That(written.BestiaryKills.Exists(r => r.Enemy == type.name && r.Kills == 1), Is.True);
            Assert.That(save.FilePath, Is.Not.EqualTo(RealSave));
            Assert.That(File.Exists(RealSave) ? File.ReadAllText(RealSave) : null, Is.EqualTo(before), "The real save is unchanged.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
