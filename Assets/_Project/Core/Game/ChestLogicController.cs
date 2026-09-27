using UnityEngine;
using SoulHunter.Gameplay.Combat;
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
    /// 3. Gold Bag (Agar kuch bhi nahi mila, sab max level ho chuka ho): 25 gold per item (VS Big Coin Bag).
    /// Plus VS chest gold every time (1 item 100-200, 3 items 300-600, 5 items 500-1000), and at most one
    /// evolution per chest, only from chests that can evolve (VS: bosses at 10:00 or later, and the 1:00 chest).
    /// Yeh script UI ke bina bhi background mein logic run karti hai.
    /// </summary>
    public class ChestLogicController : MonoBehaviour
    {
        [Tooltip("Base % chance (at Luck 1) that a chest holds 5 items. Provisional balance value.")]
        [SerializeField, Range(0f, 100f)] private float _fiveItemChancePercent = 3f;
        [Tooltip("Base % chance (at Luck 1) that a chest holds 3 items. Provisional balance value.")]
        [SerializeField, Range(0f, 100f)] private float _threeItemChancePercent = 10f;

        public const int GoldWhenEmpty = 100;
        /// <summary>VS Big Coin Bag: an item slot with nothing left to upgrade pays this much gold (then Greed).</summary>
        public const int GoldPerMaxedItem = 25;

        /// <summary>
        /// VS wiki (Treasure_Chest): every chest also holds gold, by how many items it has:
        /// 1 item 100-200, 3 items 300-600, 5 items 500-1000 (then Greed).
        /// </summary>
        public static int ChestGold(int itemCount, float roll01)
        {
            int low = itemCount >= 5 ? 500 : itemCount >= 3 ? 300 : 100;
            return Mathf.RoundToInt(Mathf.Lerp(low, low * 2, Mathf.Clamp01(roll01)));
        }

        /// <summary>
        /// VS (Mad Forest wave table, evo flags): only chests from bosses at or after 10:00 can evolve a weapon, plus
        /// the first chest at 1:00. Earlier chests (3, 5, 7, 9) only give upgrades.
        /// </summary>
        public static bool CanEvolveAt(float stageSeconds) => stageSeconds >= 600f || (stageSeconds >= 60f && stageSeconds < 120f);

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

        public string ProcessChest(bool canEvolve = true)
        {
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            var levelUpManager = FindFirstObjectByType<LevelUpManager>();
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            float luck = player != null && player.Stats != null ? player.Stats.Luck : 1f;
            return ProcessChest(weaponManager, levelUpManager, RollItemCount(luck, Random.value), canEvolve, Random.value);
        }

        /// <summary>
        /// VS Arcana chest (Arcana mode, the 11:00 and 21:00 elites): gives one Arcana the player does not hold instead of
        /// items. VS offers a choice of four; Soul Hunter has no Arcana choice screen yet, so a random one is given and
        /// named in the chest message. With three or more Arcanas held (VS), or none left, it opens as a regular chest.
        /// </summary>
        public string ProcessArcanaChest(bool canEvolve = true)
        {
            var arcanas = ArcanaManager.Instance;
            if (arcanas == null || !arcanas.CanGrantFromChest) return ProcessChest(canEvolve);
            var arcana = arcanas.GrantRandomAvailableArcana(Random.value);
            if (arcana == null) return ProcessChest(canEvolve);
            return ArcanaMessage(arcana);
        }

        public static string ArcanaMessage(SoulHunter.Gameplay.Data.ArcanaData arcana)
        {
            var message = new StringBuilder("ARCANA CHEST!\n\n");
            message.Append("<color=#C79BFF>Arcana ").Append(arcana.CardNumber).Append(" - ").Append(arcana.CardName).Append("</color>");
            if (!string.IsNullOrWhiteSpace(arcana.Description)) message.Append('\n').Append(arcana.Description);
            return message.ToString();
        }

        /// <summary>
        /// Opens a chest holding <paramref name="itemCount"/> items and returns the reward text. VS rules: at most one
        /// evolution per chest (only when <paramref name="canEvolve"/>), one upgrade per other item, 25 gold for an item
        /// with nothing left to upgrade, and the chest's own gold on top.
        /// </summary>
        public string ProcessChest(WeaponManager weaponManager, LevelUpManager levelUpManager, int itemCount,
                                   bool canEvolve = true, float goldRoll01 = 0f)
        {
            var rewards = new List<string>();
            bool evolved = false;
            int maxedItems = 0;
            for (int i = 0; i < itemCount; i++)
            {
                // 1. Try Evolution / Union: VS gives at most one per chest, in the first reward.
                if (canEvolve && !evolved && weaponManager != null && weaponManager.TryEvolveWeapon(out string evolvedName))
                {
                    evolved = true;
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

                maxedItems++; // 3. nothing left to upgrade: VS pays a Big Coin Bag for this item
            }

            // VS: the chest's gold, plus 25 per maxed item, all with Greed.
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            int gold = SoulHunter.Gameplay.Pickups.GoldRewards.Grant(
                ChestGold(itemCount, goldRoll01) + maxedItems * GoldPerMaxedItem, player != null ? player.Stats : null);
            if (gold > 0) Debug.Log($"[ChestLogicController] Chest se {gold} Gold mile!");

            var message = new StringBuilder(itemCount > 1 ? $"TREASURE x{itemCount}!\n" : "You Found a Chest!\n");
            foreach (var reward in rewards) message.Append('\n').Append(reward);
            if (gold > 0) message.Append("\n\n<color=yellow>Gold: +").Append(gold).Append("</color>");
            return message.ToString();
        }
    }
}
