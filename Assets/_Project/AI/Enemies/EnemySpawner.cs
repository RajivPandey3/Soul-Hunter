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

        /// <summary>
        /// VS (wiki Curse): "Total enemy quantity allowed on screen is capped at 500, regardless of Curse and Charm value."
        /// Map events stop at it. Stage bosses and chest elites are not held back (Soul Hunter: never lose a boss or a chest).
        /// </summary>
        public const int HardCap = 500;
        [SerializeField] private int _prewarmPerEnemyType = 32;
        [SerializeField] private int _prewarmPerBossType = 1;
        [Tooltip("Most enemies spawned in one frame while refilling the wave's enemy minimum")]
        [SerializeField, Min(1)] private int _maxMinimumTopUpPerFrame = 8;

        [Header("Chest elites (VS rule)")]
        [Tooltip("Stage minutes at which a chest-carrying elite arrives. Owner (2026-09-27): the VS minutes, read from the " +
                 "Mad Forest wave table (vampire.survivors.wiki/w/Mad_Forest?action=raw). 25:00 is the stage boss; the " +
                 "11:00 and 21:00 VS chests only hold Arcanas and are left out until Arcana chests exist.")]
        [SerializeField] private float[] _chestEliteMinutes = DefaultChestEliteMinutes;
        public static readonly float[] DefaultChestEliteMinutes = { 1f, 3f, 5f, 7f, 9f, 10f, 12f, 14f, 15f, 16f, 18f, 20f, 22f, 23f, 27f, 29f };
        [SerializeField, Min(1f)] private float _chestEliteSizeMultiplier = 1.5f;
        private int _chestElitesSpawned;
        private int _arcanaChestsSpawned;
        private int _chestlessBossesSpawned;
        public static readonly float[] ArcanaChestMinutes = { 11f, 21f };
        private System.Collections.Generic.IReadOnlyList<StageEventEntry> _stageEvents;
        private int _nextStageEvent;
        // VS wave events repeat: (stage time, entry) of each pending firing.
        private readonly List<(float time, StageEventEntry entry)> _eventFirings = new List<(float, StageEventEntry)>();
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
            EnemyDrop.PendingChests.Clear(); // a new run owes no chests
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
            _arcanaChestsSpawned = 0;
            _chestlessBossesSpawned = 0;
            _stageEvents = _currentWave.GetEvents();
            _nextStageEvent = 0;
            _eventFirings.Clear();
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

            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            // The arena was just wiped; keep it empty until the next stage starts.
            if (progression.IsStageTransitioning) return;
            _stageTimePassed = progression.StageElapsedTime;

            // Like the boss, a chest elite is never blocked by a full arena, nor by a boss fight (VS: they come on
            // the stage clock).
            if (_chestElitesSpawned < ChestElitesDue(_stageTimePassed, _chestEliteMinutes))
            {
                _chestElitesSpawned++;
                SpawnChestEliteAt(_chestEliteMinutes[_chestElitesSpawned - 1]);
            }
            // VS Arcana mode: Arcana chests at 11:00 and 21:00.
            if (RunModes.Arcana && _arcanaChestsSpawned < ChestElitesDue(_stageTimePassed, ArcanaChestMinutes))
            {
                _arcanaChestsSpawned++;
                SpawnChestEliteAt(ArcanaChestMinutes[_arcanaChestsSpawned - 1])?.GetComponent<EnemyDrop>()?.CarryArcanaThisLife();
            }

            // VS wave bosses that carry no chest, on every level (ChestlessWaveBosses: Level 1 Mad Forest 8:00, 21:00,
            // 24:00; Levels 2-10 from their source stages' "Bosses & Treasure" cells without a {{treasure}}).
            {
                var chestless = ChestlessWaveBosses.For(progression.CurrentStage);
                while (_chestlessBossesSpawned < chestless.Length && chestless[_chestlessBossesSpawned].minute * 60f <= _stageTimePassed)
                {
                    var due = chestless[_chestlessBossesSpawned++];
                    // A boss the Debug clock skipped more than a minute past is not sent late.
                    if (_stageTimePassed - due.minute * 60f < 60f) SpawnChestlessBoss(due.id);
                }
            }

            var level = progression.CurrentLevel;
            float bossStartTime = level.BossStartSeconds;
            var phase = _currentWave.GetPhase(_stageTimePassed);

            // VS wave events: on the stage clock, also during a boss fight. Each one can repeat, and each firing
            // happens with its chance divided by the player's Luck.
            while (_stageEvents != null && _nextStageEvent < _stageEvents.Count &&
                   _stageEvents[_nextStageEvent].TimeSeconds <= _stageTimePassed)
            {
                var stageEvent = _stageEvents[_nextStageEvent++];
                for (int r = 0; r < Mathf.Max(1, stageEvent.Repeat); r++)
                    _eventFirings.Add((stageEvent.TimeSeconds + r * stageEvent.RepeatDelaySeconds, stageEvent));
            }
            for (int i = _eventFirings.Count - 1; i >= 0; i--)
            {
                if (_eventFirings[i].time > _stageTimePassed) continue;
                var firing = _eventFirings[i].entry;
                bool missed = _stageTimePassed - _eventFirings[i].time > 10f; // the clock was skipped past it (Debug)
                _eventFirings.RemoveAt(i);
                if (!missed && EventFires(firing.ChancePercent, CurrentLuck(), Random.value)) RunStageEvent(firing, level, phase);
            }

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
            // VS: the minute waves go on during a boss fight (Mad Forest keeps a minimum of 100 at 25:00). Before
            // 2026-09-28 the horde stopped while the boss lived, so a boss fight was the boss alone on screen.
            if (waveIsActive && !bossPhase)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    // A VS minute table sets the pace itself; the automatic phases add a group member every 5 minutes.
                    int fiveMinutePressure = _currentWave.HasMinuteTable ? 0 : Mathf.FloorToInt(_stageTimePassed / 300f);
                    int groupSize = Mathf.Max(1, _currentWave.EnemiesPerSpawn + phase.EnemiesPerSpawnBonus + fiveMinutePressure);
                    int availableSlots = Mathf.Max(0, _maxAliveEnemies - EnemyController.ActiveEnemies.Count);
                    int spawnCount = Mathf.Min(groupSize, availableSlots);
                    if (phase.EnemyTypes != null && phase.EnemyTypes.Count > 0)
                    {
                        // VS wiki (Enemies, Waves): above the minimum, each tick sends one of each of the wave's types.
                        foreach (var type in phase.EnemyTypes)
                            if (EnemyController.ActiveEnemies.Count < _maxAliveEnemies) GivePendingChest(Defang(SpawnEnemy(type)));
                    }
                    else
                        for (int i = 0; i < spawnCount; i++) GivePendingChest(Defang(SpawnEnemy(PickSwarmPrefab(level, phase))));
                    float pressure = level.SpawnIntervalMultiplier * phase.SpawnIntervalMultiplier;
                    // A VS minute table's interval is used as the wiki gives it (Mad Forest 0.1 s, Cappella Magna 0.02 s at
                    // 21:00 and 26:00). The old 0.25 floor on this factor turned every interval under a quarter of the
                    // wave's SpawnInterval into that quarter (Level 1's 0.1 s ticks ran at 0.25 s). It stays for the
                    // automatic phases of a wave without a table. (P2-02, 2026-10-09.)
                    if (!_currentWave.HasMinuteTable) pressure = Mathf.Max(0.25f, pressure);
                    // VS rule: Curse makes enemies arrive more often, and so does each Endless cycle. Hyper does not:
                    // the wiki gives Hyper only the enemy minimum x1.25 (stage infoboxes, enemy-min2).
                    _timer = Mathf.Max(0.02f, _currentWave.SpawnInterval * pressure / (MinuteCurse() * RunModes.FrequencyMultiplier));
                }

                // VS rule: each wave keeps an enemy minimum. Killing faster than the spawn
                // interval does not empty the screen; the horde is refilled straight away.
                // VS: Curse raises the wave quantity, and Charm adds a flat number after it (Curse and Charm pages).
                int wanted = Mathf.RoundToInt(phase.MinimumEnemies * RunModes.MinimumMultiplier * MinuteCurse()) + CurrentCharm();
                int missing = Mathf.Min(wanted, _maxAliveEnemies) - EnemyController.ActiveEnemies.Count;
                // Below the minimum the quota is filled; a wave with enemy types fills it with its types in turn (VS).
                for (int i = 0; i < Mathf.Min(missing, _maxMinimumTopUpPerFrame); i++) GivePendingChest(Defang(SpawnEnemy(NextWavePrefab(level, phase))));
            }
        }

        private int _waveTypeCursor;

        /// <summary>The next enemy of the wave: its types in turn (VS), or the stage's own enemy when it lists none.</summary>
        private GameObject NextWavePrefab(SoulHunter.Gameplay.Data.CampaignLevelDefinition level, WavePhase phase)
        {
            if (phase.EnemyTypes == null || phase.EnemyTypes.Count == 0) return PickSwarmPrefab(level, phase);
            _waveTypeCursor = (_waveTypeCursor + 1) % phase.EnemyTypes.Count;
            return phase.EnemyTypes[_waveTypeCursor];
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
            Vector3 heading = PlayerHeading();
            // Re-enter just outside the screen ahead of the player, inside the trigger distance (or they would move again).
            float edge = SpawnDistance(heading.sqrMagnitude > 1e-4f ? heading : Vector3.forward);
            float minRadius = Mathf.Min(edge, _relocateDistance * 0.8f);
            float maxRadius = Mathf.Min(edge + OffScreenSpread, _relocateDistance * 0.9f);

            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                // Event fliers follow their own path and leave on their own. Bosses are brought back too: VS wiki
                // (Enemies, Bosses): "Bosses do not despawn when the player moves away from them, but get teleported back
                // to the screen." (Before 2026-09-28 a boss left behind stayed there.)
                if (enemy == null || enemy.IsFlying) continue;

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
            Vector3 bossDir = new Vector3(randomDir.x, 0f, randomDir.y);
            Vector3 spawnPos = StageCorridor.Clamp(_playerTransform.position + bossDir * SpawnDistance(bossDir));
            
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
                _bossControllers.Add(bossController); // a stage boss (IsBoss); brought back to the screen when left behind (VS)
                bossController.IsStageBoss = true;
                bossController.SetCastsShadows(true);
            }
            bossObj.SetActive(true);
            // P1-14: the 25:00 stage boss's chest uses Mad Forest's Giant Blue Venus config (3/10/100, evo=1). Set after
            // SetActive, whose OnEnable clears the previous life's chest.
            bossObj.GetComponent<EnemyDrop>()?.DropChestThisLife(SoulHunter.Gameplay.Core.ChestTiers.StageBoss);
            bossObj.transform.localScale = Vector3.one * 3f; // 3x Bigger
            float curse = MinuteCurse(); // speed: from the minute the Curse was gained in (VS wiki Curse)
            bossObj.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier * curse * RunModes.EnemySpeedMultiplier(StageMinutes));
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
                // VS rule (HP x Level): boss health is its per-level health x the player's level when it spawns. The
                // per-level values follow the VS 25:00 bosses along the stage list (owner, 2026-09-28: "pura VS").
                int bossHealth = bossData != null
                    ? bossData.HealthForPlayerLevel(CurrentPlayerLevel())
                    : 1000 * SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage;
                health.Initialize(ScaleHealthByCurse(ModeHealth(bossHealth), curse));
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
        /// <summary>
        /// VS (wiki Enemies): "Enemies generally spawn just outside the screen." The distance from <paramref name="origin"/>
        /// along <paramref name="direction"/> (ground plane) to the edge of the camera's view, found by bisection on the
        /// viewport; <paramref name="fallback"/> without a camera.
        /// </summary>
        public static float DistanceToScreenEdge(Camera camera, Vector3 origin, Vector3 direction, float fallback, float maxDistance = 80f)
        {
            direction.y = 0f;
            if (camera == null || direction.sqrMagnitude < 1e-6f) return fallback;
            direction.Normalize();
            bool Inside(float t)
            {
                Vector3 v = camera.WorldToViewportPoint(origin + direction * t);
                return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
            }
            if (!Inside(0f)) return fallback;
            if (Inside(maxDistance)) return maxDistance;
            float lo = 0f, hi = maxDistance;
            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Inside(mid)) lo = mid; else hi = mid;
            }
            return hi;
        }

        /// <summary>How far past the screen edge new enemies appear (units; Soul Hunter's own: the wiki says "just outside").</summary>
        public const float OffScreenMargin = 1.5f;
        public const float OffScreenSpread = 3f;

        private float SpawnDistance(Vector3 direction) =>
            DistanceToScreenEdge(Camera.main, _playerTransform.position, direction, _spawnRadius) + OffScreenMargin;

        private Vector3 PickSpawnPosition()
        {
            var side = _currentWave != null ? _currentWave.SpawnSide : SpawnSide.AllSides;
            float bias = _currentWave != null ? _currentWave.SideBias : 0f;
            Vector3 spawnPos = _playerTransform.position;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 direction = WaveTables.SpawnDirection(side, bias, Random.value, Random.value, Random.value);
                float distance = SpawnDistance(direction) + Random.Range(0f, OffScreenSpread); // just outside the screen
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
            float curse = CurrentCurse(); // health: at once (VS wiki Curse)
            enemyToSpawn.GetComponent<EnemyController>()?.ApplyCampaignSpeed(
                SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentLevel.EnemySpeedMultiplier * MinuteCurse() * RunModes.EnemySpeedMultiplier(StageMinutes));
            ConfigureStageDamageRule(enemyToSpawn);
            // VS movement skill: Floaty enemies weave, Fixed Direction ones take their line toward the player now.
            if (enemyController != null) enemyController.BeginMovement();
            // VS (wiki Orologion): "Enemies will continue to spawn, and new enemies will also become frozen."
            float frozenFor = SoulHunter.Gameplay.Pickups.TimeFreezePickup.WorldFreezeRemaining;
            if (enemyController != null && frozenFor > 0f) enemyController.Freeze(frozenFor);
            
            var health = enemyToSpawn.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                // VS rule: Curse makes enemies tougher. Always scale from the EnemyData value so
                // pooled enemies never compound it; enemies without data keep their own max health.
                var data = enemyController != null ? enemyController.Data : null;
                // VS "HP x Level" (wiki Enemies, Skills): an enemy with that skill multiplies its health by the player's
                // level when it spawns (EnemyData.HealthPerPlayerLevel > 0).
                if (data != null) health.Initialize(ScaleHealthByCurse(ModeHealth(data.HealthPerPlayerLevel > 0
                    ? data.HealthForPlayerLevel(CurrentPlayerLevel()) : data.MaxHealth), curse));
                else health.ResetHealth();
                // VS lives (wiki Scarleton, "Has 3 lives"): the type resurrects until its lives run out (EnemyLives).
                if (data != null && data.Lives > 1) EnemyLives.Arm(enemyToSpawn, data.Lives);
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
        /// VS (wiki Curse): "When Curse is gained during a run, the health multiplier effect is immediately applied to all
        /// newly spawned enemies. All other effects are applied at the start of the next minute." Speed, frequency and
        /// quantity use the Curse held when the current stage minute began.
        /// </summary>
        private float MinuteCurse()
        {
            int minute = Mathf.FloorToInt(StageMinutes);
            int stage = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance != null ? SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage : 0;
            if (minute != _curseMinute || stage != _curseStage)
            {
                _curseMinute = minute;
                _curseStage = stage;
                _minuteCurse = CurrentCurse();
            }
            return _minuteCurse;
        }

        private int _curseMinute = -1, _curseStage = -1;
        private float _minuteCurse = 1f;

        /// <summary>The Curse non-health effects use now (tests).</summary>
        public float CurseForSpeedAndSpawns => MinuteCurse();

        /// <summary>VS Charm: enemies added to every wave.</summary>
        private int CurrentCharm()
        {
            CurrentCurse(); // resolves _playerStats
            return _playerStats != null ? Mathf.Max(0, _playerStats.Charm) : 0;
        }

        /// <summary>
        /// VS Defang: a share of each wave's spawned enemies cannot deal damage (green). Only ordinary wave spawns: wave
        /// bosses (chest elites), stage bosses and event enemies are never defanged (Defang page).
        /// </summary>
        private GameObject Defang(GameObject enemy)
        {
            if (enemy == null) return null;
            var controller = enemy.GetComponent<EnemyController>();
            if (controller == null) return enemy;
            CurrentCurse(); // resolves _playerStats
            float chance = _playerStats != null ? _playerStats.Defang : 0f;
            // VS statboxes' res-defang (e.g. Trinacria "res-defang = 1"): such an enemy is never defanged.
            if (controller.Data != null && controller.Data.DefangResistance) chance = 0f;
            controller.SetDefanged(chance > 0f && Random.value < chance);
            return enemy;
        }

        /// <summary>The player's Luck (1 = none). VS: Luck lowers the chance of map events.</summary>
        private float CurrentLuck()
        {
            CurrentCurse(); // resolves _playerStats
            return _playerStats != null ? Mathf.Max(0.01f, _playerStats.Luck) : 1f;
        }

        /// <summary>
        /// VS wiki (Enemies, map events): each firing happens with chance = eventChance / totalLuck; an event with no
        /// chance (100 here) is guaranteed.
        /// </summary>
        public static bool EventFires(float chancePercent, float luck, float roll01) =>
            chancePercent >= 100f || roll01 * 100f < chancePercent / Mathf.Max(0.01f, luck);

        /// <summary>
        /// VS Pentagram: a chest whose carrier was erased goes to the next ordinary enemy of the horde, with the erased
        /// boss's ChestTiers (and Arcana flag) kept.
        /// </summary>
        private static void GivePendingChest(GameObject enemy)
        {
            if (EnemyDrop.PendingChests.Count == 0 || enemy == null) return;
            var drop = enemy.GetComponent<EnemyDrop>();
            if (drop == null || drop.WillDropChest) return;
            // VS Scarleton: "It cannot drop a Treasure Chest." A type with lives passes the chest to the next enemy.
            var typeController = enemy.GetComponent<EnemyController>();
            if (typeController != null && typeController.Data != null && typeController.Data.Lives > 1) return;
            var tiers = EnemyDrop.PendingChests.Dequeue();
            drop.DropChestThisLife(tiers);
            if (tiers.IsArcana) drop.CarryArcanaThisLife();
        }

        /// <summary>True for a stage boss spawned by this spawner (QA tools and the autoplay bot).</summary>
        public bool IsBoss(EnemyController enemy) => enemy != null && _bossControllers.Contains(enemy);

        /// <summary>
        /// VS pressure plates start a map event on demand: this stage's formation of the given type, with this
        /// stage's enemies, around the player. Returns the spawned enemies (empty during the boss or a transition).
        /// </summary>
        /// <param name="speed">Flight speed relative to the default event speed (StageEventEntry.Speed; Omens' Stampede).</param>
        public List<EnemyController> TriggerStageEvent(StageEventType type, int count, float speed = 1f)
        {
            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            if (progression == null || progression.IsStageTransitioning || _isBossAlive || _currentWave == null)
                return new List<EnemyController>();
            var phase = _currentWave.GetPhase(progression.StageElapsedTime);
            return RunStageEvent(new StageEventEntry { Type = type, Count = Mathf.Max(1, count), Speed = Mathf.Max(0.2f, speed) },
                progression.CurrentLevel, phase);
        }

        /// <summary>
        /// Spawns one stage event's formation around the player and sends every member along its
        /// fixed path. Returns the spawned enemies. Not blocked by the 300 cap (VS: "only bosses and enemies from map events
        /// can spawn"), but by the 500 hard cap.
        /// </summary>
        private List<EnemyController> RunStageEvent(StageEventEntry stageEvent, CampaignLevelDefinition level, WavePhase phase)
        {
            var spawned = new List<EnemyController>();
            if (_playerTransform == null || stageEvent == null) return spawned;

            // The formation travels away from the side it comes from (Part 4: a stage's sides, e.g. castle walls
            // sweep in from the left or right).
            var side = _currentWave != null ? _currentWave.SpawnSide : SpawnSide.AllSides;
            var direction = -WaveTables.SpawnDirection(side, 1f, 0f, Random.value, Random.value);
            BuildFormation(stageEvent.Type, stageEvent.Count, _playerTransform.position, direction, EventRadius(stageEvent.Type, direction), _formation);
            foreach (var (position, flyDirection) in _formation)
            {
                if (EnemyController.ActiveEnemies.Count >= HardCap) break; // VS: at most 500 on screen
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

        /// <summary>Chest-less wave bosses sent on this stage so far (tests).</summary>
        public int ChestlessBossesSpawned => _chestlessBossesSpawned;

        /// <summary>
        /// One enemy of this level's type as a wave boss without a chest (ChestlessWaveBosses; Level 1's are
        /// MadForestEnemies.ChestlessBosses). The prefab is found among the stage wave's own enemy types by name, so no
        /// scene or asset names it twice.
        /// </summary>
        public GameObject SpawnChestlessBoss(string typeId)
        {
            if (_currentWave == null || _currentWave.Minutes == null) return null;
            string wanted = ChestlessWaveBosses.PrefabName(SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.CurrentStage, typeId);
            if (string.IsNullOrEmpty(wanted)) return null;
            GameObject prefab = null;
            foreach (var minute in _currentWave.Minutes)
            {
                if (minute == null || minute.EnemyTypes == null) continue;
                foreach (var type in minute.EnemyTypes)
                    if (type != null && type.name == wanted) { prefab = type; break; }
                if (prefab != null) break;
            }
            if (prefab == null) return null;
            var boss = SpawnEnemy(prefab);
            if (boss != null) Debug.Log($"[EnemySpawner] Chest-less wave boss {typeId} at {_stageTimePassed:0}s.");
            return boss;
        }

        /// <summary>
        /// How far from the player a map event's formation starts. VS wiki (Enemies): "Enemies generally spawn just outside
        /// the screen"; the old fixed 20 units was inside a 16:9 view at its sides. A swarm or wall starts past the screen
        /// edge it comes from; a closing ring starts past the farthest edge (the corners), so no member appears in view.
        /// Never nearer than the old radius.
        /// </summary>
        public float EventRadius(StageEventType type, Vector3 travelDirection)
        {
            if (_playerTransform == null) return _spawnRadius;
            var camera = Camera.main;
            Vector3 origin = _playerTransform.position;
            float edge;
            if (type == StageEventType.ClosingRing)
            {
                edge = 0f;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8f;
                    edge = Mathf.Max(edge, DistanceToScreenEdge(camera, origin, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)), _spawnRadius));
                }
            }
            else
            {
                travelDirection.y = 0f;
                Vector3 from = travelDirection.sqrMagnitude > 0.0001f ? -travelDirection.normalized : Vector3.back;
                edge = DistanceToScreenEdge(camera, origin, from, _spawnRadius);
            }
            return Mathf.Max(_spawnRadius, edge + OffScreenMargin);
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

        /// <summary>How many chest elites are due by <paramref name="stageTime"/> seconds: one per listed minute reached.</summary>
        public static int ChestElitesDue(float stageTime, System.Collections.Generic.IReadOnlyList<float> minutes)
        {
            if (minutes == null) return 0;
            int due = 0;
            for (int i = 0; i < minutes.Count; i++) if (minutes[i] * 60f <= stageTime) due++;
            return due;
        }

        /// <summary>
        /// VS rule: strong enemies arrive at fixed times and always drop a chest (the main source
        /// of evolutions). Uses the stage elite (or the stage enemy) with extra health.
        /// </summary>
        private GameObject SpawnChestElite() => SpawnChestEliteAt(StageMinutes);

        /// <summary>
        /// VS wiki (Mad Forest chest bosses, fetched 2026-09-28; all with HP x Level unless noted): Glowing and Silver Bats
        /// 50 (most minutes, and the 11:00/21:00 Arcana holders), Giant Mantichana 150 at 10:00, Giant Werewolf 200 at
        /// 15:00, Giant Mummy 250 at 20:00; the 5:00 Mantichana has a flat 500 without HP x Level.
        /// </summary>
        public static int ChestEliteHealth(float minute, int playerLevel)
        {
            int m = Mathf.RoundToInt(minute);
            if (m == 5) return 500;
            int perLevel = m == 10 ? 150 : m == 15 ? 200 : m == 20 ? 250 : 50;
            return perLevel * Mathf.Max(1, playerLevel);
        }

        private GameObject SpawnChestEliteAt(float minute)
        {
            var prefab = _stageElitePrefab != null ? _stageElitePrefab : _stageEnemyPrefab;
            var elite = SpawnEnemy(prefab);
            if (elite == null) return null;

            elite.transform.localScale *= _chestEliteSizeMultiplier;
            var data = elite.GetComponent<EnemyController>()?.Data;
            var health = elite.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null && data != null)
                health.Initialize(ScaleHealthByCurse(ModeHealth(ChestEliteHealth(minute, CurrentPlayerLevel())), CurrentCurse()));
            // P1-14: the chest carries this minute's Mad Forest config (vampire.survivors.wiki/w/Mad_Forest, "Waves"), so its
            // item chances and evolution flag belong to the boss, not to the clock when it dies. Off-table minutes (debug
            // spawns) use ChestTiers.Unscheduled.
            elite.GetComponent<EnemyDrop>()?.DropChestThisLife(SoulHunter.Gameplay.Core.ChestTiers.ForMinute(minute));
            Debug.Log($"[EnemySpawner] Chest elite appeared at {_stageTimePassed:0}s.");
            return elite;
        }

        /// <summary>Stage minutes on the clock (for the run modes' per-minute growth).</summary>
        private static float StageMinutes
        {
            get
            {
                var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
                return progression != null ? progression.StageElapsedTime / 60f : 0f;
            }
        }

        /// <summary>Health after the run modes (Inverse, Endless cycles); unchanged when no mode is on.</summary>
        private static int ModeHealth(int baseHealth)
        {
            float m = RunModes.EnemyHealthMultiplier(StageMinutes);
            return Mathf.Approximately(m, 1f) ? baseHealth : Mathf.Max(1, Mathf.RoundToInt(baseHealth * m));
        }

        // ---- Stage hazards (Stalker, pile assaults, ...) spawn this stage's enemies through these. ----
        public GameObject StageEnemyPrefab => _stageEnemyPrefab;
        public GameObject StageElitePrefab => _stageElitePrefab != null ? _stageElitePrefab : _stageEnemyPrefab;
        public GameObject StageBossPrefab => _stageBossPrefab;

        /// <summary>Spawns one of this stage's enemies the normal way (pool, stage speed, health, damage rule) at a position.</summary>
        public GameObject SpawnHazardEnemy(GameObject prefab, Vector3 position)
        {
            var enemy = SpawnEnemy(prefab);
            if (enemy == null) return null;
            position.y = _playerTransform != null ? _playerTransform.position.y : position.y;
            enemy.transform.position = StageCorridor.Clamp(position);
            return enemy;
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
