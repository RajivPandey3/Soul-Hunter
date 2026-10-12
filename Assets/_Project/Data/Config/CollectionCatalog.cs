using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>The kinds of Collection entry (wiki Collection: "weapons, passive items, pickups, relics, and arcanas").</summary>
    public enum CollectionKind { Weapon, Evolution, Passive, Pickup, Relic, Arcana }

    /// <summary>One Collection entry: what it is, and how it is shown once found.</summary>
    public sealed class CollectionEntry
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Icon;
        public CollectionKind Kind;
    }

    /// <summary>
    /// Every item Soul Hunter has, as Collection entries (release item P3-02, 2026-10-09; vampire.survivors.wiki/w/Collection):
    /// each weapon followed by its evolutions (the wiki's table lists "Whip, Bloody Tear, Magic Wand, Holy Wand ..."), then
    /// the passive items, the pickups, the relic and the Arcanas, the wiki's own order of kinds. Built from the content
    /// catalog (WeaponDefinitions, CollectionEvolutions, Arcanas), so a new item appears without code changes. Ids are the
    /// ones CollectionBook stores on the save. Soul Hunter has no unions now (the six invented ones were removed on
    /// 2026-09-30); Crimson Shroud (VS's Laurel union) is listed with Laurel as an evolution.
    /// </summary>
    public static class CollectionCatalog
    {
        public static string ItemId(UpgradeData.UpgradeType type) => "Item." + type;
        public static string EvolutionId(WeaponEvolutionData evolution) => evolution != null ? CollectionBook.EvolutionPrefix + evolution.name : null;
        public static string ArcanaId(ArcanaData.ArcanaType type) => "Arcana." + type;

        /// <summary>The pickup entry of a Results pickup name: every Soul Reliquary size and the Arcana chest are one entry.</summary>
        public static string PickupId(string resultsName)
        {
            if (string.IsNullOrEmpty(resultsName)) return null;
            if (resultsName.StartsWith(ChestName) || resultsName == "Arcana Reliquary") return "Pickup." + ChestName;
            return "Pickup." + resultsName;
        }

        public const string GemName = "Soul Gem";
        public const string ChestName = "Soul Reliquary";

        /// <summary>
        /// Soul Hunter's pickups with what each does, in the VS Collection's pickup order (Experience Gem, Gold Coin, Coin Bag,
        /// Rich Coin Bag, Treasure Chest, Rosary, Nduja Fritta Tanto, Orologion, Vacuum, Floor Chicken, Gilded Clover, Little
        /// Clover). The numbers are the game's own (LightSourceDrops, the pickup scripts).
        /// </summary>
        private static readonly (string Name, string Description)[] Pickups =
        {
            (GemName, "Gives experience."),
            (SoulHunter.Gameplay.Pickups.PickupNames.Coin, "Gives 1 gold."),
            (SoulHunter.Gameplay.Pickups.PickupNames.CoinBag, "Gives 10 gold."),
            (SoulHunter.Gameplay.Pickups.PickupNames.RichCoinBag, "Gives 100 gold."),
            (ChestName, "Holds 1, 3 or 5 items, an evolution when one is ready, and gold."),
            (SoulHunter.Gameplay.Pickups.ScreenWipePickup.DisplayName, "Kills every enemy on screen. Stage bosses are immune."),
            (SoulHunter.Gameplay.Combat.FireBreath.DisplayName, "Breathe fire at the nearest enemy for 10 seconds."),
            (SoulHunter.Gameplay.Pickups.PickupNames.TimeFreeze, "Freezes every enemy for 10 seconds."),
            (SoulHunter.Gameplay.Pickups.PickupNames.Magnet, "Pulls every gem on the ground to you."),
            (SoulHunter.Gameplay.Pickups.PickupNames.Chicken, "Heals 30 health."),
            (SoulHunter.Gameplay.Pickups.GildedCharmPickup.DisplayName, "Pulls every coin to you and starts a Gold Fever."),
            (SoulHunter.Gameplay.Pickups.LuckCharmPickup.DisplayName, "+10% Luck for the rest of the run."),
        };

        private static GameContentCatalog _builtFrom;
        private static List<CollectionEntry> _entries;

        /// <summary>Every entry, in the Collection's order.</summary>
        public static IReadOnlyList<CollectionEntry> All()
        {
            var catalog = GameContentCatalog.Load();
            if (_entries != null && ReferenceEquals(catalog, _builtFrom)) return _entries;
            _builtFrom = catalog;
            _entries = Build(catalog);
            return _entries;
        }

        public static List<CollectionEntry> Build(GameContentCatalog catalog)
        {
            var list = new List<CollectionEntry>();
            if (catalog == null) return list;
            var ids = new HashSet<string>();
            void Add(CollectionEntry entry)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Id) && ids.Add(entry.Id)) list.Add(entry);
            }

            var definitions = catalog.WeaponDefinitions ?? new WeaponDefinition[0];
            var evolutions = catalog.CollectionEvolutions ?? new WeaponEvolutionData[0];
            foreach (var definition in definitions)
            {
                if (definition == null || definition.IsPassive) continue;
                Add(new CollectionEntry { Id = ItemId(definition.Type), Name = NameOf(definition), Description = definition.Description,
                    Icon = definition.Icon, Kind = CollectionKind.Weapon });
                foreach (var evolution in evolutions)
                    if (evolution != null && evolution.BaseWeapon == definition.Type) Add(Evolution(evolution, definition, definitions));
            }
            foreach (var evolution in evolutions) // an evolution whose base weapon has no definition still has its entry
                if (evolution != null) Add(Evolution(evolution, null, definitions));
            // VS unions (added 2026-10-12). A union that is a weapon type of its own (Twilight Flock) already has its
            // weapon entry above; one without levels (Revenant Wheel) is listed here, found once it is discovered
            // (EvolutionDiscovery keys on the union asset's name, as for evolutions).
            if (catalog.Unions != null)
                foreach (var union in catalog.Unions)
                    if (union != null && !union.HasResultType) Add(Union(union, definitions));
            foreach (var definition in definitions)
                if (definition != null && definition.IsPassive)
                    Add(new CollectionEntry { Id = ItemId(definition.Type), Name = NameOf(definition), Description = definition.Description,
                        Icon = definition.Icon, Kind = CollectionKind.Passive });
            foreach (var pickup in Pickups)
                Add(new CollectionEntry { Id = PickupId(pickup.Name), Name = pickup.Name, Description = pickup.Description,
                    Icon = PickupIcon(catalog, pickup.Name), Kind = CollectionKind.Pickup });
            Add(new CollectionEntry { Id = CollectionBook.SoulEggId, Name = "Soul Egg", Kind = CollectionKind.Relic, Icon = catalog.SoulEggIcon,
                Description = "Bought from the Wandering Merchant: raises one random stat of a character for good." });
            // Relics (RelicBook, 2026-10-12; wiki Relics: "Once collected, they are permanently added to the collection
            // menu") and the Soul Casket (CasketBook). Their ids are the ones the Results pickup names produce, so the save
            // has one record of each. No pictures exist yet: the cell shows the name.
            foreach (var relic in RelicBook.All)
                Add(new CollectionEntry { Id = RelicBook.CollectionId(relic), Name = relic.Name, Kind = CollectionKind.Relic,
                    Description = $"Lies on Level {relic.Level}. {RelicBook.GiftText(relic)}" });
            Add(new CollectionEntry { Id = CasketBook.CollectionId, Name = CasketBook.PickupName, Kind = CollectionKind.Relic,
                Description = "Guarded by a ring of elites on Levels 1 to 7. Opening it unlocks a character." });
            if (catalog.Arcanas != null)
                foreach (var arcana in catalog.Arcanas)
                    if (arcana != null)
                        Add(new CollectionEntry { Id = ArcanaId(arcana.Type), Kind = CollectionKind.Arcana, Icon = arcana.CardIcon,
                            Name = string.IsNullOrEmpty(arcana.CardNumber) ? arcana.CardName : $"{arcana.CardNumber} - {arcana.CardName}",
                            Description = arcana.Description });
            return list;
        }

        private static string NameOf(WeaponDefinition definition) =>
            !string.IsNullOrEmpty(definition.DisplayName) ? definition.DisplayName : definition.Type.ToString();

        private static CollectionEntry Evolution(WeaponEvolutionData evolution, WeaponDefinition baseDefinition, WeaponDefinition[] definitions)
        {
            baseDefinition ??= System.Array.Find(definitions, d => d != null && d.Type == evolution.BaseWeapon);
            var passive = System.Array.Find(definitions, d => d != null && d.Type == evolution.RequiredPassive);
            string recipe = $"Evolves from {(baseDefinition != null ? baseDefinition.DisplayName : evolution.BaseWeapon.ToString())}" +
                            $" with {(passive != null ? passive.DisplayName : evolution.RequiredPassive.ToString())}";
            if (evolution.AdditionalPassives != null)
                foreach (var extra in evolution.AdditionalPassives)
                {
                    var more = System.Array.Find(definitions, d => d != null && d.Type == extra.Type);
                    recipe += $" and {(more != null ? more.DisplayName : extra.Type.ToString())}";
                }
            return new CollectionEntry
            {
                Id = EvolutionId(evolution), Kind = CollectionKind.Evolution,
                Name = WeaponEvolutionData.DisplayName(!string.IsNullOrEmpty(evolution.EvolvedName) ? evolution.EvolvedName : evolution.name),
                // An evolution shows its base weapon's icon, as the HUD and the chest screen do (evolutions have no icons yet).
                Icon = baseDefinition != null ? baseDefinition.Icon : null,
                Description = recipe + "."
            };
        }

        private static CollectionEntry Union(WeaponUnionData union, WeaponDefinition[] definitions)
        {
            string NameOfType(UpgradeData.UpgradeType type)
            {
                var definition = System.Array.Find(definitions, d => d != null && d.Type == type);
                return definition != null ? definition.DisplayName : type.ToString();
            }
            var first = System.Array.Find(definitions, d => d != null && d.Type == union.WeaponA);
            string recipe = $"Unites {NameOfType(union.WeaponA)} and {NameOfType(union.WeaponB)}";
            if (union.NeedsPassive) recipe += $" with {NameOfType(union.RequiredPassive)}";
            return new CollectionEntry
            {
                Id = CollectionBook.EvolutionPrefix + union.name, Kind = CollectionKind.Evolution,
                Name = WeaponEvolutionData.DisplayName(!string.IsNullOrEmpty(union.UnionName) ? union.UnionName : union.name),
                Icon = union.UnionIcon != null ? union.UnionIcon : first != null ? first.Icon : null,
                Description = recipe + "."
            };
        }

        private static Sprite PickupIcon(GameContentCatalog catalog, string name)
        {
            if (name == SoulHunter.Gameplay.Pickups.PickupNames.Chicken) return catalog.RoastChickenIcon;
            if (name == SoulHunter.Gameplay.Pickups.PickupNames.Coin || name == SoulHunter.Gameplay.Pickups.PickupNames.CoinBag ||
                name == SoulHunter.Gameplay.Pickups.PickupNames.RichCoinBag) return catalog.GoldPouchIcon;
            return null; // no picture exists yet for the others; the cell shows the name instead
        }

        /// <summary>The evolution that replaced <paramref name="baseWeapon"/>, for recording it when the HUD shows it evolved.</summary>
        public static WeaponEvolutionData EvolutionOf(UpgradeData.UpgradeType baseWeapon)
        {
            var catalog = GameContentCatalog.Load();
            if (catalog?.CollectionEvolutions == null) return null;
            foreach (var evolution in catalog.CollectionEvolutions)
                if (evolution != null && evolution.BaseWeapon == baseWeapon) return evolution;
            return null;
        }

        /// <summary>The entry with <paramref name="id"/>, or null.</summary>
        public static CollectionEntry Find(string id)
        {
            foreach (var entry in All()) if (entry.Id == id) return entry;
            return null;
        }

        /// <summary>Found entries among the catalog's (an id the catalog no longer has is not counted on screen).</summary>
        public static int FoundCount(GameData data)
        {
            int n = 0;
            foreach (var entry in All()) if (CollectionBook.IsFound(data, entry.Id)) n++;
            return n;
        }

        public static string KindName(CollectionKind kind) => kind switch
        {
            CollectionKind.Weapon => "Weapon",
            CollectionKind.Evolution => "Evolution",
            CollectionKind.Passive => "Passive item",
            CollectionKind.Pickup => "Pickup",
            CollectionKind.Relic => "Relic",
            _ => "Arcana"
        };
    }
}
