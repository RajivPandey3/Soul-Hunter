using UnityEngine;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Jab dushman marte hain, toh wo Coin girate hain.
    /// Ye script us coin ko uthane par EconomyService ko batata hai ki Gold add kar do.
    /// </summary>
    public class GoldPickup : Pickup
    {
        [SerializeField] private int _goldAmount = 10;

        protected override void OnPickedUp(Collider player)
        {
            if (GameServices.Instance != null)
            {
                var economy = GameServices.Instance.Get<EconomyService>();
                if (economy != null)
                {
                    economy.AddGold(_goldAmount);
                    Debug.Log($"[GoldPickup] Picked up {_goldAmount} Gold! Total Gold: {economy.CurrentGold}");
                }
            }
        }
    }
}
