using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Hybrid System / 100% Accuracy Rule: 
    /// XP Gems aur Health Pickups ko Instantiate/Destroy karne ki jagah hum Object Pool use karenge.
    /// Isse Garbage Collector nahi chalega aur game lag-free (60+ FPS) chalega.
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour
    {
        public Stack<GameObject> TargetPool;
        private bool _isReturned = false;

        public void ResetForReuse()
        {
            _isReturned = false;
        }

        private void OnEnable()
        {
            _isReturned = false;
        }

        private void OnDisable()
        {
            // Learning Comment:
            // Guard against duplicate stack entries when SetActive(false) is called multiple times.
            if (TargetPool != null && !_isReturned)
            {
                _isReturned = true;
                TargetPool.Push(gameObject);
            }
        }
    }

    public class PickupPoolManager : MonoBehaviour
    {
        public static PickupPoolManager Instance { get; private set; }

        [SerializeField] private GameObject _xpGemPrefab;
        [SerializeField] private GameObject _healthChickenPrefab;
        [SerializeField] private GameObject _chestPrefab;
        [SerializeField] private GameObject _magnetPrefab;
        [SerializeField] private GameObject _timeFreezePrefab;
        [Tooltip("Coin model; GoldPickup and a trigger collider are added at runtime if missing")]
        [SerializeField] private GameObject _goldCoinPrefab;
        [SerializeField, Min(0)] private int _prewarmPerPickupType = 32;

        private Stack<GameObject> _gemPool = new Stack<GameObject>();
        private Stack<GameObject> _chickenPool = new Stack<GameObject>();
        private Stack<GameObject> _chestPool = new Stack<GameObject>();
        private Stack<GameObject> _magnetPool = new Stack<GameObject>();
        private Stack<GameObject> _timeFreezePool = new Stack<GameObject>();
        private Stack<GameObject> _goldPool = new Stack<GameObject>();

        [SerializeField] private SoulHunter.Gameplay.Core.ChestLogicController _chestLogicController;
        [SerializeField] private SoulHunter.Gameplay.UI.ChestUI _chestUI;
        [SerializeField] private SoulHunter.Gameplay.Player.PlayerController _playerController;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Keep missing content wiring visible during integration instead of silently
            // dropping a rare reward. The pool remains safe: an unassigned prefab simply
            // produces no pickup until the scene installer supplies it.
            // Rare rewards must never silently disappear. If a scene has not
            // wired the optional prefab yet, create the same safe fallback now
            // so the pool is complete before the first wave starts.
            if (_magnetPrefab == null)
                _magnetPrefab = CreateFallbackPickup<MagnetPickup>("Magnet_Pickup_Fallback", Color.cyan);
            if (_timeFreezePrefab == null)
                _timeFreezePrefab = CreateFallbackPickup<TimeFreezePickup>("Orologion_Pickup_Fallback", Color.blue);

            _goldCoinPrefab = PrepareGoldTemplate(_goldCoinPrefab);

            // Prewarm the common reward path so the first enemy wave does not
            // trigger a burst of Instantiate/GC work on low-power devices.
            PrewarmPool(_gemPool, _xpGemPrefab, _prewarmPerPickupType);
            PrewarmPool(_chickenPool, _healthChickenPrefab, _prewarmPerPickupType);
            PrewarmPool(_chestPool, _chestPrefab, Mathf.Min(4, _prewarmPerPickupType));
            PrewarmPool(_magnetPool, _magnetPrefab, Mathf.Min(4, _prewarmPerPickupType));
            PrewarmPool(_timeFreezePool, _timeFreezePrefab, Mathf.Min(4, _prewarmPerPickupType));
            PrewarmPool(_goldPool, _goldCoinPrefab, Mathf.Min(8, _prewarmPerPickupType));
        }

        public const int DefaultGemValue = 10;

        public void SpawnGem(Vector3 position) => SpawnGem(position, DefaultGemValue);

        /// <summary>A Soul Gem worth <paramref name="value"/> XP; its tier model (cyan / violet / crimson) follows the value.</summary>
        /// <summary>VS wiki (Experience_Gem): with more than 400 gems on the ground, new XP goes into one red gem instead.</summary>
        public const int MaxGemsOnGround = 400;
        private XPGem _mergeGem;

        public void SpawnGem(Vector3 position, int value)
        {
            // VS gem cap: past 400 gems, add the XP to a single gem rather than spawning more (thousands of uncollected
            // gems dragged a 30-minute editor run down to ~20 FPS).
            if (XPGem.ActiveGems.Count >= MaxGemsOnGround)
            {
                if (_mergeGem != null && _mergeGem.isActiveAndEnabled && !_mergeGem.IsMagnetized)
                {
                    _mergeGem.XPValue += Mathf.Max(1, value);
                    var mergedTier = _mergeGem.GetComponent<TieredVisual>();
                    if (mergedTier != null) mergedTier.SetValue(_mergeGem.XPValue);
                    return;
                }
                _mergeGem = null;
            }
            var gem = SpawnFromPool(_gemPool, _xpGemPrefab, position);
            if (gem != null && XPGem.ActiveGems.Count > MaxGemsOnGround) _mergeGem = gem.GetComponent<XPGem>();
            if (gem == null) return;
            var xp = gem.GetComponent<XPGem>();
            if (xp != null) xp.XPValue = Mathf.Max(1, value);
            var tier = gem.GetComponent<TieredVisual>();
            if (tier != null) tier.SetValue(value);
        }

        public GameObject SpawnChicken(Vector3 position)
        {
            return SpawnFromPool(_chickenPool, _healthChickenPrefab, position);
        }

        public GameObject SpawnChest(Vector3 position)
        {
            return SpawnFromPool(_chestPool, _chestPrefab, position);
        }

        public GameObject SpawnMagnet(Vector3 position)
        {
            return SpawnFromPool(_magnetPool, _magnetPrefab, position);
        }

        public GameObject SpawnTimeFreeze(Vector3 position)
        {
            return SpawnFromPool(_timeFreezePool, _timeFreezePrefab, position);
        }

        public GoldPickup SpawnGold(Vector3 position, int value)
        {
            var coin = SpawnFromPool(_goldPool, _goldCoinPrefab, position);
            var gold = coin != null ? coin.GetComponent<GoldPickup>() : null;
            if (gold != null) gold.Value = value;
            var tier = coin != null ? coin.GetComponent<TieredVisual>() : null;
            if (tier != null) tier.SetValue(value); // bronze / silver / gold
            return gold;
        }

        /// <summary>
        /// The coin prefab is a visual model. Build an inactive runtime template with a trigger
        /// collider and GoldPickup instead of modifying the prefab asset.
        /// </summary>
        private GameObject PrepareGoldTemplate(GameObject coinPrefab)
        {
            if (coinPrefab != null && coinPrefab.GetComponent<GoldPickup>() != null) return coinPrefab;

            GameObject template;
            if (coinPrefab != null)
            {
                template = Instantiate(coinPrefab, transform);
                template.name = coinPrefab.name + "_Pickup";
            }
            else
            {
                template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                template.name = "Gold_Pickup_Fallback";
                template.transform.SetParent(transform);
                template.transform.localScale = Vector3.one * 0.4f;
                var renderer = template.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(1f, 0.8f, 0.1f);
            }
            template.SetActive(false);

            var collider = template.GetComponent<Collider>();
            if (collider == null) collider = template.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            template.AddComponent<GoldPickup>();
            return template;
        }

        private GameObject CreateFallbackPickup<T>(string objectName, Color color) where T : Pickup
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = objectName;
            fallback.transform.SetParent(transform);
            fallback.transform.localScale = Vector3.one * 0.6f;
            var renderer = fallback.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            var collider = fallback.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            fallback.AddComponent<T>();
            return fallback;
        }

        private GameObject SpawnFromPool(Stack<GameObject> pool, GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;

            GameObject objToSpawn = null;
            if (pool.Count > 0)
            {
                objToSpawn = pool.Pop();
            }
            else
            {
                objToSpawn = Instantiate(prefab);
                SoulHunter.Gameplay.Combat.WeaponPoolManager.NoShadows(objToSpawn);
                var autoReturn = objToSpawn.AddComponent<AutoReturnToPool>();
                autoReturn.TargetPool = pool;
            }

            objToSpawn.transform.position = position;
            objToSpawn.SetActive(true);
            
            // Reset state if it has XPGem or HealthPickup script
            var gem = objToSpawn.GetComponent<XPGem>();
            if (gem != null) gem.ResetPickup();
            
            var chicken = objToSpawn.GetComponent<HealthPickup>();
            if (chicken != null)
            {
                chicken.ResetPickup();
                chicken.Initialize(_playerController != null ? _playerController.transform : null);
            }
            
            var chest = objToSpawn.GetComponent<ChestPickup>();
            if (chest != null)
            {
                chest.ResetPickup();
                chest.Initialize(_chestLogicController, _chestUI);
            }

            var pickup = objToSpawn.GetComponent<Pickup>();
            if (pickup != null) pickup.ResetPickup();
            return objToSpawn;
        }

        private void PrewarmPool(Stack<GameObject> pool, GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            // Bounded, so a pool that fails to fill can never spin forever.
            for (int missing = count - pool.Count; missing > 0; missing--)
            {
                var instance = Instantiate(prefab, transform);
                SoulHunter.Gameplay.Combat.WeaponPoolManager.NoShadows(instance);
                var autoReturn = instance.GetComponent<AutoReturnToPool>();
                if (autoReturn == null) autoReturn = instance.AddComponent<AutoReturnToPool>();
                autoReturn.TargetPool = pool;
                // Deactivating fires OnDisable, which returns the object to the pool.
                // Copies of an inactive template (the gold coin) never fire it, so push them directly.
                if (instance.activeSelf) instance.SetActive(false);
                else pool.Push(instance);
            }
        }
    }
}
