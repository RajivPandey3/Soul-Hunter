using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Vacuum Magnet Pickup.
    /// Jaise hi player isay uthayega, zameen par pare tamaam XPGems
    /// ek sath player ki taraf turn ho kar udd (fly) aayenge.
    /// </summary>
    public class MagnetPickup : Pickup
    {
        protected override void OnPickedUp(Collider player)
        {
            Debug.Log("[MagnetPickup] Magnetic Vacuum Activated! All gems flying to player.");

            var allGems = XPGem.ActiveGems;
            for (int i = allGems.Count - 1; i >= 0; i--)
            {
                var gem = allGems[i];
                if (gem == null) continue;
                // Agar gem zameen par active hai toh usay player ki taraf khinch lo
                if (gem.gameObject.activeInHierarchy)
                {
                    gem.Magnetize(player.transform);
                }
            }
        }
    }
}
