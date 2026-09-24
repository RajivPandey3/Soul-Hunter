using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Core.Services;
using System.Collections.Generic;
using System.Text;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Chest kholne par 1, 3 ya 5 items milte hain (Luck se 3/5 ka chance badhta hai).
    /// Har item ke liye:
    /// 1. Weapon Evolution (Agar conditions meet hon).
    /// 2. Free Upgrade (Agar evolution nahi hui).
    /// 3. Gold Bag (Agar kuch bhi nahi mila, sab max level ho chuka ho).
    /// Yeh script UI ke bina bhi background mein logic run karti hai.
    /// </summary>
    public class ChestLogicController : MonoBehaviour
    {
        [Tooltip("Base % chance (at Luck 1) that a chest holds 5 items. Provisional balance value.")]
        [SerializeField, Range(0f, 100f)] private float _fiveItemChancePercent = 3f;
        [Tooltip("Base % chance (at Luck 1) that a chest holds 3 items. Provisional balance value.")]
        [SerializeField, Range(0f, 100f)] private float _threeItemChancePercent = 10f;

        public const int GoldWhenEmpty = 100;

        /// <summary>
        /// Rolls how many items a chest holds. Luck multiplies both the 5- and 3-item chances.
        /// <paramref name="roll01"/> is a uniform random value in [0, 1).
        /// </summary>
        public int RollItemCount(float luck, float roll01)
        {
            luck = Mathf.Max(0f, luck);
            float five = _fiveItemChancePercent / 100f * luck;
            float three = _threeItemChancePercent / 100f * luck;
            if (roll01 < five) return 5;
            if (roll01 < five + three) return 3;
            return 1;
        }

        public string ProcessChest()
        {
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            var levelUpManager = FindFirstObjectByType<LevelUpManager>();
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            float luck = player != null && player.Stats != null ? player.Stats.Luck : 1f;
            return ProcessChest(weaponManager, levelUpManager, RollItemCount(luck, Random.value));
        }

        /// <summary>Opens a chest holding <paramref name="itemCount"/> items and returns the reward text.</summary>
        public string ProcessChest(WeaponManager weaponManager, LevelUpManager levelUpManager, int itemCount)
        {
            var rewards = new List<string>();
            for (int i = 0; i < itemCount; i++)
            {
                // 1. Try Evolution / Union
                if (weaponManager != null && weaponManager.TryEvolveWeapon(out string evolvedName))
                {
                    Debug.Log($"[ChestLogicController] Huzzah! Weapon Evolved/Unioned into {evolvedName}!");
                    rewards.Add($"<color=red>Ultimate Power: {evolvedName}</color>");
                    continue;
                }

                // 2. Give a free upgrade. Applied directly so the chest never touches
                // the level-up queue or un-pauses the game.
                var upgrades = levelUpManager != null ? levelUpManager.GetRandomValidUpgradesForChest() : null;
                if (weaponManager != null && upgrades != null && upgrades.Count > 0)
                {
                    weaponManager.ApplyUpgrade(upgrades[0]);
                    Debug.Log($"[ChestLogicController] Chest se Free Upgrade mila: {upgrades[0].UpgradeName}!");
                    rewards.Add($"<color=yellow>+ {upgrades[0].UpgradeName} (Lv {upgrades[0].Level})</color>");
                    continue;
                }

                break; // nothing left to give
            }

            if (rewards.Count > 0)
            {
                var message = new StringBuilder(itemCount > 1 ? $"TREASURE x{itemCount}!\n" : "You Found a Chest!\n");
                foreach (var reward in rewards) message.Append('\n').Append(reward);
                return message.ToString();
            }

            // 3. Agar aur kuch upgrade karne ko nahi hai, toh Rich ban jao (Gold)
            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
            {
                economy.AddGold(GoldWhenEmpty);
                Debug.Log($"[ChestLogicController] Chest se {GoldWhenEmpty} Gold mile! Total: {economy.CurrentGold}");
                return $"You Found a Chest!\n\n<color=yellow>Gold: +{GoldWhenEmpty}</color>";
            }

            return "Chest Empty!";
        }
    }
}
