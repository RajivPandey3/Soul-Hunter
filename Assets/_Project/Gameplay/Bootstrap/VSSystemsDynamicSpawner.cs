using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.Bootstrap
{
    /// <summary>
    /// Learning Comment:
    /// Strict Rule Book Enforcement: Scene mein sirf 4 root objects ho sakte hain.
    /// Ye script [BOOTSTRAP] par lagti hai aur runtime par saare VS systems (Object Pools, Spawners) ko 
    /// memory mein dynamically paida karke [BOOTSTRAP] ka child bana deti hai, taake scene hamesha clean rahay.
    /// </summary>
    public class VSSystemsDynamicSpawner : MonoBehaviour
    {
        private void Awake()
        {
            SpawnSystems();
        }

        private void SpawnSystems()
        {
            Debug.Log("[VSSystemsDynamicSpawner] Dynamically spawning VS Systems to keep Scene Root clean!");


            if (WeaponPoolManager.Instance == null)
            {
                GameObject weaponPool = new GameObject("WeaponPoolManager");
                weaponPool.transform.SetParent(this.transform);
                weaponPool.AddComponent<WeaponPoolManager>();
            }
        }
    }
}
