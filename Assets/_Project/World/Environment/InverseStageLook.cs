using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// VS Inverse look (release item P3-05; wiki Stages, Inverse Mode: "the map becomes darker and different in hue"; Options,
    /// Gameplay: "Visually Invert Stages"). While <see cref="InverseMap.Active"/>, every floor tile InfiniteMap lays is drawn
    /// with <see cref="InverseMap.Grade"/> (darker, hue turned, inside the owner's ground comfort band), through a property
    /// block per material so the shared floor materials never change. Turning the option off (or a run without Inverse)
    /// clears the blocks. The ambient light and fog are never touched, so Level 2's darkness mechanic is unchanged.
    /// GameSessionManager adds this to itself.
    /// </summary>
    public sealed class InverseStageLook : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private InfiniteMap _map;
        private MaterialPropertyBlock _block;
        /// <summary>Each tile seen, with its renderers and whether they now carry the inverted grade.</summary>
        private readonly Dictionary<GameObject, (Renderer[] Renderers, bool Graded)> _tiles =
            new Dictionary<GameObject, (Renderer[], bool)>();
        private bool _dirty;

        public static InverseStageLook Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnEnable() => SoulHunter.Core.Persistence.GameSettings.Changed += MarkDirty;

        private void OnDisable() => SoulHunter.Core.Persistence.GameSettings.Changed -= MarkDirty;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void MarkDirty() => _dirty = true;

        /// <summary>True when <paramref name="renderer"/>'s tile carries the inverted grade (tests).</summary>
        public bool IsGraded(Renderer renderer)
        {
            foreach (var tile in _tiles.Values)
                if (System.Array.IndexOf(tile.Renderers, renderer) >= 0) return tile.Graded;
            return false;
        }

        /// <summary>Applies the look to every tile now (tests, and after an option change).</summary>
        public void Refresh()
        {
            _dirty = true;
            LateUpdate();
        }

        // After InfiniteMap: a new stage lays new tiles, and an option change (Disable Blood) may swap their materials first.
        private void LateUpdate()
        {
            if (_map == null) _map = FindFirstObjectByType<InfiniteMap>();
            if (_map == null) return;
            bool active = InverseMap.Active;
            if (_dirty)
            {
                _dirty = false;
                var keys = new List<GameObject>(_tiles.Keys);
                foreach (var key in keys)
                {
                    if (key == null) { _tiles.Remove(key); continue; }
                    var tile = _tiles[key];
                    tile.Graded = Apply(tile.Renderers, active);
                    _tiles[key] = tile;
                }
            }
            for (int x = 0; x < InfiniteMap.GridSize; x++)
            for (int z = 0; z < InfiniteMap.GridSize; z++)
            {
                var chunk = _map.ChunkAt(x, z);
                if (chunk == null) continue;
                if (_tiles.TryGetValue(chunk, out var known) && known.Graded == active) continue;
                var renderers = known.Renderers ?? chunk.GetComponentsInChildren<Renderer>(true);
                _tiles[chunk] = (renderers, Apply(renderers, active));
            }
            if (_tiles.Count > InfiniteMap.GridSize * InfiniteMap.GridSize * 2) ForgetDestroyedTiles(); // tiles of earlier stages
        }

        private void ForgetDestroyedTiles()
        {
            var gone = new List<GameObject>();
            foreach (var key in _tiles.Keys) if (key == null) gone.Add(key);
            foreach (var key in gone) _tiles.Remove(key);
        }

        /// <summary>Grades (or clears) every material slot of the renderers. Returns <paramref name="graded"/>.</summary>
        private bool Apply(Renderer[] renderers, bool graded)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    _block.Clear();
                    if (graded && material != null)
                    {
                        if (material.HasProperty(BaseColorId)) _block.SetColor(BaseColorId, InverseMap.Grade(material.GetColor(BaseColorId)));
                        if (material.HasProperty(ColorId)) _block.SetColor(ColorId, InverseMap.Grade(material.GetColor(ColorId)));
                    }
                    renderer.SetPropertyBlock(_block, i);
                }
            }
            return graded;
        }
    }
}
