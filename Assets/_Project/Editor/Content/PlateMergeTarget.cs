namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15): the Rune Plate (LightSourceImporter.BuildPlatePrefab, from
    /// ArtSource/Generators/build_light_source.py rune_plate()) is 19 renderers under "Visual": the stone Slab, the dark
    /// Inset, and 17 rune parts (RuneCore, Rune0-7, Spoke0-7) that all share Plate_Rune. Merged per material it draws
    /// 3 times: Slab and Inset stay as they are (one piece each, nothing to join), the 17 rune parts become one mesh.
    ///
    /// PressurePlate stays correct: in Awake it takes every Renderer under the plate whose sharedMaterial name contains
    /// "Rune" and dims it with a MaterialPropertyBlock (_EmissionColor, _BaseColor). It looks nothing up by object name
    /// or child index, and the merger joins only renderers with the same material, so the merged rune renderer has
    /// exactly one material, SH10_Prop_Plate_Rune: it is found, and dimming it dims all 17 rune parts at once, as before.
    /// No stone part uses a "Rune" material, so no stone is dimmed. Nothing needs Keep or Split.
    /// </summary>
    public static class PlateMergeTarget
    {
        public static readonly PropMeshMerger.Target Target = new PropMeshMerger.Target
        {
            PrefabPath = LightSourceImporter.PlatePrefabPath,
            Groups = root => One(root.transform.Find("Visual")),
            Keep = PropMeshMerger.KeepByName, // the plate has no GroundGlow; kept for the same rule as the lantern
        };

        private static System.Collections.Generic.IEnumerable<UnityEngine.Transform> One(UnityEngine.Transform t)
        {
            if (t != null) yield return t;
        }
    }
}
