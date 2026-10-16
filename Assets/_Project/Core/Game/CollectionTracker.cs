using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Fills the save's VS Collection and Bestiary during real runs (release item P3-02, 2026-10-09). GameSessionManager adds
    /// it, so every run has exactly one.
    /// - Collection (wiki Collection: entries "used by the player at least once in their save file"): every weapon and
    ///   passive the inventory holds (WeaponManager.OnInventoryChanged; evolutions come from EvolutionDiscovery), every
    ///   pickup collected (RunStatsTracker.PickupRecorded) and every Arcana received (ArcanaManager.ActiveArcanas).
    ///   Each new entry can reach a count unlock (Banish ranks, favourites), shown like any other unlock.
    /// - Bestiary (wiki Ars_Gouda: "statistics for all the enemies they have killed on their save file"): each defeat of an
    ///   enemy type (RunStatsTracker.EnemyKilled). The Bestiary itself opens when the run first reaches Level 3 (see
    ///   <see cref="Bestiary"/>).
    /// Kills are saved through SaveService.SaveSoon at most every <see cref="KillSaveSeconds"/> (the heavy-kill lesson: no
    /// write per kill), a new entry or unlock at once, and whatever is left when the run ends.
    /// </summary>
    public sealed class CollectionTracker : MonoBehaviour
    {
        /// <summary>
        /// Wiki silent (a save detail); balance method: kill counts are cosmetic, so a crash may lose up to 10 s of them,
        /// five times SaveSoon's 2 s, which keeps a heavy-kill run to at most one extra write per 10 s.
        /// </summary>
        public const float KillSaveSeconds = 10f;

        private SaveService _save;
        private WeaponManager _watchedWeapons;
        private int _arcanaCount = -1;
        private int _evolutionCount = -1;
        private float _lastKillSave = -999f;
        private bool _unsaved;

        private GameData Data => _save != null ? _save.CurrentData : null;

        private void OnEnable()
        {
            RunStatsTracker.EnemyKilled += HandleKill;
            RunStatsTracker.PickupRecorded += HandlePickup;
        }

        private void OnDisable()
        {
            RunStatsTracker.EnemyKilled -= HandleKill;
            RunStatsTracker.PickupRecorded -= HandlePickup;
            if (_watchedWeapons != null) _watchedWeapons.OnInventoryChanged -= HandleInventory;
            _watchedWeapons = null;
        }

        private void Start() => GameServices.Instance?.TryGet(out _save);

        private void OnDestroy()
        {
            if (_unsaved && _save != null && _save.CurrentData != null) _save.SaveGame();
        }

        private void Update()
        {
            if (_save == null) GameServices.Instance?.TryGet(out _save);
            // Follow the run's WeaponManager (it can be created after this tracker), as RunStatsTracker does.
            var weapons = WeaponManager.Instance;
            if (weapons != _watchedWeapons)
            {
                if (_watchedWeapons != null) _watchedWeapons.OnInventoryChanged -= HandleInventory;
                _watchedWeapons = weapons;
                if (weapons != null) { weapons.OnInventoryChanged += HandleInventory; HandleInventory(); }
            }
            var arcanas = ArcanaManager.Instance;
            int held = arcanas != null ? arcanas.ActiveArcanas.Count : 0;
            if (held != _arcanaCount)
            {
                _arcanaCount = held;
                if (arcanas != null)
                    foreach (var arcana in arcanas.ActiveArcanas)
                        if (arcana != null) MarkFound(CollectionCatalog.ArcanaId(arcana.Type));
            }
            var data = Data;
            // The Soul Egg (VS Golden Egg, a relic) counts once one is bought, in the Wandering Merchant's shop.
            if (data != null && !CollectionBook.IsFound(data, CollectionBook.SoulEggId) && data.SoulEggs != null &&
                data.SoulEggs.Exists(egg => egg != null && egg.Eggs > 0)) MarkFound(CollectionBook.SoulEggId);
            // An evolution discovered by a chest (EvolutionDiscovery) fills an entry too: check the count unlocks it reaches.
            int evolutions = data?.DiscoveredEvolutions?.Count ?? 0;
            if (data != null && evolutions != _evolutionCount)
            {
                _evolutionCount = evolutions;
                var news = CollectionBook.GrantCountUnlocks(data);
                foreach (var line in news) Announce(line);
                if (news.Count > 0) _save.SaveInBackground();
            }
            var progression = LevelProgressionManager.Instance;
            if (data != null && progression != null && progression.CurrentStage >= Bestiary.UnlockLevel && Bestiary.Unlock(data))
            {
                Announce("Unlocked: the Bestiary (main menu)");
                _save.SaveInBackground();
            }
        }

        private void HandleInventory()
        {
            if (_watchedWeapons == null) return;
            foreach (bool passives in new[] { false, true })
                foreach (var slot in _watchedWeapons.GetInventory(passives))
                    MarkFound(CollectionCatalog.ItemId(slot.Type));
            // Evolutions are not marked here: EvolutionDiscovery (written when a chest evolves a weapon) is the save's one
            // record of them, and the Collection reads it.
        }

        private void HandlePickup(string resultsName) => MarkFound(CollectionCatalog.PickupId(resultsName));

        /// <summary>Marks one Collection entry found; saves and announces what it unlocks.</summary>
        public void MarkFound(string id)
        {
            var data = Data;
            if (data == null || string.IsNullOrEmpty(id) || CollectionBook.IsFound(data, id)) return;
            var news = CollectionBook.MarkFound(data, id, out bool isNew);
            foreach (var line in news) Announce(line);
            if (isNew) _save.SaveSoon();
            if (news.Count > 0) _save.SaveInBackground();
        }

        private void HandleKill(EnemyData type)
        {
            var data = Data;
            if (data == null || type == null) return;
            bool first = Bestiary.RecordKill(data, type.name);
            _unsaved = true;
            if (first || Time.unscaledTime - _lastKillSave >= KillSaveSeconds)
            {
                _lastKillSave = Time.unscaledTime;
                _unsaved = false;
                _save.SaveSoon();
            }
        }

        private void Announce(string line)
        {
            var tracker = GetComponent<CharacterUnlockTracker>();
            if (tracker != null) tracker.AnnounceUnlock(line);
            else Debug.Log($"[CollectionTracker] {line}");
        }
    }
}
