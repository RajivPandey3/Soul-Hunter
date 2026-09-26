using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS light-source spawning (vampire.survivors.wiki, Light source): once a second the game tries to place one
    /// just off-screen. The chance is the stage's base chance x Luck, capped, and only a limited number exist at once.
    /// Soul Hunter uses the Mad Forest numbers on every stage for now (10% base, 50% cap, 10 at once; provisional).
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

        /// <summary>VS: stage chance x Luck, never above the cap.</summary>
        public static float SpawnChance(float luck) => Mathf.Min(MaxChance, BaseChance * Mathf.Max(0f, luck));

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
            if (_active.Count >= MaxActive) return;
            float luck = player.Stats != null ? player.Stats.Luck : 1f;
            if (Random.value >= SpawnChance(luck)) return;
            if (TryFindSpot(player.transform.position, out var spot)) Spawn(spot);
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
        public static bool TryFindSpot(Vector3 playerPosition, out Vector3 spot)
        {
            var cam = Camera.main;
            var ground = new Plane(Vector3.up, playerPosition);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 viewport;
                float along = Random.Range(-0.08f, 1.08f);
                switch (Random.Range(0, 4))
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
                if (GroundCheck.IsOpen(spot, 0.8f)) return true;
            }
            spot = default;
            return false;
        }
    }
}
