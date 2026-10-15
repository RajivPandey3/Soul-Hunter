using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Every baked horde model (HordeVatAsset), kept in Resources so the game can find a body's VAT by the skinned mesh it
    /// draws today. Written by HordeVatBaker (menu Soul Hunter/Content/Bake Horde VAT).
    /// </summary>
    public sealed class HordeVatLibrary : ScriptableObject
    {
        public const string ResourceName = "HordeVatLibrary";
        public const string AssetPath = "Assets/Resources/" + ResourceName + ".asset";

        public HordeVatAsset[] Assets = System.Array.Empty<HordeVatAsset>();

        private Dictionary<Mesh, HordeVatAsset> _byMesh;

        /// <summary>The baked model for this skinned mesh, or null.</summary>
        public HordeVatAsset For(Mesh sourceMesh)
        {
            if (sourceMesh == null) return null;
            if (_byMesh == null)
            {
                _byMesh = new Dictionary<Mesh, HordeVatAsset>();
                foreach (var asset in Assets)
                    if (asset != null && asset.SourceMesh != null && asset.Mesh != null && asset.Materials.Length > 0) _byMesh[asset.SourceMesh] = asset;
            }
            return _byMesh.TryGetValue(sourceMesh, out var found) ? found : null;
        }

        private static HordeVatLibrary _loaded;
        private static bool _tried;

        /// <summary>The library in Resources, or null before the first bake.</summary>
        public static HordeVatLibrary Load()
        {
            if (!_tried) { _tried = true; _loaded = Resources.Load<HordeVatLibrary>(ResourceName); }
            return _loaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _loaded = null; _tried = false; }
    }
}
