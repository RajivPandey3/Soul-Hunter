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
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            var stats = player != null ? player.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>() : null;
            float luck = stats != null ? Mathf.Max(1f, stats.Luck) : 1f;

            var vfxManager = SoulHunter.Gameplay.VFX.VFXPoolManager.Instance;
            if (vfxManager == null)
            {
                // Kuch showcase/level scenes mein VFX_Systems object nahi hota.
                // Blood effect kabhi silently skip na ho, isliye safe runtime fallback.
                var vfxRoot = new GameObject("VFX_Systems_Runtime");
                vfxManager = vfxRoot.AddComponent<SoulHunter.Gameplay.VFX.VFXPoolManager>();
            }
            vfxManager.PlayEnemyDeathVFX(transform.position);

            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
            {
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddKill();
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

            // 1% Chance for Magnet
            float magnetRoll = Random.Range(0f, 100f);
            if (magnetRoll <= Mathf.Min(100f, 1f * luck))
            {
                var pickupPool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
                if (pickupPool != null)
                {
                    pickupPool.SpawnMagnet(transform.position);
                    Debug.Log("[EnemyDrop] Rare Drop: Magnet!");
                }
            }

            // 1% Chance for Time Freeze
            float timeRoll = Random.Range(0f, 100f);
            if (timeRoll <= Mathf.Min(100f, 1f * luck))
            {
                var pickupPool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
                if (pickupPool != null)
                {
                    pickupPool.SpawnTimeFreeze(transform.position);
                    Debug.Log("[EnemyDrop] Rare Drop: Time Freeze (Orologion)!");
                }
            }

            // 5% chance chicken girne ka
            float roll = Random.Range(0f, 100f);
            if (roll <= Mathf.Min(100f, ChickenDropChance * luck))
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
