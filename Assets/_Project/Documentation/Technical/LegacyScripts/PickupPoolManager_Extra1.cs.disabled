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
        private void OnDisable()
        {
            if (TargetPool != null) TargetPool.Push(gameObject);
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

        private Stack<GameObject> _gemPool = new Stack<GameObject>();
        private Stack<GameObject> _chickenPool = new Stack<GameObject>();
        private Stack<GameObject> _chestPool = new Stack<GameObject>();
        private Stack<GameObject> _magnetPool = new Stack<GameObject>();
        private Stack<GameObject> _timeFreezePool = new Stack<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Keep missing content wiring visible during integration instead of silently
            // dropping a rare reward. The pool remains safe: an unassigned prefab simply
            // produces no pickup until the scene installer supplies it.
            if (_magnetPrefab == null)
                Debug.LogWarning("[PickupPoolManager] Magnet pickup prefab is not assigned; magnet drops are disabled.", this);
            if (_timeFreezePrefab == null)
                Debug.LogWarning("[PickupPoolManager] Time-freeze pickup prefab is not assigned; Orologion drops are disabled.", this);
        }

        public void SpawnGem(Vector3 position)
        {
            SpawnFromPool(_gemPool, _xpGemPrefab, position);
        }

        public void SpawnChicken(Vector3 position)
        {
            SpawnFromPool(_chickenPool, _healthChickenPrefab, position);
        }

        public void SpawnChest(Vector3 position)
        {
            SpawnFromPool(_chestPool, _chestPrefab, position);
        }

        public void SpawnMagnet(Vector3 position)
        {
            if (_magnetPrefab == null) _magnetPrefab = CreateFallbackPickup<MagnetPickup>("Magnet_Pickup_Fallback", Color.cyan);
            SpawnFromPool(_magnetPool, _magnetPrefab, position);
        }

        public void SpawnTimeFreeze(Vector3 position)
        {
            if (_timeFreezePrefab == null) _timeFreezePrefab = CreateFallbackPickup<TimeFreezePickup>("Orologion_Pickup_Fallback", Color.blue);
            SpawnFromPool(_timeFreezePool, _timeFreezePrefab, position);
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

        private void SpawnFromPool(Stack<GameObject> pool, GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;

            GameObject objToSpawn = null;
            if (pool.Count > 0)
            {
                objToSpawn = pool.Pop();
            }
            else
            {
                objToSpawn = Instantiate(prefab);
                var autoReturn = objToSpawn.AddComponent<AutoReturnToPool>();
                autoReturn.TargetPool = pool;
            }

            objToSpawn.transform.position = position;
            objToSpawn.SetActive(true);
            
            // Reset state if it has XPGem or HealthPickup script
            var gem = objToSpawn.GetComponent<XPGem>();
            if (gem != null) gem.ResetPickup();
            
            var chicken = objToSpawn.GetComponent<HealthPickup>();
            if (chicken != null) chicken.ResetPickup();
            
            var chest = objToSpawn.GetComponent<ChestPickup>();
            if (chest != null) chest.ResetPickup();

            var pickup = objToSpawn.GetComponent<Pickup>();
            if (pickup != null) pickup.ResetPickup();
        }
    }
}
