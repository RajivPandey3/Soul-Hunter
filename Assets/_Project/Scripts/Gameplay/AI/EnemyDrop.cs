using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors Drop System: Dushman ke marne par gem girta hai.
    /// Ye script HealthController ke OnDied event ko sunti hai.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class EnemyDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _xpGemPrefab;
        [SerializeField] private GameObject _healthChickenPrefab;
        [Tooltip("Chicken drop hone ka chance (0 se 100)")]
        public float ChickenDropChance = 5f;
        public bool DropsChest = false;

        private HealthController _health;

        private void Awake()
        {
            _health = GetComponent<HealthController>();
        }

        private void OnEnable()
        {
            _health.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (SoulHunter.Gameplay.VFX.VFXPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Instance.PlayEnemyDeathVFX(transform.position);
            }

            if (DropsChest)
            {
                if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                {
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnChest(transform.position);
                    Debug.Log("Boss killed! Chest dropped!");
                }
                return;
            }

            // 5% chance chicken girne ka
            float roll = Random.Range(0f, 100f);
            if (roll <= ChickenDropChance)
            {
                if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                {
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnChicken(transform.position);
                    Debug.Log("Rare Drop: Floor Chicken!");
                }
            }
            else
            {
                if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                {
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position);
                }
            }
        }
    }
}
