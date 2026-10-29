using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using System.Linq;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// Yeh class Level Up hone par Upgrade Menu ki logic chalati hai.
    /// Ye game ko pause krti hai aur 3 random valid upgrades chunti hai.
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        private List<UpgradeData> _allUpgrades;
        [SerializeField] private PlayerExperience _experience;
        [SerializeField] private WeaponManager _weaponManager;
        [SerializeField] private PlayerController _playerController;
        
        // Is event ko UI system listen karega
        public event System.Action<List<UpgradeData>> OnLevelUpUIRound;

        // Pending level-ups ki queue (agar ek sath bohot saara XP mile aur multiple level ups hon)
        private int _pendingLevelUps = 0;
        private bool _isLevelUpActive = false;

        // VS rule: Reroll naye choices deta hai, Skip level-up chhod deta hai, Banish item ko
        // is run ke liye pool se hata deta hai. Starting charges provisional hain (baad mein
        // meta shop / character se aayenge).

        public int RerollsLeft { get; private set; }
        public int SkipsLeft { get; private set; }
        public int BanishesLeft { get; private set; }
        public bool IsLevelUpActive => _isLevelUpActive;

        private readonly HashSet<UpgradeData.UpgradeType> _banishedTypes = new HashSet<UpgradeData.UpgradeType>();

        public bool IsBanished(UpgradeData.UpgradeType type) => _banishedTypes.Contains(type);

        public const int BaseChoiceCount = 3;
        private int _currentChoiceCount = BaseChoiceCount;

        /// <summary>
        /// VS rule: the chance of a 4th level-up choice is 1 - 1/Luck (0% at Luck 1, 50% at Luck 2).
        /// <paramref name="roll01"/> is a uniform random value in [0, 1).
        /// </summary>
        public static int ChoiceCountForLuck(float luck, float roll01)
        {
            if (luck <= 1f) return BaseChoiceCount;
            float fourthChance = 1f - 1f / luck;
            return roll01 < fourthChance ? BaseChoiceCount + 1 : BaseChoiceCount;
        }

        /// <summary>The run's level-up manager (null between runs). Chests reach it here: a scene search on every chest
        /// cost 5-7 ms with 300 enemies on screen (2026-10-17).</summary>
        public static LevelUpManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            // VS (Reroll, Skip and Banish pages): a run starts with none; the PowerUps give 2 per rank (AddPowerUpCharges),
            // and Soul Eggs add theirs. The scene's _starting* values (2 each, Soul Hunter's old default) are no longer used.
            RerollsLeft = SkipsLeft = BanishesLeft = 0;
            MaxedRewards.Always = MaxedRewards.AlwaysChoice.None; // a run's "always gold/chicken" choice starts unset

            // Learning Comment:
            // "Read First, Match Later" aur Self-Healing pattern:
            // Agar Inspector mein references unlinked / null reh jayein, toh scene se dynamically dhoond kar bind karein.
            if (_experience == null)
            {
                _experience = FindFirstObjectByType<PlayerExperience>();
            }
            if (_weaponManager == null)
            {
                _weaponManager = FindFirstObjectByType<WeaponManager>();
            }
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<PlayerController>();
            }
        }

        /// <summary>Adds charges (the PowerUps at run start; tests).</summary>
        public void AddCharges(int rerolls, int skips, int banishes)
        {
            RerollsLeft += Mathf.Max(0, rerolls);
            SkipsLeft += Mathf.Max(0, skips);
            BanishesLeft += Mathf.Max(0, banishes);
        }

        /// <summary>VS PowerUps: Reroll, Skip and Banish each give 2 charges per rank.</summary>
        private void AddPowerUpCharges()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null ||
                !SoulHunter.Core.Services.GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) ||
                save.CurrentData == null) return;
            var data = save.CurrentData;
            // ActiveRank like every other PowerUp reader; Reroll, Skip and Banish can never be switched off (wiki PowerUps),
            // so for them it is always the bought rank.
            AddCharges(Mathf.RoundToInt(SoulHunter.Core.Persistence.PowerUps.Bonus(SoulHunter.Core.Persistence.PowerUpId.Reroll, SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, SoulHunter.Core.Persistence.PowerUpId.Reroll))),
                       Mathf.RoundToInt(SoulHunter.Core.Persistence.PowerUps.Bonus(SoulHunter.Core.Persistence.PowerUpId.Skip, SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, SoulHunter.Core.Persistence.PowerUpId.Skip))),
                       Mathf.RoundToInt(SoulHunter.Core.Persistence.PowerUps.Bonus(SoulHunter.Core.Persistence.PowerUpId.Banish, SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, SoulHunter.Core.Persistence.PowerUpId.Banish))));
        }

        /// <summary>
        /// Spends one Reroll charge outside a level-up (the Arcana choice: wiki Arcanas, "The player may spend Rerolls to
        /// get new choices"); Preserve may keep it. False with none left.
        /// </summary>
        public bool SpendRerollCharge()
        {
            if (RerollsLeft <= 0) return false;
            if (!Preserved()) RerollsLeft--;
            return true;
        }

        /// <summary>VS Preserve: the chance that a Reroll, Skip or Banish is kept after use.</summary>
        private bool Preserved() => _playerController != null && _playerController.Stats != null && Random.value < _playerController.Stats.Preserve;

        /// <summary>
        /// VS Skip: the level's choices are skipped "and gain experience instead". The Skip page's tips give the amount:
        /// 20% of the experience to the next level (a thin source; the page gives no formula).
        /// </summary>
        public const float SkipExperienceShare = 0.2f;

        /// <summary>VS Golden Egg rule: the selected character's Soul Eggs add whole Reroll, Skip and Banish charges.</summary>
        private void AddSoulEggCharges()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null ||
                !SoulHunter.Core.Services.GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) ||
                save.CurrentData == null) return;
            var eggs = SoulEggs.For(save.CurrentData, save.CurrentData.SelectedCharacterName);
            if (eggs == null) return;
            RerollsLeft += SoulEggs.Whole(eggs.Reroll);
            SkipsLeft += SoulEggs.Whole(eggs.Skip);
            BanishesLeft += SoulEggs.Whole(eggs.Banish);
        }

        /// <summary>Takes back the Reroll, Skip and Banish charges the Soul Eggs gave (Cappella Magna's egg snap); spent ones stay spent.</summary>
        public void RemoveSoulEggCharges(SoulHunter.Core.Persistence.SoulEggRecord eggs)
        {
            if (eggs == null) return;
            RerollsLeft -= Mathf.Min(RerollsLeft, SoulEggs.Whole(eggs.Reroll));
            SkipsLeft -= Mathf.Min(SkipsLeft, SoulEggs.Whole(eggs.Skip));
            BanishesLeft -= Mathf.Min(BanishesLeft, SoulEggs.Whole(eggs.Banish));
        }

        private void Start()
        {
            // Unity Inspector se bachne ke liye Resources folder se load kar rahe hain
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            _allUpgrades = (catalog != null ? catalog.Upgrades : Resources.LoadAll<UpgradeData>("Upgrades")).Where(u => u != null && u.Level > 0).ToList();
            AddPowerUpCharges();
            AddSoulEggCharges();

            if (_allUpgrades.Count == 0)
            {
                Debug.LogWarning("[LevelUpManager] Koi UpgradeData nahi mila! Kya wo 'Resources/Upgrades' folder mein hain?");
            }

            if (_experience != null)
            {
                _experience.OnLevelUp += HandleLevelUp;
            }
            else
            {
                Debug.LogError("[LevelUpManager] PlayerExperience nahi mila! Level Up event bind nahi ho saka.");
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            RestorePauseAction(); // a scene unload during a level-up must not leave the Pause key off for the next run
            if (_experience != null)
            {
                _experience.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] Level Up Event received! New Level: {newLevel}");
            _pendingLevelUps++;

            // Agar pehle se koi Level Up menu nahi khula, toh agla process karo
            if (!_isLevelUpActive)
            {
                ProcessNextLevelUp();
            }
        }

        private void ProcessNextLevelUp()
{
    SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] >>> ProcessNextLevelUp ENTER | pending={_pendingLevelUps} | active={_isLevelUpActive} | timeScale={Time.timeScale}");

    if (_pendingLevelUps <= 0)
    {
        SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] No pending level-ups. Resuming gameplay.");
        _isLevelUpActive = false;
        ResumeGameplay();
        return;
    }

    _isLevelUpActive = true;

    SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] >>> Preparing level-up UI | pending={_pendingLevelUps}");

    // IMPORTANT: generate choices BEFORE pausing.
    // VS rule: Luck gives a chance of a 4th choice, rolled once per level-up.
    var stats = _playerController != null ? _playerController.Stats : null;
    _currentChoiceCount = ChoiceCountForLuck(stats != null ? stats.Luck : 1f, Random.value);

    SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] >>> Calling GetRandomValidUpgrades({_currentChoiceCount})");

    List<UpgradeData> choices = GetLevelUpChoices(_currentChoiceCount);

    SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] <<< GetRandomValidUpgrades returned | count={choices.Count}");

    if (choices.Count == 0)
    {
        // VS rule: once every item is maxed, a level-up still pays out
        // (floor chicken heal + gold) instead of being thrown away. With Limit Break on, cards are offered instead
        // (see GetLevelUpChoices), so this only happens when the player holds no weapon at all.
        SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] No valid upgrades available. Granting {_pendingLevelUps} maxed-out reward(s).");
        for (; _pendingLevelUps > 0; _pendingLevelUps--) GrantMaxedOutReward();
        _isLevelUpActive = false;
        ResumeGameplay();
        return;
    }

    // VS (wiki Level_up): with "always" gold or chicken chosen, a maxed level-up pays out at once, with no screen.
    if (AreMaxedRewardCards(choices) && MaxedRewards.Always != MaxedRewards.AlwaysChoice.None)
    {
        for (; _pendingLevelUps > 0; _pendingLevelUps--) MaxedRewards.ApplyAlways(_playerController);
        _isLevelUpActive = false;
        ResumeGameplay();
        return;
    }

    // VS Random LevelUp (Soul Hunter: Blind Fate, release item P3-05): "removes the player's ability to manually select
    // items during level-ups. Instead, the game automatically designates a random weapon or passive item to be added or
    // upgraded ... If Limit Break is also enabled, Random LevelUp will continue to apply its effects to it as well" (wiki
    // Stages); "each level up randomly chooses an option for the player" (wiki Level_up). No screen opens and the game
    // never pauses.
    if (SoulHunter.Gameplay.Data.RunModes.RandomLevelUp)
    {
        var pick = RandomPick(choices, Random.value);
        if (pick != null)
        {
            SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] Blind Fate picked {pick.UpgradeName}.");
            OnRandomPick?.Invoke(pick);
            SelectUpgrade(pick); // the 240 ms after-pick invulnerability still comes from ResumeGameplay
            return;
        }
    }

    SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] >>> Valid choices found:");

    for (int i = 0; i < choices.Count; i++)
    {
        SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] Choice {i}: {choices[i].UpgradeName} | Level={choices[i].Level} | Type={choices[i].Type}");
    }

    // ONLY pause once we know the UI can be presented.
    SoulHunter.Core.Services.GameTime.Pause(this);
    DisablePauseAction(); // Bug MIS UI-01: Esc/Start must not open the pause menu under the level-up screen

    SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] >>> Game PAUSED | timeScale={Time.timeScale}");

    if (OnLevelUpUIRound != null)
    {
        SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] >>> Sending choices to LevelUpUI");

        _offered = choices;
        OnLevelUpUIRound.Invoke(choices);

        SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] <<< LevelUpUI event returned");
    }
    else
    {
        Debug.LogWarning("[LevelUpManager] No LevelUpUI subscriber found. Auto-selecting first upgrade.");

        SelectUpgrade(choices[0]);
    }
}
        
        /// <summary>
        /// Jab player UI mein kisi ek card par click karega, toh yeh function call hoga.
        /// </summary>
        public void SelectUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return;
            // VS Limit Break: a Limit Break card strengthens its weapon beyond max level instead of levelling it.
            if (!MaxedRewards.TryApply(upgrade, _playerController) && !LimitBreak.TryApplyCard(upgrade) && _weaponManager != null)
            {
                _weaponManager.ApplyUpgrade(upgrade);
            }
            
            SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] Acquired Upgrade: {upgrade.UpgradeName}");
            CompleteCurrentLevelUp();
        }

        /// <summary>A Blind Fate (VS Random LevelUp) pick, raised before it is applied (the HUD shows its icon over the hero).</summary>
        public event System.Action<UpgradeData> OnRandomPick;

        /// <summary>
        /// The Blind Fate pick among a level-up's choices. The wiki does not say how the random item is weighted; the choices
        /// are already drawn with the VS rarity weights and owned-item checks (DrawChoices), so one of them is taken with equal
        /// chance, and the pick follows the same odds as the cards a player would see (balance method, 2026-10-09).
        /// Maxed-out levels: the wiki is silent; Gold Pouch and Roast Chicken are equally likely, and the "Always ..." cards are
        /// never picked, because a random pick must not lock a run-long choice the player never made (balance method).
        /// Reroll, Skip and Banish are not used: no screen opens, so nothing is spent and the charges stay for later.
        /// </summary>
        public static UpgradeData RandomPick(List<UpgradeData> choices, float roll01)
        {
            if (choices == null) return null;
            var pool = choices.Where(c => c != null && !(MaxedRewards.TryGetKind(c, out var kind)
                                                          && (kind == MaxedRewards.Kind.AlwaysGold || kind == MaxedRewards.Kind.AlwaysChicken))).ToList();
            if (pool.Count == 0) return null;
            return pool[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(roll01) * pool.Count), 0, pool.Count - 1)];
        }

        /// <summary>The choices on screen now (null between level-ups).</summary>
        private List<UpgradeData> _offered;

        /// <summary>VS (wiki Level_up): "Skips and Rerolls are unavailable when the player has maxed out their inventory."</summary>
        public bool OfferingMaxedChoices => _offered != null && (AreMaxedRewardCards(_offered) || _offered.Any(LimitBreak.IsCard));
        public bool CanReroll => _isLevelUpActive && RerollsLeft > 0 && !OfferingMaxedChoices;
        public bool CanSkip => _isLevelUpActive && SkipsLeft > 0 && !OfferingMaxedChoices;
        /// <summary>
        /// Banish on the maxed-out Gold Pouch / Roast Chicken cards spent a charge and did nothing (Bug MIS CORE-09,
        /// 2026-10-19). VS wiki (Banish, raw 2026-10-19): Banish "affects the amount of times the player can remove an item
        /// choice for the rest of the run"; gold and a Floor Chicken are not items, and the page says nothing of them, so by
        /// the balance method Banish is unavailable there, as Skip and Reroll are (wiki Level_up). Limit Break cards keep it:
        /// "Banished weapons can be offered Limit Break upgrades", and banishing one deals new cards.
        /// </summary>
        public bool CanBanish => _isLevelUpActive && BanishesLeft > 0 && !AreMaxedRewardCards(_offered);

        private static bool AreMaxedRewardCards(List<UpgradeData> choices) => choices != null && choices.Count > 0 && choices.All(MaxedRewards.IsCard);

        /// <summary>Offers a fresh set of choices for the current level-up. Returns false when not allowed.</summary>
        public bool Reroll()
        {
            if (!CanReroll) return false;
            if (!Preserved()) RerollsLeft--;
            // VS (wiki Reroll): "The items that were discarded by a reroll cannot appear in the new set of items."
            var discarded = new HashSet<UpgradeData.UpgradeType>();
            if (_offered != null) foreach (var item in _offered) if (item != null) discarded.Add(item.Type);
            PresentChoicesOrPayOut(GetLevelUpChoices(_currentChoiceCount, discarded));
            return true;
        }

        /// <summary>Gives up the current level-up without taking anything. Returns false when not allowed.</summary>
        public bool Skip()
        {
            if (!CanSkip) return false;
            if (!Preserved()) SkipsLeft--;
            SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] Level-up skipped.");
            CompleteCurrentLevelUp();
            // "and gain experience instead" (after the level-up closes, so the XP can start the next one).
            if (_experience != null) _experience.AddXP(Mathf.Max(1, Mathf.RoundToInt(_experience.XPToNextLevel * SkipExperienceShare)));
            return true;
        }

        /// <summary>
        /// Removes an item from the pool for the rest of the run (level-ups and chests),
        /// then re-rolls the current choices. Returns false when not allowed.
        /// </summary>
        public bool Banish(UpgradeData upgrade)
        {
            if (!CanBanish || upgrade == null || MaxedRewards.IsCard(upgrade)) return false;
            if (!Preserved()) BanishesLeft--;
            // A Limit Break card is not an item: banishing it only deals new cards and keeps its weapon in the pool.
            if (!LimitBreak.IsCard(upgrade))
            {
                _banishedTypes.Add(upgrade.Type);
                SoulHunter.Core.Services.GameLog.Info($"[LevelUpManager] Banished {upgrade.Type} for this run.");
            }
            PresentChoicesOrPayOut(GetLevelUpChoices(_currentChoiceCount));
            return true;
        }

        private void PresentChoicesOrPayOut(List<UpgradeData> choices)
        {
            if (choices.Count == 0)
            {
                // Banishing the last available item: pay out like a maxed-out level-up.
                GrantMaxedOutReward();
                CompleteCurrentLevelUp();
                return;
            }
            _offered = choices;
            OnLevelUpUIRound?.Invoke(choices);
        }

        private void CompleteCurrentLevelUp()
        {
            _offered = null;
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);

            // Agar aur level ups pending hain toh agla round dikhao, warna game resume karo
            if (_pendingLevelUps > 0)
            {
                ProcessNextLevelUp();
            }
            else
            {
                _isLevelUpActive = false;
                ResumeGameplay();
                SoulHunter.Core.Services.GameLog.Info("[LevelUpManager] All Level Ups processed. Game Resumed.");
            }
        }

        /// <summary>
        /// A maxed-out level-up that no card screen can show (no WeaponManager, so no reward cards are built). It gave both
        /// 30 health and 25 gold, with no Recovery bonus and no Big Coin Bag counted (Bug MIS ITM-13, 2026-10-19). VS wiki
        /// (Level_up, raw 2026-10-19) offers "the options to gain extra Gold or restore health with Floor Chicken", one of
        /// the two, and "The 'Always Floor Chicken' option will only give Floor Chickens if the player has missing health.
        /// If the player is fully healed, they will receive Gold instead." With no screen to choose on, the run's "always"
        /// choice pays if one was made; otherwise the same rule as Always Floor Chicken (balance method: the wiki is silent
        /// on a level-up with no screen): a Floor Chicken with Recovery when hurt, else one counted Big Coin Bag.
        /// </summary>
        private void GrantMaxedOutReward()
        {
            if (MaxedRewards.ApplyAlways(_playerController)) return;
            MaxedRewards.GiveChickenOrGold(_playerController);
        }

        /// <summary>
        /// VS wiki (Level_up): "After selecting an item, the game resumes and the character gains a brief moment of
        /// invulnerability." The wiki gives no length; Soul Hunter uses the hero's own hit invulnerability (VS
        /// Invulnerability Time, 240 ms).
        /// </summary>
        public const float FallbackInvulnerabilitySeconds = SoulHunter.Gameplay.Combat.HealthController.VsInvulnerabilityTime;

        // The same switch as the chest, Arcana and Revive screens (ChestUI.DisablePauseAction): while a level-up holds the
        // pause, Esc / Start would open the pause menu drawn under this screen (HUD canvas order 0 against the level-up's
        // 300) and move keyboard focus to its hidden Resume button.

        private void DisablePauseAction()
        {
            SoulHunter.Core.Input.PauseKeyHolds.Hold(this, PauseAction()); // counted: overlapping screens share the key
        }

        private void RestorePauseAction()
        {
            SoulHunter.Core.Input.PauseKeyHolds.Release(this, PauseAction());
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null) return null;
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }

        private void ResumeGameplay()
        {
            SoulHunter.Core.Services.GameTime.Resume(this);
            RestorePauseAction();
            if (_playerController != null)
            {
                _playerController.ResumeMovementAfterMenu();
                var health = _playerController.GetComponent<HealthController>();
                if (health != null)
                    health.GrantInvulnerability(health.PostHitInvulnerability > 0f ? health.PostHitInvulnerability : FallbackInvulnerabilitySeconds);
            }
        }

        /// <summary>
        /// The choices of one level-up: normal upgrades, or, when nothing is left to offer and VS Limit Break mode is on,
        /// Limit Break cards (one per weapon held, picked at random). Chests keep using GetRandomValidUpgrades (VS: Limit
        /// Break does not change treasure chests).
        /// </summary>
        public List<UpgradeData> GetLevelUpChoices(int count) => GetLevelUpChoices(count, null);

        private List<UpgradeData> GetLevelUpChoices(int count, HashSet<UpgradeData.UpgradeType> exclude)
        {
            var choices = GetRandomValidUpgrades(count, true, exclude);
            if (choices.Count == 0 && SoulHunter.Gameplay.Data.RunModes.LimitBreak)
                choices = LimitBreak.BuildCards(_weaponManager, count, IconFor);
            // VS wiki (Level_up): with nothing left to upgrade, the level-up offers gold or a Floor Chicken to choose from.
            if (choices.Count == 0 && _weaponManager != null)
                choices = MaxedRewards.BuildCards(SoulHunter.Gameplay.Pickups.GoldRewards.BigCoinBagsReceived >= MaxedRewards.AlwaysAfterBags);
            return choices;
        }

        private Sprite IconFor(UpgradeData.UpgradeType type)
        {
            if (_allUpgrades != null)
                foreach (var upgrade in _allUpgrades)
                    if (upgrade != null && upgrade.Type == type && upgrade.Icon != null) return upgrade.Icon;
            return null;
        }

        /// <summary>
        /// The level-up draw (VS wiki, https://vampire.survivors.wiki/w/Level_up): the eligible items, then the two
        /// "owned item" checks, then rarity-weighted picks without repetition. Chests call it without the owned checks.
        /// </summary>
        public List<UpgradeData> GetRandomValidUpgrades(int count) => GetRandomValidUpgrades(count, ownedChecks: true);

        private List<UpgradeData> GetRandomValidUpgrades(int count, bool ownedChecks, HashSet<UpgradeData.UpgradeType> exclude = null)
        {
            var validUpgrades = GetValidUpgrades();
            // VS (wiki Metaglio_Left / _Right): they "cannot be obtained from level ups" (stage items only); a chest can
            // still level one the player found.
            if (ownedChecks) validUpgrades.RemoveAll(u => UpgradeRarity.IsStageItemOnly(u.Type));
            if (validUpgrades.Count == 0) return new List<UpgradeData>();
            // A reroll's discarded items are left out of the new set; if nothing else is left, they may come back.
            if (exclude != null && exclude.Count > 0)
            {
                var rest = validUpgrades.Where(u => !exclude.Contains(u.Type)).ToList();
                if (rest.Count > 0) validUpgrades = rest;
            }

            // VS "Receiving owned items": only while the player's inventory is not full.
            float ownedChance = ownedChecks && !_weaponManager.IsInventoryFull ? OwnedChance(CurrentLuck(), LevelBeingOffered()) : 0f;
            var weapons = _weaponManager;
            return DrawChoices(validUpgrades, count, type => weapons.GetWeaponLevel(type) > 0, ownedChance, () => Random.value);
        }

        /// <summary>
        /// Every upgrade that can be offered now: the next level of an item (level 1 only while a slot is free), within its
        /// max level, not banished, and one per item (VS: an item cannot be offered more than once per level up).
        /// </summary>
        public List<UpgradeData> GetValidUpgrades()
        {
            List<UpgradeData> validUpgrades = new List<UpgradeData>();
            if (_weaponManager == null || _allUpgrades == null) return validUpgrades;
            var seen = new HashSet<UpgradeData.UpgradeType>();
            // VS item unlocks (wiki Weapons / Passive_items, fetched 2026-10-12: "Most weapons must be unlocked before they
            // can be offered to the player"): a locked item is never offered as a new item. No save (EditMode tests) = open.
            var unlockSave = UnlockSave();

            foreach (var up in _allUpgrades)
            {
                if (up == null) continue;
                // Logic: Agar Upgrade Level 1 hai, toh player ke paas wo Level 0 par hona chahiye.
                int currentLvl = _weaponManager.GetWeaponLevel(up.Type);
                // New items are only offered while a slot is free; otherwise picking
                // one would be rejected by WeaponManager and waste the level-up.
                if (currentLvl == up.Level - 1 && up.Level <= _weaponManager.GetMaxWeaponLevel(up.Type) &&
                    // A union's result is never offered new: it only comes from a chest (VS wiki Evolution).
                    !(currentLvl == 0 && (WeaponManager.IsStagePassive(up.Type) || WeaponUnionData.IsUnionResult(up.Type))) &&
                    !(currentLvl == 0 && unlockSave != null && !SoulHunter.Core.Persistence.ItemUnlocks.IsUnlocked(unlockSave, up.Type.ToString())) &&
                    _weaponManager.HasSlotFor(up.Type) && !_banishedTypes.Contains(up.Type) && seen.Add(up.Type))
                {
                    validUpgrades.Add(up);
                }
            }
            return validUpgrades;
        }

        /// <summary>The save the item locks are read from, or null when there is none.</summary>
        private static SoulHunter.Core.Persistence.GameData UnlockSave() =>
            SoulHunter.Core.Services.GameServices.Instance != null &&
            SoulHunter.Core.Services.GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) ? save.CurrentData : null;

        private float CurrentLuck()
        {
            var stats = _playerController != null ? _playerController.Stats : null;
            return stats != null ? stats.Luck : 1f;
        }

        /// <summary>The level the open level-up screen is for (the player's level when none is open).</summary>
        public int LevelOnScreen => _isLevelUpActive ? LevelBeingOffered() : (_experience != null ? _experience.CurrentLevel : 1);

        /// <summary>The level this level-up reaches (several queued level-ups are offered in order).</summary>
        private int LevelBeingOffered()
        {
            if (_experience == null) return 1;
            return Mathf.Max(1, _experience.CurrentLevel - Mathf.Max(0, _pendingLevelUps - 1));
        }

        /// <summary>
        /// VS wiki (Level_up, "Receiving owned items"): ownedChance = 1 + 0.3x - 1/totalLuck, where x = 2 if the player's
        /// level is even and x = 1 if it is odd. At Luck 1 that is 30% on odd levels and 60% on even levels.
        /// </summary>
        public static float OwnedChance(float totalLuck, int playerLevel)
        {
            float luck = Mathf.Max(0.0001f, totalLuck);
            int x = playerLevel % 2 == 0 ? 2 : 1;
            return Mathf.Clamp01(1f + 0.3f * x - 1f / luck);
        }

        /// <summary>
        /// The VS level-up draw on an already filtered pool (VS wiki, https://vampire.survivors.wiki/w/Level_up):
        /// 1. "Receiving owned items": the owned-item check is performed twice; each success adds an owned item selected
        ///    at random (uniformly: the wiki says "randomly selected" and gives no weight). If the second check selects the
        ///    same item as the first, it is discarded and a new item is pulled from the pool of all items instead.
        /// 2. "Random selection of items": the remaining options are drawn from the pool at random without repetition,
        ///    each with P = itemRarity / itemPoolWeight (UpgradeRarity.GetWeight).
        /// <paramref name="roll01"/> returns uniform values in [0, 1); an <paramref name="ownedChance"/> of 0 skips step 1.
        /// </summary>
        public static List<UpgradeData> DrawChoices(List<UpgradeData> valid, int count, System.Func<UpgradeData.UpgradeType, bool> isOwned,
            float ownedChance, System.Func<float> roll01)
        {
            var result = new List<UpgradeData>();
            if (valid == null || count <= 0) return result;
            var pool = new List<UpgradeData>(valid);

            if (ownedChance > 0f && isOwned != null)
            {
                var owned = valid.Where(u => isOwned(u.Type)).ToList();
                for (int check = 0; check < 2 && owned.Count > 0 && result.Count < count; check++)
                {
                    if (roll01() >= ownedChance) continue;
                    var pick = owned[Mathf.Clamp(Mathf.FloorToInt(roll01() * owned.Count), 0, owned.Count - 1)];
                    if (result.Contains(pick))
                    {
                        // Same item as the first check: discarded, and a new item comes from the pool of all items.
                        if (pool.Count == 0) continue;
                        int index = PickWeightedIndex(pool, roll01());
                        result.Add(pool[index]);
                        pool.RemoveAt(index);
                    }
                    else
                    {
                        result.Add(pick);
                        pool.Remove(pick);
                    }
                }
            }

            // Rarity-weighted picks without duplicates.
            while (result.Count < count && pool.Count > 0)
            {
                int index = PickWeightedIndex(pool, roll01());
                result.Add(pool[index]);
                pool.RemoveAt(index); // Ensure no duplicates
            }
            return result;
        }

        /// <summary>
        /// Picks an index with probability proportional to each upgrade's rarity weight
        /// (VS wiki Level_up: P(item) = itemRarity / itemPoolWeight).
        /// <paramref name="roll01"/> is a uniform random value in [0, 1).
        /// </summary>
        public static int PickWeightedIndex(List<UpgradeData> upgrades, float roll01)
        {
            int total = 0;
            foreach (var up in upgrades) total += Mathf.Max(0, UpgradeRarity.GetWeight(up.Type));
            if (total <= 0) return Mathf.Clamp(Mathf.FloorToInt(roll01 * upgrades.Count), 0, upgrades.Count - 1);

            float target = roll01 * total;
            for (int i = 0; i < upgrades.Count; i++)
            {
                target -= Mathf.Max(0, UpgradeRarity.GetWeight(upgrades[i].Type));
                if (target < 0f) return i;
            }
            return upgrades.Count - 1;
        }

        /// <summary>
        /// A chest's item. vampire.survivors.wiki/w/Treasure_Chest: chests contain "a random weapon or passive item the player
        /// currently has, leveling up the item by one" (patch 0.2.7: "can now only level up existing weapons"). So only
        /// owned items are drawn, by rarity weight. Nothing owned left to level: an empty list (the chest pays 25 gold).
        /// Chests used to draw from the whole level-up pool and hand out new items (Bug MIS B04, 2026-09-30).
        /// </summary>
        public List<UpgradeData> GetRandomValidUpgradesForChest() => GetRandomValidUpgradesForChest(weaponsOnly: false);

        /// <summary>
        /// As above; with <paramref name="weaponsOnly"/> only owned weapons are drawn (vampire.survivors.wiki/w/Template:Treasure:
        /// a "rewardN=weapon" slot is "Upgrade (weapon)").
        /// </summary>
        public List<UpgradeData> GetRandomValidUpgradesForChest(bool weaponsOnly)
        {
            if (_weaponManager == null) return new List<UpgradeData>();
            var owned = GetValidUpgrades().FindAll(up => _weaponManager.GetWeaponLevel(up.Type) > 0 &&
                                                         (!weaponsOnly || WeaponManager.IsWeaponType(up.Type)));
            if (owned.Count == 0) return owned;
            var weapons = _weaponManager;
            return DrawChoices(owned, 1, type => weapons.GetWeaponLevel(type) > 0, 0f, () => Random.value);
        }
    }
}
