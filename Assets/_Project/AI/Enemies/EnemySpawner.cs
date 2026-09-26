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
        [Tooltip("Most enemies spawned in one frame while refilling the wave's enemy minimum")]
        [SerializeField, Min(1)] private int _maxMinimumTopUpPerFrame = 8;

        [Header("Chest elites (VS rule)")]
        [Tooltip("A chest-carrying elite arrives every this many seconds of stage time, before the boss. Provisional.")]
        [SerializeField, Min(0f)] private float _chestEliteIntervalSeconds = 300f;
        [Tooltip("Chest elite health = its EnemyData.MaxHealth x this (then Curse). Provisional.")]
        [SerializeField, Min(1f)] private float _chestEliteHealthMultiplier = 5f;
        [SerializeField, Min(1f)] private float _chestEliteSizeMultiplier = 1.5f;
        private int _chestElitesSpawned;
        private System.Collections.Generic.IReadOnlyList<StageEventEntry> _stageEvents;
        private int _nextStageEvent;
        private readonly List<(Vector3 position, Vector3 direction)> _formation = new List<(Vector3 position, Vector3 direction)>();

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
            StopAllCoroutines();
        }

        // Stage tracking lives as long as the spawner: a disabled spawner stops spawning (no Update) but must not keep
        // the previous stage's enemies. It used to unsubscribe in OnDisable and never subscribe again (only Start does).
        private void OnDestroy()
        {
            var progression = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
            if (progression != null) progression.OnStageStarted -= HandleStageStarted;
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
            _chestElitesSpawned = 0;
            _stageEvents = _currentWave.GetEvents();
            _nextStageEvent = 0;
            // A corridor stage (the Ruined Castle) is walled along world X around where the player stands now.
            StageCorridor.Begin(WaveTables.CorridorHalfWidth(stageNum), _playerTransform != null ? _playerTransform.position.z : 0f);
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

            // Like the boss, a chest elite is never blocked by a full arena.
            if (_chestElitesSpawned < ChestElitesDue(_stageTimePassed, _chestEliteIntervalSeconds, bossStartTime))
            {
                _chestElitesSpawned++;
                SpawnChestElite();
            }

            // VS stage events: scripted formations on the stage clock, before the boss.
            while (_stageEvents != null && _nextStageEvent < _stageEvents.Count &&
                   _stageEvents[_nextStageEvent].TimeSeconds <= _stageTimePassed)
            {
                var stageEvent = _stageEvents[_nextStageEvent++];
                if (stageEvent.TimeSeconds < bossStartTime) RunStageEvent(stageEvent, level, phase);
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
                    // A VS minute table sets the pace itself; the automatic phases add a group member every 5 minutes.
                    int fiveMinutePressure = _currentWave.HasMinuteTable ? 0 : Mathf.FloorToInt(_stageTimePassed / 300f);
                    int groupSize = Mathf.Max(1, _currentWave.EnemiesPerSpawn + phase.EnemiesPerSpawnBonus + fiveMinutePressure);
                    int availableSlots = Mathf.Max(0, _maxAliveEnemies - EnemyController.ActiveEnemies.Count);
                    int spawnCount = Mathf.Min(groupSize, availableSlots);
                    for (int i = 0; i < spawnCount; i++) SpawnEnemy(PickSwarmPrefab(level, phase));
                    float pressure = Mathf.Max(0.25f, level.SpawnIntervalMultiplier * phase.SpawnIntervalMultiplier);
                    // VS rule: Curse makes enemies arrive more often.
                    _timer = Mathf.Max(0.05f, _currentWave.SpawnInterval * pressure / CurrentCurse());
                }

                // VS rule: each wave keeps an enemy minimum. Killing faster than the spawn
                // interval does not empty the screen; the horde is refilled straight away.
                int missing = Mathf.Min(phase.MinimumEnemies, _maxAliveEnemies) - EnemyController.ActiveEnemies.Count;
                for (int i = 0; i < Mathf.Min(missing, _maxMinimumTopUpPerFrame); i++) SpawnEnemy(PickSwarmPrefab(level, phase));
            }
        }

        private GameObject PickSwarmPrefab(SoulHunter.Gameplay.Data.CampaignLevelDefinition level, WavePhase phase)
        {
            bool eliteOnly = level.Signature == SoulHunter.Gameplay.Data.CampaignSignature.BloodArenas;
            bool elitePressure = _stageElitePrefab != null &&
                (eliteOnly || (CurrentEliteChance(level, _stageTimePassed) + phase.EliteChanceBonus > Random.value));
            return elitePressure ? _stageElitePrefab : (phase.EnemyPrefab != null ? phase.EnemyPrefab : _stageEnemyPrefab);
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
                // Event fliers follow their own path and leave on their own.
                if (enemy == null || _bossControllers.Contains(enemy) || enemy.IsFlying) continue;

                Vector3 offset = enemy.transform.position - playerPos;
                offset.y = 0f;
                if (offset.sqrMagnitude <= maxDistanceSqr) continue;

                // Stragglers always re-enter ahead of the player, also on side-rule stages: those stages are not walled
                // corridors (VS's Inlaid Library is), so side-only re-entry let a player running up or down leave the
                // whole horde behind (editor play: 9 kills in 3 minutes on the Ruined Castle).
                enemy.transform.position = StageCorridor.Clamp(ComputeRelocationPoint(playerPos, heading, _relocateSpreadDegrees,
                    minRadius, maxRadius, Random.value, Random.value));
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
            var bossAudio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (bossAudio != null) bossAudio.PlaySFX(bossAudio.BossWarningSound, 0.9f);
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            // In a corridor the boss walks in from one end, like everything else.
            if (StageCorridor.Active) randomDir = new Vector2(randomDir.x >= 0f ? 1f : -1f, 0f);
            Vector3 spawnPos = StageCorridor.Clamp(_playerTransform.position + new Vector3(randomDir.x * _spawnRadius, 0f, randomDir.y * _spawnRadius));
            
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
                bossController.SetCastsShadows(true);
            }
            bossObj.SetActive(true);
            bossObj.transform.localScale = Vector3.one * 3f; // 3x Bigger
            float curse = CurrentCurse();
            bossObj.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier * curse);
            ConfigureStageDamageRule(bossObj);

            // LEVEL 10 REQUIREMENT: Shadow Kael Boss Controller
            if (SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.Number == 10)
            {
                var shadowKael = bossObj.GetComponent<ShadowKaelController>();
                if (shadowKael == null) shadowKael = bossObj.AddComponent<ShadowKaelController>();
                // Encounter start: the loadout is captured now, not a frame later.
                shadowKael.CaptureFrom(_playerTransform != null ? _playerTransform.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() : null);
            }
            
            var health = bossObj.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                // Stage boss EnemyData is authoritative. Fallback bosses built from
                // ordinary enemy prefabs keep the stage formula, not enemy health.
                var bossData = bossController != null && basePrefab == _stageBossPrefab ? bossController.Data : null;
                // VS rule: boss health scales with the player's level, so a strong build still gets a fight.
                // Owner's story (2026-09-27): each stage boss is that stage's guardian, so it is never weaker than
                // the stage's chest elites; the per-level health also rises with the story (50 x stage).
                int bossHealth = bossData != null
                    ? StoryBossHealth(bossData.HealthForPlayerLevel(CurrentPlayerLevel()), ChestEliteBaseHealth())
                    : 1000 * SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage;
                health.Initialize(ScaleHealthByCurse(bossHealth, curse));
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

        /// <summary>
        /// A point on the spawn ring, from the stage's sides (Part 4: e.g. the Ruined Castle mostly left/right), and never
        /// inside a stage obstacle (VS: solid structures block spawns from their direction). Falls back to the last try.
        /// </summary>
        private Vector3 PickSpawnPosition()
        {
            var side = _currentWave != null ? _currentWave.SpawnSide : SpawnSide.AllSides;
            float bias = _currentWave != null ? _currentWave.SideBias : 0f;
            Vector3 spawnPos = _playerTransform.position;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 direction = WaveTables.SpawnDirection(side, bias, Random.value, Random.value, Random.value);
                float distance = Random.Range(_spawnRadius, _spawnRadius + 10f);
                spawnPos = _playerTransform.position + direction * distance;
                spawnPos.y = _playerTransform.position.y;
                spawnPos = StageCorridor.Clamp(spawnPos); // a corridor keeps its enemies between the walls
                if (SoulHunter.Gameplay.Pickups.GroundCheck.IsOpen(spawnPos, 0.6f)) break;
            }
            return spawnPos;
        }

        private GameObject SpawnEnemy(GameObject prefab)
        {
            if (prefab == null || _playerTransform == null) return null;
            Vector3 spawnPos = PickSpawnPosition();

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
                // A pooled enemy destroyed by something else (scene cleanup, a test) must not be handed out again.
                while (enemyToSpawn == null && pool.Count > 0) enemyToSpawn = pool.Pop();
            }
            if (enemyToSpawn == null)
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
            float curse = CurrentCurse();
            enemyToSpawn.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier * curse);
            ConfigureStageDamageRule(enemyToSpawn);
            // VS movement skill: Floaty enemies weave, Fixed Direction ones take their line toward the player now.
            if (enemyController != null) enemyController.BeginMovement();
            
            var health = enemyToSpawn.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                // VS rule: Curse makes enemies tougher. Always scale from the EnemyData value so
                // pooled enemies never compound it; enemies without data keep their own max health.
                var data = enemyController != null ? enemyController.Data : null;
                if (data != null) health.Initialize(ScaleHealthByCurse(data.MaxHealth, curse));
                else health.ResetHealth();
            }
            return enemyToSpawn;
        }

        private SoulHunter.Gameplay.Player.PlayerStats _playerStats;

        /// <summary>The player's Curse (1 = none). VS: Curse raises enemy health, speed and spawn rate.</summary>
        private float CurrentCurse()
        {
            if (_playerTransform == null) return 1f;
            if (_playerStats == null || _playerStats.transform != _playerTransform)
                _playerStats = _playerTransform.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>();
            return _playerStats != null ? Mathf.Max(0.1f, _playerStats.Curse) : 1f;
        }

        /// <summary>
        /// VS pressure plates start a map event on demand: this stage's formation of the given type, with this
        /// stage's enemies, around the player. Returns the spawned enemies (empty during the boss or a transition).
        /// </summary>
        /// <summary>True for a stage boss spawned by this spawner (QA tools and the autoplay bot).</summary>
        public bool IsBoss(EnemyController enemy) => enemy != null && _bossControllers.Contains(enemy);

        public List<EnemyController> TriggerStageEvent(StageEventType type, int count)
        {
            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            if (progression == null || progression.IsStageTransitioning || _isBossAlive || _currentWave == null)
                return new List<EnemyController>();
            var phase = _currentWave.GetPhase(progression.StageElapsedTime);
            return RunStageEvent(new StageEventEntry { Type = type, Count = Mathf.Max(1, count) }, progression.CurrentLevel, phase);
        }

        /// <summary>
        /// Spawns one stage event's formation around the player and sends every member along its
        /// fixed path. Returns the spawned enemies. Not blocked by the enemy cap (like chest elites).
        /// </summary>
        private List<EnemyController> RunStageEvent(StageEventEntry stageEvent, CampaignLevelDefinition level, WavePhase phase)
        {
            var spawned = new List<EnemyController>();
            if (_playerTransform == null || stageEvent == null) return spawned;

            // The formation travels away from the side it comes from (Part 4: a stage's sides, e.g. castle walls
            // sweep in from the left or right).
            var side = _currentWave != null ? _currentWave.SpawnSide : SpawnSide.AllSides;
            var direction = -WaveTables.SpawnDirection(side, 1f, 0f, Random.value, Random.value);
            BuildFormation(stageEvent.Type, stageEvent.Count, _playerTransform.position, direction, _spawnRadius, _formation);
            foreach (var (position, flyDirection) in _formation)
            {
                var enemyObject = SpawnEnemy(PickSwarmPrefab(level, phase));
                var enemy = enemyObject != null ? enemyObject.GetComponent<EnemyController>() : null;
                if (enemy == null) continue;
                enemyObject.transform.position = StageCorridor.Clamp(position);
                enemy.StartFlyingMode(flyDirection, stageEvent.Speed);
                spawned.Add(enemy);
            }
            Debug.Log($"[EnemySpawner] Stage event {stageEvent.Type} ({spawned.Count} enemies) at {_stageTimePassed:0}s.");
            return spawned;
        }

        /// <summary>
        /// Start positions and flight directions for a stage event, on the ground plane at the
        /// player's height. <paramref name="direction"/> is the travel direction for Swarm and Wall.
        /// </summary>
        public static void BuildFormation(StageEventType type, int count, Vector3 player, Vector3 direction, float radius,
            List<(Vector3 position, Vector3 direction)> results)
        {
            results.Clear();
            count = Mathf.Max(1, count);
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            var across = new Vector3(-direction.z, 0f, direction.x);
            // Swarm and Wall start just outside the spawn ring on the far side and cross the player's position.
            var start = player - direction * (radius + 5f);

            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case StageEventType.Swarm:
                        // Tight cluster on a sunflower spiral: even spacing without random overlaps.
                        float r = 3f * Mathf.Sqrt((i + 0.5f) / count);
                        float a = i * 2.39996f;
                        results.Add((start + across * (Mathf.Cos(a) * r) + direction * (Mathf.Sin(a) * r), direction));
                        break;
                    case StageEventType.Wall:
                        const float spacing = 1.5f;
                        results.Add((start + across * ((i - (count - 1) * 0.5f) * spacing), direction));
                        break;
                    default: // ClosingRing
                        float angle = i * Mathf.PI * 2f / count;
                        var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        results.Add((player + outward * radius, -outward));
                        break;
                }
            }
        }

        private int CurrentPlayerLevel()
        {
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var experience = player != null ? player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>() : null;
            return experience != null ? experience.CurrentLevel : 1;
        }

        /// <summary>
        /// How many chest elites are due by <paramref name="stageTime"/>: one every
        /// <paramref name="interval"/> seconds, only before the stage boss arrives.
        /// </summary>
        public static int ChestElitesDue(float stageTime, float interval, float bossStartSeconds)
        {
            if (interval <= 0f) return 0;
            float lastTime = Mathf.Min(stageTime, bossStartSeconds - 0.001f);
            return Mathf.Max(0, Mathf.FloorToInt(lastTime / interval));
        }

        /// <summary>
        /// VS rule: strong enemies arrive at fixed times and always drop a chest (the main source
        /// of evolutions). Uses the stage elite (or the stage enemy) with extra health.
        /// </summary>
        private GameObject SpawnChestElite()
        {
            var prefab = _stageElitePrefab != null ? _stageElitePrefab : _stageEnemyPrefab;
            var elite = SpawnEnemy(prefab);
            if (elite == null) return null;

            elite.transform.localScale *= _chestEliteSizeMultiplier;
            var data = elite.GetComponent<EnemyController>()?.Data;
            var health = elite.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null && data != null)
                health.Initialize(ScaleHealthByCurse(Mathf.RoundToInt(data.MaxHealth * _chestEliteHealthMultiplier), CurrentCurse()));
            elite.GetComponent<EnemyDrop>()?.DropChestThisLife();
            Debug.Log($"[EnemySpawner] Chest elite appeared at {_stageTimePassed:0}s.");
            return elite;
        }

        /// <summary>How many chest elites' health a stage boss has at least (owner's story: the guardian outlasts its guards).</summary>
        public const float BossOverEliteHealth = 2f;

        /// <summary>A stage boss's health before Curse: its per-level health, but never under twice a chest elite's.</summary>
        public static int StoryBossHealth(int perLevelHealth, int chestEliteHealth) =>
            Mathf.Max(perLevelHealth, Mathf.RoundToInt(chestEliteHealth * BossOverEliteHealth));

        /// <summary>A chest elite's health before Curse on the current stage (0 if the stage has no elite data).</summary>
        private int ChestEliteBaseHealth()
        {
            var prefab = _stageElitePrefab != null ? _stageElitePrefab : _stageEnemyPrefab;
            var data = prefab != null ? prefab.GetComponent<EnemyController>()?.Data : null;
            return data != null ? Mathf.RoundToInt(data.MaxHealth * _chestEliteHealthMultiplier) : 0;
        }

        public static int ScaleHealthByCurse(int baseHealth, float curse) =>
            Mathf.Max(1, Mathf.RoundToInt(baseHealth * Mathf.Max(0.1f, curse)));

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
