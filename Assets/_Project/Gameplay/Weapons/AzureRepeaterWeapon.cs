using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Azure Repeater: Soul Hunter's second gun (own name, 2026-10-12; not yet owner-approved), built on VS Eight The
    /// Sparrow (vampire.survivors.wiki/w/Eight_The_Sparrow, raw, fetched 2026-10-12). Its infobox and level table are
    /// the first gun's (Phiera Der Tuphello): 5 damage, 1.4 s cooldown, 0.2 s interval, knockback 0.1, 1 bullet, pierce
    /// 1; L2 +1 bullet, L3 +2 pierce, L4 +1 bullet, L5 +5 damage, L6 +2 pierce, L7 +5 damage and Speed +50%, L8 +2
    /// pierce; "Each point in Amount grants an additional bullet in all four directions". In VS it "shoots a circular
    /// blue energy bullet toward each of the four corners of the screen"; the first gun fires "in the four ordinal
    /// directions", so both use the same four lanes. Soul Hunter keeps the owner's aiming rule for guns (2026-09-24: one
    /// arm aimed at the nearest enemy, the 4-way pattern kept), so this is the Gun script with its own blue bullet.
    /// Its union with the Gun (and Tirajisu) is Revenant Wheel (RevenantWheelWeapon).
    /// Arcanas (the page): Gemini and Iron Blue Will as the Gun; Jail of Crystal instead of the Gun's Heart of Fire
    /// (ArcanaWeapon.AzureRepeater).
    /// </summary>
    public class AzureRepeaterWeapon : GunWeapon
    {
        protected override string ResultsName => "Azure Repeater";
    }
}
