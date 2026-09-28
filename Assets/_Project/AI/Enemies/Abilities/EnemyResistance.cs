using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// VS resistance rules as pure decisions (vampire.survivors.wiki/w/Enemies, "Resistances", read 2026-09-28):
    /// <list type="bullet">
    /// <item>Freeze: "If the enemy's freeze resistance is higher than freeze chance of the weapon they were hit by it
    /// will not be frozen. Freeze from Orologion goes through freeze resistance."</item>
    /// <item>Instant kill: "immunity to effects that instantly defeat an enemy ... applied by Pentagram, Gorgeous Moon and
    /// Rosary" (Soul Hunter: Pentagram and the Soul Toll pickup). Stage bosses stay immune as before.</item>
    /// <item>Debuff: "the knockback and freeze resistance reductions from Garlic and Soul Eater" are blocked.
    /// Garlic (vampire.survivors.wiki/w/Garlic): "each hit applies +0.3 knockback ... and -0.1 to freeze resistance
    /// until it reaches 0".</item>
    /// </list>
    /// </summary>
    public static class EnemyResistance
    {
        /// <summary>VS Garlic: -0.1 freeze resistance per hit, down to 0.</summary>
        public const float GarlicFreezeResistanceStep = 0.1f;

        /// <summary>Pentagram and Soul Toll skip this enemy.</summary>
        public static bool ResistsInstantKill(bool isStageBoss, bool instantKillResistance) =>
            isStageBoss || instantKillResistance;

        /// <summary>A weapon freeze (Clock Lancet) lands unless the resistance is higher than the weapon's freeze chance.</summary>
        public static bool WeaponFreezes(float freezeResistance, float weaponFreezeChance) =>
            !(freezeResistance > weaponFreezeChance);

        /// <summary>Orologion (Time Freeze pickup) goes through freeze resistance.</summary>
        public static bool OrologionFreezes(float freezeResistance) => true;

        /// <summary>Garlic's debuffs (more knockback taken, less freeze resistance) land unless the enemy resists debuffs.</summary>
        public static bool GarlicDebuffApplies(bool debuffResistance) => !debuffResistance;

        /// <summary>Freeze resistance after one Garlic hit.</summary>
        public static float AfterGarlicHit(float freezeResistance) =>
            Mathf.Max(0f, freezeResistance - GarlicFreezeResistanceStep);
    }
}
