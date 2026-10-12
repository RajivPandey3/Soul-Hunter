using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>Where a level's Soul Casket and relics lie, as offsets from the level's start.</summary>
    public static class UnlockSites
    {
        /// <summary>
        /// The wiki puts relics "to the far west / far south / far north of the starting area" (Grim_Grimoire, Ars_Gouda,
        /// Randomazzo, fetched 2026-10-12) and gives no distance. Balance method: beyond the stage items (55-85 units,
        /// StageItems.Distances), so "far" is further than anything else the level places; the casket lies further still.
        /// </summary>
        public const float RelicDistance = 95f, CasketDistance = 110f;

        public static Vector3 RelicOffset(in RelicInfo relic)
        {
            var direction = new Vector3(relic.DirX, 0f, relic.DirZ);
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            return direction.normalized * RelicDistance;
        }

        /// <summary>
        /// The casket's direction turns a quarter per level, 22.5 degrees off the compass points, so it never shares a line
        /// with a relic (compass points and one diagonal), a Rune Plate (compass points) or a stage item (diagonals).
        /// </summary>
        public static Vector3 CasketOffset(int level) =>
            Quaternion.Euler(0f, 22.5f + 90f * (Mathf.Clamp(level, 1, 10) - 1), 0f) * Vector3.forward * CasketDistance;

        /// <summary>In a corridor (Level 4) there is no north or south: the spot lies along it at the same distance, near a wall.</summary>
        public static Vector3 InCorridor(Vector3 offset, float halfWidth)
        {
            float side = Mathf.Abs(offset.x) > 0.01f ? Mathf.Sign(offset.x) : Mathf.Sign(offset.z);
            float across = Mathf.Abs(offset.z) > 0.01f ? Mathf.Sign(offset.z) * halfWidth * 0.45f : 0f;
            return new Vector3(side * offset.magnitude, 0f, across);
        }
    }

    /// <summary>
    /// Places each level's Soul Casket (CasketBook) and relics (RelicBook) when the level starts and removes the previous
    /// level's. A casket already opened and a relic already found on the save are not placed (wiki Relics: "do not appear
    /// again"). A relic with an appearing time (wiki Achievements: "Find the Apoplexy at minute 9:00") is placed when the
    /// level clock reaches it. GameSessionManager adds this, so every run has exactly one.
    /// </summary>
    public sealed class UnlockSiteSpawner : MonoBehaviour
    {
        private readonly List<RelicPickup> _relics = new List<RelicPickup>();
        private readonly List<RelicInfo> _waiting = new List<RelicInfo>();
        private SoulHunter.Gameplay.Core.LevelProgressionManager _progression;
        private Vector3 _origin;
        private int _placedFor;

        public SoulCasket Casket { get; private set; }
        public IReadOnlyList<RelicPickup> Relics => _relics;
        /// <summary>The level the sites were last placed for (0 = none yet).</summary>
        public int PlacedFor => _placedFor;

        private void Start()
        {
            _progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            if (_progression != null) _progression.OnStageStarted += HandleStageStarted;
            StartCoroutine(PlaceWhenReady(_progression != null ? _progression.CurrentStage : 1));
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnStageStarted -= HandleStageStarted;
        }

        private void HandleStageStarted(int stage) => StartCoroutine(PlaceWhenReady(stage));

        /// <summary>One frame later, after InfiniteMap has laid this level's floor (as StageItemSpawner does).</summary>
        private System.Collections.IEnumerator PlaceWhenReady(int stage)
        {
            yield return null;
            UnityEngine.Physics.SyncTransforms();
            Place(stage);
        }

        private static GameData SaveData()
        {
            return GameServices.Instance != null && GameServices.Instance.TryGet<SaveService>(out var save) ? save.CurrentData : null;
        }

        private Vector3 Spot(Vector3 offset)
        {
            if (SoulHunter.Gameplay.Data.StageCorridor.Active)
                offset = UnlockSites.InCorridor(offset, SoulHunter.Gameplay.Data.StageCorridor.HalfWidth);
            // VS Inverse turns the layout about the start, as it does for stage items (InverseMap).
            Vector3 spot = GroundCheck.NearestOpen(_origin + SoulHunter.Gameplay.Data.InverseMap.Layout(offset), 1.4f);
            spot.y = _origin.y;
            return spot;
        }

        /// <summary>Places the level's casket and relics around the hero's position now (also called by tests).</summary>
        public void Place(int stage)
        {
            Clear();
            var player = PlayerController.Instance;
            var data = SaveData();
            if (player == null || data == null) return;
            _placedFor = stage;
            _origin = player.transform.position;

            var casket = CasketBook.OnLevel(stage);
            if (casket.HasValue && !CasketBook.IsOpened(data, stage))
                Casket = SoulCasket.Create(casket.Value, Spot(UnlockSites.CasketOffset(stage)));

            float clock = _progression != null ? _progression.StageElapsedTime : 0f;
            foreach (var relic in RelicBook.OnLevel(stage))
            {
                if (RelicBook.IsFound(data, relic)) continue;
                if (relic.AppearsAtSeconds > clock) _waiting.Add(relic);
                else _relics.Add(RelicPickup.Create(relic, Spot(UnlockSites.RelicOffset(relic))));
            }
        }

        private void Update()
        {
            if (_waiting.Count == 0 || _progression == null || _progression.CurrentStage != _placedFor) return;
            float clock = _progression.StageElapsedTime;
            for (int i = _waiting.Count - 1; i >= 0; i--)
            {
                if (_waiting[i].AppearsAtSeconds > clock) continue;
                var relic = _waiting[i];
                _waiting.RemoveAt(i);
                _relics.Add(RelicPickup.Create(relic, Spot(UnlockSites.RelicOffset(relic))));
            }
        }

        /// <summary>Removes the previous level's casket and relics that were not taken.</summary>
        public void Clear()
        {
            if (Casket != null) Destroy(Casket.gameObject);
            Casket = null;
            foreach (var relic in _relics) if (relic != null) Destroy(relic.gameObject);
            _relics.Clear();
            _waiting.Clear();
        }
    }
}
