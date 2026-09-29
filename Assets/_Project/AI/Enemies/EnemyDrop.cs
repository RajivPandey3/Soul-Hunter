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
        [Tooltip("XP in the dropped Soul Gem; its colour and size follow it")]
        public int GemValue = 10;

        private HealthController _health;
        private EnemyController _enemy;
        private bool _dropsChestThisLife;
        private bool _erasedThisLife;
        private bool _arcanaThisLife;

        /// <summary>VS Arcana mode: this life's chest is an Arcana chest (11:00 and 21:00). Cleared on respawn.</summary>
        public void CarryArcanaThisLife() => _arcanaThisLife = true;
        public bool CarriesArcana => _arcanaThisLife;

        /// <summary>
        /// VS Pentagram: "if a boss that would have dropped [a chest] is erased, the Treasure Chest is then dropped by a
        /// different enemy". Chests of erased carriers wait here for the next enemy the spawner sends.
        /// </summary>
        public static int PendingChests;

        /// <summary>Makes this enemy drop a chest the next time it dies (timed chest elites). Cleared when it respawns.</summary>
        public void DropChestThisLife() => _dropsChestThisLife = true;

        /// <summary>VS Pentagram: this life ends with no drops (a chest it carried passes to another enemy). Cleared on respawn.</summary>
        public void EraseDropsThisLife() => _erasedThisLife = true;

        /// <summary>True when this enemy drops a chest on death (bosses, and chest elites for this life).</summary>
        public bool WillDropChest => DropsChest || _dropsChestThisLife;

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            _enemy = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            _dropsChestThisLife = false;
            _erasedThisLife = false;
            _arcanaThisLife = false;
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

            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up * 0.8f);
            // Ghosts (Level 7) have no body to bleed; everything else leaves blood unless Disable Blood is on.
            if (_enemy == null || !_enemy.IsSpectral) SoulHunter.Gameplay.VFX.VFXPoolManager.PlayBlood(transform.position);

            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
            {
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddKill();
            }

            if (_erasedThisLife)
            {
                if (WillDropChest) PendingChests++;
                return;
            }

            // VS Gold Fever (Gilded Charm): while it runs, each kill has a 75% chance to drop a fever coin.
            SoulHunter.Gameplay.Pickups.GoldFever.OnEnemyKilled(transform.position);

            if (DropsChest || _dropsChestThisLife)
            {
                if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                {
                    var chest = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnChest(transform.position);
                    var chestPickup = chest != null ? chest.GetComponent<SoulHunter.Gameplay.Pickups.ChestPickup>() : null;
                    var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
                    if (chestPickup != null && progression != null)
                        chestPickup.CanEvolve = SoulHunter.Gameplay.Core.ChestLogicController.CanEvolveAt(progression.StageElapsedTime);
                    if (chestPickup != null) chestPickup.IsArcana = _arcanaThisLife;
                    // Owner's story (2026-09-27): Kael harvests souls, so a boss or chest elite also leaves its big
                    // soul gem (EnemyData.ExperienceValue: 50 elite, 250 boss), set a little aside from the chest.
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position + new Vector3(-1.2f, 0f, 0f), GemValue);
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
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position, GemValue);
                }
            }
        }
    }
}
