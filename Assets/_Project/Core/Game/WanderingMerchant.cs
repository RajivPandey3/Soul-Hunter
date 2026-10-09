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
    ///   Arcana"). Soul Hunter has no Arcana choice screen, so a random Arcana the player does not hold is given, as
    ///   ArcanaManager does for chests. It is not held back by the three-Arcana chest limit (that is a chest rule).
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

        private bool _isShopOpen;
        private bool _hasLeft;
        // Per visit: the charges held when the shop opened (the "already start with 20" case) and what was bought.
        private readonly Dictionary<ChargeWare, int> _heldAtOpen = new Dictionary<ChargeWare, int>();
        private readonly HashSet<ChargeWare> _chargesBought = new HashSet<ChargeWare>();
        private bool _arcanaBought;

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
            if (data == null || string.IsNullOrEmpty(character) || spendGold == null) return false;
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

        public bool BuyArcana()
        {
            if (!TryGetEconomy(out var economy)) return false;
            bool bought = TryBuyArcana(ArcanaManager.Instance, economy.SpendGold, UnityEngine.Random.value);
            if (bought) ShowCost(_arcanaPrice);
            RefreshShopUI();
            return bought;
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

        /// <summary>Where the ware cards may go, as a share of the panel (above the message line).</summary>
        public const float WaresTop = 0.79f, WaresBottom = 0.36f;
        /// <summary>Up to this many wares stay in one column (the approved look); more go in two columns.</summary>
        public const int SingleColumnWares = 3;
        private const float RowStep = 0.14f, RowGap = 0.02f, ColumnGap = 0.02f, WaresLeft = 0.05f, WaresRight = 0.95f;

        public static int WareColumns(int count) => count > SingleColumnWares ? 2 : 1;

        /// <summary>
        /// The panel-relative cell of ware <paramref name="index"/> of <paramref name="count"/>: one column of rows 0.14 apart
        /// (the approved look) while it fits, else two columns whose rows share the space between
        /// <see cref="WaresTop"/> and <see cref="WaresBottom"/>, so every card stays inside the panel and above the message.
        /// </summary>
        public static void WareCell(int index, int count, out Vector2 min, out Vector2 max)
        {
            int columns = WareColumns(count);
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float step = Mathf.Min(RowStep, (WaresTop - WaresBottom + RowGap) / rows);
            float width = (WaresRight - WaresLeft - ColumnGap * (columns - 1)) / columns;
            int row = index / columns, column = index % columns;
            float top = WaresTop - row * step;
            float left = WaresLeft + column * (width + ColumnGap);
            min = new Vector2(left, top - (step - RowGap));
            max = new Vector2(left + width, top);
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
            wares.Add(new Ware
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
                Description = "One more Arcana for this run, drawn at random.",
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

        private void BuildShopUI()
        {
            if (_shopUI != null || !Application.isPlaying) return;
            _shopUI = new GameObject("MerchantShop", typeof(RectTransform));
            var canvas = _shopUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = _shopUI.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // The whole 1920x1080 layout fits any window (a wide, short window cut the chest screen's OPEN off, 2026-10-01).
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            _shopUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            int gold = CurrentGold();
            var wares = Wares(gold);
            bool grid = WareColumns(wares.Count) > 1;

            // Owner (2026-10-04): dark, full contrast (UITheme), on a dark veil over the paused game. Two columns of wares
            // (Inverse mode) get a wider, taller panel so the cards keep room for their name and price.
            SoulHunter.Gameplay.UI.UITheme.AddVeil(_shopUI.transform);
            var panel = grid
                ? ShopRect("Panel", _shopUI.transform, new Vector2(0.14f, 0.06f), new Vector2(0.86f, 0.94f))
                : ShopRect("Panel", _shopUI.transform, new Vector2(0.27f, 0.14f), new Vector2(0.73f, 0.86f));
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(panel.gameObject.AddComponent<UnityEngine.UI.Image>());
            SoulHunter.Gameplay.UI.UITheme.ApplyText(ShopText("Title", panel, new Vector2(0f, 0.89f), new Vector2(1f, 0.98f), "WANDERING MERCHANT", 40f), SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
            GoldRow(panel, gold);

            var navigable = new List<(UnityEngine.UI.Button button, Vector2 centre)>();
            UnityEngine.UI.Button focus = null;
            for (int i = 0; i < wares.Count; i++)
            {
                var ware = wares[i];
                WareCell(i, wares.Count, out var min, out var max);
                var button = WareCard(panel, min, max, ware.Icon, ware.Name, ware.Description, ware.State, ware.OnClick);
                SetAvailable(button, ware.Available);
                navigable.Add((button, (min + max) * 0.5f));
                if (focus == null && button.interactable) focus = button;
            }

            ShopText("Message", panel, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.34f), LastMessage, 24f).color = SoulHunter.Gameplay.UI.UITheme.Rewards.Soul;
            var leave = ShopButton(panel, 0.18f, "Leave (the merchant moves on)", Leave);
            navigable.Add((leave, new Vector2(0.5f, 0.12f)));
            WireNavigation(navigable);
            if (focus == null) focus = leave;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        /// <summary>
        /// Keyboard and controller move only between the shop's usable buttons, by their place on the panel (automatic
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

        /// <summary>The player's gold with the theme's coin, like the Power Up screen.</summary>
        private static void GoldRow(RectTransform panel, int gold)
        {
            var row = ShopRect("GoldRow", panel, new Vector2(0.3f, 0.81f), new Vector2(0.7f, 0.89f));
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
        }

        /// <summary>
        /// A ware as a card like the Power Up and level-up cards (owner, 2026-10-08: the Pro look everywhere): the item's
        /// icon, its name and what it does, and its price with a coin (or OWNED / NO FREE SLOT). The state text is the
        /// card's first text.
        /// </summary>
        private UnityEngine.UI.Button WareCard(RectTransform panel, Vector2 min, Vector2 max, Sprite icon, string name, string description,
            string state, Action onClick)
        {
            var rect = ShopRect("Ware", panel, min, max);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            bool price = state.EndsWith(" G");
            // A price shows its coin instead of the "G" (like the Power Up screen); OWNED / NO FREE SLOT stay words.
            string shown = price && int.TryParse(state.Substring(0, state.Length - 2), out var amount) ? amount.ToString("N0") : state;
            var stateText = ShopText("Price", rect, new Vector2(0.74f, 0.1f), new Vector2(0.97f, 0.9f), shown, 30f);
            stateText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            if (price)
            {
                // The coin sits just left of the number.
                stateText.ForceMeshUpdate();
                float width = stateText.GetPreferredValues(shown).x;
                var coin = ShopRect("Coin", stateText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                coin.pivot = new Vector2(1f, 0.5f);
                coin.sizeDelta = new Vector2(34f, 34f);
                coin.anchoredPosition = new Vector2(-(width + 10f), 0f);
                var coinImage = coin.gameObject.AddComponent<UnityEngine.UI.Image>();
                coinImage.sprite = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin);
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;
            }
            var iconRect = ShopRect("Icon", rect, new Vector2(0.015f, 0.1f), new Vector2(0.015f, 0.9f));
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(64f, 0f);
            var iconImage = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var nameText = ShopText("Name", rect, new Vector2(0.14f, 0.5f), new Vector2(0.72f, 0.95f), name, 32f);
            nameText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            var descText = ShopText("Description", rect, new Vector2(0.14f, 0.06f), new Vector2(0.72f, 0.5f), description, 22f);
            descText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Card, SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: false,
                size: SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(nameText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(descText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.CardSoft);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(stateText, price ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Tag);
            return button;
        }

        private void DestroyShopUI()
        {
            if (_shopUI == null) return;
            if (Application.isPlaying) Destroy(_shopUI); else DestroyImmediate(_shopUI);
            _shopUI = null;
        }

        private UnityEngine.UI.Button ShopButton(RectTransform panel, float top, string label, Action onClick)
        {
            var rect = ShopRect("Ware", panel, new Vector2(0.05f, top - 0.12f), new Vector2(0.95f, top));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            ShopText("Text", rect, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), label, 26f);
            bool leave = label.StartsWith("Leave");
            // Wares keep their two-line, left-aligned text and their row size; Leave is a standard action button.
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, leave ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger : SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: leave,
                size: leave ? SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Standard : SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
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
            if (_isShopOpen) CloseShop();
        }

        private void OnDestroy()
        {
            DestroyShopUI();
            if (_isShopOpen) GameTime.Resume(this);
        }
    }
}
