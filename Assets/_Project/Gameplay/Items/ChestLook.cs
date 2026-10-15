using SoulHunter.Gameplay.VFX;
using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>The three chest looks Soul Hunter shows (VS wiki Treasure_Chest, "Types": bronze, silver, Arcana).</summary>
    public enum ChestKind { Bronze, Silver, Arcana }

    /// <summary>
    /// VS (vampire.survivors.wiki/w/Treasure_Chest, fetched 2026-10-01): "Bronze chests are usually incapable of providing
    /// weapon evolutions ... typically obtained as drops from bosses spawned before 10:00"; "Silver chests are capable of
    /// providing weapon evolutions and unions"; Arcana chests give an Arcana. Soul Hunter keeps its approved Soul Reliquary
    /// model (owner, 2026-10-01: "Pehle approved VFX se") and only recolours it: the iron bands turn bronze or silver,
    /// and an Arcana chest's soul seam glows violet. The colours are Soul Hunter's own and not yet owner-approved.
    /// Golden and dark chests are not built (Soul Hunter has no chest that gives unowned weapons, nor dark chests).
    /// </summary>
    public static class ChestLook
    {
        public static readonly Color BronzeBands = new Color(0.55f, 0.32f, 0.13f);
        public static readonly Color SilverBands = new Color(0.78f, 0.8f, 0.86f);
        public static readonly Color ArcanaBands = new Color(0.3f, 0.16f, 0.42f);
        public static readonly Color ArcanaSoul = new Color(0.75f, 0.35f, 1f);
        public static readonly Color ArcanaSoulGlow = new Color(0.55f, 0.2f, 1f);

        public static ChestKind KindFor(bool canEvolve, bool arcana) =>
            arcana ? ChestKind.Arcana : canEvolve ? ChestKind.Silver : ChestKind.Bronze;

        public static Color BandsFor(ChestKind kind) =>
            kind == ChestKind.Bronze ? BronzeBands : kind == ChestKind.Silver ? SilverBands : ArcanaBands;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static MaterialPropertyBlock _block;

        /// <summary>
        /// Recolours every chest renderer under <paramref name="root"/>; the shared materials are untouched. An ordinary
        /// renderer is recoloured per submesh as before. A merged one (PropMeshMerger, one PropMerged material with a
        /// PropMergedLook) holds its iron bands as mask A and its soul seam as mask B in one mesh, so its one property
        /// block is cleared once and then both parts are set on it (clearing per part would undo the other).
        /// </summary>
        public static void Apply(Transform root, ChestKind kind)
        {
            if (root == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.TryGetComponent(out PropMergedLook _))
                {
                    _block.Clear();
                    renderer.SetPropertyBlock(_block);
                    if (PropMergedLook.TryFind(renderer, "Chest_Iron", out var iron))
                        PropMergedLook.Set(iron, _block, BandsFor(kind), null);
                    if (kind == ChestKind.Arcana && PropMergedLook.TryFind(renderer, "Chest_Soul", out var soul))
                        PropMergedLook.Set(soul, _block, ArcanaSoul, ArcanaSoulGlow * 2f);
                    continue;
                }
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    _block.Clear();
                    if (material.name.Contains("Chest_Iron"))
                        _block.SetColor(BaseColor, BandsFor(kind));
                    else if (material.name.Contains("Chest_Soul") && kind == ChestKind.Arcana)
                    {
                        _block.SetColor(BaseColor, ArcanaSoul);
                        _block.SetColor(EmissionColor, ArcanaSoulGlow * 2f);
                    }
                    renderer.SetPropertyBlock(_block, i);
                }
            }
        }

        /// <summary>The band colour the chest shows (tests): the property block's, or the material's own (merged or not).</summary>
        public static Color BandColour(Transform root)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (PropMergedLook.TryFind(renderer, "Chest_Iron", out var part))
                    return PropMergedLook.CurrentBase(part, _block);
            return Color.clear;
        }

        /// <summary>The soul seam's glow (tests): the property block's, or the material's own (merged or not).</summary>
        public static Color SoulGlow(Transform root)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (PropMergedLook.TryFind(renderer, "Chest_Soul", out var part))
                    return PropMergedLook.CurrentEmission(part, _block);
            return Color.clear;
        }
    }
}
