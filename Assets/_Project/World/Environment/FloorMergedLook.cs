using UnityEngine;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// The two looks a merged stage floor (shader SoulHunter/FloorMerged, built by FloorMeshMerger) can switch to, as
    /// global shader values so the 25 tiles keep one material and the SRP Batcher: the Inverse grade (InverseStageLook)
    /// and VS "Disable blood" (InfiniteMap), which draws the Blood Arenas stains as the plain floor. Both tints are baked
    /// into the mesh, so switching costs nothing.
    /// </summary>
    public static class FloorMergedLook
    {
        public const string ShaderName = "SoulHunter/FloorMerged";
        private static readonly int InverseId = Shader.PropertyToID("_SH_FloorInverse");
        private static readonly int HideBloodId = Shader.PropertyToID("_SH_FloorHideBlood");

        public static bool Inverse { get; private set; }
        public static bool HideBlood { get; private set; }

        public static void SetInverse(bool on)
        {
            Inverse = on;
            Shader.SetGlobalFloat(InverseId, on ? 1f : 0f);
        }

        public static void SetHideBlood(bool on)
        {
            HideBlood = on;
            Shader.SetGlobalFloat(HideBloodId, on ? 1f : 0f);
        }

        /// <summary>True when the renderer draws with the merged floor material.</summary>
        public static bool IsMerged(Renderer renderer)
        {
            var material = renderer != null ? renderer.sharedMaterial : null;
            return material != null && material.shader != null && material.shader.name == ShaderName;
        }
    }
}
