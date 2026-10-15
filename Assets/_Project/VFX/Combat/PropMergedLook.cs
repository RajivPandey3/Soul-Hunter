using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "sab ko single mesh or single material mein convert karo"): PropMeshMerger joins a
    /// prop's parts into one mesh drawn with the one shared SoulHunter/PropMerged material, with each part's colour,
    /// metallic, smoothness and glow baked into the mesh. This component, on that merged renderer, remembers which
    /// original materials went into it, and which two of them are the mesh's mask A and mask B parts, so scripts that
    /// found a part by its material's name (ChestLook's iron bands and soul seam, the chest preview's seam, the Soul
    /// Casket, PressurePlate's runes) still find and recolour it. Use <see cref="TryFind"/> and <see cref="Set"/> on any
    /// renderer: an unmerged one is handled per submesh exactly as before.
    /// </summary>
    public sealed class PropMergedLook : MonoBehaviour
    {
        public const string ShaderName = "SoulHunter/PropMerged";

        /// <summary>Every original material baked into this renderer's mesh.</summary>
        public Material[] Sources = new Material[0];
        /// <summary>The original material of the parts marked mask A (UV2.z = 1) and mask B (UV2.z = 2), or null.</summary>
        public Material MaskA;
        public Material MaskB;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int MaskABase = Shader.PropertyToID("_MaskA_Base");
        private static readonly int MaskAEmission = Shader.PropertyToID("_MaskA_Emission");
        private static readonly int MaskBBase = Shader.PropertyToID("_MaskB_Base");
        private static readonly int MaskBEmission = Shader.PropertyToID("_MaskB_Emission");

        /// <summary>One recolourable part: a submesh of an ordinary renderer, or a mask of a merged one.</summary>
        public struct Part
        {
            public Renderer Renderer;
            /// <summary>The submesh (ordinary renderer) or -1 (merged).</summary>
            public int Submesh;
            /// <summary>1 or 2 on a merged renderer, 0 on an ordinary one.</summary>
            public int Mask;
            /// <summary>The part's own original material: its colour and glow before any recolour.</summary>
            public Material Source;
        }

        /// <summary>True when this renderer draws a material whose name contains <paramref name="nameContains"/> (as a
        /// submesh, or as a merged mask part). <paramref name="index"/> picks among several matching submeshes.</summary>
        public static bool TryFind(Renderer renderer, string nameContains, out Part part, int index = 0)
        {
            part = default;
            if (renderer == null) return false;
            if (renderer.TryGetComponent(out PropMergedLook look))
            {
                int found = 0;
                for (int mask = 1; mask <= 2; mask++)
                {
                    var source = mask == 1 ? look.MaskA : look.MaskB;
                    if (source == null || !source.name.Contains(nameContains) || found++ != index) continue;
                    part = new Part { Renderer = renderer, Submesh = -1, Mask = mask, Source = source };
                    return true;
                }
                return false;
            }
            var materials = renderer.sharedMaterials;
            int seen = 0;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].name.Contains(nameContains)) continue;
                if (seen++ != index) continue;
                part = new Part { Renderer = renderer, Submesh = i, Mask = 0, Source = materials[i] };
                return true;
            }
            return false;
        }

        /// <summary>True when the renderer draws such a material in any form (merged or not).</summary>
        public static bool Draws(Renderer renderer, string nameContains)
        {
            if (renderer == null) return false;
            if (renderer.TryGetComponent(out PropMergedLook look))
                foreach (var m in look.Sources) if (m != null && m.name.Contains(nameContains)) return true;
            foreach (var m in renderer.sharedMaterials) if (m != null && m.name.Contains(nameContains)) return true;
            return false;
        }

        /// <summary>
        /// Recolours a part through a property block, as the scripts did on the old materials: <paramref name="baseColour"/>
        /// and <paramref name="emission"/> (null leaves that one as it was). Colours are given as they would be on the
        /// URP Lit material (_BaseColor, _EmissionColor). Other parts and other properties already set are kept.
        /// </summary>
        public static void Set(Part part, MaterialPropertyBlock block, Color? baseColour, Color? emission)
        {
            if (part.Renderer == null) return;
            if (part.Mask == 0)
            {
                part.Renderer.GetPropertyBlock(block, part.Submesh);
                if (baseColour.HasValue) block.SetColor(BaseColor, baseColour.Value);
                if (emission.HasValue) block.SetColor(EmissionColor, emission.Value);
                part.Renderer.SetPropertyBlock(block, part.Submesh);
                return;
            }
            part.Renderer.GetPropertyBlock(block);
            if (baseColour.HasValue) { var c = baseColour.Value; c.a = 1f; block.SetColor(part.Mask == 1 ? MaskABase : MaskBBase, c); }
            if (emission.HasValue) { var e = emission.Value; e.a = 1f; block.SetColor(part.Mask == 1 ? MaskAEmission : MaskBEmission, e); }
            part.Renderer.SetPropertyBlock(block);
        }

        /// <summary>Puts a part back to its own colour and glow.</summary>
        public static void Clear(Part part, MaterialPropertyBlock block)
        {
            if (part.Renderer == null) return;
            if (part.Mask == 0) { block.Clear(); part.Renderer.SetPropertyBlock(block, part.Submesh); return; }
            part.Renderer.GetPropertyBlock(block);
            block.SetColor(part.Mask == 1 ? MaskABase : MaskBBase, Color.clear);
            block.SetColor(part.Mask == 1 ? MaskAEmission : MaskBEmission, Color.clear);
            part.Renderer.SetPropertyBlock(block);
        }

        /// <summary>The part's colour and glow now: the property block's if one is set, else its original material's.</summary>
        public static Color CurrentBase(Part part, MaterialPropertyBlock block)
        {
            if (part.Mask == 0)
            {
                part.Renderer.GetPropertyBlock(block, part.Submesh);
                return block.isEmpty || !block.HasColor(BaseColor) ? part.Source.GetColor(BaseColor) : block.GetColor(BaseColor);
            }
            part.Renderer.GetPropertyBlock(block);
            int id = part.Mask == 1 ? MaskABase : MaskBBase;
            if (block.HasColor(id)) { var c = block.GetColor(id); if (c.a > 0f) return new Color(c.r, c.g, c.b, part.Source.GetColor(BaseColor).a); }
            return part.Source.GetColor(BaseColor);
        }

        public static Color CurrentEmission(Part part, MaterialPropertyBlock block)
        {
            if (part.Mask == 0)
            {
                part.Renderer.GetPropertyBlock(block, part.Submesh);
                return block.isEmpty || !block.HasColor(EmissionColor) ? part.Source.GetColor(EmissionColor) : block.GetColor(EmissionColor);
            }
            part.Renderer.GetPropertyBlock(block);
            int id = part.Mask == 1 ? MaskAEmission : MaskBEmission;
            if (block.HasColor(id)) { var c = block.GetColor(id); if (c.a > 0f) return new Color(c.r, c.g, c.b, part.Source.GetColor(EmissionColor).a); }
            return part.Source.GetColor(EmissionColor);
        }
    }
}
