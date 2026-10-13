#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Gives every UpgradeData without an icon one from the project's own Assets/Art/UI sprites,
    /// so the character select cards and the HUD weapon slots show what each weapon/passive is.
    /// Menu: Soul Hunter/Content/Assign Missing Upgrade Icons, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.UpgradeIconAssigner.AssignFromCommandLine -quit
    /// </summary>
    public static class UpgradeIconAssigner
    {
        /// <summary>Icon sprite (Assets/Art/UI/sprite_N.png) for each upgrade type.</summary>
        public static readonly Dictionary<UpgradeData.UpgradeType, int> IconSprites = new Dictionary<UpgradeData.UpgradeType, int>
        {
            // Weapons
            { UpgradeData.UpgradeType.MagicWand, 37 },
            { UpgradeData.UpgradeType.Whip, 40 },
            { UpgradeData.UpgradeType.Garlic, 45 },
            { UpgradeData.UpgradeType.Axe, 34 },
            { UpgradeData.UpgradeType.Bible, 112 },
            { UpgradeData.UpgradeType.Cross, 39 },
            { UpgradeData.UpgradeType.LightningRing, 87 },
            { UpgradeData.UpgradeType.FireWand, 38 },
            { UpgradeData.UpgradeType.SantaWater, 103 },
            { UpgradeData.UpgradeType.Knife, 32 },
            { UpgradeData.UpgradeType.Runetracer, 107 },
            { UpgradeData.UpgradeType.Pentagram, 92 },
            { UpgradeData.UpgradeType.ClockLancet, 109 },
            { UpgradeData.UpgradeType.Laurel, 89 },
            { UpgradeData.UpgradeType.Bone, 48 },
            { UpgradeData.UpgradeType.Peachone, 36 },
            { UpgradeData.UpgradeType.SongOfMana, 90 },
            { UpgradeData.UpgradeType.CherryBomb, 88 },
            { UpgradeData.UpgradeType.Gun, 93 },
            // Passives
            { UpgradeData.UpgradeType.PlayerSpeed, 113 },
            { UpgradeData.UpgradeType.MaxHealth, 98 },
            { UpgradeData.UpgradeType.EmptyTome, 110 },
            { UpgradeData.UpgradeType.Spinach, 105 },
            { UpgradeData.UpgradeType.Bracer, 101 },
            { UpgradeData.UpgradeType.Candelabrador, 77 },
            { UpgradeData.UpgradeType.Spellbinder, 12 },
            { UpgradeData.UpgradeType.Duplicator, 11 },
            { UpgradeData.UpgradeType.Armor, 104 },
            { UpgradeData.UpgradeType.Pummarola, 106 },
            { UpgradeData.UpgradeType.Attractorb, 44 },
            { UpgradeData.UpgradeType.Clover, 16 },
            { UpgradeData.UpgradeType.Crown, 97 },
            { UpgradeData.UpgradeType.SkullOManiac, 102 },
            { UpgradeData.UpgradeType.Tiragisu, 35 },
            // Added 2026-10-12: placeholders, each a related item's icon (not owner-approved art).
            { UpgradeData.UpgradeType.MisersIdol, 97 },       // Crown's
            { UpgradeData.UpgradeType.RingOfLongHours, 109 }, // Clock Lancet's
            { UpgradeData.UpgradeType.RingOfDarkHours, 102 }, // Skull O'Maniac's
            { UpgradeData.UpgradeType.CursedCoffer, 102 },    // Skull O'Maniac's
            { UpgradeData.UpgradeType.DuskRaven, 36 },        // Peachone's
            { UpgradeData.UpgradeType.AzureRepeater, 93 },    // Gun's
            { UpgradeData.UpgradeType.TwilightFlock, 36 },    // Peachone's
        };

        [MenuItem("Soul Hunter/Content/Assign Missing Upgrade Icons")]
        public static void AssignFromCommandLine()
        {
            int assigned = AssignUpgradeIcons();
            AssetDatabase.SaveAssets();
            Debug.Log($"[UpgradeIconAssigner] Assigned {assigned} upgrade icons.");
        }

        public static int AssignUpgradeIcons()
        {
            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:UpgradeData"))
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (upgrade == null || upgrade.Icon != null || !IconSprites.TryGetValue(upgrade.Type, out int index)) continue;
                var sprite = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/UI/sprite_{index}.png").OfType<Sprite>().FirstOrDefault();
                if (sprite == null) { Debug.LogWarning($"[UpgradeIconAssigner] Missing icon sprite_{index} for {upgrade.Type}."); continue; }
                upgrade.Icon = sprite;
                EditorUtility.SetDirty(upgrade);
                assigned++;
            }
            return assigned;
        }
    }
}
#endif
