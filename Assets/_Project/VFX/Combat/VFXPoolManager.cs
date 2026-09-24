using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Learning Comment:
    /// Option 4 (Visual Polish): Ye VFX Pool Manager hai.
    /// Ye game me dhuaan aur khoon (particles) paida karta hai.
    /// Khaas baat: Agar aapke paas Asset nahi hai, toh ye CODE KE ZARIYE 
    /// Unity ka apna default Particle System bana dega jo 100% Commercial safe hai!
    /// </summary>
    public class VFXPoolManager : MonoBehaviour
    {
        public static VFXPoolManager Instance { get; private set; }

        [Header("Optional: Apne khud ke VFX Prefabs dalein (Nahi dalenge toh Code khud banayega)")]
        public GameObject EnemyDeathVFXPrefab;
        public GameObject ChestOpenVFXPrefab;
        
        private List<GameObject> _enemyDeathPool = new List<GameObject>();
        private List<GameObject> _chestOpenPool = new List<GameObject>();

        private static Material _sharedParticleMaterial;
        private const int MAX_POOL_CAPACITY = 25;
        private int _reuseIndex = 0;

        private static Material GetSharedMaterial()
        {
            if (_sharedParticleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                             ?? Shader.Find("Particles/Standard Unlit") 
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _sharedParticleMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }
            }
            return _sharedParticleMaterial;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Prewarm a small pool to avoid frame spikes during intense combat
                PrewarmPool(ref _enemyDeathPool, EnemyDeathVFXPrefab, Color.red, 15, 10);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void PrewarmPool(ref List<GameObject> pool, GameObject prefab, Color fallbackColor, short burstCount, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewVFX(prefab, fallbackColor, burstCount);
                obj.SetActive(false);
                pool.Add(obj);
            }
        }

        public void PlayEnemyDeathVFX(Vector3 position)
        {
            // Khoon (Red) ka effect
            GameObject vfx = GetOrCreateVFX(ref _enemyDeathPool, EnemyDeathVFXPrefab, position, Color.red, 15);
            if (vfx == null) return;
            vfx.SetActive(true);
            var particles = vfx.GetComponentInChildren<ParticleSystem>();
            if (particles != null) { particles.Clear(true); particles.Play(true); }
        }

        public void PlayChestOpenVFX(Vector3 position)
        {
            // Sunehri chamak (Yellow) ka effect
            GameObject vfx = GetOrCreateVFX(ref _chestOpenPool, ChestOpenVFXPrefab, position, Color.yellow, 50);
            if (vfx == null) return;
            vfx.SetActive(true);
            var particles = vfx.GetComponentInChildren<ParticleSystem>();
            if (particles != null) { particles.Clear(true); particles.Play(true); }
        }

        /// <summary>
        /// Learning Comment:
        /// Memory-Safe Pooling:
        /// 1. Inactive object reuse karta hai.
        /// 2. Agar koi inactive na ho aur capacity bachi ho toh naya object banata hai.
        /// 3. Agar pool full ho (25+) toh sabse purane ko foran recycle karta hai taake
        /// 80+ enemies marne par bhi memory spike na ho aur game kabbhi crash/hang na kare.
        /// </summary>
        private GameObject GetOrCreateVFX(ref List<GameObject> pool, GameObject prefab, Vector3 position, Color fallbackColor, short burstCount)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].activeInHierarchy)
                {
                    pool[i].transform.position = position;
                    return pool[i];
                }
            }

            // Pool capacity limit check
            if (pool.Count < MAX_POOL_CAPACITY)
            {
                GameObject newObj = CreateNewVFX(prefab, fallbackColor, burstCount);
                newObj.transform.position = position;
                pool.Add(newObj);
                return newObj;
            }

            // Pool full hone par oldest active object ko reuse karein
            _reuseIndex = (_reuseIndex + 1) % pool.Count;
            GameObject recycled = pool[_reuseIndex];
            if (recycled != null)
            {
                recycled.transform.position = position;
                var ps = recycled.GetComponentInChildren<ParticleSystem>();
                if (ps != null) ps.Clear(true);
                return recycled;
            }

            return null;
        }

        private GameObject CreateNewVFX(GameObject prefab, Color fallbackColor, short burstCount)
        {
            GameObject newObj = null;
            if (prefab != null)
            {
                newObj = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                if (newObj.GetComponent<VFXAutoDisable>() == null)
                    newObj.AddComponent<VFXAutoDisable>();
            }
            else
            {
                newObj = new GameObject("Procedural_Particle_VFX");
                newObj.transform.position = transform.position;
                newObj.transform.SetParent(transform);

                var ps = newObj.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = 0.5f;
                main.startLifetime = 0.5f;
                main.startSpeed = 8f;
                main.startSize = 0.3f;
                main.startColor = fallbackColor;
                main.loop = false;
                main.playOnAwake = false;

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burstCount) });

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;

                // Shared Material use karein taaki lakhoon unmanaged material copies na banein
                var renderer = newObj.GetComponent<ParticleSystemRenderer>();
                var sharedMat = GetSharedMaterial();
                if (sharedMat != null)
                {
                    renderer.sharedMaterial = sharedMat;
                }

                newObj.AddComponent<VFXAutoDisable>();
            }

            return newObj;
        }
    }

    /// <summary>
    /// Ye choti script effect khatam hone par usko automatic chupa (disable) deti hai.
    /// </summary>
    public class VFXAutoDisable : MonoBehaviour
    {
        private ParticleSystem _ps;
        private void Awake() { _ps = GetComponentInChildren<ParticleSystem>(); }
        private void Update() 
        { 
            if (_ps != null && !_ps.IsAlive(true)) 
            {
                gameObject.SetActive(false); 
            }
        }
    }
}
