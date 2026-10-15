using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// CPU audit (2026-10-14): enemy tints and the hit flash were property blocks, and a renderer with a property block
    /// leaves the SRP Batcher, so the tinted horde paid a SetPass per material per enemy (450-1,050 a frame late in a
    /// level). Enemy models now draw with one SoulHunter/HordeMerged material (HordeMeshMerger), and a tint is a shared
    /// copy of that material with _Color set and _TintAmount 1: one copy per base material and colour, made once and
    /// reused by every enemy of that look, so the SRP Batcher batches them all.
    /// </summary>
    public static class HordeMaterials
    {
        public const string ShaderName = "SoulHunter/HordeMerged";
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");

        // The hit flash is brighter than 1, so the key keeps colours above 1 apart (Color32 would clamp them to white).
        private static readonly Dictionary<(int, Vector4Int), Material> Tinted = new Dictionary<(int, Vector4Int), Material>();
        private readonly struct Vector4Int : System.IEquatable<Vector4Int>
        {
            private readonly int r, g, b, a;
            public Vector4Int(Color c) { r = Mathf.RoundToInt(c.r * 512); g = Mathf.RoundToInt(c.g * 512); b = Mathf.RoundToInt(c.b * 512); a = Mathf.RoundToInt(c.a * 512); }
            public bool Equals(Vector4Int o) => r == o.r && g == o.g && b == o.b && a == o.a;
            public override bool Equals(object o) => o is Vector4Int v && Equals(v);
            public override int GetHashCode() => ((r * 397 ^ g) * 397 ^ b) * 397 ^ a;
        }
        private static Vector4Int Key(Color c) => new Vector4Int(c);
        private static readonly Dictionary<Material, Material> BaseOf = new Dictionary<Material, Material>();

        /// <summary>True when the material is a horde material (or a tinted copy of one).</summary>
        public static bool IsHorde(Material material) => material != null && material.HasProperty(TintAmountId);

        /// <summary>The untinted material a tinted copy was made from (the material itself when it is not a copy).</summary>
        public static Material BaseFor(Material material) =>
            material != null && BaseOf.TryGetValue(material, out var b) ? b : material;

        /// <summary>The shared copy of <paramref name="baseMaterial"/> drawn in <paramref name="tint"/>.</summary>
        public static Material Tint(Material baseMaterial, Color tint)
        {
            baseMaterial = BaseFor(baseMaterial);
            var key = (baseMaterial.GetInstanceID(), Key(tint));
            if (Tinted.TryGetValue(key, out var material) && material != null) return material;
            material = new Material(baseMaterial) { name = baseMaterial.name + " (tint)", hideFlags = HideFlags.DontSave };
            material.SetColor(ColorId, tint);
            material.SetFloat(TintAmountId, 1f);
            Tinted[key] = material;
            BaseOf[material] = baseMaterial;
            return material;
        }

        private static readonly Dictionary<(int, Vector4Int), Material> Shaded = new Dictionary<(int, Vector4Int), Material>();

        /// <summary>
        /// The shared copy of <paramref name="baseMaterial"/> with the model's own colours multiplied by <paramref name="shade"/>
        /// (Death's Shadow darkens its body this way, keeping each part's colour).
        /// </summary>
        public static Material Shade(Material baseMaterial, Color shade)
        {
            baseMaterial = BaseFor(baseMaterial);
            var key = (baseMaterial.GetInstanceID(), Key(shade));
            if (Shaded.TryGetValue(key, out var material) && material != null) return material;
            material = new Material(baseMaterial) { name = baseMaterial.name + " (shade)", hideFlags = HideFlags.DontSave };
            material.SetColor(ColorId, shade);
            material.SetFloat(TintAmountId, 0f);
            Shaded[key] = material;
            BaseOf[material] = baseMaterial;
            return material;
        }

        /// <summary>How many tinted copies exist (tests).</summary>
        public static int TintedCount => Tinted.Count;
    }
}
