using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Level up par game pause hota hai aur 3 random cards aate hain.
    /// Ye script buttons par random upgrades set karti hai aur click hone par WeaponManager ko bhejti hai.
    /// Reroll / Skip / Banish buttons: agar Inspector mein assign na hon toh pehle upgrade button ko
    /// clone karke cards ke neeche ek row mein bana diye jaate hain.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private SoulHunter.Gameplay.Core.LevelUpManager _levelUpManager;
        [SerializeField] private GameObject _levelUpPanel;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _banishButton;

        private const string ActionBarName = "LevelUp_ActionBar";
        private List<UpgradeData> _currentChoices = new List<UpgradeData>();
        private bool _banishMode;

        private void Awake()
        {
            // Learning Comment:
            // "Read First, Match Later":
            // Agar Inspector mein _levelUpManager assign nahi hai toh dynamic find karo.
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpPanel == null)
            {
                _levelUpPanel = gameObject;
            }

            // Game shuru hote waqt LevelUp Panel ko hide rakhein (Awake mein ek martaba).
            // Not while a level-up is waiting on it: a panel saved inactive in the scene runs this Awake only when the
            // first level-up switches it on, and hiding it then left the game paused with no cards (owner, 2026-09-30).
            bool roundOpen = _levelUpManager != null && _levelUpManager.IsLevelUpActive;
            if (_levelUpPanel != null && !roundOpen)
            {
                _levelUpPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound += ShowLevelUpScreen;
            }
            UiScreen.Changed += RelayoutForScreen;
            
            // CRITICAL FIX: Yahan se _levelUpPanel.SetActive(false) hata diya gaya hai!
            // Agar yeh script panel par hi lagi ho, to ShowLevelUpScreen ke SetActive(true) call
            // karte hi OnEnable chalega aur panel usi frame mein band ho jata tha.
        }

        private void OnDisable()
        {
            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound -= ShowLevelUpScreen;
            }
            UiScreen.Changed -= RelayoutForScreen;
        }

        /// <summary>
        /// A rotation or window change while the screen is open lays it out again for the new shape (owner, 2026-10-10:
        /// the same screen in landscape and portrait), without rebuilding the cards.
        /// </summary>
        private void RelayoutForScreen()
        {
            if (this == null || _levelUpPanel == null || !_levelUpPanel.activeInHierarchy) return;
            FitCardWidth();
            Canvas.ForceUpdateCanvases();
            var statsPanel = _levelUpPanel.transform.Find(StatsPanel.ObjectName)?.GetComponent<StatsPanel>();
            if (statsPanel != null)
            {
                statsPanel.FitBetween(HeadingRect(), _upgradeButtons, 0.23f, 0.83f);
                statsPanel.Refresh();
            }
        }

        private void ShowLevelUpScreen(List<UpgradeData> choices)
{
    if (choices == null)
    {
        Debug.LogError("[LevelUpUI] choices == NULL");
        return;
    }

    _currentChoices = choices;
    _banishMode = false;
    EnsureOwnCanvas();
    EvenDim();
    EnsureActionButtons();
    EnsureCardCount(choices.Count);
    StyleHeading();
    // VS: the player's stats are shown when leveling up (wiki Player_stats); a 4-column grid between the heading and the cards.
    if (_levelUpPanel != null)
    {
        var stats = StatsPanel.Attach(_levelUpPanel.transform, new Vector2(0.23f, 0.56f), new Vector2(0.83f, 0.78f), 4, 22f);
        if (stats != null && stats.Avoid == null)
        {
            var slots = FindFirstObjectByType<InventoryHud>();
            if (slots != null) stats.Avoid = (RectTransform)slots.transform;
        }
        // From 23% of the width: the HUD's item slots end at about 21% (Bug MIS B09: the panel covered the 6th column).
        if (stats != null) stats.FitBetween(HeadingRect(), _upgradeButtons, 0.23f, 0.83f);
    }

    if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
    {
        SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(
            SoulHunter.Gameplay.Audio.AudioManager.Instance.LevelUpSound
        );
    }

    for (int i = 0; i < _upgradeButtons.Length; i++)
    {
        if (_upgradeButtons[i] == null)
        {
            Debug.LogError($"[LevelUpUI] Button {i} is NULL");
            continue;
        }

        if (i < choices.Count)
        {
            _upgradeButtons[i].gameObject.SetActive(true);

            UpgradeData chosenUpgrade = choices[i];

            StyleCard(_upgradeButtons[i], chosenUpgrade);

            _upgradeButtons[i].onClick.RemoveAllListeners();

            UpgradeData finalUpgrade = chosenUpgrade;

            _upgradeButtons[i].onClick.AddListener(
                () => OnCardClicked(finalUpgrade)
            );
        }
        else
        {
            _upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    UpdateSpacers(Mathf.Min(choices.Count, _upgradeButtons.Length));
    FitCardWidth();

    BindActionButtons();
    RefreshActionButtons();

    if (_levelUpPanel != null)
    {
        _levelUpPanel.SetActive(true);
        // Lay the cards out now, so the stats panel fits the real free band from the first frame (it measured the old
        // card positions and showed only 2 of its 5 rows for a frame or two; owner, 2026-10-01).
        Canvas.ForceUpdateCanvases();
        var statsPanel = _levelUpPanel.transform.Find(StatsPanel.ObjectName)?.GetComponent<StatsPanel>();
        if (statsPanel != null)
        {
            statsPanel.FitBetween(HeadingRect(), _upgradeButtons, 0.23f, 0.83f);
            statsPanel.Refresh();
            statsPanel.FitBetween(HeadingRect(), _upgradeButtons, 0.23f, 0.83f);
        }
    }
    else
    {
        Debug.LogError("[LevelUpUI] _levelUpPanel is NULL!");
    }

    // Keyboard/controller: focus the first card so a choice can be made without a mouse.
    if (UnityEngine.EventSystems.EventSystem.current != null)
        foreach (var card in _upgradeButtons)
            if (card != null && card.gameObject.activeInHierarchy && card.interactable)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(card.gameObject);
                break;
            }
}

        private static int OwnedLevel(UpgradeData upgrade)
        {
            var weapons = WeaponManager.Instance;
            return weapons != null && upgrade != null ? weapons.GetWeaponLevel(upgrade.Type) : 0;
        }

        // ---------------------------------------------------------------- VS-style cards
        // Owner request 2026-09-25: the cards were plain white buttons with internal names ("SongOfMana Lv.2 (Lv 2)").
        // Each card now shows the upgrade's icon, a readable name, a NEW!/Level tag and what it does.

        public const string IconName = "Card_Icon", TitleName = "Card_Title", TagName = "Card_Tag", DescriptionName = "Card_Description";

        private void StyleCard(Button card, UpgradeData upgrade)
        {
            if (card == null || upgrade == null) return;
            var background = card.GetComponent<Image>();
            if (background != null) UITheme.ApplyButton(card, background, UITheme.ButtonKind.Card);
            TMP_FontAsset font = null;
            foreach (var label in card.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!label.name.StartsWith("Card_"))
                {
                    font = label.font;
                    label.gameObject.SetActive(false); // the old one-line label
                }
            // The scene's cards also carry old legacy Text labels ("Magic Wand (Level Up)") that showed through.
            foreach (var legacy in card.GetComponentsInChildren<Text>(true))
                legacy.gameObject.SetActive(false);
            if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);

            var icon = CardPart<Image>(card.transform, IconName, new Vector2(0.012f, 0.1f), new Vector2(0.075f, 0.9f));
            icon.sprite = upgrade.Icon;
            icon.preserveAspect = true;
            icon.enabled = upgrade.Icon != null;
            icon.raycastTarget = false;

            int owned = OwnedLevel(upgrade);
            CardText(card.transform, TitleName, font, new Vector2(0.09f, 0.5f), new Vector2(0.8f, 0.95f), UpgradeText.DisplayName(upgrade),
                44f, ThemedElement.TextRole.Card, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            CardText(card.transform, TagName, font, new Vector2(0.8f, 0.5f), new Vector2(0.985f, 0.95f),
                _banishMode ? "BANISH" : UpgradeText.Tag(upgrade, owned), 34f, _banishMode ? ThemedElement.TextRole.Danger : ThemedElement.TextRole.Tag,
                TextAlignmentOptions.BottomRight, FontStyles.Bold);
            var hints = EvolutionHintsFor(upgrade);
            float descriptionRight = hints.Count > 0 ? EvoHintLeft - 0.01f : 0.985f;
            CardText(card.transform, DescriptionName, font, new Vector2(0.09f, 0.06f), new Vector2(descriptionRight, 0.5f),
                UpgradeText.Describe(upgrade, owned), 30f, ThemedElement.TextRole.CardSoft, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            var description = (RectTransform)card.transform.Find(DescriptionName);
            description.anchorMax = new Vector2(descriptionRight, 0.5f); // CardPart places a part once; the hint narrows it
            StyleEvolutionHint(card.transform, font, upgrade.Type, hints);
        }

        // ---------------------------------------------------------------- VS evolution hint (release item P3-01)
        // vampire.survivors.wiki/w/Level_up (raw, update history, fetched 2026-10-09): Patch 0.3.2 "Added info for discovered
        // evolutions on Level Up panels"; 1.4.200 "Fix for level up evo indicators not showing on passives", so weapon and
        // passive cards both carry it. The wiki does not show the hint's look; balance method (2026-10-09), no new art: the
        // other item's icon(s) in item slots, then the result shown as its base weapon's icon in the gold evolved-slot frame
        // the HUD uses (evolutions have no icons yet), and the recipe in plain words, so it is never told by colour alone.
        // Discovery: EvolutionDiscovery (saved when a chest evolves the weapon). Rules for which recipes: EvolutionRecipes.HintsFor.

        public const string EvoHintName = "Card_EvoHint", EvoHintTextName = "Card_EvoHintText", EvoHintIconName = "Card_EvoHintIcon";
        /// <summary>Icon boxes in the hint: up to two other items and the result.</summary>
        public const int EvoHintIconCount = 3;
        private const float EvoHintLeft = 0.6f, EvoHintIconStep = 0.15f, EvoHintIconWidth = 0.135f;

        /// <summary>
        /// The discovered recipes this card hints. None in Banish mode (the card is being removed, not chosen) and none on the
        /// maxed-out or Limit Break reward cards (wiki Level_up: those offer gold, chicken or a stat, not an item to build on).
        /// </summary>
        private List<EvolutionRecipe> EvolutionHintsFor(UpgradeData upgrade)
        {
            if (upgrade == null || _banishMode || (_levelUpManager != null && _levelUpManager.OfferingMaxedChoices))
                return new List<EvolutionRecipe>();
            return EvolutionRecipes.HintsFor(upgrade.Type, WeaponManager.Instance, EvolutionRecipes.CurrentSave());
        }

        private static void StyleEvolutionHint(Transform card, TMP_FontAsset font, UpgradeData.UpgradeType item, List<EvolutionRecipe> hints)
        {
            bool shown = hints != null && hints.Count > 0;
            var existing = card.Find(EvoHintName);
            if (!shown) { if (existing != null) existing.gameObject.SetActive(false); return; }
            var block = CardPart<RectTransform>(card, EvoHintName, new Vector2(EvoHintLeft, 0.06f), new Vector2(0.985f, 0.5f));
            block.gameObject.SetActive(true);

            var first = hints[0];
            var pictures = new List<(Sprite icon, bool result)>();
            foreach (var other in EvolutionRecipes.Counterparts(first, item))
                if (pictures.Count < EvoHintIconCount - 1) pictures.Add((EvolutionRecipes.IconOf(other), false));
            pictures.Add((EvolutionRecipes.ResultIconOf(first), true));

            for (int i = 0; i < EvoHintIconCount; i++)
            {
                float left = i * EvoHintIconStep;
                var frame = CardPart<Image>(block, EvoHintIconName + i, new Vector2(left, 0.04f), new Vector2(left + EvoHintIconWidth, 0.96f));
                bool used = i < pictures.Count;
                frame.gameObject.SetActive(used);
                if (!used) continue;
                frame.raycastTarget = false;
                UITheme.ApplySurface(frame, ThemedElement.SurfaceRole.SlotFilled);
                // The result wears the HUD's evolved-slot frame; the other items a plain slot edge.
                UITheme.Edge(frame.gameObject, pictures[i].result ? UITheme.Hud.SlotEvolved : UITheme.Hud.SlotBorder, pictures[i].result ? 3f : 1.5f);
                var picture = CardPart<Image>(frame.transform, "Icon", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
                picture.sprite = pictures[i].icon;
                picture.preserveAspect = true;
                picture.enabled = pictures[i].icon != null;
                picture.raycastTarget = false;
            }

            float textLeft = Mathf.Min(pictures.Count * EvoHintIconStep, 0.6f);
            CardText(block, EvoHintTextName, font, new Vector2(textLeft, 0f), new Vector2(1f, 1f),
                EvolutionRecipes.HintText(item, hints), 24f, ThemedElement.TextRole.Card, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            var text = (RectTransform)block.Find(EvoHintTextName);
            text.anchorMin = new Vector2(textLeft, 0f); // the number of icons can change between cards
        }

        private static T CardPart<T>(Transform card, string name, Vector2 min, Vector2 max) where T : Component
        {
            var existing = card.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.layer = card.gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(card, false);
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static void CardText(Transform card, string name, TMP_FontAsset font, Vector2 min, Vector2 max, string value,
            float maxSize, ThemedElement.TextRole role, TextAlignmentOptions alignment, FontStyles style)
        {
            var text = CardPart<TextMeshProUGUI>(card, name, min, max);
            if (font != null) text.font = font;
            text.text = value;
            // Bug MIS B12 (2026-09-30): the first level-up used larger fonts. The cards are built while the panel is still
            // inactive, so TMP's Awake ran later and replaced the limits with its defaults (auto-size up to 72). A set size
            // keeps them (the same fix as the character select screen).
            text.fontSize = maxSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = maxSize;
            UITheme.ApplyText(text, role); // the theme's colour for this part of a card, kept live
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = name == TagName ? TextWrappingModes.NoWrap : TextWrappingModes.Normal; // "Level 2" stays on one line (phone, 2026-10-10)
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            foreach (var tmpText in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!tmpText.name.StartsWith("Card_")) { tmpText.text = label; return; }
            var legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = label;
        }

        private void OnCardClicked(UpgradeData upgrade)
        {
            if (_banishMode)
            {
                _banishMode = false;
                if (_levelUpManager != null) _levelUpManager.Banish(upgrade);
                HidePanelIfLevelUpFinished();
                return;
            }
            OnUpgradeSelected(upgrade);
        }

        private void OnRerollClicked()
        {
            _banishMode = false;
            if (_levelUpManager != null) _levelUpManager.Reroll();
            HidePanelIfLevelUpFinished();
        }

        private void OnSkipClicked()
        {
            if (_levelUpManager == null || !_levelUpManager.CanSkip) return;
            // Close first so a queued level-up can reopen the panel cleanly.
            if (_levelUpPanel != null) _levelUpPanel.SetActive(false);
            _levelUpManager.Skip();
        }

        private void OnBanishClicked()
        {
            if (_levelUpManager == null) return;
            if (!_banishMode && _levelUpManager.BanishesLeft <= 0) return;
            // Banish is a two-step action: arm it, then click the card to remove.
            _banishMode = !_banishMode;
            for (int i = 0; i < _upgradeButtons.Length && i < _currentChoices.Count; i++)
                StyleCard(_upgradeButtons[i], _currentChoices[i]);
            RefreshActionButtons();
        }

        private void HidePanelIfLevelUpFinished()
        {
            if (_levelUpPanel != null && _levelUpManager != null && !_levelUpManager.IsLevelUpActive)
                _levelUpPanel.SetActive(false);
        }

        private void BindActionButtons()
        {
            if (_rerollButton != null) { _rerollButton.onClick.RemoveAllListeners(); _rerollButton.onClick.AddListener(OnRerollClicked); }
            if (_skipButton != null) { _skipButton.onClick.RemoveAllListeners(); _skipButton.onClick.AddListener(OnSkipClicked); }
            if (_banishButton != null) { _banishButton.onClick.RemoveAllListeners(); _banishButton.onClick.AddListener(OnBanishClicked); }
        }

        private void RefreshActionButtons()
        {
            if (_levelUpManager == null) return;
            foreach (var action in new[] { _rerollButton, _skipButton, _banishButton })
                WhitenLabels(action); // readable on the dark action buttons (was red)
            // VS (wiki Level_up): Reroll, Skip and Banish "become available in this screen if the player invests in these
            // PowerUps" or plays a character that starts with them; they start at 0 (Reroll/Skip/Banish pages). So a
            // button shows only while it has a use left (owner, 2026-10-01: the "(0)" buttons looked broken).
            if (_rerollButton != null)
            {
                SetButtonLabel(_rerollButton, $"Reroll ({_levelUpManager.RerollsLeft})");
                ShowAction(_rerollButton, _levelUpManager.CanReroll); // also off on the maxed-out cards (wiki Level_up)
            }
            if (_skipButton != null)
            {
                SetButtonLabel(_skipButton, $"Skip ({_levelUpManager.SkipsLeft})");
                ShowAction(_skipButton, _levelUpManager.CanSkip);
            }
            if (_banishButton != null)
            {
                SetButtonLabel(_banishButton, _banishMode ? "Cancel Banish" : $"Banish ({_levelUpManager.BanishesLeft})");
                ShowAction(_banishButton, _banishMode || _levelUpManager.BanishesLeft > 0);
            }
            // The shared row goes too when it has nothing to offer, so the cards do not keep an empty strip.
            var anyAction = new[] { _rerollButton, _skipButton, _banishButton }.FirstOrDefault(b => b != null);
            if (anyAction != null && anyAction.transform.parent != null && anyAction.transform.parent.name == ActionBarName)
            {
                bool anyShown = new[] { _rerollButton, _skipButton, _banishButton }.Any(b => b != null && b.gameObject.activeSelf);
                if (anyAction.transform.parent.gameObject.activeSelf != anyShown) anyAction.transform.parent.gameObject.SetActive(anyShown);
            }
        }

        private static void ShowAction(Button button, bool available)
        {
            button.interactable = available;
            if (button.gameObject.activeSelf != available) button.gameObject.SetActive(available);
        }

        /// <summary>
        /// Builds the Reroll / Skip / Banish row under the upgrade cards by cloning the first card,
        /// unless the buttons are assigned in the Inspector. The row is created once and shared,
        /// so a second LevelUpUI on the same panel reuses it.
        /// </summary>
        private void EnsureActionButtons()
        {
            if (_rerollButton != null && _skipButton != null && _banishButton != null) return;

            Button template = null;
            foreach (var button in _upgradeButtons)
                if (button != null) { template = button; break; }
            if (template == null) return;

            Transform container = template.transform.parent;
            Transform bar = container.Find(ActionBarName);
            if (bar == null)
            {
                var templateRect = (RectTransform)template.transform;
                var barObject = new GameObject(ActionBarName, typeof(RectTransform));
                barObject.layer = template.gameObject.layer;
                bar = barObject.transform;
                bar.SetParent(container, false);
                bar.SetAsLastSibling();
                ((RectTransform)bar).sizeDelta = templateRect.sizeDelta;

                var layout = barObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 24f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                barObject.AddComponent<LayoutElement>().preferredHeight = templateRect.sizeDelta.y;

                CreateActionButton(template, bar, "Reroll_Button");
                CreateActionButton(template, bar, "Skip_Button");
                CreateActionButton(template, bar, "Banish_Button");
            }

            // Smaller, dark action buttons under the cards (VS style), with the same focus colours.
            // The theme's standard button size, centred with the standard gap (not stretched across the row).
            var barLayout = bar.GetComponent<LayoutElement>();
            if (barLayout != null) barLayout.preferredHeight = UITheme.Layout.ButtonHeight;
            // Responsive (owner, 2026-10-10): the row wraps onto more lines when the screen is narrow (portrait) instead of
            // running past its edges; the shared ThemedRow places the buttons, not a horizontal layout group.
            var barGroup = bar.GetComponent<HorizontalLayoutGroup>();
            if (barGroup != null) DestroyImmediateOrLater(barGroup);
            if (bar.GetComponent<ThemedRow>() == null) bar.gameObject.AddComponent<ThemedRow>();
            foreach (var action in bar.GetComponentsInChildren<Button>(true))
            {
                var image = action.GetComponent<Image>();
                if (image != null) UITheme.ApplyButton(action, image, UITheme.ButtonKind.Normal);
                WhitenLabels(action);
                foreach (Transform part in action.transform)
                    if (part.name.StartsWith("Card_")) part.gameObject.SetActive(false); // clones of a styled card
            }

            if (_rerollButton == null) _rerollButton = FindActionButton(bar, "Reroll_Button");
            if (_skipButton == null) _skipButton = FindActionButton(bar, "Skip_Button");
            if (_banishButton == null) _banishButton = FindActionButton(bar, "Banish_Button");
        }

        /// <summary>
        /// Luck can deal a 4th choice, but the panel only ships 3 cards. Clone the last card for any
        /// missing ones (reusing an existing clone by name, so a second LevelUpUI shares it).
        /// Extra cards are hidden on rounds with fewer choices by the normal card loop.
        /// </summary>
        private void EnsureCardCount(int count)
        {
            if (_upgradeButtons == null || _upgradeButtons.Length >= count) return;

            Button template = null;
            for (int i = _upgradeButtons.Length - 1; i >= 0 && template == null; i--)
                template = _upgradeButtons[i];
            if (template == null) return;

            Transform container = template.transform.parent;
            var cards = new List<Button>(_upgradeButtons);
            for (int i = cards.Count; i < count; i++)
            {
                string cardName = $"Upgrade_Button_{i}";
                Transform existing = container.Find(cardName);
                Button card = existing != null ? existing.GetComponent<Button>() : null;
                if (card == null)
                {
                    card = Instantiate(template, container, false);
                    card.name = cardName;
                    card.onClick = new Button.ButtonClickedEvent();
                    // Keep the new card with the others, above the action row.
                    card.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                }
                cards.Add(card);
                template = card;
            }
            _upgradeButtons = cards.ToArray();
        }

        /// <summary>
        /// Bug MIS B15 (2026-09-30): the cards sit in a layout group that shares the height among them, so with one choice
        /// left the card stretched and the heading and stats panel jumped. Invisible spacers take the places of the missing
        /// cards (up to the panel's 3), so every round keeps the 3-card layout. They are not buttons: focus skips them.
        /// </summary>
        public const int BaseCardCount = 3;

        private void UpdateSpacers(int shown)
        {
            Button last = null;
            for (int i = 0; i < _upgradeButtons.Length && i < Mathf.Max(1, shown); i++)
                if (_upgradeButtons[i] != null) last = _upgradeButtons[i];
            if (last == null) return;
            Transform container = last.transform.parent;
            int need = Mathf.Max(0, BaseCardCount - shown);
            for (int i = 0; i < BaseCardCount; i++)
            {
                string name = "Card_Spacer_" + i;
                Transform spacer = container.Find(name);
                if (spacer == null && i < need)
                {
                    var go = new GameObject(name, typeof(RectTransform));
                    go.layer = container.gameObject.layer;
                    spacer = go.transform;
                    spacer.SetParent(container, false);
                }
                if (spacer == null) continue;
                // The same layout size as a card, so the group shares the height exactly as with three cards.
                var cardRect = (RectTransform)last.transform;
                var size = spacer.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
                size.minHeight = LayoutUtility.GetMinHeight(cardRect);
                size.preferredHeight = LayoutUtility.GetPreferredHeight(cardRect);
                size.flexibleHeight = LayoutUtility.GetFlexibleHeight(cardRect);
                bool on = i < need;
                spacer.gameObject.SetActive(on);
                if (on) spacer.SetSiblingIndex(last.transform.GetSiblingIndex() + 1 + i);
            }
        }

        /// <summary>
        /// Bug MIS v2 C05 (2026-10-02): on its own Expand canvas the cards ran edge to edge (0-1920), with the icon and the
        /// NEW!/Level tag 1,600 px apart. The card column is now at most this wide, centred.
        /// </summary>
        public const float CardMaxWidth = 1100f;

        /// <summary>Under this height (canvas units) the stats move beside the cards (a phone held sideways is about 510).</summary>
        public const float ShortScreenHeight = 700f;
        public const float SideStatsWidth = 400f;

        /// <summary>
        /// Bug MIS v2 C08 (2026-10-02): the panel's full-screen image was a tinted texture tile (sprite_1_0), so the HUD
        /// behind the level-up screen looked dimmed in uneven blocks (labels dark, numbers bright). The panel is now one
        /// even dark veil.
        /// </summary>
        public static Color DimColor => UITheme.Veil; // the theme's veil

        private void EvenDim()
        {
            if (_levelUpPanel == null) return;
            var image = _levelUpPanel.GetComponent<Image>();
            if (image == null) return;
            image.sprite = null;
            image.type = Image.Type.Simple;
            UITheme.ApplySurface(image, ThemedElement.SurfaceRole.Veil);
        }
        private RectOffset _basePadding;
        private float _baseSpacing;

        private void FitCardWidth()
        {
            Button first = null;
            foreach (var b in _upgradeButtons) if (b != null) { first = b; break; }
            if (first == null) return;
            var container = first.transform.parent as RectTransform;
            var group = container != null ? container.GetComponent<HorizontalOrVerticalLayoutGroup>() : null;
            if (group == null) return;
            if (_basePadding == null) { _basePadding = new RectOffset(group.padding.left, group.padding.right, group.padding.top, group.padding.bottom); _baseSpacing = group.spacing; }
            // Responsive (owner, 2026-10-10): the cards stay clear of the notch (safe area). On a short screen (a phone held
            // sideways) the band over the cards is too low for the stats, so the cards move right and the stats take their
            // left (StatsPanel.Fit).
            SafeAreaFitter.Fit(container);
            Canvas.ForceUpdateCanvases();
            float width = container.rect.width;
            bool shortScreen = container.rect.height < ShortScreenHeight;
            int statsRoom = shortScreen ? Mathf.RoundToInt(Mathf.Min(SideStatsWidth, width * 0.36f)) : 0;
            int side = Mathf.Max(0, Mathf.RoundToInt((width - statsRoom - CardMaxWidth) * 0.5f));
            group.padding.left = Mathf.Max(_basePadding.left, statsRoom + side);
            group.padding.right = Mathf.Max(_basePadding.right, side);
            // The cards never go under the touch target (x1.3 for the icon, name and text); on a short screen the heading
            // and the gaps give way instead (the cards were 46 units, 33 dp, on a phone held sideways).
            group.spacing = shortScreen ? UITheme.Layout.ButtonGap : _baseSpacing;
            float cardMin = UiScreen.MinTouchUnits * 1.3f;
            foreach (var card in _upgradeButtons)
            {
                if (card == null) continue;
                var element = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
                element.minHeight = cardMin;
            }
            var heading = HeadingRect();
            if (heading != null)
            {
                var text = heading.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null) text.fontSize = shortScreen ? 44f : 64f;
                var headingElement = heading.GetComponent<LayoutElement>() ?? heading.gameObject.AddComponent<LayoutElement>();
                headingElement.preferredHeight = shortScreen ? 56f : -1f;
                headingElement.flexibleHeight = shortScreen ? 0f : -1f;
            }
            LayoutRebuilder.MarkLayoutForRebuild(container);
        }

        public const string HeadingText = "LEVEL UP!";

        /// <summary>
        /// The panel's "LEVEL UP" heading (a text beside the cards in ButtonContainer) was drawn at the top of the screen,
        /// over the HUD stage name. It now sits at the bottom of its box, just above the cards, in gold (VS style).
        /// </summary>
        /// <summary>The "LEVEL UP!" heading above the cards (null when the panel has none).</summary>
        public const string ScreenCanvasName = "LevelUpScreenCanvas";
        public const int ScreenSortingOrder = 300; // above the HUD, below the chest screen (400) and unlock notices (500)

        /// <summary>
        /// Owner, 2026-10-01 ("level up screen bhi chest jaisa kar do"): the panel moves (once) from the HUD canvas, which
        /// scales by width only, to its own overlay canvas that expands to fit, like the chest screen. In a wide, short
        /// window the HUD canvas is shorter than 1080 and the lower cards and Reroll/Skip/Banish could fall off the bottom.
        /// The HUD keeps its own scaling; the panel's prefab and scene are not edited.
        /// </summary>
        private void EnsureOwnCanvas()
        {
            if (_levelUpPanel == null) return;
            var current = _levelUpPanel.transform.parent != null ? _levelUpPanel.transform.parent.GetComponentInParent<Canvas>(true) : null;
            if (current != null && current.name == ScreenCanvasName) return;
            Transform home = current != null ? current.rootCanvas.transform.parent : null;
            var existing = home != null ? home.Find(ScreenCanvasName) : null;
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(ScreenCanvasName, typeof(RectTransform));
                if (home != null) go.transform.SetParent(home, false);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = ScreenSortingOrder;
                SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
                go.AddComponent<GraphicRaycaster>();
            }
            var rect = (RectTransform)_levelUpPanel.transform;
            Vector2 anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, offsetMin = rect.offsetMin, offsetMax = rect.offsetMax;
            rect.SetParent(go.transform, false);
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        private RectTransform HeadingRect()
        {
            foreach (var button in _upgradeButtons)
            {
                if (button == null) continue;
                foreach (Transform child in button.transform.parent)
                    if (child.GetComponent<Button>() == null && child.TryGetComponent<TextMeshProUGUI>(out var heading)) return heading.rectTransform;
                break;
            }
            return null;
        }

        private void StyleHeading()
        {
            Button first = null;
            foreach (var button in _upgradeButtons)
                if (button != null) { first = button; break; }
            if (first == null) return;
            foreach (Transform child in first.transform.parent)
            {
                if (child.GetComponent<Button>() != null || !child.TryGetComponent<TextMeshProUGUI>(out var heading)) continue;
                heading.text = HeadingText;
                heading.alignment = TextAlignmentOptions.Bottom;
                heading.enableAutoSizing = false;
                heading.fontSize = 64f;
                heading.fontStyle = FontStyles.Bold;
                UITheme.ApplyText(heading, ThemedElement.TextRole.Title);
                heading.raycastTarget = false;
            }
        }

        /// <summary>
        /// The action buttons are clones of a card, whose label in HUD_Canvas is a red legacy Text; both TMP and legacy
        /// labels are turned white (the hidden Card_ parts keep their own colours).
        /// </summary>
        private static void WhitenLabels(Button action)
        {
            if (action == null) return;
            // Light or dark text, whichever reads best on the button's theme colour (as every themed button label).
            bool light = UITheme.LightTextReads(UITheme.Button);
            foreach (var label in action.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!label.name.StartsWith("Card_")) UITheme.ApplyText(label, light ? ThemedElement.TextRole.Body : ThemedElement.TextRole.Card);
            foreach (var label in action.GetComponentsInChildren<Text>(true))
                label.color = light ? UITheme.Text : UITheme.CardText; // legacy Text labels (no TMP): set from the theme
        }

        private static void DestroyImmediateOrLater(Object target)
        {
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }

        private static void CreateActionButton(Button template, Transform parent, string name)
        {
            Button clone = Instantiate(template, parent, false);
            clone.name = name;
            clone.gameObject.SetActive(true);
            clone.onClick = new Button.ButtonClickedEvent(); // drop any listeners copied from the card
        }

        private static Button FindActionButton(Transform bar, string name)
        {
            Transform child = bar.Find(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void OnUpgradeSelected(UpgradeData selectedUpgrade)
        {
            // Panel ko pehle band karein taake agar agla level up queue mein ho toh wo cleanly open ho sake
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.SelectUpgrade(selectedUpgrade);
            }
        }
    }
}
