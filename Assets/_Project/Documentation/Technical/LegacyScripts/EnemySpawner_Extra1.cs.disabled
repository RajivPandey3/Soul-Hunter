using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;

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
        [SerializeField] private float _spawnRadius = 20f;
        
        private Transform _playerTransform;
        private float _timer;
        private WaveData _currentWave;
        private bool _bossSpawnedForCurrentWave;

        // Har prefab ke liye alag Object Pool (Stack for O(1) performance)
        private Dictionary<GameObject, Stack<GameObject>> _enemyPools = new Dictionary<GameObject, Stack<GameObject>>();
        private Dictionary<GameObject, Stack<GameObject>> _bossPools = new Dictionary<GameObject, Stack<GameObject>>();
        private HashSet<SoulHunter.Gameplay.Combat.HealthController> _bossCallbacks = new HashSet<SoulHunter.Gameplay.Combat.HealthController>();
        private Dictionary<GameObject, Vector3> _prefabScales = new Dictionary<GameObject, Vector3>();

        private float _stageTimePassed;
        private bool _isBossAlive;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            // Fix: Accessing Instance now automatically creates it if missing
            SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.OnStageStarted += HandleStageStarted;
            
            // Fix: Agar Manager ne Start() pehle chala diya tha toh hum event miss kar chuke honge
            HandleStageStarted(SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage);
        }

        private void OnDisable()
        {
            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
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
            _isBossAlive = false;
            _stageTimePassed = 0f;
        }

        private void Update()
        {
            if (_playerTransform == null || _currentWave == null) return;

            // Agar is stage ka boss zinda hai, toh mazeed dushman na nikalo (ya nikal bhi sakte ho, GDD choice)
            if (_isBossAlive) return;

            _stageTimePassed += Time.deltaTime;

            float stageDuration = 60f; // Har stage 60 seconds (1 minute) ka hoga

            // Agar 60 seconds poore ho gaye toh Boss nikalo
            if (_stageTimePassed >= stageDuration && !_bossSpawnedForCurrentWave)
            {
                SpawnBoss(_currentWave.EnemyPrefab); // Asal mein alag boss prefab hoga, par abhi current wave ka dushman bada kar denge
                _bossSpawnedForCurrentWave = true;
                return;
            }

            // Normal Swarm Spawning
            if (!_bossSpawnedForCurrentWave)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    for (int i = 0; i < _currentWave.EnemiesPerSpawn; i++)
                    {
                        SpawnEnemy(_currentWave.EnemyPrefab);
                    }
                float pressure = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.SpawnIntervalMultiplier;
                _timer = _currentWave.SpawnInterval * Mathf.Max(0.25f, pressure);
                }
            }
        }

        private void SpawnBoss(GameObject basePrefab)
        {
            Debug.Log("[EnemySpawner] BOSS HAS APPEARED!");
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = _playerTransform.position + new Vector3(randomDir.x * _spawnRadius, 0f, randomDir.y * _spawnRadius);
            
            // Spawn normal enemy but make it big and tanky to act as a boss
            if (!_bossPools.ContainsKey(basePrefab)) _bossPools[basePrefab] = new Stack<GameObject>();
            var bossPool = _bossPools[basePrefab];
            GameObject bossObj = bossPool.Count > 0
                ? bossPool.Pop()
                : Instantiate(basePrefab, spawnPos, Quaternion.identity);
            if (bossObj.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>() == null)
            {
                var autoReturn = bossObj.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = bossPool;
            }
            bossObj.transform.position = spawnPos;
            bossObj.SetActive(true);
            bossObj.transform.localScale = Vector3.one * 3f; // 3x Bigger
            bossObj.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier);
            
            var health = bossObj.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.Initialize(1000 * SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage); 
                health.KnockbackResistance = 1f; // Immune to knockback
                
                if (!_bossCallbacks.Contains(health))
                {
                    _bossCallbacks.Add(health);
                    health.OnDied += () =>
                    {
                        _isBossAlive = false;
                        SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.ReportBossDefeated();
                    };
                }
            }

            _isBossAlive = true;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(_spawnRadius, _spawnRadius + 10f);
            
            Vector3 spawnPos = _playerTransform.position + new Vector3(randomDir.x * randomDist, 0f, randomDir.y * randomDist);
            spawnPos.y = _playerTransform.position.y; 

            // Initialize the pool stack for this specific prefab if it doesn't exist
            if (!_enemyPools.ContainsKey(prefab))
            {
                _enemyPools[prefab] = new Stack<GameObject>();
            }

            Stack<GameObject> pool = _enemyPools[prefab];
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
            enemyToSpawn.SetActive(true);
            enemyToSpawn.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier);
            
            var health = enemyToSpawn.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.ResetHealth();
            }
        }
    }
}
