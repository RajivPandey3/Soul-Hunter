using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
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
    /// <summary>One item a chest gave, for the chest-opening screen.</summary>
    public struct ChestReward
    {
        public string Name;
        public Sprite Icon;
        public int Level;
        public bool IsEvolution;
        /// <summary>VS Big Coin Bag: nothing was left to upgrade, so this item paid 25 gold.</summary>
        public bool IsCoinBag;
    }

    /// <summary>What the last chest gave: the chest-opening screen plays it (VS wiki, Treasure_Chest).</summary>
    public sealed class ChestResult
    {
        public int ItemCount;
        public bool IsArcana;
        /// <summary>The chest's look (bronze, silver, Arcana), set by the ChestPickup that was opened.</summary>
        public SoulHunter.Gameplay.Pickups.ChestKind Kind = SoulHunter.Gameplay.Pickups.ChestKind.Silver;
        public int Gold;
        public readonly List<ChestReward> Rewards = new List<ChestReward>();
        /// <summary>Every item became a coin bag (VS: such chests skip their animation once skipping is unlocked).</summary>
        public bool OnlyCoins => Rewards.Count > 0 && Rewards.TrueForAll(r => r.IsCoinBag);
    }

    public class ChestLogicController : MonoBehaviour
    {
        /// <summary>The chest processed last (null before the first).</summary>
        public ChestResult LastResult { get; private set; }

        // The old global 3% / 10% chances (_fiveItemChancePercent/_threeItemChancePercent) are gone (P1-14, 2026-10-09):
        // each boss carries its own ChestTiers. The scene may still hold their serialized values; Unity ignores them.

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
        /// Since 2026-10-09 (P1-14) a scheduled chest takes its flag from its boss's ChestTiers, not from the clock at death;
        /// this rule is only the fallback for chests with no scheduled config (ChestTiers.Unscheduled).
        /// </summary>
        public static bool CanEvolveAt(float stageSeconds) => stageSeconds >= 600f || (stageSeconds >= 60f && stageSeconds < 120f);

        /// <summary>
        /// Rolls how many items a chest holds, from its boss's config. vampire.survivors.wiki/w/Treasure_Chest (raw, fetched
        /// 2026-10-09): "there is a base 3% chance for a 5-item chest, and if that roll fails then a 10% chance for a 3-item
        /// chest, and if that roll fails then a 50% chance for a 1-item chest. If the third roll also fails, then this
        /// specific Silver Bat has a level 1 fallback"; "chanceWithLuck = levelChance · totalLuck ... compared against a
        /// randomly generated number between 0 and 100, with each check having a separate random number."
        /// <paramref name="next01"/> gives a new uniform value in [0, 1) for each check.
        /// Before 2026-10-09 one shared number was compared cumulatively against global 3% / 10% chances.
        /// </summary>
        public static int RollItemCount(ChestTiers tiers, float luck, System.Func<float> next01)
        {
            luck = Mathf.Max(0f, luck);
            if (next01() * 100f < tiers.FivePercent * luck) return 5;
            if (next01() * 100f < tiers.ThreePercent * luck) return 3;
            if (next01() * 100f < tiers.OnePercent * luck) return 1;
            return tiers.FallbackItems;
        }

        /// <summary>
        /// VS wiki (Treasure_Chest): "The first six chests picked up in a save will always drop items in the set sequence
        /// 1-1-3-1-1-5." Counted by GameData.ChestsOpened, which ChestUI raises for every chest (Arcana chests included).
        /// </summary>
        public static readonly int[] FirstChestSequence = { 1, 1, 3, 1, 1, 5 };

        /// <summary>The item count fixed by the first-chests sequence, or 0 once the save has opened six chests.</summary>
        public static int FirstChestItems(int chestsOpenedBefore) =>
            chestsOpenedBefore >= 0 && chestsOpenedBefore < FirstChestSequence.Length ? FirstChestSequence[chestsOpenedBefore] : 0;

        /// <summary>The first-chests sequence count if it still applies (whatever the boss), else the boss's Luck rolls.</summary>
        public static int ItemCountFor(int chestsOpenedBefore, ChestTiers tiers, float luck, System.Func<float> next01)
        {
            int fixedCount = FirstChestItems(chestsOpenedBefore);
            return fixedCount > 0 ? fixedCount : RollItemCount(tiers, luck, next01);
        }

        /// <summary>A chest with no boss config of its own (old callers): the 3/10/100 config, evolving or not.</summary>
        public string ProcessChest(bool canEvolve = true) =>
            ProcessChest(canEvolve ? ChestTiers.StageBoss : ChestTiers.StageBoss.AsRegular(false));

        /// <summary>Opens a chest dropped by a boss with <paramref name="tiers"/>: its item count, evolution flag and slots.</summary>
        public string ProcessChest(ChestTiers tiers)
        {
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            var levelUpManager = FindFirstObjectByType<LevelUpManager>();
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            float luck = player != null && player.Stats != null ? player.Stats.Luck : 1f;
            SoulHunter.Core.Persistence.SaveService save = null;
            SoulHunter.Core.Services.GameServices.Instance?.TryGet(out save);
            // Without a save (no services) the sequence is skipped and the chest rolls as usual.
            int opened = save != null && save.CurrentData != null ? save.CurrentData.ChestsOpened : FirstChestSequence.Length;
            return ProcessChest(weaponManager, levelUpManager, ItemCountFor(opened, tiers, luck, () => Random.value), tiers, Random.value);
        }

        /// <summary>An Arcana chest with the Mad Forest 11:00 / 21:00 config.</summary>
        public string ProcessArcanaChest() => ProcessArcanaChest(ChestTiers.ArcanaMinutes);

        /// <summary>
        /// VS Arcana chest (Arcana mode, the 11:00 and 21:00 elites): gives one Arcana the player does not hold instead of
        /// items. VS offers a choice of four; Soul Hunter has no Arcana choice screen yet, so a random one is given and
        /// named in the chest message.
        /// When no Arcana can be given it opens as a regular chest. vampire.survivors.wiki/w/Treasure_Chest (Arcana, raw,
        /// fetched 2026-10-09): "If two Arcanas are obtained from these chests while extra Arcana chests exist on the stage,
        /// they will turn into bronze chests. If a newer player has all of their unlocked arcanas in use ... any extra
        /// Arcana chests will act like Silver chests instead." So the Arcana limit reached (or no Arcana system) gives a
        /// bronze chest that cannot evolve, and every Arcana already in use gives a silver chest that can. Before 2026-10-09
        /// the fallback took its evolution flag from the stage clock (11:00 and 21:00 are after 10:00, so it could evolve).
        /// </summary>
        public string ProcessArcanaChest(ChestTiers tiers)
        {
            var arcanas = ArcanaManager.Instance;
            var arcana = arcanas != null && arcanas.CanGrantFromChest ? arcanas.GrantRandomAvailableArcana(Random.value) : null;
            if (arcana == null)
            {
                bool silver = arcanas != null && arcanas.ActiveArcanas.Count < ArcanaManager.MaxArcanasFromChests && arcanas.AvailableCount == 0;
                return ProcessChest(tiers.AsRegular(silver));
            }
            LastResult = new ChestResult { ItemCount = 1, IsArcana = true, Kind = SoulHunter.Gameplay.Pickups.ChestKind.Arcana };
            LastResult.Rewards.Add(new ChestReward { Name = $"Arcana {arcana.CardNumber} - {arcana.CardName}", Icon = arcana.CardIcon, Level = 1 });
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
        /// Opens a chest holding <paramref name="itemCount"/> items with no slot rules (every slot "any"), evolving or not.
        /// </summary>
        public string ProcessChest(WeaponManager weaponManager, LevelUpManager levelUpManager, int itemCount,
                                   bool canEvolve = true, float goldRoll01 = 0f) =>
            ProcessChest(weaponManager, levelUpManager, itemCount,
                canEvolve ? ChestTiers.StageBoss : ChestTiers.StageBoss.AsRegular(false), goldRoll01);

        /// <summary>
        /// Opens a chest holding <paramref name="itemCount"/> items and returns the reward text. VS rules: at most one
        /// evolution per chest (only when <paramref name="tiers"/> can evolve), one upgrade per other item, 25 gold for an
        /// item with nothing left to upgrade, and the chest's own gold on top.
        /// Slots follow vampire.survivors.wiki/w/Template:Treasure (raw, fetched 2026-10-09): slot 1 is "Evolution" when
        /// evo=1 ("any = {{#ifexpr: evo|Evolution|Upgrade (any)}}", "weapon = {{#ifexpr: evo|Evolution|Upgrade (weapon)}}");
        /// slots 2-5 are "Upgrade (any)", or "Upgrade (weapon)" when rewardN=weapon. Before 2026-10-09 any slot could
        /// evolve (the first one that managed it).
        /// </summary>
        public string ProcessChest(WeaponManager weaponManager, LevelUpManager levelUpManager, int itemCount,
                                   ChestTiers tiers, float goldRoll01 = 0f)
        {
            var rewards = new List<string>();
            var result = new ChestResult
            {
                ItemCount = itemCount,
                Kind = tiers.CanEvolve ? SoulHunter.Gameplay.Pickups.ChestKind.Silver : SoulHunter.Gameplay.Pickups.ChestKind.Bronze
            };
            int maxedItems = 0;
            for (int i = 0; i < itemCount; i++)
            {
                // 1. Evolution / Union: slot 1 of a chest that can evolve (Template:Treasure), so at most one per chest
                // (Treasure_Chest: "Generally, only one weapon can be evolved per chest").
                if (i == 0 && tiers.CanEvolve && weaponManager != null && weaponManager.TryEvolveWeapon(out string evolvedName))
                {
                    Debug.Log($"[ChestLogicController] Huzzah! Weapon Evolved/Unioned into {evolvedName}!");
                    string shownName = WeaponEvolutionData.DisplayName(evolvedName);
                    rewards.Add($"<color=red>Ultimate Power: {shownName}</color>");
                    result.Rewards.Add(new ChestReward { Name = shownName, Icon = weaponManager.LastEvolvedIcon, Level = 1, IsEvolution = true });
                    continue;
                }

                // 2. Give a free upgrade. Applied directly so the chest never touches
                // the level-up queue or un-pauses the game. A "weapon" slot (Template:Treasure rewardN=weapon) levels an
                // owned weapon. With no weapon left to level, it pays coins: Treasure_Chest Patch 0.2.7, chests "can now only
                // level up existing weapons (or give extra coins if no weapons can be leveled up)". The wiki gives no amount for
                // one slot; balance method: one Big Coin Bag, as for any slot with nothing left (wiki research 2026-10-09).
                List<UpgradeData> upgrades = null;
                if (levelUpManager != null) upgrades = levelUpManager.GetRandomValidUpgradesForChest(weaponsOnly: tiers.IsWeaponSlot(i));
                if (weaponManager != null && upgrades != null && upgrades.Count > 0)
                {
                    weaponManager.ApplyUpgrade(upgrades[0]);
                    Debug.Log($"[ChestLogicController] Chest se Free Upgrade mila: {upgrades[0].UpgradeName}!");
                    rewards.Add($"<color=yellow>+ {upgrades[0].UpgradeName} (Lv {upgrades[0].Level})</color>");
                    var definition = WeaponDefinitionLookup.Find(upgrades[0].Type);
                    result.Rewards.Add(new ChestReward
                    {
                        Name = UpgradeText.DisplayName(upgrades[0]), Level = upgrades[0].Level,
                        Icon = upgrades[0].Icon != null ? upgrades[0].Icon : definition != null ? definition.Icon : null
                    });
                    continue;
                }

                var content = GameContentCatalog.Load();
                result.Rewards.Add(new ChestReward { Name = SoulHunter.Gameplay.Combat.MaxedRewards.GoldCardName, IsCoinBag = true, Icon = content != null ? content.GoldPouchIcon : null });
                maxedItems++; // 3. nothing left to upgrade: VS pays a Big Coin Bag for this item
            }

            // VS: the chest's gold, plus 25 per maxed item, all with Greed.
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            int gold = SoulHunter.Gameplay.Pickups.GoldRewards.Grant(
                ChestGold(itemCount, goldRoll01) + maxedItems * GoldPerMaxedItem, player != null ? player.Stats : null);
            SoulHunter.Gameplay.Pickups.GoldRewards.CountBigCoinBags(maxedItems); // VS: Big Coin Bags from any source count
            if (gold > 0) Debug.Log($"[ChestLogicController] Chest se {gold} Gold mile!");
            result.Gold = gold;
            LastResult = result;

            var message = new StringBuilder(itemCount > 1 ? $"TREASURE x{itemCount}!\n" : "You Found a Chest!\n");
            foreach (var reward in rewards) message.Append('\n').Append(reward);
            if (gold > 0) message.Append("\n\n<color=yellow>Gold: +").Append(gold).Append("</color>");
            return message.ToString();
        }
    }
}
