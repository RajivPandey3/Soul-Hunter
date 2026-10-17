using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS wiki (Treasure_Chest skipping rules): the first six chests on a save cannot be skipped; after that 1- and
    /// 3-item chests can be, and 5-item chests once 50 of them (or 500 chests) have been opened. Once 5-item chests can be
    /// skipped, chests holding only coin bags skip their animation by themselves. (The wiki's third unlock, having seen
    /// "the final fireworks", has no Soul Hunter counterpart.)
    /// </summary>
    public static class ChestSkipRules
    {
        public const int UnskippableChests = 6, FiveItemChestsToSkip = 50, ChestsToSkipFiveItems = 500;

        public static bool FiveItemSkipUnlocked(int chestsOpenedBefore, int fiveItemChestsBefore) =>
            fiveItemChestsBefore >= FiveItemChestsToSkip || chestsOpenedBefore >= ChestsToSkipFiveItems;

        public static bool CanSkip(int chestsOpenedBefore, int fiveItemChestsBefore, int itemCount)
        {
            if (chestsOpenedBefore < UnskippableChests) return false;
            return itemCount < 5 || FiveItemSkipUnlocked(chestsOpenedBefore, fiveItemChestsBefore);
        }

        public static bool SkipsItself(int chestsOpenedBefore, int fiveItemChestsBefore, bool onlyCoins) =>
            onlyCoins && FiveItemSkipUnlocked(chestsOpenedBefore, fiveItemChestsBefore);
    }

    /// <summary>
    /// The VS-style chest-opening screen (vampire.survivors.wiki/w/Treasure_Chest: the intro blast, the Open button,
    /// the background lighting up on 3- and 5-item chests, reels, stars behind each item, coin particles and a coin
    /// count-up that scales in and pulses, fireworks on 5-item chests, a Done button that rotates in, and the
    /// "Treasure Found" jingle followed by one jingle per chest size). Soul Hunter's own art: the approved Soul Reliquary
    /// chest and Soul Coins (ChestPreviewStage) and the approved VFX v2 sprites. The game is paused, so everything runs
    /// on unscaled time. Timings are provisional.
    /// </summary>
    public sealed class ChestOpeningScreen : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public enum Phase { Intro, WaitingToOpen, Revealing, Done, Closed }

        // Reward colours come from the UI theme (Theme Studio > Rewards).
        private static Color Gold => UITheme.Rewards.Gold;
        private static Color Soul => UITheme.Rewards.Soul;
        private static Color ArcanaTint => UITheme.Rewards.Arcana;
        private static Color Evolution => UITheme.Rewards.Evolution;
        /// <summary>The theme's veil colour at a given strength (the chest screen fades it in itself).</summary>
        private static Color Veil(float alpha, bool arcana)
        {
            var veil = arcana ? Color.Lerp(UITheme.Veil, ArcanaTint, 0.12f) : UITheme.Veil;
            veil.a = alpha;
            return veil;
        }

        public Phase Current { get; private set; } = Phase.Closed;
        public bool IsOpen => Current != Phase.Closed;
        public bool CanSkip { get; private set; }
        public int ItemCount => _result != null ? _result.ItemCount : 0;
        public int RevealedCount { get; private set; }
        private int _shownGold;
        /// <summary>The gold counted so far; the HUD holds back the rest (HudGoldHold) until the count-up reaches it.</summary>
        public int ShownGold
        {
            get => _shownGold;
            private set
            {
                _shownGold = value;
                HudGoldHold.Set(gameObject.activeInHierarchy && _result != null ? _result.Gold - value : 0);
            }
        }
        public bool BackgroundLit => _circle != null && _circle.color.a > 0.05f;
        public int FireworksShown { get; private set; }
        public ChestPreviewStage Preview => _stage;
        public Button OpenButton => _openButton;
        public Button DoneButton => _doneButton;
        /// <summary>The safe-area content root every part of the screen sits in (the backdrop stays full screen).</summary>
        public RectTransform Content => _content;
        /// <summary>The reward cards, in order (empty until the reveal builds them).</summary>
        public IReadOnlyList<RectTransform> Cards => _cards;
        /// <summary>Short screens (a phone held sideways): title, chest and gold on the left; cards and the button on the right.</summary>
        public bool SideBySide { get; private set; }
        /// <summary>The reward cards' size in canvas units (190 when there is room, never under <see cref="MinCardSize"/>).</summary>
        public float CardSize { get; private set; } = RoomyCardSize;
        public event System.Action Closed;

        // Layout, in canvas units (owner 2026-10-10: the same screen in landscape and portrait). The 1920x1080 layout's
        // sizes are the roomy ones; a smaller screen steps the cards and the chest down, wraps the cards onto more rows,
        // and on a short screen puts the chest beside the rewards.
        public const float RoomyCardSize = 190f, MinCardSize = 140f, ChestSize = 440f, MinChestSize = 220f;
        private static readonly float[] CardSizes = { 190f, 170f, 150f, 140f };
        private const float TitleHeight = 110f, CoinHeight = 90f, HintHeight = 44f, HintWidth = 420f, EdgeMargin = 24f;
        private static float Gap => UiThemeAsset.Active != null ? UiThemeAsset.Active.Layouts.ButtonGap : 16f;

        private ChestResult _result;
        private ChestPreviewStage _stage;
        private RectTransform _root, _content;
        private Vector2 _laidOutSize = new Vector2(-1f, -1f);
        private Image _backdrop;
        /// <summary>
        /// Bug MIS v2 C22 (2026-10-02): at 0.88 the paused world's brightest effects (a lightning bolt, damage numbers)
        /// still read through, looking attached to the chest. The backdrop is darker.
        /// </summary>
        public const float BackdropAlpha = 0.94f;
        private RawImage _chestImage, _circle, _flash, _ring;
        private TextMeshProUGUI _title, _coinText, _skipHint;
        private Button _openButton, _doneButton;
        private readonly List<RectTransform> _cards = new List<RectTransform>();
        private readonly List<Image> _cardIcons = new List<Image>();
        private readonly List<RawImage> _cardStars = new List<RawImage>();
        private readonly List<Sprite> _reelIcons = new List<Sprite>();
        private readonly List<(RawImage img, Vector2 v, float age)> _sparks = new List<(RawImage, Vector2, float)>();
        private Texture2D _sparkleTexture;
        private Coroutine _sequence;
        private float _coinPulse;

        /// <summary>
        /// Makes a hidden 3D preview ahead of the first chest (ChestUI does it as the run starts). It is switched off at once,
        /// so its camera never renders during play; the first Show takes its texture, as every later chest does.
        /// </summary>
        public void Prewarm(GameObject chestPrefab, GameObject coinPrefab)
        {
            if (_stage != null || chestPrefab == null) return;
            _stage = ChestPreviewStage.Create(chestPrefab, coinPrefab, false);
            _stage.gameObject.SetActive(false);
        }

        public static ChestOpeningScreen Create(Canvas canvas)
        {
            var go = new GameObject("ChestOpeningScreen", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var screen = go.AddComponent<ChestOpeningScreen>();
            screen._root = rect;
            screen.Build();
            go.SetActive(false);
            return screen;
        }

        // ---------- build ----------

        private void Build()
        {
            var content = GameContentCatalog.Load();
            _sparkleTexture = content != null ? content.ChestFxSparkle : null;

            _backdrop = gameObject.AddComponent<Image>();
            _backdrop.color = Veil(0f, false);
            _backdrop.raycastTarget = true; // the game underneath does not take clicks

            // Everything but the backdrop sits inside the safe area (notch, gesture bar); Layout() places it.
            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(_root, false);
            SafeAreaFitter.Fit(_content);

            _circle = Raw("Light", content != null ? content.ChestFxCircle : null, Vector2.zero, 760f);
            _chestImage = Raw("Chest", null, Vector2.zero, ChestSize);
            _flash = Raw("Flash", content != null ? content.ChestFxFlash : null, Vector2.zero, 560f);
            _ring = Raw("Ring", content != null ? content.ChestFxRing : null, Vector2.zero, 460f);

            _title = Label("Title", Vector2.zero, new Vector2(1200f, TitleHeight), 78f);
            UITheme.ApplyText(_title, ThemedElement.TextRole.Title); // the same title look as LEVEL UP!, RESULTS and OPTIONS
            _coinText = Label("Coins", Vector2.zero, new Vector2(700f, CoinHeight), 64f);
            UITheme.ApplyText(_coinText, ThemedElement.TextRole.Gold);
            _skipHint = Label("SkipHint", Vector2.zero, new Vector2(HintWidth, HintHeight), 26f);
            _skipHint.text = "Esc / Right click / B: skip";
            UITheme.ApplyText(_skipHint, ThemedElement.TextRole.Dim);

            _openButton = MakeButton("OPEN", Vector2.zero, PressOpen);
            _doneButton = MakeButton("DONE", Vector2.zero, PressDone);

            foreach (var definition in GameContentCatalog.Load() != null ? GameContentCatalog.Load().WeaponDefinitions : new WeaponDefinition[0])
                if (definition != null && definition.Icon != null) _reelIcons.Add(definition.Icon);
        }

        private RawImage Raw(string name, Texture texture, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_content, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            return image;
        }

        private TextMeshProUGUI Label(string name, Vector2 position, Vector2 size, float fontSize, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent != null ? parent : _content, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            UITheme.ApplyText(label, ThemedElement.TextRole.Body);
            ThemedElement.Outline(label, UiThemeAsset.Active);
            return label;
        }

        private Button MakeButton(string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_content, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var label = Label("Label", Vector2.zero, rect.sizeDelta, UITheme.Layout.ButtonTextMax, rect);
            label.text = text;
            // After the label exists, so the theme lays it out too; the size is the theme's standard button size.
            UITheme.ApplyButton(button, go.GetComponent<Image>(), UITheme.ButtonKind.Primary, UITheme.IconSlot.Confirm);
            go.SetActive(false);
            return button;
        }

        // ---------- flow ----------

        /// <summary>Shows the screen for <paramref name="result"/>. The caller pauses the game.</summary>
        public void Show(ChestResult result, bool canSkip, GameObject chestPrefab, GameObject coinPrefab)
        {
            using (SoulHunter.Core.Services.SlowSpan.Measure("ChestOpeningScreen.Show"))
            {
                _result = result ?? new ChestResult { ItemCount = 1 };
                CanSkip = canSkip;
                RevealedCount = 0;
                FireworksShown = 0;
                using (SoulHunter.Core.Services.SlowSpan.Measure("Show.Activate")) gameObject.SetActive(true);
                ShownGold = 0; // after activation: the HUD now holds back the whole chest gold
                transform.SetAsLastSibling();
                using (SoulHunter.Core.Services.SlowSpan.Measure("Show.ClearCards")) ClearCards();
                // The preview's RenderTexture is kept between chests (2026-10-17): a new 512x512 MSAA texture on every chest
                // was part of the 16-20 ms frame in which the screen opens. The stage that leaves gives it back first.
                RenderTexture keep = null;
                if (_stage != null) { keep = _stage.HandBackTexture(); Destroy(_stage.gameObject); }
                _stage = ChestPreviewStage.Create(chestPrefab, coinPrefab, _result.IsArcana, _result.Kind, keep);
                _chestImage.texture = _stage.Texture;
                _chestImage.color = Color.white;
                _title.text = _result.IsArcana ? "ARCANA CHEST!" : _result.ItemCount >= 5 ? "TREASURE x5!" : _result.ItemCount >= 3 ? "TREASURE x3!" : "TREASURE!";
                UITheme.ApplyText(_title, _result.IsArcana ? ThemedElement.TextRole.TitleArcana : ThemedElement.TextRole.Title);
                _coinText.text = "";
                _coinText.transform.localScale = Vector3.zero;
                _skipHint.gameObject.SetActive(canSkip);
                _skipHint.text = SkipHintText;
                _circle.color = new Color(1f, 1f, 1f, 0f);
                _openButton.gameObject.SetActive(false);
                _doneButton.gameObject.SetActive(false);
                using (SoulHunter.Core.Services.SlowSpan.Measure("Show.Layout")) Layout();
                Current = Phase.Intro;
                _sequence = StartCoroutine(Intro());
            }
        }

        private IEnumerator Intro()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.TreasureFoundSound != null ? audio.TreasureFoundSound : audio.ChestOpenSound);
            // Intro blast: the backdrop dims, the chest drops in with a flash and a ring.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                _backdrop.color = Veil(BackdropAlpha * t, _result.IsArcana);
                yield return null;
            }
            // The loop stops before t reaches 1, and on a phone the first frame (the 3D preview being made) can be longer
            // than the whole fade: the backdrop then stayed nearly clear and the game showed through (owner's phone,
            // 2026-10-10). It always ends at its full strength.
            _backdrop.color = Veil(BackdropAlpha, _result.IsArcana);
            Blast(0.9f);
            yield return Wait(0.35f);
            Current = Phase.WaitingToOpen;
            yield return RotateIn(_openButton);
        }

        public void PressOpen()
        {
            if (Current != Phase.WaitingToOpen) return;
            if (_sequence != null) StopCoroutine(_sequence);
            _openButton.gameObject.SetActive(false);
            _sequence = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            Current = Phase.Revealing;
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null && !_result.IsArcana) audio.PlaySFX(audio.TreasureJingle(_result.ItemCount));
            else if (audio != null) audio.PlaySFX(audio.TreasureCSound);
            int coins = _result.ItemCount >= 5 ? 36 : _result.ItemCount >= 3 ? 22 : 12;
            _stage.Open(coins);
            yield return Wait(ChestPreviewStage.LidOpenSeconds * 0.7f);
            Blast(1.2f);
            if (_result.ItemCount >= 3 || _result.IsArcana) _circle.color = new Color(1f, 1f, 1f, 0.01f); // the panel lights up (not on 1-item chests)

            BuildCards();
            var counting = StartCoroutine(CountCoins(1.1f));
            for (int i = 0; i < _cards.Count; i++)
            {
                yield return RevealCard(i, _result.ItemCount > 1 ? 0.55f : 0.2f);
                if (_result.ItemCount >= 5) Fireworks(3);
            }
            if (_result.ItemCount >= 5) { Fireworks(6); yield return Wait(0.4f); Fireworks(6); }
            yield return counting; // DONE only once the gold has finished counting up
            yield return Wait(0.5f);
            Current = Phase.Done;
            yield return RotateIn(_doneButton);
        }

        /// <summary>
        /// Skipping on a touch screen (2026-10-10). The wiki (Treasure_Chest, Patch 0.2.6b) names only "ESC, right mouse
        /// button, or "B"/cancel on a gamepad" and is silent on phones; balance method: a tap on the screen outside the
        /// buttons is the phone's cancel. The same skip rules apply (TrySkip). The hint says so.
        /// </summary>
        public static string SkipHintText => UiScreen.IsTouch ? "Tap to skip" : "Esc / Right click / B: skip";

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // Only the backdrop or the content's empty space reaches here; OPEN and DONE take their own clicks.
            if (UiScreen.IsTouch && CanSkip) TrySkip();
        }

        /// <summary>VS skip: jumps to the end (everything revealed, Done shown). Returns false when this chest cannot be skipped.</summary>
        public bool TrySkip()
        {
            if (!CanSkip || Current == Phase.Done || Current == Phase.Closed) return false;
            if (_sequence != null) StopCoroutine(_sequence);
            StopAllCoroutines();
            _openButton.gameObject.SetActive(false);
            _backdrop.color = Veil(BackdropAlpha, _result.IsArcana);
            _stage.FinishNow();
            if (_cards.Count == 0) BuildCards();
            for (int i = 0; i < _cards.Count; i++) ShowCard(i);
            RevealedCount = _cards.Count;
            ShownGold = _result.Gold;
            _coinText.text = "+" + _result.Gold;
            _coinText.transform.localScale = Vector3.one;
            if (_result.ItemCount >= 3 || _result.IsArcana) _circle.color = new Color(1f, 1f, 1f, 0.35f);
            Current = Phase.Done;
            _doneButton.gameObject.SetActive(true);
            _doneButton.transform.localRotation = Quaternion.identity;
            _doneButton.transform.localScale = Vector3.one;
            Focus(_doneButton);
            return true;
        }

        public void PressDone()
        {
            if (Current != Phase.Done) return;
            Close();
        }

        private void Close()
        {
            StopAllCoroutines();
            Current = Phase.Closed;
            if (_stage != null) { Destroy(_stage.gameObject); _stage = null; }
            foreach (var spark in _sparks) if (spark.img != null) Destroy(spark.img.gameObject);
            _sparks.Clear();
            HudGoldHold.Set(0);
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void OnDestroy()
        {
            HudGoldHold.Set(0);
            if (_stage != null) Destroy(_stage.gameObject);
        }

        // ---------- pieces ----------

        private void BuildCards()
        {
            ClearCards();
            float width = RoomyCardSize; // Layout() gives each card its place and size for this screen
            for (int i = 0; i < _result.Rewards.Count; i++)
            {
                var reward = _result.Rewards[i];
                var card = new GameObject("Reward" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                card.SetParent(_content, false);
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(width, width);
                var frame = card.GetComponent<Image>();
                frame.raycastTarget = false;
                UITheme.ApplySurface(frame, ThemedElement.SurfaceRole.Card);
                if (reward.IsEvolution) UITheme.Edge(card.gameObject, Evolution, 4f); // an evolution's card has the theme's evolution frame

                var star = new GameObject("Stars", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                star.rectTransform.SetParent(card, false);
                star.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                star.rectTransform.sizeDelta = new Vector2(170f, 170f);
                star.texture = _sparkleTexture;
                star.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
                star.raycastTarget = false;

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.rectTransform.SetParent(card, false);
                icon.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                icon.rectTransform.sizeDelta = new Vector2(100f, 100f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                var name = Label("Name", new Vector2(0f, -52f), new Vector2(width - 8f, 40f), 22f, card);
                name.text = reward.Name;
                name.enableAutoSizing = true; // its limits follow the screen (SizeCard)
                UITheme.ApplyText(name, ThemedElement.TextRole.Card); // on the theme's card
                name.outlineWidth = 0f;
                var level = Label("Level", new Vector2(0f, -80f), new Vector2(width - 8f, 28f), 20f, card);
                level.text = reward.IsEvolution ? "EVOLUTION!" : reward.IsCoinBag ? "+25 gold" : reward.Level <= 1 ? "NEW!" : "Level " + reward.Level;
                level.enableAutoSizing = true;
                UITheme.ApplyText(level, reward.IsEvolution ? ThemedElement.TextRole.Evolution : ThemedElement.TextRole.Tag);
                level.outlineWidth = 0f;
                name.gameObject.SetActive(false);
                level.gameObject.SetActive(false);
                card.localScale = Vector3.zero;
                _cards.Add(card);
                _cardIcons.Add(icon);
                _cardStars.Add(star);
            }
            Layout(); // places and sizes the new cards for this screen
        }

        // ---------- layout (one path for every screen: roomy, stacked tall, or side by side when short) ----------

        private void OnEnable() => UiScreen.Changed += Relayout;

        private void OnDisable() => UiScreen.Changed -= Relayout;

        private void Relayout() => Layout();

        private Vector2 ButtonSize()
        {
            var element = _openButton != null ? _openButton.GetComponent<ThemedElement>() : null;
            if (element != null && UiThemeAsset.Active != null && element.StandardSize(UiThemeAsset.Active) is Vector2 size) return size;
            return new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
        }

        private int PlannedCards => _cards.Count > 0 ? _cards.Count : Mathf.Max(1, _result != null ? _result.Rewards.Count : 1);

        private static float CardsHeight(int count, int columns, float card, float gap)
        {
            int rows = Mathf.CeilToInt(count / (float)Mathf.Max(1, columns));
            return rows * card + (rows - 1) * gap;
        }

        /// <summary>
        /// Places the screen in its safe area. Roomy and tall screens stack it: title, chest, gold, the cards (wrapping
        /// onto more rows on a narrow screen), and OPEN / DONE pinned to the bottom edge. The cards step down from 190 to
        /// 140 and the chest from 440 to 220 before a short screen (a phone held sideways) puts the title, chest and gold
        /// on the left and the cards, skip hint and button on the right. Runs again on every screen change.
        /// </summary>
        private void Layout()
        {
            if (_content == null) return;
            Vector2 size = _content.rect.size;
            if (size.x <= 1f || size.y <= 1f) return;
            _laidOutSize = size;
            float w = size.x, h = size.y, m = EdgeMargin, gap = Gap;
            Vector2 button = ButtonSize();
            int n = PlannedCards;
            bool hint = _skipHint.gameObject.activeSelf;
            float textMin = Mathf.Max(8f, UiScreen.MinTextUnits);

            // Stacked: the size of cards that leaves the biggest chest (and, after that, the biggest cards).
            float column = w - 2f * m;
            bool hintBeside = w * 0.5f - button.x * 0.5f - gap - m >= HintWidth;
            float hintRow = hint && !hintBeside ? HintHeight + gap : 0f;
            float bottomBlock = button.y + hintRow;
            float bestScore = -1f, chest = 0f, card = RoomyCardSize;
            int columns = 1;
            foreach (float s in CardSizes)
            {
                int cols = UiScreen.ColumnsFor(column, s, gap, n);
                float rest = m + TitleHeight + gap + gap + CoinHeight + gap + CardsHeight(n, cols, s, gap) + gap + bottomBlock + m;
                float c = Mathf.Min(ChestSize, column, h - rest);
                if (c < MinChestSize) continue;
                float score = c + s * 0.5f;
                if (score > bestScore) { bestScore = score; chest = c; card = s; columns = cols; }
            }
            SideBySide = bestScore < 0f;

            float top = h * 0.5f; // y of the content's top edge, around its centre
            if (!SideBySide)
            {
                float by = -h * 0.5f + m + button.y * 0.5f;
                Place(_openButton, 0f, by);
                Place(_doneButton, 0f, by);
                if (hint)
                {
                    if (hintBeside) PlaceText(_skipHint, w * 0.5f - m - HintWidth * 0.5f, by, HintWidth, HintHeight, 26f, Mathf.Max(textMin, 16f));
                    else PlaceText(_skipHint, 0f, by + button.y * 0.5f + gap + HintHeight * 0.5f, Mathf.Min(HintWidth, column), HintHeight, 26f, Mathf.Max(textMin, 16f));
                }
                float cardsH = CardsHeight(n, columns, card, gap);
                float block = TitleHeight + gap + chest + gap + CoinHeight + gap + cardsH;
                float room = h - m - (m + bottomBlock + gap);
                float y = top - m - Mathf.Max(0f, room - block) * 0.5f; // the stack is centred in the room above the button
                PlaceText(_title, 0f, y - TitleHeight * 0.5f, Mathf.Min(1200f, column), TitleHeight, 78f, Mathf.Max(textMin, 30f));
                y -= TitleHeight + gap;
                PlaceChest(0f, y - chest * 0.5f, chest);
                y -= chest + gap;
                PlaceText(_coinText, 0f, y - CoinHeight * 0.5f, Mathf.Min(700f, column), CoinHeight, 64f, Mathf.Max(textMin, 28f));
                y -= CoinHeight + gap;
                PlaceCards(0f, y, card, columns, gap);
                return;
            }

            // Side by side (short screens): title, chest and gold on the left; the cards and the button on the right, so
            // the cards keep their full height. The title is a line shorter here (it autosizes to the narrower column).
            float leftWidth = w * 0.42f, sideTitle = TitleHeight * 0.75f;
            float leftColumn = leftWidth - 2f * m, leftX = -w * 0.5f + leftWidth * 0.5f;
            chest = Mathf.Max(120f, Mathf.Min(ChestSize, leftColumn, h - 2f * m - sideTitle - gap - gap - CoinHeight));
            float leftBlock = sideTitle + gap + chest + gap + CoinHeight;
            float ly = top - m - Mathf.Max(0f, h - 2f * m - leftBlock) * 0.5f;
            PlaceText(_title, leftX, ly - sideTitle * 0.5f, leftColumn, sideTitle, 78f, Mathf.Max(textMin, 30f));
            ly -= sideTitle + gap;
            PlaceChest(leftX, ly - chest * 0.5f, chest);
            ly -= chest + gap;
            PlaceText(_coinText, leftX, ly - CoinHeight * 0.5f, Mathf.Min(700f, leftColumn), CoinHeight, 64f, Mathf.Max(textMin, 28f));

            float rightWidth = w - leftWidth - m;
            float rightX = -w * 0.5f + leftWidth + rightWidth * 0.5f;
            float rby = -h * 0.5f + m + button.y * 0.5f;
            Place(_openButton, rightX, rby);
            Place(_doneButton, rightX, rby);
            float above = rby + button.y * 0.5f + gap; // the lowest y the cards may reach
            if (hint)
            {
                PlaceText(_skipHint, rightX, above + HintHeight * 0.5f, Mathf.Min(HintWidth, rightWidth), HintHeight, 26f, Mathf.Max(textMin, 16f));
                above += HintHeight + gap;
            }
            float ry = top - m;
            float cardRoom = ry - above;
            card = MinCardSize;
            columns = UiScreen.ColumnsFor(rightWidth, MinCardSize, gap, n);
            foreach (float s in CardSizes)
            {
                int cols = UiScreen.ColumnsFor(rightWidth, s, gap, n);
                if (CardsHeight(n, cols, s, gap) <= cardRoom) { card = s; columns = cols; break; }
            }
            float sideCards = CardsHeight(n, columns, card, gap);
            PlaceCards(rightX, ry - Mathf.Max(0f, cardRoom - sideCards) * 0.5f, card, columns, gap);
        }

        private static void Place(Button button, float x, float y)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void PlaceText(TextMeshProUGUI label, float x, float y, float width, float height, float max, float min)
        {
            var rect = label.rectTransform;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            label.enableAutoSizing = true;
            label.fontSizeMax = Mathf.Max(max, min);
            label.fontSizeMin = min;
        }

        private void PlaceChest(float x, float y, float size)
        {
            // The light, flash and ring keep their 1920x1080 proportions to the chest (760 / 560 / 460 around a 440 chest).
            float k = size / ChestSize;
            SetBox(_chestImage, x, y, size);
            SetBox(_circle, x, y, 760f * k);
            SetBox(_flash, x, y, 560f * k);
            SetBox(_ring, x, y, 460f * k);
        }

        private static void SetBox(RawImage image, float x, float y, float size)
        {
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            image.rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>The cards in centred rows of <paramref name="columns"/>, from <paramref name="topY"/> down.</summary>
        private void PlaceCards(float centreX, float topY, float card, int columns, float gap)
        {
            CardSize = card;
            int n = _cards.Count;
            for (int i = 0; i < n; i++)
            {
                int row = i / columns, col = i % columns;
                int inRow = Mathf.Min(columns, n - row * columns);
                float x = centreX + (col - (inRow - 1) * 0.5f) * (card + gap);
                float y = topY - row * (card + gap) - card * 0.5f;
                if (_cards[i] == null) continue;
                _cards[i].anchoredPosition = new Vector2(x, y);
                SizeCard(_cards[i], card);
            }
        }

        /// <summary>A card's pieces at the 190-unit card's proportions; its text never under the screen's floor.</summary>
        private static void SizeCard(RectTransform card, float size)
        {
            float k = size / RoomyCardSize, textMin = Mathf.Max(12f, UiScreen.MinTextUnits);
            card.sizeDelta = new Vector2(size, size);
            foreach (Transform child in card)
            {
                if (!(child is RectTransform part)) continue;
                switch (part.name)
                {
                    case "Stars": part.anchoredPosition = new Vector2(0f, 22f * k); part.sizeDelta = Vector2.one * 170f * k; break;
                    case "Icon": part.anchoredPosition = new Vector2(0f, 22f * k); part.sizeDelta = Vector2.one * 100f * k; break;
                    // A smaller card keeps the name a little higher, so the floor-sized text does not run into the level line.
                    case "Name": SizeCardText(part, -52f * k + 12f * (1f - k), size - 8f, Mathf.Max(40f * k, textMin * 1.3f), 22f, textMin); break;
                    case "Level": SizeCardText(part, -80f * k, size - 8f, Mathf.Max(28f * k, textMin * 1.2f), 20f, textMin); break;
                }
            }
        }

        private static void SizeCardText(RectTransform part, float y, float width, float height, float max, float min)
        {
            part.anchoredPosition = new Vector2(0f, y);
            part.sizeDelta = new Vector2(width, height);
            var label = part.GetComponent<TextMeshProUGUI>();
            if (label == null) return;
            label.enableAutoSizing = true;
            label.fontSizeMin = min;
            label.fontSizeMax = Mathf.Max(max, min);
        }

        private void ClearCards()
        {
            foreach (var card in _cards) if (card != null) Destroy(card.gameObject);
            _cards.Clear(); _cardIcons.Clear(); _cardStars.Clear();
        }

        private IEnumerator RevealCard(int index, float reelSeconds)
        {
            var card = _cards[index];
            var icon = _cardIcons[index];
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.15f) { card.localScale = Vector3.one * t; yield return null; }
            card.localScale = Vector3.one;
            // Reel: icons spin past before the prize stops.
            icon.enabled = _reelIcons.Count > 0;
            float next = 0f;
            for (float t = 0f; t < reelSeconds; t += Time.unscaledDeltaTime)
            {
                if (t >= next && _reelIcons.Count > 0)
                {
                    icon.sprite = _reelIcons[Random.Range(0, _reelIcons.Count)];
                    next = t + Mathf.Lerp(0.04f, 0.12f, t / reelSeconds);
                }
                yield return null;
            }
            ShowCard(index);
            RevealedCount = Mathf.Max(RevealedCount, index + 1);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.7f);
            Burst(card.anchoredPosition + new Vector2(0f, 22f * CardSize / RoomyCardSize), 8, _result.Rewards[index].IsEvolution ? Evolution : Gold);
        }

        private void ShowCard(int index)
        {
            var reward = _result.Rewards[index];
            var card = _cards[index];
            card.localScale = Vector3.one;
            var icon = _cardIcons[index];
            icon.sprite = reward.Icon;
            icon.enabled = reward.Icon != null;
            _cardStars[index].color = new Color(Gold.r, Gold.g, Gold.b, 0.9f);
            foreach (Transform child in card) child.gameObject.SetActive(true);
            if (!icon.enabled) icon.gameObject.SetActive(false);
        }

        private IEnumerator CountCoins(float seconds)
        {
            // Scales in, counts up, then pulses.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.2f) { _coinText.transform.localScale = Vector3.one * t; yield return null; }
            _coinText.transform.localScale = Vector3.one;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                ShownGold = Mathf.RoundToInt(_result.Gold * Mathf.Clamp01(t / seconds));
                _coinText.text = "+" + ShownGold;
                yield return null;
            }
            ShownGold = _result.Gold;
            _coinText.text = "+" + ShownGold;
            _coinPulse = 1f;
        }

        private void Blast(float strength)
        {
            StartCoroutine(Pulse(_flash, 0.35f, 0.6f, 1.25f * strength, Color.white));
            StartCoroutine(Pulse(_ring, 0.5f, 0.3f, 1.9f * strength, _result != null && _result.IsArcana ? ArcanaTint : Soul));
        }

        private static IEnumerator Pulse(RawImage image, float seconds, float fromScale, float toScale, Color color)
        {
            if (image == null || image.texture == null) yield break;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, 1f - (1f - t) * (1f - t));
                image.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }
            image.color = new Color(color.r, color.g, color.b, 0f);
        }

        private IEnumerator RotateIn(Button button)
        {
            button.gameObject.SetActive(true);
            var t0 = button.transform;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                t0.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * -120f);
                t0.localScale = Vector3.one * t;
                yield return null;
            }
            t0.localRotation = Quaternion.identity;
            t0.localScale = Vector3.one;
            Focus(button);
        }

        private static void Focus(Button button)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private void Fireworks(int count)
        {
            FireworksShown++;
            var colors = new[] { Gold, Soul, Evolution, UITheme.Rarity.Good, ArcanaTint };
            for (int i = 0; i < count; i++)
            {
                // Over the screen's upper part: the 1920x1080 layout's spread (+-820, -120..460) on any screen.
                Vector2 size = _content.rect.size;
                Burst(new Vector2(Random.Range(-0.43f, 0.43f) * size.x, Random.Range(-0.11f, 0.43f) * size.y), 10, colors[Random.Range(0, colors.Length)]);
            }
        }

        private void Burst(Vector2 at, int sparks, Color color)
        {
            if (_sparkleTexture == null) return;
            for (int i = 0; i < sparks; i++)
            {
                var img = new GameObject("Spark", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                img.rectTransform.SetParent(_content, false);
                img.rectTransform.anchoredPosition = at;
                img.rectTransform.sizeDelta = Vector2.one * Random.Range(26f, 48f);
                img.texture = _sparkleTexture;
                img.color = color;
                img.raycastTarget = false;
                float angle = Random.value * Mathf.PI * 2f;
                _sparks.Add((img, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(160f, 420f), 0f));
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_content != null && _content.rect.size != _laidOutSize) Layout(); // a rotation or window change, even unannounced
            if (_circle.color.a > 0f)
            {
                _circle.rectTransform.Rotate(0f, 0f, -25f * dt);
                float a = Mathf.MoveTowards(_circle.color.a, 0.35f, dt * 0.8f);
                var tint = _result != null && _result.IsArcana ? ArcanaTint : Gold;
                _circle.color = new Color(tint.r, tint.g, tint.b, a);
            }
            for (int i = 0; i < _cardStars.Count; i++)
                if (_cardStars[i] != null) _cardStars[i].rectTransform.Rotate(0f, 0f, 40f * dt);
            if (_coinPulse > 0f)
            {
                _coinPulse += dt;
                _coinText.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(_coinPulse * 6f));
            }
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (img, v, age) = _sparks[i];
                age += dt;
                if (img == null || age > 0.9f) { if (img != null) Destroy(img.gameObject); _sparks.RemoveAt(i); continue; }
                v *= 1f - 2.2f * dt;
                img.rectTransform.anchoredPosition += v * dt;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f - age / 0.9f);
                _sparks[i] = (img, v, age);
            }

            if (CanSkip && SkipPressed()) TrySkip();
        }

        private static bool SkipPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                   (pad != null && pad.buttonEast.wasPressedThisFrame);
        }
    }
}
