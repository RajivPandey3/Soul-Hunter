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
        [Tooltip("Gold coin drop hone ka chance (0 se 100), Luck se badhta hai")]
        public float GoldDropChance = 2f;
        public int GoldValue = 1;

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
            // Learning Comment:
            // Performance Optimization: Dushmano ki bheed (horde) marne par FindFirstObjectByType
            // call karne se game lag/freeze ho jati thi. Ab hum O(1) PlayerController.Instance use karte hain.
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var stats = player != null ? player.Stats : null;
            float luck = stats != null ? Mathf.Max(1f, stats.Luck) : 1f;

            var vfxManager = SoulHunter.Gameplay.VFX.VFXPoolManager.Instance;
            if (vfxManager != null)
            {
                vfxManager.PlayEnemyDeathVFX(transform.position);
            }

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

            // Gold coin (independent roll, Luck se badhta hai; value par Greed pickup ke waqt lagta hai)
            float goldRoll = Random.Range(0f, 100f);
            if (goldRoll <= Mathf.Min(100f, GoldDropChance * luck))
            {
                var pickupPool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
                if (pickupPool != null)
                {
                    // Nudge the coin aside so it does not sit exactly on the gem.
                    pickupPool.SpawnGold(transform.position + new Vector3(0.6f, 0f, 0f), GoldValue);
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
