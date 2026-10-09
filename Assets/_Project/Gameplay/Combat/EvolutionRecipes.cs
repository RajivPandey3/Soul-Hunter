using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// One evolution or union recipe as the level-up cards and the pause screen's evolution list show it: the items it
    /// needs (base weapon first), its result and its discovery key.
    /// </summary>
    public sealed class EvolutionRecipe
    {
        /// <summary>The recipe asset's name ("Evo_BloodyTear"); the save's discovery key.</summary>
        public string Id;
        /// <summary>The result's player-facing name ("Bloody Tear").</summary>
        public string Name;
        public bool IsUnion;
        /// <summary>The weapon that evolves (a union: its first weapon). Its icon stands for the result, framed in gold,
        /// because evolutions have no icons of their own yet (the HUD does the same).</summary>
        public UpgradeData.UpgradeType Base;
        /// <summary>Every item the recipe needs, the base weapon first.</summary>
        public readonly List<UpgradeData.UpgradeType> Items = new List<UpgradeData.UpgradeType>();
        /// <summary>The result's own icon when it has one, else null (show the base weapon's icon in a gold frame).</summary>
        public Sprite ResultIcon;

        public bool Uses(UpgradeData.UpgradeType item) => Items.Contains(item);

        public static EvolutionRecipe From(WeaponEvolutionData evolution)
        {
            if (evolution == null) return null;
            var recipe = new EvolutionRecipe
            {
                Id = evolution.name,
                Name = WeaponEvolutionData.DisplayName(!string.IsNullOrEmpty(evolution.EvolvedName) ? evolution.EvolvedName : evolution.name),
                Base = evolution.BaseWeapon,
                ResultIcon = evolution.EvolvedIcon
            };
            recipe.Items.Add(evolution.BaseWeapon);
            if (!recipe.Items.Contains(evolution.RequiredPassive)) recipe.Items.Add(evolution.RequiredPassive);
            if (evolution.AdditionalPassives != null)
                foreach (var extra in evolution.AdditionalPassives)
                    if (!recipe.Items.Contains(extra.Type)) recipe.Items.Add(extra.Type);
            return recipe;
        }

        public static EvolutionRecipe From(WeaponUnionData union)
        {
            if (union == null) return null;
            var recipe = new EvolutionRecipe
            {
                Id = union.name,
                Name = WeaponEvolutionData.DisplayName(!string.IsNullOrEmpty(union.UnionName) ? union.UnionName : union.name),
                IsUnion = true,
                Base = union.WeaponA,
                ResultIcon = union.UnionIcon
            };
            recipe.Items.Add(union.WeaponA);
            if (!recipe.Items.Contains(union.WeaponB)) recipe.Items.Add(union.WeaponB);
            return recipe;
        }
    }

    /// <summary>
    /// Release items P3-01 (level-up evolution hints) and P3-06 (the pause screen's evolution list), 2026-10-09.
    /// vampire.survivors.wiki/w/Level_up (raw, update history, fetched 2026-10-09): Patch 0.3.2 "Added info for discovered
    /// evolutions on Level Up panels"; 1.4.200 "Fix for level up evo indicators not showing on passives for DLC weapons",
    /// so weapon cards and passive cards both carry the hint. vampire.survivors.wiki/w/Grim_Grimoire (raw, fetched
    /// 2026-10-09) for the list: it "shows evolutions that have been obtained before, and hides any evolutions that have yet
    /// to be encountered"; held items are "highlighted with a yellow-greenish square"; an evolution made in the run has its
    /// equation "entirely highlighted"; with full slots, recipes that can no longer be completed by levelling up "will look
    /// more faded"; "The list of evolutions is separated by pagination".
    /// </summary>
    public static class EvolutionRecipes
    {
        /// <summary>How a recipe's row reads on the evolution list (wiki Grim_Grimoire).</summary>
        public enum RowState
        {
            /// <summary>Not discovered on this save: shown as unknown.</summary>
            Unknown,
            /// <summary>Discovered and still possible this run.</summary>
            Open,
            /// <summary>Slots are full and the rest cannot be gained by levelling up: faded.</summary>
            Faded,
            /// <summary>Made in this run: the whole equation is highlighted.</summary>
            Made
        }

        /// <summary>Every recipe the manager knows: evolutions in the manager's order, then unions.</summary>
        public static List<EvolutionRecipe> All(WeaponManager manager)
        {
            var list = new List<EvolutionRecipe>();
            if (manager == null) return list;
            foreach (var evolution in manager.Evolutions)
                if (evolution != null && evolution.EvolvedWeaponPrefab != null) list.Add(EvolutionRecipe.From(evolution));
            foreach (var union in manager.Unions)
                if (union != null && union.UnionWeaponPrefab != null) list.Add(EvolutionRecipe.From(union));
            return list;
        }

        /// <summary>The save the game is playing on (null without services, e.g. in EditMode tests).</summary>
        public static GameData CurrentSave()
        {
            SaveService save = null;
            SoulHunter.Core.Services.GameServices.Instance?.TryGet(out save);
            return save != null ? save.CurrentData : null;
        }

        /// <summary>
        /// The discovered recipes a level-up card for <paramref name="item"/> hints, weapon or passive (wiki Level_up,
        /// 0.3.2 and 1.4.200). A recipe made in this run is left out: its base weapon is gone, so there is nothing to plan.
        /// The wiki does not say whether the hint waits until the other item is held; balance method (2026-10-09): every
        /// discovered recipe the item belongs to is hinted, because the hint is most useful exactly when the player is
        /// deciding whether to take the other item, and the patch note names no ownership condition.
        /// </summary>
        public static List<EvolutionRecipe> HintsFor(UpgradeData.UpgradeType item, IEnumerable<EvolutionRecipe> recipes,
            System.Func<string, bool> discovered, System.Func<string, bool> madeThisRun)
        {
            var hints = new List<EvolutionRecipe>();
            if (recipes == null || discovered == null) return hints;
            foreach (var recipe in recipes)
                if (recipe != null && recipe.Uses(item) && discovered(recipe.Id) && (madeThisRun == null || !madeThisRun(recipe.Id)))
                    hints.Add(recipe);
            return hints;
        }

        /// <summary>The hints for a card in the running game (the manager's recipes and the current save).</summary>
        public static List<EvolutionRecipe> HintsFor(UpgradeData.UpgradeType item, WeaponManager manager, GameData save)
        {
            if (manager == null) return new List<EvolutionRecipe>();
            return HintsFor(item, All(manager), id => EvolutionDiscovery.IsDiscovered(save, id), manager.MadeThisRun);
        }

        /// <summary>The items of <paramref name="recipe"/> other than <paramref name="item"/>, in recipe order.</summary>
        public static List<UpgradeData.UpgradeType> Counterparts(EvolutionRecipe recipe, UpgradeData.UpgradeType item)
        {
            var others = new List<UpgradeData.UpgradeType>();
            if (recipe == null) return others;
            foreach (var other in recipe.Items)
                if (other != item) others.Add(other);
            return others;
        }

        /// <summary>
        /// The card's hint in plain words: "Evolves with Hollow Heart into Bloody Tear", plus "(+1 more)" when the item is in
        /// several discovered recipes. Unions say "Unites with".
        /// </summary>
        public static string HintText(UpgradeData.UpgradeType item, IList<EvolutionRecipe> hints)
        {
            if (hints == null || hints.Count == 0) return "";
            var first = hints[0];
            var names = new List<string>();
            foreach (var other in Counterparts(first, item)) names.Add(UpgradeText.NameOf(other));
            string with = names.Count == 0 ? "" : " with " + JoinNames(names);
            string text = (first.IsUnion ? "Unites" : "Evolves") + with + " into " + first.Name;
            if (hints.Count > 1) text += " (+" + (hints.Count - 1) + " more)";
            return text;
        }

        /// <summary>"A", "A and B", "A, B and C".</summary>
        public static string JoinNames(IList<string> names)
        {
            if (names == null || names.Count == 0) return "";
            if (names.Count == 1) return names[0];
            var head = new List<string>(names);
            head.RemoveAt(head.Count - 1);
            return string.Join(", ", head) + " and " + names[names.Count - 1];
        }

        /// <summary>
        /// How the recipe's row reads (wiki Grim_Grimoire). <paramref name="known"/> is "discovered on this save, or made in
        /// this run" (a debug start that hands out an evolution shows it for the run without saving it).
        /// </summary>
        public static RowState StateOf(EvolutionRecipe recipe, bool known, bool madeThisRun, System.Func<UpgradeData.UpgradeType, bool> holds,
            System.Func<UpgradeData.UpgradeType, bool> canStillGain, bool slotsFull)
        {
            if (recipe == null || !known) return RowState.Unknown;
            if (madeThisRun) return RowState.Made;
            if (!slotsFull) return RowState.Open;
            // Wiki: with full slots, a row stays fully visible when all its items are held, or at least one is held and the
            // rest can still be gained by levelling up; the rest look faded.
            bool anyHeld = false, restGainable = true;
            foreach (var item in recipe.Items)
            {
                if (holds != null && holds(item)) anyHeld = true;
                else if (canStillGain == null || !canStillGain(item)) restGainable = false;
            }
            return anyHeld && restGainable ? RowState.Open : RowState.Faded;
        }

        /// <summary>The same for the running game.</summary>
        public static RowState StateOf(EvolutionRecipe recipe, WeaponManager manager, GameData save)
        {
            if (recipe == null) return RowState.Unknown;
            bool made = manager != null && manager.MadeThisRun(recipe.Id);
            bool known = made || EvolutionDiscovery.IsDiscovered(save, recipe.Id);
            if (manager == null) return StateOf(recipe, known, made, null, null, false);
            bool slotsFull = false;
            foreach (var item in recipe.Items)
                if (!manager.HasSlotFor(item)) slotsFull = true;
            return StateOf(recipe, known, made, t => Holds(manager, t), t => CanStillGain(manager, t), slotsFull);
        }

        /// <summary>The player holds the item now.</summary>
        public static bool Holds(WeaponManager manager, UpgradeData.UpgradeType item) => manager != null && manager.GetWeaponLevel(item) > 0;

        /// <summary>
        /// The item can still be gained by levelling up: there is a slot for it and level-ups offer it (Metaglios are stage
        /// items only; wiki Grim_Grimoire counts only items "obtainable through leveling up").
        /// </summary>
        public static bool CanStillGain(WeaponManager manager, UpgradeData.UpgradeType item) =>
            manager != null && manager.HasSlotFor(item) && !UpgradeRarity.IsStageItemOnly(item);

        /// <summary>The icon of an item (its WeaponDefinition), or null.</summary>
        public static Sprite IconOf(UpgradeData.UpgradeType item)
        {
            var definition = WeaponDefinitionLookup.Find(item);
            return definition != null ? definition.Icon : null;
        }

        /// <summary>The picture for a recipe's result: its own icon, else its base weapon's (shown in a gold frame).</summary>
        public static Sprite ResultIconOf(EvolutionRecipe recipe) =>
            recipe == null ? null : recipe.ResultIcon != null ? recipe.ResultIcon : IconOf(recipe.Base);
    }
}
