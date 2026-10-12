using System;
using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Level 3 signature (The Forgotten Village), following the VS Merchant rule
    /// (vampire.survivors.wiki/w/Merchant, checked 2026-09-26):
    /// - stands near where the stage starts; touching it opens the shop (the game pauses);
    /// - sells weapons (1,000 gold each, never past the 6 weapon slots) and repeatable Soul Eggs
    ///   (10,000 gold, VS Golden Egg), paid with the player's gold;
    /// - several things can be bought in one visit, but the shop can be visited once:
    ///   leaving it makes the merchant go away for the rest of the stage.
    /// Prices are the VS numbers and provisional Soul Hunter balance.
    ///
    /// Inverse mode wares (vampire.survivors.wiki/w/Merchant, raw, fetched 2026-10-09), sold only with Inverse on:
    /// - Skip 100, Banish 500 and Reroll 1,000 gold: "Gives +1 Skip for this run ... Inverse mode only. Can be purchased
    ///   repeatedly until the player has 20 Skips. Can only buy once if the character already start with 20 Skips."
    ///   (the same for Banish and Reroll). Stages page, Patch 1.0 notes: "The merchant also sells: Skip, Banish, and
    ///   Reroll (can carry up to 20) and 1 extra Arcana", so the cap counts the charges held now.
    /// - Pick a Card, 20,000 gold: "Gives an extra Arcana for this run. ... Inverse mode only." Once per visit ("1 extra
    ///   Arcana"). Since release item P2-01 (2026-10-09) it opens the Arcana choice of every unlocked Arcana not held
    ///   (wiki Arcanas: "All unlocked Arcanas are available as choices"), with Random. It is not held back by the
    ///   three-Arcana chest limit (that is a chest rule). Endless: the merchant comes back on Level 3 of every cycle with
    ///   the ware again (wiki Arcanas: "the Merchant will respawn with another for sale at the start of each normal game
    ///   length cycle ... until all unlocked Arcanas are collected").
    /// Soul Hunter's own ware names: Reroll / Skip / Banish Sigil and Draw a Fate.
    /// Not built: the Endless mode Revival ware (the wiki gives it no price; owner question).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WanderingMerchant : MonoBehaviour
    {
        /// <summary>A ware's state on the shop screen. Full: 20 charges held. SoldOut: Draw a Fate bought or no Arcana left.</summary>
        public enum WareState { Available, Owned, SlotsFull, TooExpensive, Full, SoldOut }
        /// <summary>The Inverse mode level-up charges the merchant sells.</summary>
        public enum ChargeWare { Reroll, Skip, Banish }

        /// <summary>VS wiki Merchant: Skips, Banishes and Rerolls can be bought "until the player has 20".</summary>
        public const int MaxRunCharges = 20;

        [Header("Wares (VS merchant: Bone and Cherry Bomb are the weapons Soul Hunter shares with its list)")]
        [SerializeField] private UpgradeData.UpgradeType[] _weaponWares = { UpgradeData.UpgradeType.Bone, UpgradeData.UpgradeType.CherryBomb };
        [SerializeField] private int _weaponPrice = 1000;
        [SerializeField] private int _soulEggPrice = 10000;

        [Header("Inverse mode wares (VS wiki Merchant prices)")]
        [SerializeField] private int _skipPrice = 100;
        [SerializeField] private int _banishPrice = 500;
        [SerializeField] private int _rerollPrice = 1000;
        [SerializeField] private int _arcanaPrice = 20000;

        [Header("Endless mode ware")]
        [Tooltip("VS wiki Stages (Endless): the merchant \"sells '+1 Revival' instead of Golden Eggs\". The wiki gives no price; balance method (2026-10-09): 10,000, the Golden Egg it replaces and the Revival PowerUp's initial cost.")]
        [SerializeField] private int _revivalPrice = 10000;

        private bool _isShopOpen;
        private bool _hasLeft;
        // Per visit: the charges held when the shop opened (the "already start with 20" case) and what was bought.
        private readonly Dictionary<ChargeWare, int> _heldAtOpen = new Dictionary<ChargeWare, int>();
        private readonly HashSet<ChargeWare> _chargesBought = new HashSet<ChargeWare>();
        private bool _arcanaBought;
        private bool _revivalBought;

        public bool IsShopOpen => _isShopOpen;
        /// <summary>True once the player has closed the shop; the merchant never opens again.</summary>
        public bool HasLeft => _hasLeft;
        public IReadOnlyList<UpgradeData.UpgradeType> WeaponWares => _weaponWares;
        public int WeaponPrice => _weaponPrice;
        public int SoulEggPrice => _soulEggPrice;
        public int SkipPrice => _skipPrice;
        public int BanishPrice => _banishPrice;
        public int RerollPrice => _rerollPrice;
        public int ArcanaPrice => _arcanaPrice;
        public int RevivalPrice => _revivalPrice;
        /// <summary>VS Endless: "+1 Revival" instead of Golden Eggs (Soul Hunter's Soul Egg), once per visit (one per cycle).</summary>
        public static bool SellsRevival => RunModes.Endless;
        /// <summary>VS: Skips, Banishes, Rerolls and the extra Arcana are sold only in Inverse mode.</summary>
        public static bool SellsInverseWares => RunModes.Inverse;
        /// <summary>What the last purchase did, shown in the shop (e.g. "Soul Egg: +1% Might").</summary>
        public string LastMessage { get; private set; } = "";

        private void OnTriggerEnter(Collider other)
        {
            if (_isShopOpen || _hasLeft || !other.CompareTag("Player")) return;
            OpenShop();
        }

        public void OpenShop()
        {
            if (_hasLeft || _isShopOpen) return;
            _heldAtOpen.Clear();
            _chargesBought.Clear();
            var levelUp = FindFirstObjectByType<LevelUpManager>();
            if (levelUp != null)
                foreach (ChargeWare ware in Enum.GetValues(typeof(ChargeWare))) _heldAtOpen[ware] = Held(ware, levelUp);
            _isShopOpen = true;
            GameTime.Pause(this);
            BuildShopUI();
        }

        /// <summary>Closes the screen and resumes the game (tests and scene teardown). Players leave with <see cref="Leave"/>.</summary>
        public void CloseShop()
        {
            _isShopOpen = false;
            GameTime.Resume(this);
            DestroyShopUI();
        }

        /// <summary>VS rule: once the player exits the shop, the merchant disappears from the map.</summary>
        public void Leave()
        {
            CloseShop();
            _hasLeft = true;
            if (Application.isPlaying) Destroy(gameObject);
        }

        // ---------- Rules (no services, so EditMode tests can drive them) ----------

        public WareState WeaponState(UpgradeData.UpgradeType type, WeaponManager weapons, int gold)
        {
            if (weapons != null && weapons.GetWeaponLevel(type) > 0) return WareState.Owned;
            if (weapons == null || !weapons.HasSlotFor(type)) return WareState.SlotsFull;
            return gold < _weaponPrice ? WareState.TooExpensive : WareState.Available;
        }

        /// <summary>
        /// Buys one of the merchant's weapons at level 1. Nothing is charged when the weapon is not sold here,
        /// is already owned, or no weapon slot is free.
        /// </summary>
        public bool TryBuyWeapon(UpgradeData.UpgradeType type, Func<int, bool> spendGold, WeaponManager weapons)
        {
            if (spendGold == null || weapons == null || Array.IndexOf(_weaponWares, type) < 0) return false;
            if (weapons.GetWeaponLevel(type) > 0 || !weapons.HasSlotFor(type)) return false;
            if (!spendGold(_weaponPrice)) return false;
            var levelOne = LevelOne(type);
            if (levelOne != null) weapons.ApplyUpgrade(levelOne); else weapons.GiveWeapon(type, 1);
            LastMessage = $"Bought {Name(type)}";
            return weapons.GetWeaponLevel(type) > 0;
        }

        /// <summary>Buys a Soul Egg for <paramref name="character"/>: one random stat, permanent, from the next run.</summary>
        public bool TryBuySoulEgg(GameData data, string character, Func<int, bool> spendGold, System.Random rng, out SoulEggs.Stat stat)
        {
            stat = default;
            if (data == null || string.IsNullOrEmpty(character) || spendGold == null || SellsRevival) return false;
            if (!spendGold(_soulEggPrice)) return false;
            stat = SoulEggs.Add(data, character, rng);
            LastMessage = $"Soul Egg: {SoulEggs.Describe(stat)} for {character} (from the next run)";
            return true;
        }

        public int ChargePrice(ChargeWare ware) => ware switch
        {
            ChargeWare.Reroll => _rerollPrice,
            ChargeWare.Skip => _skipPrice,
            _ => _banishPrice
        };

        /// <summary>Soul Hunter's own ware name (the level-up buttons keep the words Reroll, Skip and Banish).</summary>
        public static string ChargeWareName(ChargeWare ware) => $"{ware} Sigil";

        private static string Plural(ChargeWare ware) => ware == ChargeWare.Banish ? "Banishes" : $"{ware}s";

        /// <summary>The charges of <paramref name="ware"/> the player holds now.</summary>
        public static int Held(ChargeWare ware, LevelUpManager levelUp)
        {
            if (levelUp == null) return 0;
            return ware switch
            {
                ChargeWare.Reroll => levelUp.RerollsLeft,
                ChargeWare.Skip => levelUp.SkipsLeft,
                _ => levelUp.BanishesLeft
            };
        }

        /// <summary>
        /// VS wiki Merchant: "Can be purchased repeatedly until the player has 20 ... Can only buy once if the character
        /// already start with 20": below 20 held it can be bought; at 20 or more only when the shop opened with 20 or
        /// more and this ware was not bought yet in this visit.
        /// </summary>
        public static bool ChargeAllowed(int held, int heldAtOpen, bool boughtThisVisit) =>
            held < MaxRunCharges || (heldAtOpen >= MaxRunCharges && !boughtThisVisit);

        private int HeldAtOpen(ChargeWare ware, int heldNow)
        {
            // Nothing else changes the charges while the shop pauses the game, so the first look is the opening count.
            if (!_heldAtOpen.TryGetValue(ware, out var held)) _heldAtOpen[ware] = held = heldNow;
            return held;
        }

        public WareState ChargeState(ChargeWare ware, LevelUpManager levelUp, int gold)
        {
            if (!SellsInverseWares || levelUp == null) return WareState.SoldOut;
            int held = Held(ware, levelUp);
            if (!ChargeAllowed(held, HeldAtOpen(ware, held), _chargesBought.Contains(ware))) return WareState.Full;
            return gold < ChargePrice(ware) ? WareState.TooExpensive : WareState.Available;
        }

        /// <summary>
        /// Inverse mode only: +1 Reroll, Skip or Banish for this run. Nothing is charged outside Inverse mode or when the
        /// 20-charge cap refuses it.
        /// </summary>
        public bool TryBuyCharge(ChargeWare ware, LevelUpManager levelUp, Func<int, bool> spendGold)
        {
            if (!SellsInverseWares || levelUp == null || spendGold == null) return false;
            int held = Held(ware, levelUp);
            if (!ChargeAllowed(held, HeldAtOpen(ware, held), _chargesBought.Contains(ware))) return false;
            if (!spendGold(ChargePrice(ware))) return false;
            levelUp.AddCharges(ware == ChargeWare.Reroll ? 1 : 0, ware == ChargeWare.Skip ? 1 : 0, ware == ChargeWare.Banish ? 1 : 0);
            _chargesBought.Add(ware);
            LastMessage = $"{ChargeWareName(ware)}: you now hold {Held(ware, levelUp)} {Plural(ware)}";
            return true;
        }

        public WareState RevivalState(int gold)
        {
            if (!SellsRevival || _revivalBought) return WareState.SoldOut;
            return gold < _revivalPrice ? WareState.TooExpensive : WareState.Available;
        }

        /// <summary>Endless mode only: +1 Revival for this run, once per visit.</summary>
        public bool TryBuyRevival(PlayerStats stats, Func<int, bool> spendGold)
        {
            if (!SellsRevival || _revivalBought || stats == null || spendGold == null) return false;
            if (!spendGold(_revivalPrice)) return false;
            stats.AddRevival(1);
            _revivalBought = true;
            LastMessage = $"+1 Revival: you now have {stats.Revivals}";
            return true;
        }

        public WareState ArcanaState(ArcanaManager arcanas, int gold)
        {
            if (!SellsInverseWares || _arcanaBought || arcanas == null || arcanas.AvailableCount == 0) return WareState.SoldOut;
            return gold < _arcanaPrice ? WareState.TooExpensive : WareState.Available;
        }

        /// <summary>
        /// Inverse mode only: one extra Arcana for this run, once per visit. Soul Hunter has no Arcana choice screen, so a
        /// random Arcana the player does not hold is given. Nothing is charged when none is left to give.
        /// </summary>
        public bool TryBuyArcana(ArcanaManager arcanas, Func<int, bool> spendGold, float roll01)
        {
            if (!SellsInverseWares || _arcanaBought || arcanas == null || spendGold == null || arcanas.AvailableCount == 0) return false;
            if (!spendGold(_arcanaPrice)) return false;
            var given = arcanas.GrantRandomAvailableArcana(roll01);
            _arcanaBought = true;
            LastMessage = given != null ? $"Draw a Fate: {given.CardName}" : "";
            return given != null;
        }

        // ---------- Purchases through the game's services (shop buttons, AutoplayBot) ----------

        public bool BuyWeapon(UpgradeData.UpgradeType type)
        {
            if (!TryGetEconomy(out var economy)) return false;
            bool bought = TryBuyWeapon(type, economy.SpendGold, WeaponManager.Instance);
            if (bought) ShowCost(_weaponPrice);
            RefreshShopUI();
            return bought;
        }

        public bool BuySoulEgg()
        {
            if (!TryGetEconomy(out var economy) || !GameServices.Instance.TryGet<SaveService>(out var save) || save.CurrentData == null)
                return false;
            bool bought = TryBuySoulEgg(save.CurrentData, save.CurrentData.SelectedCharacterName, economy.SpendGold, new System.Random(), out _);
            if (bought) { save.SaveGame(); ShowCost(_soulEggPrice); }
            RefreshShopUI();
            return bought;
        }

        public bool BuyCharge(ChargeWare ware)
        {
            if (!TryGetEconomy(out var economy)) return false;
            int price = ChargePrice(ware);
            bool bought = TryBuyCharge(ware, FindFirstObjectByType<LevelUpManager>(), economy.SpendGold);
            if (bought) ShowCost(price);
            RefreshShopUI();
            return bought;
        }

        public bool BuyRevival()
        {
            if (!TryGetEconomy(out var economy)) return false;
            bool bought = TryBuyRevival(PlayerController.Instance != null ? PlayerController.Instance.Stats : null, economy.SpendGold);
            if (bought) ShowCost(_revivalPrice);
            RefreshShopUI();
            return bought;
        }

        public bool BuyArcana()
        {
            if (!TryGetEconomy(out var economy)) return false;
            var arcanas = ArcanaManager.Instance;
            if (!SellsInverseWares || _arcanaBought || arcanas == null || arcanas.AvailableCount == 0 || !economy.SpendGold(_arcanaPrice))
            {
                RefreshShopUI();
                return false;
            }
            _arcanaBought = true;
            ShowCost(_arcanaPrice);
            // The choice screen opens over the shop; the card is given when the player picks it.
            arcanas.OpenMerchantDraft(card =>
            {
                LastMessage = card != null ? $"Draw a Fate: {card.CardName}" : "";
                RefreshShopUI();
            });
            RefreshShopUI();
            return true;
        }

        private static bool TryGetEconomy(out EconomyService economy)
        {
            economy = null;
            return GameServices.Instance != null && GameServices.Instance.TryGet(out economy);
        }

        private static int CurrentGold() => TryGetEconomy(out var economy) ? economy.CurrentGold : 0;

        private void ShowCost(int cost)
        {
            if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-cost, transform.position + Vector3.up * 2f);
        }

        private static UpgradeData LevelOne(UpgradeData.UpgradeType type)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (catalog == null || catalog.Upgrades == null) return null;
            foreach (var upgrade in catalog.Upgrades)
                if (upgrade != null && upgrade.Type == type && upgrade.Level == 1) return upgrade;
            return null;
        }

        private static string Name(UpgradeData.UpgradeType type)
        {
            var levelOne = LevelOne(type);
            return levelOne != null ? UpgradeText.DisplayName(levelOne) : type.ToString();
        }

        // ---------- Shop screen (uGUI, so keyboard and controller can use it) ----------

        private GameObject _shopUI;
        /// <summary>The shop screen while open.</summary>
        public GameObject ShopUI => _shopUI;

        private void RefreshShopUI()
        {
            if (!_isShopOpen) return;
            DestroyShopUI();
            BuildShopUI();
        }

        /// <summary>One card on the shop screen.</summary>
        private struct Ware
        {
            public Sprite Icon;
            public string Name, Description, State;
            public bool Available;
            public Action OnClick;
        }

        private List<Ware> Wares(int gold)
        {
            var wares = new List<Ware>();
            foreach (var type in _weaponWares)
            {
                var state = WeaponState(type, WeaponManager.Instance, gold);
                var levelOne = LevelOne(type);
                var captured = type;
                wares.Add(new Ware
                {
                    Icon = WeaponDefinitionLookup.Find(type)?.Icon,
                    Name = Name(type),
                    Description = levelOne != null ? UpgradeText.Describe(levelOne, 0) : "",
                    State = state switch { WareState.Owned => "OWNED", WareState.SlotsFull => "NO FREE SLOT", _ => $"{_weaponPrice} G" },
                    Available = state == WareState.Available,
                    OnClick = () => BuyWeapon(captured)
                });
            }

            string character = GameServices.Instance != null && GameServices.Instance.TryGet<SaveService>(out var save) && save.CurrentData != null
                ? save.CurrentData.SelectedCharacterName : "your hunter";
            var catalog = GameContentCatalog.Load();
            if (SellsRevival)
            {
                var revivalState = RevivalState(gold);
                var revivalIcon = catalog != null ? catalog.PowerUpIconFor(SoulHunter.Core.Persistence.PowerUpId.Revival) : null;
                if (revivalIcon == null) revivalIcon = WeaponDefinitionLookup.Find(UpgradeData.UpgradeType.Tiragisu)?.Icon;
                wares.Add(new Ware
                {
                    Icon = revivalIcon,
                    Name = "Second Breath",
                    Description = "+1 Revival for this run.",
                    State = revivalState == WareState.SoldOut ? "BOUGHT" : $"{_revivalPrice} G",
                    Available = revivalState == WareState.Available,
                    OnClick = () => BuyRevival()
                });
            }
            else wares.Add(new Ware
            {
                Icon = catalog != null ? catalog.SoulEggIcon : null,
                Name = "Soul Egg",
                Description = $"Permanently raises a random stat of {character}, from the next run.",
                State = $"{_soulEggPrice} G",
                Available = gold >= _soulEggPrice,
                OnClick = () => BuySoulEgg()
            });

            if (!SellsInverseWares) return wares;

            var arcanaState = ArcanaState(ArcanaManager.Instance, gold);
            wares.Add(new Ware
            {
                Icon = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Emblem),
                Name = "Draw a Fate",
                Description = "Choose one more Arcana for this run.",
                State = arcanaState == WareState.SoldOut ? (_arcanaBought ? "BOUGHT" : "NONE LEFT") : $"{_arcanaPrice} G",
                Available = arcanaState == WareState.Available,
                OnClick = () => BuyArcana()
            });

            var levelUp = FindFirstObjectByType<LevelUpManager>();
            foreach (var ware in new[] { ChargeWare.Reroll, ChargeWare.Skip, ChargeWare.Banish })
            {
                var state = ChargeState(ware, levelUp, gold);
                var id = ware == ChargeWare.Reroll ? PowerUpId.Reroll : ware == ChargeWare.Skip ? PowerUpId.Skip : PowerUpId.Banish;
                var captured = ware;
                wares.Add(new Ware
                {
                    Icon = catalog != null ? catalog.PowerUpIconFor(id) : null,
                    Name = ChargeWareName(ware),
                    Description = $"+1 {ware} for this run. You hold {Held(ware, levelUp)} of {MaxRunCharges}.",
                    State = state == WareState.Full ? "FULL" : $"{ChargePrice(ware)} G",
                    Available = state == WareState.Available,
                    OnClick = () => BuyCharge(captured)
                });
            }
            return wares;
        }

        /// <summary>Up to this many wares stay in one column (the approved look); more may take two, as the width allows.</summary>
        public const int SingleColumnWares = 3;
        /// <summary>The narrowest ware card, in canvas units: a narrower panel shows fewer columns.</summary>
        public const float MinWareWidth = 540f;
        /// <summary>Under this card width the price moves up beside the name and the description takes the card's width.</summary>
        public const float NarrowWareBelow = 600f;
        /// <summary>A safe area shorter than this is "short" (a landscape phone): one header line, Leave beside the message.</summary>
        public const float ShortShopBelow = 700f;
        private const float OneColumnPanelWidth = 900f, TwoColumnPanelWidth = 1400f;
        private const float WareHeight = 120f, NarrowWareHeight = 150f;

        /// <summary>The most columns <paramref name="count"/> wares may take (the width may allow fewer).</summary>
        public static int MaxWareColumns(int count) => count > SingleColumnWares ? 2 : 1;

        /// <summary>The ware columns for <paramref name="count"/> wares in a list <paramref name="width"/> units wide.</summary>
        public static int WareColumns(int count, float width, float gap) =>
            SoulHunter.Gameplay.UI.UiScreen.ColumnsFor(width, MinWareWidth, gap, MaxWareColumns(count));

        /// <summary>A ware card's height at <paramref name="width"/>: taller when narrow, never under the touch target.</summary>
        public static float WareCardHeight(float width) =>
            Mathf.Max(width < NarrowWareBelow ? NarrowWareHeight : WareHeight, SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits * 1.3f);

        // The open shop's parts, kept so a rotation or a window change lays them out again without a rebuild.
        private RectTransform _shopPanel, _shopTitle, _shopGold, _shopWares, _shopMessage;
        private UnityEngine.UI.ScrollRect _shopScroll;
        private UnityEngine.UI.GridLayoutGroup _shopGrid;
        private UnityEngine.UI.Button _shopLeave;
        private readonly List<UnityEngine.UI.Button> _wareButtons = new List<UnityEngine.UI.Button>();
        private Vector2 _shopLaidOutFor = new Vector2(-1f, -1f);

        /// <summary>The wares' scrolling list (tests: a ware is on screen when it lies inside its viewport).</summary>
        public UnityEngine.UI.ScrollRect ShopWaresScroll => _shopScroll;
        /// <summary>The shop's Leave button, pinned to the bottom of the panel.</summary>
        public UnityEngine.UI.Button ShopLeaveButton => _shopLeave;

        private void BuildShopUI()
        {
            if (_shopUI != null || !Application.isPlaying) return;
            _shopUI = new GameObject("MerchantShop", typeof(RectTransform));
            var canvas = _shopUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            _shopUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            int gold = CurrentGold();
            var wares = Wares(gold);

            // Owner (2026-10-04): dark, full contrast (UITheme), on a dark veil over the paused game. Responsive (owner,
            // 2026-10-10): the veil stays full screen, the panel keeps to the safe area (LayoutShop sizes it).
            SoulHunter.Gameplay.UI.UITheme.AddVeil(_shopUI.transform);
            var safe = ShopRect("Safe", _shopUI.transform, Vector2.zero, Vector2.one);
            SoulHunter.Gameplay.UI.SafeAreaFitter.Fit(safe);
            _shopPanel = ShopRect("Panel", safe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(_shopPanel.gameObject.AddComponent<UnityEngine.UI.Image>());
            var title = ShopText("Title", _shopPanel, Vector2.zero, Vector2.zero, "WANDERING MERCHANT", 40f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(title, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
            _shopTitle = (RectTransform)title.transform;
            _shopGold = GoldRow(_shopPanel, gold);

            // The wares: a grid whose columns follow the width, scrolling when there are more than the room.
            _shopWares = ShopRect("Wares", _shopPanel, Vector2.zero, Vector2.zero);
            // Keyboard and controller: the focused ware scrolls into view (KeepSelectedInView).
            _shopScroll = SoulHunter.Gameplay.UI.ResponsiveScroll.Create(_shopWares, "WareListViewport", "WareList", out var content,
                SoulHunter.Gameplay.UI.ResponsiveScroll.Bar.Mark, fitContentHeight: true, keepSelectedInView: true);
            _shopGrid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            _shopGrid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            _shopGrid.childAlignment = TextAnchor.UpperCenter;

            _wareButtons.Clear();
            UnityEngine.UI.Button focus = null;
            foreach (var ware in wares)
            {
                var button = WareCard(content, ware.Icon, ware.Name, ware.Description, ware.State, ware.OnClick);
                SetAvailable(button, ware.Available);
                _wareButtons.Add(button);
                if (focus == null && button.interactable) focus = button;
            }

            var message = ShopText("Message", _shopPanel, Vector2.zero, Vector2.zero, LastMessage, 24f);
            message.color = SoulHunter.Gameplay.UI.UITheme.Rewards.Soul;
            _shopMessage = (RectTransform)message.transform;
            _shopLeave = ShopButton(_shopPanel, "Leave (the merchant moves on)", Leave);

            Canvas.ForceUpdateCanvases(); // a canvas made this frame has no scaled size yet
            LayoutShop();
            if (focus == null) focus = _shopLeave;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        private void OnEnable() => SoulHunter.Gameplay.UI.UiScreen.Changed += LayoutShop;

        private void LateUpdate()
        {
            if (_shopUI == null || _shopPanel == null) return;
            if (((RectTransform)_shopPanel.parent).rect.size != _shopLaidOutFor) LayoutShop();
        }

        /// <summary>
        /// Lays the open shop out for the current canvas, in one code path (owner, 2026-10-10: landscape and portrait from
        /// the same screen). The panel hugs its wares inside the safe area; the wares take one or two columns as the width
        /// allows and scroll when they are more than the room; Leave stays pinned to the panel's bottom edge. A short screen
        /// puts the title and gold on one line and the message beside Leave, so the wares keep their room.
        /// </summary>
        private void LayoutShop()
        {
            if (_shopUI == null || _shopPanel == null) return;
            var safe = (RectTransform)_shopPanel.parent;
            Vector2 room = safe.rect.size;
            if (room.x <= 0f || room.y <= 0f) return;
            _shopLaidOutFor = room;
            var theme = SoulHunter.Gameplay.UI.UiThemeAsset.Active;
            bool roomy = room.y >= ShortShopBelow;
            float margin = roomy ? 32f : 12f, pad = roomy ? 24f : 16f;
            float gap = Mathf.Max(8f, SoulHunter.Gameplay.UI.UITheme.Layout.ButtonGap);
            int count = _wareButtons.Count;

            // Width: one or two columns of cards, as many as fit.
            float panelWidth = Mathf.Min(room.x - 2f * margin, MaxWareColumns(count) > 1 ? TwoColumnPanelWidth : OneColumnPanelWidth);
            float listWidth = panelWidth - 2f * pad - SoulHunter.Gameplay.UI.ResponsiveScroll.MarkRoom;
            int columns = WareColumns(count, listWidth, gap);
            float cardWidth = Mathf.Floor((listWidth - gap * (columns - 1)) / columns);
            float cardHeight = Mathf.Floor(WareCardHeight(cardWidth));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float listHeight = rows * cardHeight + (rows - 1) * gap;

            // Leave at the theme's standard (touch) size.
            var leaveTheme = SoulHunter.Gameplay.UI.ThemedElement.For(_shopLeave.gameObject);
            Vector2 leaveSize = leaveTheme.StandardSize(theme) ?? new Vector2(SoulHunter.Gameplay.UI.UITheme.Layout.ButtonWidth, SoulHunter.Gameplay.UI.UITheme.Layout.ButtonHeight);
            const float titleHeight = 64f, goldHeight = 52f, messageHeight = 44f, shortHeader = 60f;
            float header = roomy ? titleHeight + gap + goldHeight : shortHeader;
            float footer = roomy ? messageHeight + gap + leaveSize.y : leaveSize.y;
            float chrome = pad + header + gap + gap + footer + pad;
            float panelHeight = Mathf.Min(room.y - 2f * margin, chrome + listHeight);
            _shopPanel.anchorMin = _shopPanel.anchorMax = _shopPanel.pivot = new Vector2(0.5f, 0.5f);
            _shopPanel.anchoredPosition = Vector2.zero;
            _shopPanel.sizeDelta = new Vector2(panelWidth, panelHeight);
            float inner = panelWidth - 2f * pad;

            var titleText = _shopTitle.GetComponent<TMPro.TextMeshProUGUI>();
            if (roomy)
            {
                PlaceTop(_shopTitle, pad, pad, inner, titleHeight);
                titleText.alignment = TMPro.TextAlignmentOptions.Center;
                PlaceTop(_shopGold, pad + (inner - 360f) * 0.5f, pad + titleHeight + gap, 360f, goldHeight);
            }
            else
            {
                PlaceTop(_shopTitle, pad, pad, inner - 300f - gap, shortHeader);
                titleText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                PlaceTop(_shopGold, pad + inner - 300f, pad + (shortHeader - goldHeight) * 0.5f, 300f, goldHeight);
            }

            // The list between the header and the footer.
            float listTop = pad + header + gap;
            PlaceTop(_shopWares, pad, listTop, inner, panelHeight - listTop - (gap + footer + pad));
            _shopGrid.spacing = new Vector2(gap, gap);
            _shopGrid.constraintCount = columns;
            _shopGrid.cellSize = new Vector2(cardWidth, cardHeight);
            foreach (var card in _wareButtons) LayoutWareCard((RectTransform)card.transform, cardWidth, cardHeight);

            // Leave pinned to the bottom edge (centred; at the right on a short screen with the message beside it).
            var leave = (RectTransform)_shopLeave.transform;
            leave.anchorMin = leave.anchorMax = new Vector2(roomy ? 0.5f : 1f, 0f);
            leave.pivot = new Vector2(0.5f, 0.5f);
            leave.sizeDelta = leaveSize;
            leave.anchoredPosition = new Vector2(roomy ? 0f : -(pad + leaveSize.x * 0.5f), pad + leaveSize.y * 0.5f);
            if (roomy) PlaceBottom(_shopMessage, pad, pad + leaveSize.y + gap, inner, messageHeight);
            else PlaceBottom(_shopMessage, pad, pad, inner - leaveSize.x - gap, leaveSize.y);

            foreach (var text in _shopUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                if (text.GetComponentInParent<UnityEngine.UI.Button>() != _shopLeave) SoulHunter.Gameplay.UI.UiTextFloor.Apply(text);

            // Keyboard and controller: neighbours by the grid's own columns and rows, Leave under them.
            var navigable = new List<(UnityEngine.UI.Button button, Vector2 centre)>();
            for (int i = 0; i < _wareButtons.Count; i++) navigable.Add((_wareButtons[i], new Vector2(i % columns, -(i / columns))));
            navigable.Add((_shopLeave, new Vector2(roomy ? (columns - 1) * 0.5f : columns - 1, -rows)));
            WireNavigation(navigable);

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_shopGrid.GetComponent<RectTransform>());
        }

        /// <summary>A rect measured from its parent's top-left corner, in canvas units.</summary>
        private static void PlaceTop(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>A rect measured from its parent's bottom-left corner, in canvas units.</summary>
        private static void PlaceBottom(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Keyboard and controller move only between the shop's usable buttons, by their place in the shop (automatic
        /// navigation could reach buttons of the paused screens under the veil).
        /// </summary>
        private static void WireNavigation(List<(UnityEngine.UI.Button button, Vector2 centre)> items)
        {
            foreach (var (button, centre) in items)
            {
                button.navigation = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = NextInDirection(items, button, centre, Vector2.up),
                    selectOnDown = NextInDirection(items, button, centre, Vector2.down),
                    selectOnLeft = NextInDirection(items, button, centre, Vector2.left),
                    selectOnRight = NextInDirection(items, button, centre, Vector2.right)
                };
            }
        }

        private static UnityEngine.UI.Selectable NextInDirection(List<(UnityEngine.UI.Button button, Vector2 centre)> items,
            UnityEngine.UI.Button from, Vector2 centre, Vector2 direction)
        {
            UnityEngine.UI.Selectable best = null;
            float bestScore = float.MaxValue;
            var across = new Vector2(-direction.y, direction.x);
            foreach (var (button, other) in items)
            {
                if (button == from || !button.interactable) continue;
                var offset = other - centre;
                float along = Vector2.Dot(offset, direction);
                if (along <= 0.001f) continue;
                float score = along + 2f * Mathf.Abs(Vector2.Dot(offset, across));
                if (score < bestScore) { bestScore = score; best = button; }
            }
            return best;
        }

        // Sold-out or unaffordable wares stay readable: the theme's unusable plate with its locked text colour
        // (ThemedElement.ShowUsable restyles them when the button's state changes).
        private static void SetAvailable(UnityEngine.UI.Button button, bool available)
        {
            button.interactable = available;
            if (!available) SoulHunter.Gameplay.UI.ThemedElement.For(button.gameObject).ShowUsable(false);
        }

        /// <summary>The player's gold with the theme's coin, like the Power Up screen (LayoutShop places the row).</summary>
        private static RectTransform GoldRow(RectTransform panel, int gold)
        {
            var row = ShopRect("GoldRow", panel, Vector2.zero, Vector2.zero);
            var coin = ShopRect("Coin", row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            coin.sizeDelta = new Vector2(44f, 44f);
            coin.anchoredPosition = new Vector2(-70f, 0f);
            var coinImage = coin.gameObject.AddComponent<UnityEngine.UI.Image>();
            coinImage.sprite = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin);
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            var text = ShopText("Gold", row, new Vector2(0.5f, 0f), new Vector2(1f, 1f), gold.ToString("N0"), 34f);
            text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            ((RectTransform)text.transform).offsetMin = new Vector2(-40f, 0f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(text, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold);
            return row;
        }

        /// <summary>
        /// A ware as a card like the Power Up and level-up cards (owner, 2026-10-08: the Pro look everywhere): the item's
        /// icon, its name and what it does, and its price with a coin (or OWNED / NO FREE SLOT). The state text is the
        /// card's first text. The grid sizes the card; <see cref="LayoutWareCard"/> places its parts.
        /// </summary>
        private UnityEngine.UI.Button WareCard(RectTransform list, Sprite icon, string name, string description, string state, Action onClick)
        {
            var rect = ShopRect("Ware", list, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            bool price = state.EndsWith(" G");
            // A price shows its coin instead of the "G" (like the Power Up screen); OWNED / NO FREE SLOT stay words.
            string shown = price && int.TryParse(state.Substring(0, state.Length - 2), out var amount) ? amount.ToString("N0") : state;
            var stateText = ShopText("Price", rect, Vector2.zero, Vector2.zero, shown, 30f);
            stateText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            if (price)
            {
                var coin = ShopRect("Coin", stateText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                coin.pivot = new Vector2(1f, 0.5f);
                coin.sizeDelta = new Vector2(34f, 34f);
                var coinImage = coin.gameObject.AddComponent<UnityEngine.UI.Image>();
                coinImage.sprite = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin);
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;
            }
            var iconRect = ShopRect("Icon", rect, Vector2.zero, Vector2.zero);
            var iconImage = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var nameText = ShopText("Name", rect, Vector2.zero, Vector2.zero, name, 32f);
            nameText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            var descText = ShopText("Description", rect, Vector2.zero, Vector2.zero, description, 22f);
            descText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Card, SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: false,
                size: SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(nameText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(descText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.CardSoft);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(stateText, price ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Tag);
            return button;
        }

        /// <summary>
        /// Places a ware card's parts for its size, in canvas units: icon at the left; wide, the price takes a column at
        /// the right; narrow (a phone held upright), the price sits beside the name and the description takes the width.
        /// </summary>
        private static void LayoutWareCard(RectTransform card, float width, float height)
        {
            const float inset = 14f, priceWide = 190f, priceNarrow = 150f, coinGap = 10f;
            bool narrow = width < NarrowWareBelow;
            float iconSize = Mathf.Min(72f, height - 2f * inset);
            var icon = (RectTransform)card.Find("Icon");
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(inset, 0f);
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            float textLeft = inset + iconSize + inset;
            var price = (RectTransform)card.Find("Price");
            var name = (RectTransform)card.Find("Name");
            var description = (RectTransform)card.Find("Description");
            if (narrow)
            {
                const float nameHeight = 44f;
                PlaceTop(price, width - inset - priceNarrow, inset * 0.6f, priceNarrow, nameHeight);
                PlaceTop(name, textLeft, inset * 0.6f, width - textLeft - priceNarrow - inset * 2f, nameHeight);
                float top = inset * 0.6f + nameHeight + 4f;
                PlaceTop(description, textLeft, top, width - textLeft - inset, height - top - inset * 0.6f);
            }
            else
            {
                PlaceTop(price, width - inset - priceWide, inset, priceWide, height - 2f * inset);
                float textWidth = width - textLeft - priceWide - inset * 2f;
                PlaceTop(name, textLeft, inset * 0.6f, textWidth, height * 0.5f - inset * 0.6f);
                PlaceTop(description, textLeft, height * 0.5f + 2f, textWidth, height * 0.5f - inset);
            }
            // The coin sits just left of the number.
            var coin = price.Find("Coin") as RectTransform;
            if (coin != null)
            {
                var priceText = price.GetComponent<TMPro.TextMeshProUGUI>();
                priceText.ForceMeshUpdate();
                float number = Mathf.Min(priceText.GetPreferredValues(priceText.text).x, price.rect.width - coin.sizeDelta.x - coinGap);
                coin.anchoredPosition = new Vector2(-(number + coinGap), 0f);
            }
        }

        private void DestroyShopUI()
        {
            if (_shopUI == null) return;
            _shopUI.SetActive(false); // Destroy waits for the end of the frame: the old screen must not show beside its rebuild
            if (Application.isPlaying) Destroy(_shopUI); else DestroyImmediate(_shopUI);
            _shopUI = null;
            _shopPanel = null;
            _shopScroll = null;
            _shopLeave = null;
            _wareButtons.Clear();
        }

        /// <summary>Leave: a standard action button (the theme's size, touch-sized on a phone), placed by LayoutShop.</summary>
        private UnityEngine.UI.Button ShopButton(RectTransform panel, string label, Action onClick)
        {
            var rect = ShopRect("Leave", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            ShopText("Text", rect, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), label, 26f);
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: true, size: SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Standard);
            return button;
        }

        private static RectTransform ShopRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMPro.TextMeshProUGUI ShopText(string name, Transform parent, Vector2 min, Vector2 max, string text, float size)
        {
            var label = ShopRect(name, parent, min, max).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = text;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = size;
            label.raycastTarget = false;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(label, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body); // the theme's font, never the scene default
            return label;
        }

        // The shop must never leave the game paused (stage change, scene unload).
        private void OnDisable()
        {
            SoulHunter.Gameplay.UI.UiScreen.Changed -= LayoutShop;
            if (_isShopOpen) CloseShop();
        }

        private void OnDestroy()
        {
            SoulHunter.Gameplay.UI.UiScreen.Changed -= LayoutShop;
            DestroyShopUI();
            if (_isShopOpen) GameTime.Resume(this);
        }
    }
}
