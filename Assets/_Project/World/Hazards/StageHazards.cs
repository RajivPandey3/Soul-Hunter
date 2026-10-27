using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Runs the VS stage hazards on the stage clock (StageHazardTable): Death's Shadow (Stages 5 and 9), Soul Meteors
    /// (Stages 6 and 10), Pile Assaults (Stage 8), and lays the rail carts of Stage 3 when it starts. Added by
    /// GameSessionManager. Everything runs on game time (nothing moves or fires while GameTime pauses the game), nothing
    /// starts during a stage transition, and a stage change or stage clear removes every hazard of the old stage.
    /// </summary>
    public class StageHazards : MonoBehaviour
    {
        public static StageHazards Instance { get; private set; }

        private readonly List<StageHazardEntry> _schedule = new List<StageHazardEntry>();
        private int _next;
        private int _stage;
        private LevelProgressionManager _progression;
        private EnemySpawner _spawner;

        private readonly List<DeathsShadow> _shadows = new List<DeathsShadow>();
        private readonly List<PileShooter> _pile = new List<PileShooter>();
        private readonly List<SoulMeteor> _meteors = new List<SoulMeteor>();
        private readonly List<RailCartTrack> _tracks = new List<RailCartTrack>();
        private readonly List<MineSentry> _mines = new List<MineSentry>();
        private readonly List<ThornCircle> _thornCircles = new List<ThornCircle>();
        private readonly List<PulsingRing> _pulsingRings = new List<PulsingRing>();
        private readonly List<RisingTide> _tides = new List<RisingTide>();
        private readonly List<(float at, Vector3 offset)> _pendingMarkers = new List<(float, Vector3)>();
        private float _volleyClock;

        public IReadOnlyList<DeathsShadow> Shadows => _shadows;
        public IReadOnlyList<PileShooter> PileShooters => _pile;
        public IReadOnlyList<SoulMeteor> Meteors => _meteors;
        public IReadOnlyList<RailCartTrack> Tracks => _tracks;
        public IReadOnlyList<MineSentry> Mines => _mines;
        public IReadOnlyList<ThornCircle> ThornCircles => _thornCircles;
        public IReadOnlyList<PulsingRing> PulsingRings => _pulsingRings;
        public IReadOnlyList<RisingTide> Tides => _tides;
        public int Stage => _stage;

        private void Awake()
        {
            if (Instance == null || Instance == this) Instance = this;
        }

        private void Start()
        {
            _progression = LevelProgressionManager.Instance;
            if (_progression == null) return;
            _progression.OnStageStarted += HandleStageStarted;
            _progression.OnStageCleared += HandleStageCleared;
            HandleStageStarted(_progression.CurrentStage);
        }

        private void OnDestroy()
        {
            if (_progression != null)
            {
                _progression.OnStageStarted -= HandleStageStarted;
                _progression.OnStageCleared -= HandleStageCleared;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleStageStarted(int stage)
        {
            ClearAll();
            _stage = stage;
            _schedule.Clear();
            _schedule.AddRange(StageHazardTable.For(stage));
            _next = 0;
            ShadowEventsSkipped = 0;
            if (StageHazardTable.HasRailCarts(stage)) StartCoroutine(LayRailsWhenReady(stage));
        }

        private void HandleStageCleared(int stage) => ClearAll();

        private void Update()
        {
            if (_progression == null) return;
            PruneLists();
            if (_progression.IsStageTransitioning) return;
            float clock = _progression.StageElapsedTime;

            while (_next < _schedule.Count && _schedule[_next].TimeSeconds <= clock)
            {
                var entry = _schedule[_next++];
                if (clock - entry.TimeSeconds > StageHazardTable.MissedAfterSeconds) continue; // skipped past (Debug)
                Run(entry);
            }

            // A volley's markers appear one after another (game time).
            if (_pendingMarkers.Count > 0)
            {
                _volleyClock += Time.deltaTime;
                for (int i = _pendingMarkers.Count - 1; i >= 0; i--)
                {
                    if (_pendingMarkers[i].at > _volleyClock) continue;
                    var offset = _pendingMarkers[i].offset;
                    _pendingMarkers.RemoveAt(i);
                    DropMeteorNearPlayer(offset);
                }
            }
        }

        /// <summary>
        /// Roll for Death's Shadow's map-event chance (0-1). Null uses Random.value; tests set a fixed roll.
        /// </summary>
        public System.Func<float> ShadowRoll { get; set; }
        /// <summary>Death's Shadow events whose chance roll failed on this stage (logs and tests).</summary>
        public int ShadowEventsSkipped { get; private set; }

        /// <summary>
        /// A scheduled hazard. Death's Shadow rolls its VS chance first (The_Stalker XL: "30% chance"), divided by the
        /// player's Luck (wiki Luck, "Skipping events"), and stays for the entry's duration.
        /// </summary>
        public void Run(StageHazardEntry entry)
        {
            if (entry.Kind == StageHazardKind.DeathsShadow)
            {
                float roll = ShadowRoll != null ? ShadowRoll() : Random.value;
                if (!StageHazardTable.ShadowEventFires(entry.ChancePercent, PlayerLuck(), roll))
                {
                    ShadowEventsSkipped++;
                    Debug.Log($"[StageHazards] Death's Shadow did not come on Stage {_stage} ({entry.ChancePercent:0}% chance).");
                    return;
                }
                SpawnDeathsShadow(entry.DurationSeconds > 0f ? entry.DurationSeconds : StageHazardTable.ShadowLifetimeSeconds);
                return;
            }
            Run(entry.Kind);
        }

        /// <summary>Starts a hazard now (no chance roll).</summary>
        public void Run(StageHazardKind kind)
        {
            switch (kind)
            {
                case StageHazardKind.DeathsShadow: SpawnDeathsShadow(); break;
                case StageHazardKind.SoulMeteors: StartMeteorVolley(); break;
                case StageHazardKind.PileAssault: StartPileAssault(); break;
            }
        }

        private EnemySpawner Spawner()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<EnemySpawner>();
            return _spawner;
        }

        // ------------------------------------------------------------------ Death's Shadow

        /// <summary>Sends Death's Shadow after the player from just off-screen. Returns it, or null.</summary>
        public DeathsShadow SpawnDeathsShadow(float lifetimeSeconds = StageHazardTable.ShadowLifetimeSeconds)
        {
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition()) return null;
            var prefab = spawner.StageBossPrefab != null ? spawner.StageBossPrefab : spawner.StageElitePrefab;
            if (prefab == null) return null;
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            // VS wiki (Enemies): "Enemies generally spawn just outside the screen". The old fixed 18 units was inside a 16:9
            // view at its sides, so it is only the least distance now.
            Vector3 direction = new Vector3(dir.x, 0f, dir.y);
            float distance = Mathf.Max(StageHazardTable.ShadowSpawnDistance,
                EnemySpawner.DistanceToScreenEdge(Camera.main, player.transform.position, direction, StageHazardTable.ShadowSpawnDistance) + EnemySpawner.OffScreenMargin);
            Vector3 at = player.transform.position + direction * distance;
            var enemy = spawner.SpawnHazardEnemy(prefab, at);
            // VS "HP x Level", and Curse ("allows it to move very fast") with the run modes' enemy speed.
            float minutes = _progression != null ? _progression.StageElapsedTime / 60f : 0f;
            float curseAndModes = spawner.CurseForSpeedAndSpawns * SoulHunter.Gameplay.Data.RunModes.EnemySpeedMultiplier(minutes);
            var shadow = DeathsShadow.Begin(enemy, lifetimeSeconds, PlayerLevel(), curseAndModes);
            if (shadow != null)
            {
                _shadows.Add(shadow);
                Debug.Log($"[StageHazards] Death's Shadow follows the player on Stage {_stage} for {lifetimeSeconds:0} s.");
            }
            return shadow;
        }

        private static int PlayerLevel()
        {
            var player = PlayerController.Instance;
            var experience = player != null ? player.GetComponent<PlayerExperience>() : null;
            return experience != null ? experience.CurrentLevel : 1;
        }

        private static float PlayerLuck()
        {
            var player = PlayerController.Instance;
            var stats = player != null ? player.GetComponent<PlayerStats>() : null;
            return stats != null ? stats.Luck : 1f;
        }

        // ------------------------------------------------------------------ Soul Meteors

        /// <summary>A volley of markers around the player, laid one after another.</summary>
        public void StartMeteorVolley(int count = StageHazardTable.MeteorVolleyCount)
        {
            if (PlayerController.Instance == null || InTransition()) return;
            _volleyClock = 0f;
            _pendingMarkers.Clear();
            for (int i = 0; i < count; i++)
                _pendingMarkers.Add((i * StageHazardTable.MeteorStaggerSeconds, StageHazardTable.MeteorOffset(i, count, Random.value, Random.value)));
            Debug.Log($"[StageHazards] Soul Meteors: {count} markers on Stage {_stage}.");
        }

        private void DropMeteorNearPlayer(Vector3 offset)
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            DropMeteor(StageCorridor.Clamp(player.transform.position + offset));
        }

        /// <summary>One marker at a position (on the player's plane). Strikes after the VS 2 s.</summary>
        public SoulMeteor DropMeteor(Vector3 position, float delaySeconds = StageHazardTable.MeteorDelaySeconds)
        {
            var player = PlayerController.Instance;
            if (player != null) position.y = player.transform.position.y;
            var meteor = SoulMeteor.Create(position, StageHazardTable.MeteorColor(_stage), delaySeconds);
            _meteors.Add(meteor);
            return meteor;
        }

        // ------------------------------------------------------------------ Pile Assault

        /// <summary>A still ring of this stage's enemies around the player that shoots at it. Returns the shooters.</summary>
        public List<PileShooter> StartPileAssault(int count = StageHazardTable.PileCount,
            float durationSeconds = StageHazardTable.PileDurationSeconds)
        {
            var spawned = new List<PileShooter>();
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition() || spawner.StageEnemyPrefab == null) return spawned;
            Vector3 centre = player.transform.position;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = StageCorridor.Clamp(centre + StageHazardTable.PileOffset(i, count));
                var enemy = spawner.SpawnHazardEnemy(spawner.StageEnemyPrefab, at);
                // Staggered first shots, so the ring does not fire as one.
                var shooter = PileShooter.Begin(enemy, durationSeconds, 1f + StageHazardTable.PileFireIntervalSeconds * i / Mathf.Max(1, count));
                if (shooter == null) continue;
                spawned.Add(shooter);
                _pile.Add(shooter);
            }
            Debug.Log($"[StageHazards] Pile Assault: {spawned.Count} shooters on Stage {_stage}.");
            return spawned;
        }

        /// <summary>
        /// Two still columns of this stage's enemies, left and right of the player, that shoot at it (VS Skull Pile Pile /
        /// the Random Event "Scorpion Lair"; Soul Hunter's Omen "Crossfire Rows"). Returns the shooters.
        /// </summary>
        public List<PileShooter> StartPileColumns(int perColumn = StageHazardTable.PileColumnLength,
            float durationSeconds = StageHazardTable.PileDurationSeconds)
        {
            var spawned = new List<PileShooter>();
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition() || spawner.StageEnemyPrefab == null) return spawned;
            Vector3 centre = player.transform.position;
            int count = Mathf.Max(1, perColumn) * 2;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = StageCorridor.Clamp(centre + StageHazardTable.PileColumnOffset(i, perColumn));
                var enemy = spawner.SpawnHazardEnemy(spawner.StageEnemyPrefab, at);
                var shooter = PileShooter.Begin(enemy, durationSeconds, 1f + StageHazardTable.PileFireIntervalSeconds * i / count);
                if (shooter == null) continue;
                spawned.Add(shooter);
                _pile.Add(shooter);
            }
            Debug.Log($"[StageHazards] Pile columns: {spawned.Count} shooters on Stage {_stage}.");
            return spawned;
        }

        // ------------------------------------------------------------------ Omens: Powder Ring, Thorn Circle, Rising Tide

        /// <summary>
        /// A still ring of this stage's enemies around the player that blow themselves up when it comes close (VS Random
        /// Event "Roulette"; Soul Hunter's Omen "Powder Ring"). Returns the members.
        /// </summary>
        public List<MineSentry> StartMineRing(int count = StageHazardTable.MineCount, float durationSeconds = StageHazardTable.MineSeconds)
        {
            var spawned = new List<MineSentry>();
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition() || spawner.StageEnemyPrefab == null) return spawned;
            Vector3 centre = player.transform.position;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = StageCorridor.Clamp(centre + StageHazardTable.PileOffset(i, count));
                var mine = MineSentry.Begin(spawner.SpawnHazardEnemy(spawner.StageEnemyPrefab, at), durationSeconds);
                if (mine == null) continue;
                spawned.Add(mine);
                _mines.Add(mine);
            }
            Debug.Log($"[StageHazards] Powder Ring: {spawned.Count} members on Stage {_stage}.");
            return spawned;
        }

        /// <summary>
        /// A ring of Thorn Walls around the player that closes in and stops the hero (VS Random Event "Flower Wall"; Soul
        /// Hunter's Omen "Thorn Circle"). Returns it, or null.
        /// </summary>
        public ThornCircle StartThornCircle(float durationSeconds = StageHazardTable.ThornSeconds, GameObject memberPrefab = null)
        {
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition()) return null;
            var catalog = GameContentCatalog.Load();
            var prefab = memberPrefab != null ? memberPrefab
                : catalog != null && catalog.OmenThornWall != null ? catalog.OmenThornWall : spawner.StageElitePrefab;
            var circle = ThornCircle.Begin(spawner, prefab, player.transform.position, durationSeconds);
            if (circle != null)
            {
                _thornCircles.Add(circle);
                Debug.Log($"[StageHazards] Thorn Circle: {circle.Standing} members on Stage {_stage}.");
            }
            return circle;
        }

        /// <summary>
        /// A ring of <paramref name="memberPrefab"/> round the player that closes in and moves away again (VS Jellyfish
        /// Wall). Returns it, or null.
        /// </summary>
        public PulsingRing StartPulsingRing(GameObject memberPrefab, int count, float durationSeconds)
        {
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition() || memberPrefab == null) return null;
            var ring = PulsingRing.Begin(spawner, memberPrefab, player.transform.position, count, durationSeconds);
            if (ring != null) _pulsingRings.Add(ring);
            return ring;
        }

        /// <summary>
        /// The Tide Caller at the bottom-left of the screen and its water rising from the screen's bottom edge (VS Random
        /// Event "The Drowner"; Soul Hunter's Omen "Rising Tide"). Returns it, or null.
        /// </summary>
        public RisingTide StartRisingTide(float seconds = StageHazardTable.TideSeconds)
        {
            var spawner = Spawner();
            var player = PlayerController.Instance;
            if (spawner == null || player == null || InTransition()) return null;
            var camera = Camera.main;
            Vector3 origin = player.transform.position;
            float down = EnemySpawner.DistanceToScreenEdge(camera, origin, Vector3.back, 12f);
            float up = EnemySpawner.DistanceToScreenEdge(camera, origin, Vector3.forward, 12f);
            float left = EnemySpawner.DistanceToScreenEdge(camera, origin, Vector3.left, 20f);
            float line = origin.z - down;
            float rise = (down + up) / Mathf.Max(1f, seconds); // one screen height over the event
            var prefab = spawner.StageBossPrefab != null ? spawner.StageBossPrefab : spawner.StageElitePrefab;
            Vector3 corner = origin + Vector3.left * Mathf.Max(1f, left - StageHazardTable.TideCallerInset)
                                    + Vector3.back * Mathf.Max(1f, down - StageHazardTable.TideCallerInset);
            var body = prefab != null ? spawner.SpawnHazardEnemy(prefab, corner) : null;
            // A full screen (500, Bug MIS AI-08) holds the Tide Caller back: no flood without a caller to defeat.
            if (body == null && prefab != null) return null;
            var tide = RisingTide.Begin(body, line, rise, seconds, PlayerLevel());
            _tides.Add(tide);
            Debug.Log($"[StageHazards] Rising Tide on Stage {_stage} for {seconds:0} s.");
            return tide;
        }

        // ------------------------------------------------------------------ Rail carts

        private System.Collections.IEnumerator LayRailsWhenReady(int stage)
        {
            yield return null; // after InfiniteMap has laid this stage's floor (same event)
            UnityEngine.Physics.SyncTransforms();
            if (_stage == stage) LayRails();
        }

        /// <summary>Lays this stage's rails on open ground near the player (they do not cross each other).</summary>
        public void LayRails()
        {
            ClearRails();
            var player = PlayerController.Instance;
            var catalog = GameContentCatalog.Load();
            if (player == null) return;
            if (catalog == null || catalog.RailCart == null)
            {
                Debug.LogWarning("[StageHazards] No RailCart in GameContentCatalog (Soul Hunter/Content/Build Stage Hazards); no rail carts.");
                return;
            }
            Vector3 origin = player.transform.position;
            var chosen = new List<(Vector3 a, Vector3 b, Vector3 centre, float yaw)>();
            var candidates = StageHazardTable.RailCandidates();
            // First pass: fully open rails only; second pass: the most open of the rest.
            for (int pass = 0; pass < 2 && chosen.Count < StageHazardTable.RailCount; pass++)
            {
                var ranked = new List<(int open, Vector3 centre, float yaw)>();
                foreach (var (offset, yaw) in candidates)
                {
                    Vector3 centre = StageCorridor.Clamp(origin + offset, 2f);
                    ranked.Add((OpenSamples(centre, yaw), centre, yaw));
                }
                if (pass == 1) ranked.Sort((x, y) => y.open.CompareTo(x.open));
                foreach (var (open, centre, yaw) in ranked)
                {
                    if (chosen.Count >= StageHazardTable.RailCount) break;
                    if (pass == 0 && open < RailSamples) continue;
                    var (a, b) = StageHazardTable.RailEnds(centre, yaw, RailCartTrack.Length);
                    bool apart = true;
                    foreach (var other in chosen) apart &= StageHazardTable.SegmentsApart(a, b, other.a, other.b, 3f);
                    if (apart) chosen.Add((a, b, centre, yaw));
                }
            }
            foreach (var rail in chosen)
            {
                Vector3 at = rail.centre;
                at.y = origin.y;
                var go = Instantiate(catalog.RailCart, at, Quaternion.Euler(0f, rail.yaw, 0f));
                var track = go.GetComponent<RailCartTrack>();
                if (track == null) track = go.AddComponent<RailCartTrack>();
                _tracks.Add(track);
            }
        }

        private const int RailSamples = 9;

        private static int OpenSamples(Vector3 centre, float yaw)
        {
            var (a, b) = StageHazardTable.RailEnds(centre, yaw, RailCartTrack.Length);
            int open = 0;
            for (int i = 0; i < RailSamples; i++)
                if (GroundCheck.IsOpen(Vector3.Lerp(a, b, i / (RailSamples - 1f)), 0.8f)) open++;
            return open;
        }

        // ------------------------------------------------------------------ cleanup

        private bool InTransition() => _progression != null && _progression.IsStageTransitioning;

        private void PruneLists()
        {
            _shadows.RemoveAll(s => s == null || !s.isActiveAndEnabled);
            _pile.RemoveAll(p => p == null || !p.isActiveAndEnabled);
            _meteors.RemoveAll(m => m == null);
            _mines.RemoveAll(m => m == null || !m.isActiveAndEnabled);
            _thornCircles.RemoveAll(c => c == null);
            _pulsingRings.RemoveAll(c => c == null);
            _tides.RemoveAll(t => t == null);
        }

        /// <summary>Removes every hazard of the current stage (stage change, stage clear).</summary>
        public void ClearAll()
        {
            StopAllCoroutines();
            _pendingMarkers.Clear();
            foreach (var shadow in _shadows) if (shadow != null && shadow.isActiveAndEnabled) shadow.Leave();
            _shadows.Clear();
            foreach (var shooter in _pile) if (shooter != null && shooter.isActiveAndEnabled) shooter.Leave();
            _pile.Clear();
            foreach (var meteor in _meteors) if (meteor != null) Destroy(meteor.gameObject);
            _meteors.Clear();
            foreach (var mine in _mines) if (mine != null && mine.isActiveAndEnabled) mine.Leave();
            _mines.Clear();
            foreach (var circle in _thornCircles) if (circle != null) circle.Leave();
            _thornCircles.Clear();
            foreach (var ring in _pulsingRings) if (ring != null) ring.Leave();
            _pulsingRings.Clear();
            foreach (var tide in _tides) if (tide != null) tide.End();
            _tides.Clear();
            HazardBullets.EndAll(); // Bug MIS AI-11: enemy bullets go with the stage
            ClearRails();
        }

        private void ClearRails()
        {
            foreach (var track in _tracks) if (track != null) Destroy(track.gameObject);
            _tracks.Clear();
        }
    }
}
