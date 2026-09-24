using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Pickups;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: PickupPoolPrewarmTests guards the startup crash where the gold coin pool never filled:
    /// copies of an inactive template never fire OnDisable, so the prewarm loop created coins forever.
    /// The pool must fill to its size, stop, and hand out working coins.
    /// </summary>
    public class PickupPoolPrewarmTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject _managerObject;
        private GameObject _coinModel;
        private GameObject _otherPickup;

        [TearDown]
        public void TearDown()
        {
            if (_managerObject != null) Object.DestroyImmediate(_managerObject);
            if (_coinModel != null) Object.DestroyImmediate(_coinModel);
            if (_otherPickup != null) Object.DestroyImmediate(_otherPickup);
            foreach (var coin in Object.FindObjectsByType<GoldPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(coin.gameObject);
        }

        [Test]
        public void GoldPool_FromModelWithoutScripts_FillsToSizeAndSpawnsCoins()
        {
            // Learning Comment: Coin model par koi script nahi; pool runtime template banata hai jo inactive hota hai.
            _coinModel = new GameObject("CoinModel");
            _managerObject = new GameObject("PickupPool");
            var manager = _managerObject.AddComponent<PickupPoolManager>();
            typeof(PickupPoolManager).GetField("_goldCoinPrefab", Private).SetValue(manager, _coinModel);
            typeof(PickupPoolManager).GetField("_prewarmPerPickupType", Private).SetValue(manager, 4);
            // Stand-ins so Awake does not build its runtime fallback spheres (renderer.material errors in EditMode).
            _otherPickup = new GameObject("OtherPickup");
            typeof(PickupPoolManager).GetField("_magnetPrefab", Private).SetValue(manager, _otherPickup);
            typeof(PickupPoolManager).GetField("_timeFreezePrefab", Private).SetValue(manager, _otherPickup);

            // EditMode does not run Awake; this is the startup path that used to loop forever.
            typeof(PickupPoolManager).GetMethod("Awake", Private).Invoke(manager, null);

            var goldPool = (Stack<GameObject>)typeof(PickupPoolManager).GetField("_goldPool", Private).GetValue(manager);
            Assert.That(goldPool.Count, Is.EqualTo(4), "Gold pool apne size tak bhare aur ruk jaye.");

            manager.SpawnGold(Vector3.zero, 10);

            Assert.That(goldPool.Count, Is.EqualTo(3));
            var spawned = Object.FindObjectsByType<GoldPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(spawned.Length, Is.EqualTo(1), "Ek active coin spawn ho.");
            Assert.That(spawned[0].Value, Is.EqualTo(10));
        }
    }
}
