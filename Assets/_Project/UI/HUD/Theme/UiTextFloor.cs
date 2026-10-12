using TMPro;
using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Keeps text at or over the readable floor (<see cref="UiScreen.MinTextUnits"/>, 12 sp on a touch screen), in one place
    /// for the screens that used to hold the same lines each. Call it again after a theme restyle or a rotation.
    /// </summary>
    public static class UiTextFloor
    {
        /// <summary>The smallest a fitted line may shrink to on a PC (canvas units).</summary>
        public const float PcMinimum = 14f;

        /// <summary>
        /// An auto-sized line never shrinks under max(<see cref="PcMinimum"/>, the floor), and its ceiling is never below
        /// that (the Arcana screen, the merchant's shop, the Omens wheel).
        /// </summary>
        public static void Apply(TMP_Text text)
        {
            if (text == null) return;
            float floor = Mathf.Max(PcMinimum, UiScreen.MinTextUnits);
            text.fontSizeMin = floor;
            if (text.fontSizeMax < floor) text.fontSizeMax = floor;
        }

        /// <summary>
        /// Every text under <paramref name="root"/>: auto-sized ones get the floor as their smallest size (keeping a larger
        /// one of their own), fixed ones are raised to it (character select, music selection).
        /// </summary>
        public static void RaiseAll(Transform root)
        {
            if (root == null) return;
            float floor = UiScreen.MinTextUnits;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = Mathf.Max(text.fontSizeMin, floor);
                    text.fontSizeMax = Mathf.Max(text.fontSizeMax, text.fontSizeMin);
                }
                else if (text.fontSize < floor) text.fontSize = floor;
            }
        }
    }
}
