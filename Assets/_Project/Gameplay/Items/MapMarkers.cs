using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay
{
    /// <summary>
    /// Anything that wants a mark on the pause map implements this (a relic, a Soul Casket, a new kind of pickup): the map
    /// finds every active component that does, so a new thing appears on the map without the map being edited. The things
    /// that existed before the map (stage items, chests, pickups, lanterns, the merchant, the boss) are gathered by
    /// <see cref="SoulHunter.Gameplay.Pickups.MapMarkers"/> itself, so their classes stay as they are.
    /// <see cref="MapKind"/> is the name of a <see cref="SoulHunter.Gameplay.Pickups.MapMarkerKind"/> ("Relic", "Casket";
    /// "Soul Casket" and "Coffin" also mean Casket); any other word is drawn as Unknown, the "?" mark.
    /// </summary>
    public interface IMapMarker
    {
        string MapKind { get; }
        Vector3 MapPosition { get; }
        /// <summary>False while the thing should not be shown (already collected, hidden, not yet revealed).</summary>
        bool MapVisible { get; }
    }
}

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>What a mark on the pause map stands for.</summary>
    public enum MapMarkerKind { Hero, StageItem, Pickup, Chest, RunePlate, Lantern, Merchant, Boss, Relic, Casket, Unknown }

    /// <summary>One mark for the map: what it is, where it is in the world, and its picture and name when it has them.</summary>
    public struct MapMark
    {
        public MapMarkerKind Kind;
        public Vector3 Position;
        /// <summary>The thing's own picture (a stage item's icon), or null: the map then draws the kind's letter.</summary>
        public Sprite Icon;
        /// <summary>The thing's own name when it has one (a pickup's), else null.</summary>
        public string Name;
        /// <summary>The object the mark was read from (null for none); tests match marks to things with it.</summary>
        public Object Source;
    }

    /// <summary>
    /// Reads the world for the pause map (vampire.survivors.wiki/w/Milky_Way_Map, raw, fetched 2026-10-12): "The map displays
    /// the player's location as well as the locations of pickups, stage items, relics, coffins, and Treasure Chests in any
    /// stage"; Patch 0.7.2: "Added little icons for Vacuum, Rosary, Orologion, Nduja, and Floor Chicken on the map"; Patch
    /// 0.3.1: "treasure chests are now visible on the map".
    ///
    /// Soul Hunter's reading:
    /// - Player, stage items (weapons, passives and the stage's own chicken, Soul Magnet or Time Freeze) and chests: as the
    ///   wiki says.
    /// - Pickups: the ones the 0.7.2 note names and their Soul Hunter counterparts (Roast Chicken, Soul Magnet, Time Freeze,
    ///   Soul Toll, Hellfire Breath) plus the two charms (VS clovers, the same kind of light-source pickup). Soul Gems and
    ///   Soul Coins are left out: the wiki names no icon for them, and up to 400 gems would bury every other mark (balance
    ///   method).
    /// - Relics and caskets (VS coffins) come through <see cref="IMapMarker"/> when they exist.
    /// - Rune Plates, Soul Lanterns, the Wandering Merchant and the level's boss are NOT on the wiki's list. They are shown
    ///   because the map's task asked for them; the owner may want them off (<see cref="IsBeyondTheWiki"/> names them).
    /// It runs when the map opens (the game is paused), never per frame.
    /// </summary>
    public static class MapMarkers
    {
        /// <summary>True for the kinds the wiki's Milky Way Map page does not list (shown at the task's request).</summary>
        public static bool IsBeyondTheWiki(MapMarkerKind kind) =>
            kind == MapMarkerKind.RunePlate || kind == MapMarkerKind.Lantern || kind == MapMarkerKind.Merchant || kind == MapMarkerKind.Boss;

        /// <summary>Every mark the map shows now. The hero comes first when there is one.</summary>
        public static List<MapMark> Collect()
        {
            var marks = new List<MapMark>();
            var seen = new HashSet<Object>();

            var player = PlayerController.Instance;
            if (player != null) Add(marks, seen, MapMarkerKind.Hero, player.transform.position, player, null, null);

            // Stage items and Rune Plates: what the stage placed and nobody has taken yet.
            var spawner = Object.FindFirstObjectByType<StageItemSpawner>();
            if (spawner != null)
            {
                foreach (var item in spawner.Placed)
                {
                    if (item == null || !item.activeInHierarchy) continue; // taken (destroyed) or back in its pool
                    Add(marks, seen, MapMarkerKind.StageItem, item.transform.position, item, IconOf(item), NameOf(item));
                    MarkComponents(seen, item); // the pickup scan below must not count it a second time
                }
                foreach (var plate in spawner.Plates)
                    if (plate != null && plate.gameObject.activeInHierarchy)
                        Add(marks, seen, MapMarkerKind.RunePlate, plate.transform.position, plate, null, null);
            }

            foreach (var chest in Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None))
                Add(marks, seen, MapMarkerKind.Chest, chest.transform.position, chest, null, null);

            foreach (var chicken in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                Add(marks, seen, MapMarkerKind.Pickup, chicken.transform.position, chicken, null, PickupNames.Chicken);
            foreach (var pickup in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            {
                string name = SpecialPickupName(pickup);
                if (name != null) Add(marks, seen, MapMarkerKind.Pickup, pickup.transform.position, pickup, null, name);
            }

            foreach (var lantern in Object.FindObjectsByType<LightSource>(FindObjectsSortMode.None))
                Add(marks, seen, MapMarkerKind.Lantern, lantern.transform.position, lantern, null, null);

            foreach (var merchant in Object.FindObjectsByType<SoulHunter.Gameplay.Core.WanderingMerchant>(FindObjectsSortMode.None))
                if (!merchant.HasLeft) Add(marks, seen, MapMarkerKind.Merchant, merchant.transform.position, merchant, null, null);

            var enemies = SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsStageBoss && enemy.gameObject.activeInHierarchy)
                    Add(marks, seen, MapMarkerKind.Boss, enemy.transform.position, enemy, null, null);
            }

            // Anything else that asks for a mark (relics, caskets, later additions).
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!(behaviour is IMapMarker marker) || !marker.MapVisible) continue;
                Add(marks, seen, KindOf(marker.MapKind), marker.MapPosition, behaviour, null, null);
            }
            return marks;
        }

        /// <summary>The kind an <see cref="IMapMarker"/> names; a word the map does not know is Unknown.</summary>
        public static MapMarkerKind KindOf(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return MapMarkerKind.Unknown;
            string key = word.Replace(" ", "").Trim();
            if (key.Equals("SoulCasket", System.StringComparison.OrdinalIgnoreCase) || key.Equals("Coffin", System.StringComparison.OrdinalIgnoreCase))
                return MapMarkerKind.Casket;
            return System.Enum.TryParse(key, true, out MapMarkerKind kind) && System.Enum.IsDefined(typeof(MapMarkerKind), kind)
                ? kind : MapMarkerKind.Unknown;
        }

        /// <summary>True for the kinds the in-play guide arrows point at (see MapGuides).</summary>
        public static bool IsGuided(MapMarkerKind kind) =>
            kind == MapMarkerKind.StageItem || kind == MapMarkerKind.Relic || kind == MapMarkerKind.Casket || kind == MapMarkerKind.Unknown;

        private static void Add(List<MapMark> marks, HashSet<Object> seen, MapMarkerKind kind, Vector3 position, Object source, Sprite icon, string name)
        {
            if (source != null && !seen.Add(source)) return;
            marks.Add(new MapMark { Kind = kind, Position = position, Icon = icon, Name = name, Source = source });
        }

        private static void MarkComponents(HashSet<Object> seen, GameObject item)
        {
            foreach (var component in item.GetComponents<MonoBehaviour>())
                if (component != null) seen.Add(component);
        }

        /// <summary>The name of a pickup the map shows, or null for one it leaves out (gems, coins).</summary>
        private static string SpecialPickupName(Pickup pickup)
        {
            switch (pickup)
            {
                case MagnetPickup _: return PickupNames.Magnet;
                case TimeFreezePickup _: return PickupNames.TimeFreeze;
                case ScreenWipePickup _: return ScreenWipePickup.DisplayName;
                case FireBreathPickup _: return SoulHunter.Gameplay.Combat.FireBreath.DisplayName;
                case LuckCharmPickup _: return LuckCharmPickup.DisplayName;
                case GildedCharmPickup _: return GildedCharmPickup.DisplayName;
                default: return null;
            }
        }

        private static string NameOf(GameObject item)
        {
            var stageItem = item.GetComponent<StageItemPickup>();
            if (stageItem != null) return SoulHunter.Gameplay.Combat.UpgradeText.NameOf(stageItem.Upgrade);
            if (item.GetComponent<HealthPickup>() != null) return PickupNames.Chicken;
            var pickup = item.GetComponent<Pickup>();
            return pickup != null ? SpecialPickupName(pickup) : null;
        }

        /// <summary>A stage weapon or passive lies on the ground as its own icon (StageItemPickup): the map shows the same picture.</summary>
        private static Sprite IconOf(GameObject item)
        {
            if (item.GetComponent<StageItemPickup>() == null) return null;
            var renderer = item.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }
    }
}
