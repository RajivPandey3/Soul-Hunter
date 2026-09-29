using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS light-source spawning (vampire.survivors.wiki, Light source): once a second the game tries to place one
    /// just off-screen. The chance is the stage's base chance x Luck, capped, and only a limited number exist at once.
    /// Balance pass (owner, 2026-09-29): each level uses a VS stage's numbers (wiki Light source, destructible table),
    /// following the climb of the stage bonuses: L1 Mad Forest 10% / 50% / 10, L2 Inlaid Library 7.5% / 50% / 10, L3 Dairy
    /// Plant 20% / 60% / 10, L4 Gallo Tower 7.5% / 50% / 10, L5-L10 Cappella Magna 20% / 80% / 20. At the limit VS keeps
    /// trying: a success (base chance, no Luck while capped) places a new lantern at the spot nearest the player and
    /// despawns the oldest. Inlaid Library, Dairy Plant and Gallo Tower's fixed spawn spots are not modelled.
    /// Lanterns never appear inside a stage obstacle, and ones left far behind are recycled.
    /// </summary>
    public class LightSourceSpawner : MonoBehaviour
    {
        public const float AttemptIntervalSeconds = 1f;
        public const float BaseChance = 0.10f;
        public const float MaxChance = 0.50f;
        public const int MaxActive = 10;
        /// <summary>Lanterns this far from the player go back to the pool (like the horde's far limit). Provisional.</summary>
        public const float RecycleDistance = 40f;

        private readonly List<LightSource> _active = new List<LightSource>();
        private readonly Stack<LightSource> _pool = new Stack<LightSource>();
        private GameObject _prefab;
        private float _nextAttempt;

        public IReadOnlyList<LightSource> Active => _active;

        /// <summary>Lanterns that replaced the oldest one at the limit (tests).</summary>
        public int Replaced { get; private set; }

        /// <summary>A stage's lantern numbers: base chance per attempt, the cap with Luck, and how many at once.</summary>
        public readonly struct StageLanterns
        {
            public readonly float Chance, MaxChance;
            public readonly int Max;
            public StageLanterns(float chance, float maxChance, int max) { Chance = chance; MaxChance = maxChance; Max = max; }
        }

        private static readonly StageLanterns MadForest = new StageLanterns(0.10f, 0.50f, 10), InlaidLibrary = new StageLanterns(0.075f, 0.50f, 10),
            DairyPlant = new StageLanterns(0.20f, 0.60f, 10), GalloTower = new StageLanterns(0.075f, 0.50f, 10), CappellaMagna = new StageLanterns(0.20f, 0.80f, 20);

        /// <summary>The lantern numbers of a Soul Hunter level (1-10).</summary>
        public static StageLanterns ForStage(int stage)
        {
            switch (Mathf.Clamp(stage, 1, 10))
            {
                case 1: return MadForest;
                case 2: return InlaidLibrary;
                case 3: return DairyPlant;
                case 4: return GalloTower;
                default: return CappellaMagna;
            }
        }

        /// <summary>The stage being played (Stage 1's numbers outside a run).</summary>
        public static StageLanterns Current => ForStage(SoulHunter.Gameplay.Data.CampaignLevelCatalog.CurrentStage > 0 ? SoulHunter.Gameplay.Data.CampaignLevelCatalog.CurrentStage : 1);

        /// <summary>VS: stage chance x Luck, never above the cap (Mad Forest's numbers).</summary>
        public static float SpawnChance(float luck) => SpawnChance(MadForest, luck);

        public static float SpawnChance(StageLanterns stage, float luck) => Mathf.Min(stage.MaxChance, stage.Chance * Mathf.Max(0f, luck));

        private void Start()
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            _prefab = catalog != null ? catalog.SoulLantern : null;
            if (_prefab == null) Debug.LogWarning("[LightSourceSpawner] No Soul Lantern in GameContentCatalog; light sources are off.");
            _nextAttempt = Time.time + AttemptIntervalSeconds;
        }

        private void Update()
        {
            if (_prefab == null || Time.timeScale <= 0f) return;
            var player = PlayerController.Instance;
            if (player == null) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var lantern = _active[i];
                if (lantern == null) { _active.RemoveAt(i); continue; }
                bool far = (lantern.transform.position - player.transform.position).sqrMagnitude > RecycleDistance * RecycleDistance;
                if (!lantern.gameObject.activeSelf || far)
                {
                    lantern.gameObject.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Push(lantern);
                }
            }

            if (Time.time < _nextAttempt) return;
            _nextAttempt = Time.time + AttemptIntervalSeconds;
            var stage = Current;
            if (_active.Count >= stage.Max)
            {
                // VS: at the limit the chance is not raised by Luck; a new lantern replaces the oldest, nearest the player.
                if (Random.value >= stage.Chance) return;
                ReplaceOldest(player.transform.position);
                return;
            }
            float luck = player.Stats != null ? player.Stats.Luck : 1f;
            if (Random.value >= SpawnChance(stage, luck)) return;
            if (TryFindSpot(player.transform.position, out var spot)) Spawn(spot);
        }

        /// <summary>
        /// VS at the limit: the oldest lantern despawns and a new one appears at the off-screen spot nearest the player.
        /// Returns the new lantern, or null when no open spot was found (then nothing is removed).
        /// </summary>
        public LightSource ReplaceOldest(Vector3 playerPosition)
        {
            if (_active.Count == 0 || !TryFindSpot(playerPosition, out var near, nearest: true)) return null;
            var oldest = _active[0];
            _active.RemoveAt(0);
            if (oldest != null) { oldest.gameObject.SetActive(false); _pool.Push(oldest); }
            var lantern = Spawn(near);
            Replaced++;
            return lantern;
        }

        /// <summary>Places a lantern at a world position (the spawner's own attempts, and tests).</summary>
        public LightSource Spawn(Vector3 position)
        {
            if (_prefab == null) return null;
            var lantern = _pool.Count > 0 ? _pool.Pop() : Instantiate(_prefab).GetComponent<LightSource>();
            if (lantern == null) return null;
            lantern.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            lantern.gameObject.SetActive(true);
            _active.Add(lantern);
            return lantern;
        }

        /// <summary>
        /// VS: "barely off-screen". A point just outside the camera view (8% past a random screen edge) on the
        /// player's ground plane, not inside or against a stage obstacle.
        /// </summary>
        public static bool TryFindSpot(Vector3 playerPosition, out Vector3 spot, bool nearest = false)
        {
            var cam = Camera.main;
            var ground = new Plane(Vector3.up, playerPosition);
            bool found = false;
            Vector3 best = default;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 viewport;
                float along = Random.Range(-0.08f, 1.08f);
                // In a corridor only its two open ends are off-screen ground; top and bottom are behind the walls.
                switch (Random.Range(0, SoulHunter.Gameplay.Data.StageCorridor.Active ? 2 : 4))
                {
                    case 0: viewport = new Vector2(-0.08f, along); break;
                    case 1: viewport = new Vector2(1.08f, along); break;
                    case 2: viewport = new Vector2(along, -0.08f); break;
                    default: viewport = new Vector2(along, 1.08f); break;
                }
                if (cam != null)
                {
                    var ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
                    if (!ground.Raycast(ray, out float distance)) continue;
                    spot = ray.GetPoint(distance);
                }
                else
                {
                    // No camera (tests): a ring just outside a typical view.
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    spot = playerPosition + new Vector3(dir.x, 0f, dir.y) * 16f;
                }
                spot.y = playerPosition.y;
                spot = SoulHunter.Gameplay.Data.StageCorridor.Clamp(spot, 1.5f);
                if (!GroundCheck.IsOpen(spot, 0.8f)) continue;
                if (!nearest) return true;
                // VS at the limit: "a new one will spawn in a location closest to the player".
                if (!found || (spot - playerPosition).sqrMagnitude < (best - playerPosition).sqrMagnitude) best = spot;
                found = true;
            }
            spot = best;
            return found;
        }
    }
}
