using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// A VS Gemini (I) counterpart (vampire.survivors.wiki/w/Gemini_(I), raw, fetched 2026-10-09): "certain weapons are now
    /// accompanied by functionally identical counterparts and level up at the same time"; they "do not take up weapon
    /// slots or show in the weapons list in the upper-left corner, cannot be evolved or united, and remain as independent
    /// weapons if their counterparts unite". Spawned by ArcanaManager next to its original on the player, but never
    /// registered with WeaponManager, so it has no slot, no HUD icon, no evolution and Shadow Kael does not copy it
    /// (WeaponManager.GetEquippedWeapons). Each frame it takes up any level its original has gained since. Once the
    /// original is gone or switched off (replaced by an evolution or union) it stays as it is, on its own.
    /// </summary>
    public sealed class GeminiCounterpart : MonoBehaviour
    {
        /// <summary>The weapon this is the counterpart of.</summary>
        public AutoAttackWeapon Original { get; private set; }
        /// <summary>This counterpart's own weapon.</summary>
        public AutoAttackWeapon Weapon { get; private set; }

        public void Bind(AutoAttackWeapon original, AutoAttackWeapon weapon)
        {
            Original = original;
            Weapon = weapon;
        }

        private void Update()
        {
            if (Original == null || Weapon == null || !Original.isActiveAndEnabled) return;
            if (Original.CurrentLevel <= Weapon.CurrentLevel) return;
            WeaponManager.ApplyWeaponLevel(gameObject, Original.CurrentLevel);
            // VS (wiki Weapons): "Upgrading a weapon will also reset its Cooldown and activate it instantly."
            Weapon.ResetCooldownAndFire();
        }
    }
}
