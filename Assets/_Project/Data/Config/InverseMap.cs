using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// VS Inverse map (release item P3-05). vampire.survivors.wiki/w/Stages, "Inverse Mode" (raw page re-fetched
    /// 2026-10-09): "In Inverse Mode, the stage's layout is rotated by 180°, and the map becomes darker and different in hue.
    /// On certain stages, stage items may be placed differently due to the inverted layout. The layout inversion effect can
    /// be toggled in the Settings, using the "Visually Invert Stages" option in the Gameplay section."
    ///
    /// Soul Hunter's floors are endless seamless tiles that already lie at a random quarter turn each (InfiniteMap), so
    /// turning the floor itself 180 degrees would look the same. The closest faithful equivalent is:
    /// - the layout laid out from the start is turned 180 degrees about the start: stage items, the Metaglios and the Rune
    ///   Plates (StageItemSpawner, through <see cref="Layout"/>); the Castle corridor still runs along X, so its items swap ends;
    /// - the floor is drawn darker and in a different hue (InverseStageLook, through <see cref="Grade"/>), kept inside the
    ///   owner's ground eye-comfort band (2026-10-02), so a 30-minute stage never turns near-black.
    /// Nothing touches the ambient light or fog, so Level 2's growing darkness (the GDD mechanic, CampaignSignatureSystem)
    /// works as before. The Inverse numbers (RunModes) do not depend on the option: it changes only how the stage looks and is
    /// laid out, as the wiki names it ("Visually Invert Stages").
    /// </summary>
    public static class InverseMap
    {
        /// <summary>
        /// "Different in hue": the wiki gives no number. Balance method (2026-10-09): half a turn of the colour wheel, the
        /// hue's own inverse, so every level's ground reads as its inverted twin (green moss turns violet, sand turns blue)
        /// while keeping its order of light and dark. Floor saturation is already capped at 0.55 x 0.9 by the comfort grade,
        /// so the turned colours stay calm.
        /// </summary>
        public const float HueShift = 0.5f;

        /// <summary>
        /// "Darker": the wiki gives no number. Balance method: 80% of the colour's value, the smallest step that reads as
        /// clearly darker side by side, never below the comfort band's floor (<see cref="ComfortMinTint"/>).
        /// </summary>
        public const float Darken = 0.8f;

        /// <summary>
        /// The owner's ground eye-comfort band (StageFloorImporter.ComfortMin/ComfortMax 0.30-0.68, 2026-10-02). The floor
        /// material's base colour is that grade divided by the detail texture's mean (StageFloorImporter.TextureMean 0.8), so
        /// in base-colour terms the floor of the band is 0.30 / 0.8 = 0.375. These copy the editor importer's numbers
        /// (runtime code cannot reference the editor assembly); InverseMapTests pins them.
        /// </summary>
        public const float ComfortMin = 0.30f, ComfortMax = 0.68f, TextureMean = 0.8f;
        public const float ComfortMinTint = ComfortMin / TextureMean;

        /// <summary>True while the inverted look and layout apply: Inverse mode on and "Visually Invert Stages" on.</summary>
        public static bool Active => RunModes.Inverse && SoulHunter.Core.Persistence.GameSettings.VisuallyInvertStages;

        /// <summary>An offset from the stage's start, turned 180 degrees about the start while <see cref="Active"/>.</summary>
        public static Vector3 Layout(Vector3 offset) => Active ? Turn(offset) : offset;

        /// <summary>The 180-degree turn on the ground plane (height kept).</summary>
        public static Vector3 Turn(Vector3 offset) => new Vector3(-offset.x, offset.y, -offset.z);

        /// <summary>
        /// The inverted floor colour of a floor material's base colour: the hue turned by <see cref="HueShift"/> and the
        /// value darkened by <see cref="Darken"/>, kept at or above the comfort band's floor (a colour already below it
        /// keeps its value). Saturation and alpha are kept.
        /// </summary>
        public static Color Grade(Color baseColour)
        {
            Color.RGBToHSV(baseColour, out float h, out float s, out float v);
            h = Mathf.Repeat(h + HueShift, 1f);
            float darker = v * Darken;
            v = v < ComfortMinTint ? v : Mathf.Max(darker, ComfortMinTint);
            var graded = Color.HSVToRGB(h, s, v);
            graded.a = baseColour.a;
            return graded;
        }
    }
}
