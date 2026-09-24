using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: BiblePrefabTests guards the Bible defect: _biblePrefab pointed at the prefab asset
    /// (fileID 100100000) instead of its root GameObject, so spawning books threw InvalidCastException.
    /// Bible and its Unholy Vespers evolution must reference a GameObject that instantiates cleanly.
    /// </summary>
    public class BiblePrefabTests
    {
        [TestCase("Assets/Prefabs/Weapons/Bible_Weapon.prefab")]
        [TestCase("Assets/Prefabs/Weapons/Generated/UnholyVespers.prefab")]
        public void BookPrefab_IsAGameObjectThatInstantiates(string path)
        {
            var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(weaponPrefab, Is.Not.Null, path);
            var bible = weaponPrefab.GetComponent<BibleWeapon>();
            Assert.That(bible, Is.Not.Null, $"{path} BibleWeapon.");

            var book = typeof(BibleWeapon).GetField("_biblePrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bible) as GameObject;
            Assert.That(book, Is.Not.Null, $"{path} ka _biblePrefab GameObject hona chahiye.");
            Assert.That(book.GetComponent<BibleWeapon>(), Is.Null, "Bible apne aap ko book ke taur par spawn na kare.");

            var instance = Object.Instantiate(book);
            try
            {
                Assert.That(instance, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
