using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Destructible Props (Torches/Mashaal).
    /// HealthController use karke inhe torra ja sakta hai. 
    /// Totne par health (chicken) ya gold (gems) nikalta hai.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class DestructibleProp : MonoBehaviour
    {
        private HealthController _health;

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            if (_health != null)
            {
                _health.OnDied += OnPropDestroyed;
                // Props 1 hit mein marte hain, aur unhe knockback nahi parta
                _health.Initialize(10); 
                _health.KnockbackResistance = 1f; 
            }
        }

        private void OnPropDestroyed()
        {
            if (PickupPoolManager.Instance != null)
            {
                // 15% chance for Health Drop, 85% for big Gold/Gem drop
                if (Random.value < 0.15f)
                {
                    PickupPoolManager.Instance.SpawnChicken(transform.position);
                }
                else
                {
                    PickupPoolManager.Instance.SpawnGem(transform.position); 
                }
            }
            
            // VFX
            if (SoulHunter.Gameplay.VFX.VFXPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Instance.PlayChestOpenVFX(transform.position); 
            }
            
            gameObject.SetActive(false);
        }
    }
}
