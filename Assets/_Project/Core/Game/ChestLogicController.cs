using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Core.Services;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Chest kholne par 3 cheezon mein se ek milti hai:
    /// 1. Weapon Evolution (Agar conditions meet hon).
    /// 2. Free Upgrade (Agar evolution nahi hui).
    /// 3. Gold Bag (Agar sab max level ho chuka ho).
    /// Yeh script UI ke bina bhi background mein logic run karti hai.
    /// </summary>
    public class ChestLogicController : MonoBehaviour
    {
        public string ProcessChest()
        {
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            
            // 1. Try Evolution / Union
            if (weaponManager != null && weaponManager.TryEvolveWeapon(out string evolvedName))
            {
                Debug.Log($"[ChestLogicController] Huzzah! Weapon Evolved/Unioned into {evolvedName}!");
                return $"WEAPON UPGRADED!\n\n<color=red>Ultimate Power: {evolvedName}</color>";
            }

            // 2. Give a free upgrade
            var levelUpManager = FindFirstObjectByType<LevelUpManager>();
            if (levelUpManager != null)
            {
                var upgrades = levelUpManager.GetRandomValidUpgradesForChest();
                if (upgrades != null && upgrades.Count > 0)
                {
                    levelUpManager.SelectUpgrade(upgrades[0]);
                    Debug.Log($"[ChestLogicController] Chest se Free Upgrade mila: {upgrades[0].UpgradeName}!");
                    return $"You Found a Chest!\n\n<color=yellow>+ {upgrades[0].UpgradeName} (Lv {upgrades[0].Level})</color>";
                }
            }

            // 3. Agar aur kuch upgrade karne ko nahi hai, toh Rich ban jao (Gold)
            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
            {
                economy.AddGold(100);
                Debug.Log($"[ChestLogicController] Chest se 100 Gold mile! Total: {economy.CurrentGold}");
                return "You Found a Chest!\n\n<color=yellow>Gold: +100</color>";
            }
            
            return "Chest Empty!";
        }
    }
}
