using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>Deactivates a pooled transient object after its flight/effect lifetime.</summary>
    public sealed class PooledLifetime : MonoBehaviour
    {
        private Coroutine _routine;

        public void Arm(float seconds)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ReturnAfter(seconds));
        }

        private System.Collections.IEnumerator ReturnAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            gameObject.SetActive(false);
            _routine = null;
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }

    /// <summary>
    /// Learning Comment:
    /// Hybrid System / 100% Accuracy Rule: 
    /// Bullets aur doosre projectiles ko baar-baar banan/destroy karne se lag hota hai.
    /// Isliye hum unhe yahan se pool karte hain. Scene mein ek hi 'WeaponPoolManager' hoga.
    /// </summary>
    public class WeaponPoolManager : MonoBehaviour
    {
        public static WeaponPoolManager Instance { get; private set; }

        // 100% VS Quality: O(1) Object Pooling
        private Dictionary<GameObject, Stack<GameObject>> _pools = new Dictionary<GameObject, Stack<GameObject>>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Stack<GameObject>();
            }

            Stack<GameObject> pool = _pools[prefab];
            GameObject objToSpawn = null;

            // A pooled object can be destroyed while it waits: Bloody Tear parents its whips to itself, so they die with
            // the weapon (e.g. when a union replaces it). Skip those instead of returning a destroyed object (found by
            // EvolvedWeaponSweepPlayModeTests, 2026-09-28).
            while (pool.Count > 0 && objToSpawn == null)
            {
                objToSpawn = pool.Pop();
            }
            if (objToSpawn == null)
            {
                objToSpawn = Instantiate(prefab);
                NoShadows(objToSpawn);
                var autoReturn = objToSpawn.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = pool;
                objToSpawn.AddComponent<PooledLifetime>();
            }

            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            var body = objToSpawn.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            objToSpawn.SetActive(true);

            return objToSpawn;
        }

        // Purani bullets ke liye compatibility function
        public void SpawnProjectile(GameObject bulletPrefab, Vector3 position, Quaternion rotation)
        {
            GetFromPool(bulletPrefab, position, rotation);
        }
    
        /// <summary>
        /// Pooled projectiles and pickups do not cast shadows: hundreds of them at once cost too much on the low-end
        /// reference GPU (UHD 620). The hero, bosses and stage obstacles cast them.
        /// </summary>
        public static void NoShadows(GameObject instance)
        {
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
