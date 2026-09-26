using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Lays the corridor walls of a corridor stage (StageCorridor; the Ruined Castle, as VS's Inlaid Library): a row of
    /// 20-unit castle wall modules along each side, kept around the player as it walks left or right, so the hallway
    /// never ends. The walls block only the player (Obstacle layer).
    /// </summary>
    public class CorridorWalls : MonoBehaviour
    {
        public const float ModuleLength = 20f;
        public const int ModulesEachWay = 3; // 7 modules a side: 140 units, well past the screen

        private readonly List<Transform> _north = new List<Transform>();
        private readonly List<Transform> _south = new List<Transform>();
        private int _builtVersion = -1;
        private int _centreModule = int.MinValue;
        private GameObject _prefab;

        public IReadOnlyList<Transform> NorthWalls => _north;
        public IReadOnlyList<Transform> SouthWalls => _south;

        private void Update()
        {
            if (_builtVersion != StageCorridor.Version)
            {
                _builtVersion = StageCorridor.Version;
                Rebuild();
            }
            if (!StageCorridor.Active || _north.Count == 0) return;
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            if (player == null) return;
            int centre = Mathf.RoundToInt(player.transform.position.x / ModuleLength);
            if (centre != _centreModule) Lay(centre);
        }

        private void Rebuild()
        {
            foreach (var wall in _north) if (wall != null) Destroy(wall.gameObject);
            foreach (var wall in _south) if (wall != null) Destroy(wall.gameObject);
            _north.Clear();
            _south.Clear();
            _centreModule = int.MinValue;
            if (!StageCorridor.Active) return;

            if (_prefab == null)
            {
                var catalog = GameContentCatalog.Load();
                _prefab = catalog != null ? catalog.CastleWall : null;
            }
            if (_prefab == null)
            {
                Debug.LogWarning("[CorridorWalls] No CastleWall in GameContentCatalog; the corridor has no walls.");
                return;
            }
            for (int i = -ModulesEachWay; i <= ModulesEachWay; i++)
            {
                _north.Add(Instantiate(_prefab, transform).transform);
                _south.Add(Instantiate(_prefab, transform).transform);
            }
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            Lay(player != null ? Mathf.RoundToInt(player.transform.position.x / ModuleLength) : 0);
        }

        /// <summary>Places the modules end to end around module index <paramref name="centre"/>, on the floor (y 0).</summary>
        private void Lay(int centre)
        {
            _centreModule = centre;
            float thickness = 1.4f;
            float northZ = StageCorridor.CentreZ + StageCorridor.HalfWidth + thickness / 2f;
            float southZ = StageCorridor.CentreZ - StageCorridor.HalfWidth - thickness / 2f;
            for (int i = 0; i < _north.Count; i++)
            {
                float x = (centre - ModulesEachWay + i) * ModuleLength;
                _north[i].SetPositionAndRotation(new Vector3(x, 0f, northZ), Quaternion.identity);
                _south[i].SetPositionAndRotation(new Vector3(x, 0f, southZ), Quaternion.Euler(0f, 180f, 0f));
            }
            UnityEngine.Physics.SyncTransforms();
        }

        private void OnDestroy()
        {
            // A fresh run starts open until its spawner sets the stage (a corridor must not leak across runs).
            StageCorridor.End();
        }
    }
}
