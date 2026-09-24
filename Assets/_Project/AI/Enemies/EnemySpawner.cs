using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Kami #1 Fix: Ab ye spawner WaveData ke hisaab se kaam karega.
    /// Time badhne par naye dushman aur bosses aayenge.
    /// Kyunki alag-alag dushman aayenge, humein Object Pool ko Dictionary mein badalna pada.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private List<WaveData> _waves;
        [SerializeField] private GameObject _bossPrefab;
        [SerializeField] private List<GameObject> _stageEnemyPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> _stageElitePrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> _stageBossPrefabs = new List<GameObject>();
        [SerializeField] private float _spawnRadius = 20f;
        [SerializeField] private int _maxAliveEnemies = 300;
        [SerializeField] private int _prewarmPerEnemyType = 32;
        [SerializeField] private int _prewarmPerBossType = 1;

        [Header("Straggler relocation (VS rule)")]
        [Tooltip("Non-boss enemies further than this from the player are moved back onto the spawn ring ahead of them")]
        [SerializeField] private float _relocateDistance = 40f;
        [SerializeField, Min(0.05f)] private float _relocateCheckInterval = 0.5f;
        [Tooltip("Relocated enemies land within this many degrees either side of the player's heading")]
        [SerializeField, Range(0f, 180f)] private float _relocateSpreadDegrees = 60f;
        private float _nextRelocateCheck;
        private Rigidbody _playerBody;
        private readonly HashSet<EnemyController> _bossControllers = new HashSet<EnemyController>();
        
        private Transform _playerTransform;
        private float _timer;
        private WaveData _currentWave;
        private bool _bossSpawnedForCurrentWave;
        private bool _bossDefeatedForCurrentWave;

        // Har prefab ke liye alag Object Pool (Stack for O(1) performance)
        private Dictionary<GameObject, Stack<GameObject>> _enemyPools = new Dictionary<GameObject, Stack<GameObject>>();
        private Dictionary<GameObject, Stack<GameObject>> _bossPools = new Dictionary<GameObject, Stack<GameObject>>();
        private HashSet<SoulHunter.Gameplay.Combat.HealthController> _bossCallbacks = new HashSet<SoulHunter.Gameplay.Combat.HealthController>();
        private Dictionary<GameObject, Vector3> _prefabScales = new Dictionary<GameObject, Vector3>();

        private float _stageTimePassed;
        private float _nextPlayerLookupTime;
        private bool _isBossAlive;
        private GameObject _stageEnemyPrefab;
        private GameObject _stageElitePrefab;
        private GameObject _stageBossPrefab;

        private void Start()
        {
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            // Fix: Accessing Instance now automatically creates it if missing
            SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.OnStageStarted += HandleStageStarted;
            
            // Fix: Agar Manager ne Start() pehle chala diya tha toh hum event miss kar chuke honge
            HandleStageStarted(SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage);
            PrewarmPools();
        }

        private void OnDisable()
        {
            var progression = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
            if (progression != null) progression.OnStageStarted -= HandleStageStarted;
            StopAllCoroutines();
        }

        private void HandleStageStarted(int stageNum)
        {
            if (_waves == null || _waves.Count == 0) return;
            
            // Map stage 1-10 to wave array 0-9
            int waveIndex = Mathf.Clamp(stageNum - 1, 0, _waves.Count - 1);
            _currentWave = _waves[waveIndex];
            _bossSpawnedForCurrentWave = false;
            _bossDefeatedForCurrentWave = false;
            _isBossAlive = false;
            _stageTimePassed = 0f;
            _timer = 0f;
            int stageIndex = Mathf.Clamp(stageNum - 1, 0, 9);
            _stageEnemyPrefab = stageIndex < _stageEnemyPrefabs.Count && _stageEnemyPrefabs[stageIndex] != null
                ? _stageEnemyPrefabs[stageIndex] : _currentWave.EnemyPrefab;
            _stageElitePrefab = stageIndex < _stageElitePrefabs.Count ? _stageElitePrefabs[stageIndex] : null;
            _stageBossPrefab = stageIndex < _stageBossPrefabs.Count && _stageBossPrefabs[stageIndex] != null
                ? _stageBossPrefabs[stageIndex] : _bossPrefab;

            // Prepare only the active stage. Prewarming every asset for all ten
            // stages at startup would create a large low-end-device hitch.
            PrewarmActiveStagePools();
        }

        private void Update()
        {
            // Player visuals are selected/instantiated during scene startup,
            // which can occur after this manager's Start(). Reacquire cheaply
            // instead of permanently disabling all ten stage spawners.
            if (_playerTransform == null && Time.time >= _nextPlayerLookupTime)
            {
                var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
                _playerTransform = player != null ? player.transform : null;
                _nextPlayerLookupTime = Time.time + 0.25f;
            }
            if (_playerTransform == null || _currentWave == null) return;

            // Runs during boss fights too, so the swarm never piles up off-screen.
            RelocateStragglers();
            if (_isBossAlive) return;

            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            // The arena was just wiped; keep it empty until the next stage starts.
            if (progression.IsStageTransitioning) return;
            var level = progression.CurrentLevel;
            _stageTimePassed = progression.StageElapsedTime;
            float bossStartTime = level.BossStartSeconds;
            var phase = _currentWave.GetPhase(_stageTimePassed);

            if (_stageTimePassed >= bossStartTime && !_bossSpawnedForCurrentWave)
            {
                // Bosses have priority over the normal swarm cap. A full arena
                // must never prevent the stage boss from appearing.
                _bossSpawnedForCurrentWave = SpawnBoss(
                    _stageBossPrefab != null ? _stageBossPrefab : (_bossPrefab != null ? _bossPrefab : _stageEnemyPrefab));
                return;
            }

            if (EnemyController.ActiveEnemies.Count >= _maxAliveEnemies) return;

            // Normal Swarm Spawning. WaveData is authoritative for the first
            // activation time; a boss-designated wave never leaks normal mobs.
            bool waveIsActive = _stageTimePassed >= Mathf.Max(0f, _currentWave.StartTimeInSeconds);
            bool bossPhase = _currentWave.IsBossWave || phase.IsBossPhase;
            if ((!_bossSpawnedForCurrentWave || _bossDefeatedForCurrentWave) && waveIsActive && !bossPhase)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    int fiveMinutePressure = Mathf.FloorToInt(_stageTimePassed / 300f);
                    int groupSize = Mathf.Max(1, _currentWave.EnemiesPerSpawn + phase.EnemiesPerSpawnBonus + fiveMinutePressure);
                    int availableSlots = Mathf.Max(0, _maxAliveEnemies - EnemyController.ActiveEnemies.Count);
                    int spawnCount = Mathf.Min(groupSize, availableSlots);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        bool eliteOnly = level.Signature == SoulHunter.Gameplay.Data.CampaignSignature.BloodArenas;
                        bool elitePressure = _stageElitePrefab != null &&
                            (eliteOnly || (CurrentEliteChance(level, _stageTimePassed) + phase.EliteChanceBonus > Random.value));
                        SpawnEnemy(elitePressure ? _stageElitePrefab : (phase.EnemyPrefab != null ? phase.EnemyPrefab : _stageEnemyPrefab));
                    }
                    float pressure = Mathf.Max(0.25f, level.SpawnIntervalMultiplier * phase.SpawnIntervalMultiplier);
                    _timer = Mathf.Max(0.05f, _currentWave.SpawnInterval * pressure);
                }
            }
        }

        /// <summary>
        /// VS rule: enemies left far behind are not wasted. Every check, any non-boss enemy beyond
        /// _relocateDistance is moved onto the spawn ring in front of the player, keeping its health.
        /// </summary>
        private void RelocateStragglers()
        {
            if (Time.time < _nextRelocateCheck) return;
            _nextRelocateCheck = Time.time + _relocateCheckInterval;

            Vector3 playerPos = _playerTransform.position;
            float maxDistanceSqr = _relocateDistance * _relocateDistance;
            // Land inside the trigger distance, or enemies would be relocated again at once.
            float minRadius = Mathf.Min(_spawnRadius, _relocateDistance * 0.5f);
            float maxRadius = Mathf.Min(_spawnRadius + 10f, _relocateDistance * 0.9f);
            Vector3 heading = PlayerHeading();

            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null || _bossControllers.Contains(enemy)) continue;

                Vector3 offset = enemy.transform.position - playerPos;
                offset.y = 0f;
                if (offset.sqrMagnitude <= maxDistanceSqr) continue;

                enemy.transform.position = ComputeRelocationPoint(playerPos, heading, _relocateSpreadDegrees,
                    minRadius, maxRadius, Random.value, Random.value);
                if (enemy.Rigidbody != null) enemy.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        private Vector3 PlayerHeading()
        {
            if (_playerBody == null || _playerBody.transform != _playerTransform)
                _playerBody = _playerTransform.GetComponent<Rigidbody>();

            Vector3 velocity = _playerBody != null ? _playerBody.linearVelocity : Vector3.zero;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.01f) return velocity.normalized;

            // Standing still: no "ahead", so pick any direction.
            Vector2 random = Random.insideUnitCircle.normalized;
            return random.sqrMagnitude > 0f ? new Vector3(random.x, 0f, random.y) : Vector3.forward;
        }

        /// <summary>
        /// A point on the ring [minRadius, maxRadius] around the player, within spreadDegrees either side
        /// of heading. angleRoll01 and radiusRoll01 are uniform random values in [0, 1].
        /// </summary>
        public static Vector3 ComputeRelocationPoint(Vector3 playerPos, Vector3 heading, float spreadDegrees,
            float minRadius, float maxRadius, float angleRoll01, float radiusRoll01)
        {
            heading.y = 0f;
            heading = heading.sqrMagnitude > 0f ? heading.normalized : Vector3.forward;
            float angle = Mathf.Lerp(-spreadDegrees, spreadDegrees, angleRoll01);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * heading;
            float radius = Mathf.Lerp(minRadius, maxRadius, radiusRoll01);
            Vector3 point = playerPos + direction * radius;
            point.y = playerPos.y;
            return point;
        }

        private float CurrentEliteChance(SoulHunter.Gameplay.Data.CampaignLevelDefinition level, float time)
        {
            if (level.Signature == SoulHunter.Gameplay.Data.CampaignSignature.BloodArenas) return 1f;
            int stageIndex = Mathf.Clamp(level.Number - 1, 0, 9);
            float stageBias = stageIndex * 0.025f;
            return Mathf.Clamp01(0.05f + stageBias + (time / Mathf.Max(1f, level.BossStartSeconds)) * 0.20f);
        }

        private bool SpawnBoss(GameObject basePrefab)
        {
            if (basePrefab == null || _playerTransform == null) return false;
            Debug.Log("[EnemySpawner] BOSS HAS APPEARED!");
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = _playerTransform.position + new Vector3(randomDir.x * _spawnRadius, 0f, randomDir.y * _spawnRadius);
            
            // Spawn normal enemy but make it big and tanky to act as a boss
            if (!_bossPools.TryGetValue(basePrefab, out var bossPool))
            {
                bossPool = new Stack<GameObject>();
                _bossPools[basePrefab] = bossPool;
            }
            GameObject bossObj = bossPool.Count > 0
                ? bossPool.Pop()
                : Instantiate(basePrefab, spawnPos, Quaternion.identity);
            if (bossObj.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>() == null)
            {
                var autoReturn = bossObj.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = bossPool;
            }
            bossObj.transform.position = spawnPos;
            var bossController = bossObj.GetComponent<EnemyController>();
            if (bossController != null)
            {
                bossController.Target = _playerTransform;
                _bossControllers.Add(bossController); // bosses are never relocated
            }
            bossObj.SetActive(true);
            bossObj.transform.localScale = Vector3.one * 3f; // 3x Bigger
            bossObj.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier);
            ConfigureStageDamageRule(bossObj);

            // LEVEL 10 REQUIREMENT: Shadow Kael Boss Controller
            if (SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.Number == 10)
            {
                if (bossObj.GetComponent<ShadowKaelController>() == null)
                {
                    bossObj.AddComponent<ShadowKaelController>();
                }
            }
            
            var health = bossObj.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                // Stage boss EnemyData is authoritative. Fallback bosses built from
                // ordinary enemy prefabs keep the stage formula, not enemy health.
                var bossData = bossController != null && basePrefab == _stageBossPrefab ? bossController.Data : null;
                health.Initialize(bossData != null
                    ? bossData.MaxHealth
                    : 1000 * SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage);
                health.KnockbackResistance = 1f; // Immune to knockback
                
                if (!_bossCallbacks.Contains(health))
                {
                    _bossCallbacks.Add(health);
                    health.OnDied += () =>
                    {
                        _isBossAlive = false;
                        _bossDefeatedForCurrentWave = true;
                        var progression = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
                        if (progression != null) progression.ReportBossDefeated();
                    };
                }
            }

            _isBossAlive = true;
            return true;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            if (prefab == null || _playerTransform == null) return;
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(_spawnRadius, _spawnRadius + 10f);
            
            Vector3 spawnPos = _playerTransform.position + new Vector3(randomDir.x * randomDist, 0f, randomDir.y * randomDist);
            spawnPos.y = _playerTransform.position.y; 

            // Initialize the pool stack for this specific prefab if it doesn't exist
            if (!_enemyPools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                _enemyPools[prefab] = pool;
            }
            GameObject enemyToSpawn = null;
            
            if (pool.Count > 0)
            {
                enemyToSpawn = pool.Pop();
            }
            else
            {
                enemyToSpawn = Instantiate(prefab, spawnPos, Quaternion.identity);
                var autoReturn = enemyToSpawn.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = pool;
            }

            enemyToSpawn.transform.position = spawnPos;
            if (!_prefabScales.TryGetValue(prefab, out var originalScale))
            {
                originalScale = enemyToSpawn.transform.localScale;
                _prefabScales[prefab] = originalScale;
            }
            enemyToSpawn.transform.localScale = originalScale;
            var enemyController = enemyToSpawn.GetComponent<EnemyController>();
            if (enemyController != null) enemyController.Target = _playerTransform;
            enemyToSpawn.SetActive(true);
            enemyToSpawn.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier);
            ConfigureStageDamageRule(enemyToSpawn);
            
            var health = enemyToSpawn.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.ResetHealth();
            }
        }

        private void ConfigureStageDamageRule(GameObject enemyObject)
        {
            var controller = enemyObject != null ? enemyObject.GetComponent<EnemyController>() : null;
            if (controller == null) return;
            var signature = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.Signature;
            bool isSpectral = signature == CampaignSignature.SeaOfLostSouls;
            controller.ConfigureDamageVulnerability(
                isSpectral ? DamageType.Holy : DamageType.Normal,
                isSpectral ? 2f : 1f,
                isSpectral);
        }

        private void PrewarmPools()
        {
            PrewarmActiveStagePools();
        }

        private void PrewarmActiveStagePools()
        {
            if (_prewarmPerEnemyType > 0)
            {
                if (_stageEnemyPrefab != null) PrewarmPrefab(_stageEnemyPrefab, _enemyPools, _prewarmPerEnemyType);
                if (_stageElitePrefab != null) PrewarmPrefab(_stageElitePrefab, _enemyPools, _prewarmPerEnemyType);
            }

            if (_prewarmPerBossType > 0 && _stageBossPrefab != null)
            {
                PrewarmPrefab(_stageBossPrefab, _bossPools, _prewarmPerBossType);
            }
        }

        private void PrewarmPrefab(GameObject prefab, Dictionary<GameObject, Stack<GameObject>> pools, int count)
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>(count);
                pools[prefab] = pool;
            }

            while (pool.Count < count)
            {
                var instance = Instantiate(prefab, transform);
                var autoReturn = instance.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                if (autoReturn == null) autoReturn = instance.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = pool;
                instance.SetActive(false);
            }
        }
    }
}
