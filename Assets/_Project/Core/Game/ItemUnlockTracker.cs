using System.Collections;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// The one place that checks the VS item unlock conditions during a real run (ItemUnlocks; vampire.survivors.wiki/w/
    /// Achievements, raw, fetched 2026-10-12: "Achievements, displayed as Unlocks in-game, unlock new items, characters,
    /// stages, gameplay features, or gold coins when completed"). It replaces the unused AchievementManager, which kept a
    /// second, in-run list no screen read. GameSessionManager adds it, so every run has exactly one.
    /// - Counts on the save: Soul Lanterns broken (LightSource.AnyBroken) and Roast Chickens picked up
    ///   (RunStatsTracker.PickupRecorded). Counters are saved through SaveService.SaveSoon (no write per lantern).
    /// - Checks every item's condition once per level second, when the inventory changes and when a level is cleared;
    ///   a new unlock is saved at once and shown by the existing unlock notice with what the item does.
    /// - Omni (the Cursed Coffer at its ninth level, PowerUpUnlocks.OmniItem).
    /// - Gives the run the lasting boons of the relics the save has found (RelicBook), once, at the start.
    /// </summary>
    public sealed class ItemUnlockTracker : MonoBehaviour
    {
        private SaveService _save;
        private LevelProgressionManager _progression;
        private WeaponManager _watchedWeapons;
        private int _lastCheckedSecond = -1;
        private bool _checkSoon = true;
        // Bug MIS ITM-08: every pickup (each Gold Fever coin, each gem in a stream) made the whole check run on its frame.
        // A pickup or a broken lantern now only marks the check as due, and it runs at most once every PickupCheckSeconds
        // of real time (as CharacterUnlockTracker does for kills). An unlock shows at most that much later; the per-second,
        // inventory and stage-clear checks are unchanged.
        private bool _pickupCheckDue;
        private float _nextPickupCheck;
        public const float PickupCheckSeconds = 0.25f;
        private float _clearedSeconds;

        /// <summary>How many times the full check has run (tests).</summary>
        public int EvaluationCount { get; private set; }

        /// <summary>The relic boons this run was given (tests): charges and fractions, by kind.</summary>
        public bool BoonsApplied { get; private set; }

        private GameData Data => _save != null ? _save.CurrentData : null;

        private void OnEnable()
        {
            LightSource.AnyBroken += HandleLantern;
            RunStatsTracker.PickupRecorded += HandlePickup;
        }

        private void OnDisable()
        {
            LightSource.AnyBroken -= HandleLantern;
            RunStatsTracker.PickupRecorded -= HandlePickup;
            if (_watchedWeapons != null) _watchedWeapons.OnInventoryChanged -= HandleInventory;
            _watchedWeapons = null;
            if (_progression != null) _progression.OnStageCleared -= HandleStageCleared;
            _progression = null;
        }

        private IEnumerator Start()
        {
            GameServices.Instance?.TryGet(out _save);
            // One frame later: the player's stats and the level-up manager have run their own Start (character, PowerUps,
            // Soul Eggs); the boons only add to them, so the order does not change any value.
            yield return null;
            ApplyRelicBoons();
        }

        private void Update()
        {
            if (_save == null) GameServices.Instance?.TryGet(out _save);
            if (_progression == null)
            {
                _progression = LevelProgressionManager.Instance;
                if (_progression != null) _progression.OnStageCleared += HandleStageCleared;
            }
            var weapons = WeaponManager.Instance;
            if (weapons != _watchedWeapons)
            {
                if (_watchedWeapons != null) _watchedWeapons.OnInventoryChanged -= HandleInventory;
                _watchedWeapons = weapons;
                if (weapons != null) weapons.OnInventoryChanged += HandleInventory;
                _checkSoon = true;
            }
            int second = _progression != null ? Mathf.FloorToInt(_progression.StageElapsedTime) : 0;
            bool pickupCheck = _pickupCheckDue && Time.unscaledTime >= _nextPickupCheck;
            if (second != _lastCheckedSecond || _checkSoon || pickupCheck)
            {
                _lastCheckedSecond = second;
                _checkSoon = false;
                _pickupCheckDue = false;
                _nextPickupCheck = Time.unscaledTime + PickupCheckSeconds;
                Evaluate();
            }
        }

        private void HandleInventory() => _checkSoon = true;

        private void HandleStageCleared(int stage)
        {
            // A level clears at 30:00: the full 30 minutes survived, even though the clock moves on to the next level.
            _clearedSeconds = 30f * 60f;
            Evaluate();
            _clearedSeconds = 0f;
        }

        private void HandleLantern(LightSource lantern, LightSourceDrops.Drop drop)
        {
            var data = Data;
            if (data == null) return;
            if (data.LightSourcesBroken < int.MaxValue) data.LightSourcesBroken++;
            _save.SaveSoon();
            _pickupCheckDue = true;
        }

        private void HandlePickup(string resultsName)
        {
            var data = Data;
            if (data == null) return;
            if (resultsName == PickupNames.Chicken)
            {
                if (data.FloorChickensFound < int.MaxValue) data.FloorChickensFound++;
                _save.SaveSoon();
            }
            // "Find a ..." conditions read the Collection, which CollectionTracker fills from this same event: check next
            // frame (or PickupCheckSeconds later), when it has been marked whichever handler ran first.
            _pickupCheckDue = true;
        }

        /// <summary>What the run has reached now, for ItemUnlocks.</summary>
        public ItemUnlockProgress CurrentProgress()
        {
            var data = Data;
            var player = PlayerController.Instance;
            var experience = player != null ? player.GetComponent<PlayerExperience>() : null;
            var weapons = WeaponManager.Instance;
            return new ItemUnlockProgress
            {
                Character = data != null ? GameContentCatalog.CurrentName(data.SelectedCharacterName) : null,
                PlayerLevel = experience != null ? experience.CurrentLevel : 1,
                StageSeconds = Mathf.Max(_clearedSeconds, _progression != null ? _progression.StageElapsedTime : 0f),
                WeaponsHeld = weapons != null ? weapons.GetActiveWeaponsCount() : 0,
                EvolutionsHeld = weapons != null ? EvolvedHeld(weapons) : 0,
                // Cached delegates (no two closures per check); they read the manager and the save when called.
                ItemLevel = _itemLevel ??= ItemLevelNow,
                CharacterUnlocked = _characterUnlocked ??= CharacterUnlockedNow,
            };
        }

        private System.Func<string, int> _itemLevel;
        private System.Func<string, bool> _characterUnlocked;

        private static int ItemLevelNow(string name)
        {
            var weapons = WeaponManager.Instance;
            return weapons != null && TryType(name, out var type) ? weapons.GetWeaponLevel(type) : 0;
        }

        private bool CharacterUnlockedNow(string name)
        {
            var character = GameContentCatalog.FindCharacterExact(name);
            return character != null && CharacterUnlocks.IsUnlocked(character, Data);
        }

        /// <summary>An UpgradeType by its name; false for a name this build's enum does not have.</summary>
        public static bool TryType(string name, out UpgradeData.UpgradeType type) =>
            System.Enum.TryParse(name, out type) && System.Enum.IsDefined(typeof(UpgradeData.UpgradeType), type);

        /// <summary>The notice line of a new item unlock: what was unlocked and what it does.</summary>
        public static string NoticeLine(in ItemUnlockInfo info)
        {
            if (!TryType(info.Item, out var type)) return $"New item unlocked: {info.Item}!";
            string what = UpgradeText.FallbackDescription(type);
            return string.IsNullOrEmpty(what) ? $"New item unlocked: {UpgradeText.NameOf(type)}!" : $"New item unlocked: {UpgradeText.NameOf(type)}. {what}";
        }

        /// <summary>Unlocks (and saves) every item whose condition the save and this run have met.</summary>
        public void Evaluate()
        {
            var data = Data;
            if (data == null) return;
            EvaluationCount++;
            var news = ItemUnlocks.Evaluate(data, CurrentProgress());
            bool changed = false;
            foreach (var info in news)
            {
                changed = true;
                // An item this build does not have (a type name the table lists for later) is recorded but not announced.
                if (TryType(info.Item, out _)) Announce(NoticeLine(info));
            }
            // Omni (wiki Omni: "unlocked by leveling up Torrona's Box to the maximum level"; see PowerUpUnlocks).
            var weapons = WeaponManager.Instance;
            if (weapons != null && PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Omni) == 0 && TryType(PowerUpUnlocks.OmniItem, out var coffer) &&
                weapons.GetWeaponLevel(coffer) >= PowerUpUnlocks.OmniItemLevel && PowerUpUnlocks.UnlockWhole(data, PowerUpId.Omni))
            {
                changed = true;
                Announce($"PowerUp unlocked: Omni. {PowerUps.Info(PowerUpId.Omni).Description}");
            }
            if (changed) _save.SaveInBackground();
        }

        /// <summary>Evolved (or united) weapons the inventory holds now.</summary>
        public static int EvolvedHeld(WeaponManager weapons)
        {
            int n = 0;
            if (weapons == null) return 0;
            foreach (var slot in weapons.GetInventory(false)) if (slot.Evolved) n++;
            return n;
        }

        /// <summary>
        /// The lasting boons of the relics on the save, once per run (RelicBook): stat fractions onto the player's stats,
        /// charges onto the level-up manager. Shadow Kael's copy is taken from the player's stats later, as with PowerUps.
        /// </summary>
        public void ApplyRelicBoons()
        {
            if (BoonsApplied) return;
            var data = Data;
            var player = PlayerController.Instance;
            if (data == null || player == null) return;
            BoonsApplied = true;
            foreach (var relic in RelicBook.All)
                if (RelicBook.IsFound(data, relic)) ApplyBoon(relic);
        }

        /// <summary>
        /// Gives the run in play one relic's boon (a relic found this run gives it at once; ApplyRelicBoons gives the
        /// save's relics at the start of a run).
        /// </summary>
        public void ApplyBoon(RelicInfo relic)
        {
            var player = PlayerController.Instance;
            var stats = player != null ? player.Stats : null;
            int charges = Mathf.RoundToInt(relic.Amount);
            switch (relic.Boon)
            {
                case RelicBoon.Luck: if (stats != null) stats.AddLuck(relic.Amount); break;
                case RelicBoon.Greed: if (stats != null) stats.AddGreed(relic.Amount); break;
                case RelicBoon.Growth: if (stats != null) stats.AddExpBonus(relic.Amount); break;
                case RelicBoon.MoveSpeed: if (stats != null) stats.AddMoveSpeed(relic.Amount); break;
                case RelicBoon.Might: if (stats != null) stats.AddMight(relic.Amount); break;
                case RelicBoon.Reroll:
                case RelicBoon.Skip:
                case RelicBoon.Banish:
                    var levelUp = FindFirstObjectByType<LevelUpManager>();
                    if (levelUp != null)
                        levelUp.AddCharges(relic.Boon == RelicBoon.Reroll ? charges : 0, relic.Boon == RelicBoon.Skip ? charges : 0,
                            relic.Boon == RelicBoon.Banish ? charges : 0);
                    break;
            }
        }

        /// <summary>Shows and lists an unlock through the run's one notice (CharacterUnlockTracker).</summary>
        public void Announce(string line)
        {
            var tracker = GetComponent<CharacterUnlockTracker>();
            if (tracker != null) tracker.AnnounceUnlock(line);
            else Debug.Log($"[ItemUnlockTracker] {line}");
        }
    }
}
