using System.Collections.Generic;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// The evolutions (and unions) found on this save (release items P3-01 / P3-06, 2026-10-09).
    /// vampire.survivors.wiki/w/Grim_Grimoire (raw, fetched 2026-10-09): the player "can view all the evolutions and unions
    /// that have been discovered on their save file"; it "shows evolutions that have been obtained before, and hides any
    /// evolutions that have yet to be encountered". So discovery is kept per save, and an entry is written when the
    /// evolution is obtained. Soul Hunter writes it only when a chest in a run evolves the weapon (ChestLogicController),
    /// never for a debug or bot start (-sh-autoplay-evolve calls WeaponManager.TryEvolveWeapon directly): those are not the
    /// player obtaining it. Ids are the WeaponEvolutionData / WeaponUnionData asset names (for example "Evo_BloodyTear").
    /// </summary>
    public static class EvolutionDiscovery
    {
        /// <summary>True when <paramref name="id"/> has been discovered on this save.</summary>
        public static bool IsDiscovered(GameData data, string id) =>
            data != null && data.DiscoveredEvolutions != null && !string.IsNullOrEmpty(id) && data.DiscoveredEvolutions.Contains(id);

        /// <summary>Records <paramref name="id"/> once. Returns true when it was new (the caller saves).</summary>
        public static bool MarkDiscovered(GameData data, string id)
        {
            if (data == null || string.IsNullOrEmpty(id)) return false;
            data.DiscoveredEvolutions ??= new List<string>();
            if (data.DiscoveredEvolutions.Contains(id)) return false;
            data.DiscoveredEvolutions.Add(id);
            return true;
        }

        /// <summary>
        /// SaveService repair: a save written before this list existed loads it as null (now empty); empty ids and repeats
        /// go. Unknown ids stay, so a recipe removed from a build and added back is still remembered.
        /// </summary>
        public static void Repair(GameData data)
        {
            if (data == null) return;
            data.DiscoveredEvolutions ??= new List<string>();
            var seen = new HashSet<string>();
            data.DiscoveredEvolutions.RemoveAll(id => string.IsNullOrEmpty(id) || !seen.Add(id));
        }
    }
}
