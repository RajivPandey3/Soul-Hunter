using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The in-play guide arrows of the VS Milky Way Map (vampire.survivors.wiki/w/Milky_Way_Map, raw, fetched 2026-10-12):
    /// "A "Show Guides"/"Hide Guides" button is also added to the pause menu, which, when toggled on, displays an arrow
    /// pointing to each item with that item's icon"; Patch 0.4.2: "Guide arrows don't jitter anymore - please enable the
    /// guides from the Pause menu if you have the Map but can't locate coffins etc."
    ///
    /// Soul Hunter's reading:
    /// - Off until the player turns them on with the map screen's Guides button ("when toggled on").
    /// - They point at what a player goes looking for: stage items, relics and caskets (the wiki's "coffins etc.").
    ///   Pickups, chests, plates, lanterns, the merchant and the boss get no arrow.
    /// - An arrow sits near the screen's edge in the thing's direction, with the thing's icon (or its map letter), and
    ///   goes away while the thing is on screen.
    ///
    /// Balance method where the wiki is silent or Soul Hunter differs (owner rule, 2026-10-09):
    /// - The wiki says an arrow for "each item". Soul Hunter shows the nearest <see cref="MaxArrows"/> only (coordinator
    ///   direction, 2026-10-12: the HUD stays calm): a level has six stage items, and six arrows ring the whole screen.
    /// - The switch lasts until the game is closed; it is not written to the save (the wiki does not say it is kept, and
    ///   this change does not touch the save's fields).
    /// - The arrows keep clear of the HUD's top band and are hidden whenever the game is paused (a menu is open) or the
    ///   run is over. They are not controls, so the touch-size rule does not apply; their text keeps the 12 sp floor.
    /// - No blinking or movement of their own (nothing for the Flashing VFX option to switch off).
    /// The pointer is a shape from the theme; the design waits for the owner's approval.
    /// </summary>
    public sealed class MapGuides : MonoBehaviour
    {
        public const string CanvasName = "MapGuidesCanvas";
        /// <summary>Over the HUD (0), under every menu (300 and up).</summary>
        public const int ScreenSortingOrder = 5;
        public const int MaxArrows = 3;
        /// <summary>How far inside the screen an arrow sits, as shares of the screen: clear of the HUD strip at the top.</summary>
        public const float InsetX = 0.06f, InsetBottom = 0.1f, InsetTop = 0.2f;
        /// <summary>A thing counts as on screen inside this margin of the view (its arrow then hides).</summary>
        public const float OnScreenMargin = 0.04f;
        public const float PlateSize = 52f;
        /// <summary>Seconds between looks for new things that ask for a mark (relics, caskets); stage items are read every frame.</summary>
        public const float ScanSeconds = 2f;

        /// <summary>The Guides switch (VS "Show Guides"/"Hide Guides"); off when the game starts.</summary>
        public static bool Enabled { get; set; }

        public static MapGuides Current { get; private set; }

        /// <summary>One arrow: where it points and its parts.</summary>
        public sealed class Arrow
        {
            public RectTransform Root, Tip;
            public Image Plate, Icon;
            public TextMeshProUGUI Letter;
            public MapMarkerKind Kind;
            public Vector3 Target;
            public bool Shown;
        }

        /// <summary>The arrows (always <see cref="MaxArrows"/>; the unused ones are hidden).</summary>
        public readonly List<Arrow> Arrows = new List<Arrow>();
        /// <summary>How many arrows show now.</summary>
        public int ShownCount { get; private set; }

        private readonly List<MonoBehaviour> _askers = new List<MonoBehaviour>();
        private readonly List<(MapMarkerKind Kind, Vector3 Position, Sprite Icon, float Distance)> _targets =
            new List<(MapMarkerKind, Vector3, Sprite, float)>();
        private StageItemSpawner _spawner;
        private float _nextScan;

        /// <summary>Makes the arrows' canvas once per run (the HUD calls it through the pause menu).</summary>
        public static MapGuides Ensure()
        {
            if (Current != null) return Current;
            var go = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ScreenSortingOrder;
            ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            var guides = go.AddComponent<MapGuides>(); // no raycaster: the arrows never take a click or a tap
            guides.Build();
            return guides;
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Build()
        {
            for (int i = 0; i < MaxArrows; i++) Arrows.Add(MakeArrow(i));
        }

        private Arrow MakeArrow(int index)
        {
            var arrow = new Arrow();
            arrow.Root = new GameObject("Guide_" + index, typeof(RectTransform)).GetComponent<RectTransform>();
            arrow.Root.SetParent(transform, false);
            arrow.Root.pivot = new Vector2(0.5f, 0.5f);
            arrow.Root.sizeDelta = new Vector2(PlateSize, PlateSize);

            // The pointer: a square on its corner behind the plate, pushed toward the thing, so one corner shows as the tip.
            arrow.Tip = new GameObject("Tip", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            arrow.Tip.SetParent(arrow.Root, false);
            arrow.Tip.anchorMin = arrow.Tip.anchorMax = arrow.Tip.pivot = new Vector2(0.5f, 0.5f);
            var tipImage = arrow.Tip.GetComponent<Image>();
            tipImage.raycastTarget = false;
            UITheme.ApplySurface(tipImage, ThemedElement.SurfaceRole.Accent);

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            plate.SetParent(arrow.Root, false);
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.one;
            plate.offsetMin = plate.offsetMax = Vector2.zero;
            arrow.Plate = plate.GetComponent<Image>();
            arrow.Plate.raycastTarget = false;
            UITheme.ApplySurface(arrow.Plate, ThemedElement.SurfaceRole.SlotFilled);
            UITheme.Edge(plate.gameObject, UITheme.Accent, 2f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            icon.SetParent(plate, false);
            icon.anchorMin = new Vector2(0.1f, 0.1f);
            icon.anchorMax = new Vector2(0.9f, 0.9f);
            icon.offsetMin = icon.offsetMax = Vector2.zero;
            arrow.Icon = icon.GetComponent<Image>();
            arrow.Icon.preserveAspect = true;
            arrow.Icon.raycastTarget = false;

            var letter = new GameObject("Letter", typeof(RectTransform)).GetComponent<RectTransform>();
            letter.SetParent(plate, false);
            letter.anchorMin = Vector2.zero;
            letter.anchorMax = Vector2.one;
            letter.offsetMin = letter.offsetMax = Vector2.zero;
            arrow.Letter = letter.gameObject.AddComponent<TextMeshProUGUI>();
            arrow.Letter.fontSize = 30f; // a size before auto-size (TMP's defaults would replace the limits)
            arrow.Letter.enableAutoSizing = true;
            arrow.Letter.fontSizeMin = 14f;
            arrow.Letter.fontSizeMax = 30f;
            arrow.Letter.alignment = TextAlignmentOptions.Center;
            arrow.Letter.textWrappingMode = TextWrappingModes.NoWrap;
            arrow.Letter.raycastTarget = false;
            UITheme.ApplyText(arrow.Letter, ThemedElement.TextRole.Accent);

            arrow.Root.gameObject.SetActive(false);
            return arrow;
        }

        private void LateUpdate() => Refresh(Camera.main);

        /// <summary>
        /// Shows the arrows for the world as it is now, seen through <paramref name="camera"/> (also called by tests).
        /// Nothing shows while the switch is off, the game is paused or over, or there is no hero or camera.
        /// </summary>
        public void Refresh(Camera camera)
        {
            var player = PlayerController.Instance;
            var session = SoulHunter.Gameplay.Core.GameSessionManager.Instance;
            bool show = Enabled && camera != null && player != null && Time.timeScale > 0f && (session == null || !session.IsRunFinished);
            if (!show) { Hide(0); return; }

            CollectTargets(player.transform.position);
            float size = Mathf.Max(PlateSize, UiScreen.MinTextUnits * 2.6f);
            Vector3 hero = camera.WorldToViewportPoint(player.transform.position);
            int used = 0;
            foreach (var target in _targets)
            {
                if (used >= MaxArrows) break;
                Vector3 view = camera.WorldToViewportPoint(target.Position);
                if (IsOnScreen(view)) continue; // the player can see it: no arrow
                var arrow = Arrows[used++];
                Vector2 direction = ScreenDirection(view, hero);
                Vector2 spot = EdgePoint(direction);
                arrow.Kind = target.Kind;
                arrow.Target = target.Position;
                arrow.Shown = true;
                arrow.Root.anchorMin = arrow.Root.anchorMax = spot;
                arrow.Root.anchoredPosition = Vector2.zero;
                arrow.Root.sizeDelta = new Vector2(size, size);
                // The tip: half the plate's size, turned on its corner, its centre on the plate's edge toward the thing.
                // The direction is in shares of the screen; on the screen itself it is stretched by the screen's shape.
                Vector2 pixels = UiScreen.Pixels;
                var drawn = new Vector2(direction.x * pixels.x, direction.y * pixels.y);
                drawn = drawn.sqrMagnitude > 1e-8f ? drawn.normalized : Vector2.up;
                arrow.Tip.sizeDelta = new Vector2(size * 0.5f, size * 0.5f);
                arrow.Tip.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(drawn.y, drawn.x) * Mathf.Rad2Deg + 45f);
                arrow.Tip.anchoredPosition = drawn * (size * 0.5f);
                arrow.Icon.sprite = target.Icon;
                arrow.Icon.enabled = target.Icon != null;
                arrow.Letter.text = target.Icon != null ? "" : LetterFor(target.Kind);
                arrow.Letter.fontSizeMin = Mathf.Max(14f, UiScreen.MinTextUnits);
                if (arrow.Letter.fontSizeMax < arrow.Letter.fontSizeMin) arrow.Letter.fontSizeMax = arrow.Letter.fontSizeMin;
                if (!arrow.Root.gameObject.activeSelf) arrow.Root.gameObject.SetActive(true);
            }
            Hide(used);
        }

        private void Hide(int from)
        {
            ShownCount = from;
            for (int i = from; i < Arrows.Count; i++)
            {
                Arrows[i].Shown = false;
                if (Arrows[i].Root.gameObject.activeSelf) Arrows[i].Root.gameObject.SetActive(false);
            }
        }

        /// <summary>The things arrows may point at, nearest the hero first.</summary>
        private void CollectTargets(Vector3 hero)
        {
            _targets.Clear();
            if (_spawner == null) _spawner = FindFirstObjectByType<StageItemSpawner>();
            if (_spawner != null)
                foreach (var item in _spawner.Placed)
                {
                    if (item == null || !item.activeInHierarchy) continue; // taken, or back in its pool
                    var renderer = item.GetComponent<StageItemPickup>() != null ? item.GetComponentInChildren<SpriteRenderer>(true) : null;
                    AddTarget(MapMarkerKind.StageItem, item.transform.position, renderer != null ? renderer.sprite : null, hero);
                }

            // Relics, caskets and anything else that asks for a mark: looked for now and then, read every frame.
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + ScanSeconds;
                _askers.Clear();
                foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (behaviour is IMapMarker) _askers.Add(behaviour);
            }
            foreach (var behaviour in _askers)
            {
                if (behaviour == null || !behaviour.isActiveAndEnabled) continue;
                var marker = (IMapMarker)behaviour;
                var kind = MapMarkers.KindOf(marker.MapKind);
                if (marker.MapVisible && MapMarkers.IsGuided(kind)) AddTarget(kind, marker.MapPosition, null, hero);
            }
            _targets.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }

        private void AddTarget(MapMarkerKind kind, Vector3 position, Sprite icon, Vector3 hero)
        {
            Vector3 offset = position - hero;
            offset.y = 0f;
            _targets.Add((kind, position, icon, offset.sqrMagnitude));
        }

        /// <summary>True when a viewport point is in front of the camera and inside the view (less a small margin).</summary>
        public static bool IsOnScreen(Vector3 viewport) =>
            viewport.z > 0f && viewport.x >= OnScreenMargin && viewport.x <= 1f - OnScreenMargin
            && viewport.y >= OnScreenMargin && viewport.y <= 1f - OnScreenMargin;

        /// <summary>The direction on screen from the hero to a thing (viewport points), unit length; up when they coincide.</summary>
        public static Vector2 ScreenDirection(Vector3 thing, Vector3 hero)
        {
            var direction = new Vector2(thing.x - hero.x, thing.y - hero.y);
            if (thing.z < 0f) direction = -direction; // behind the camera the projection is mirrored
            return direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.up;
        }

        /// <summary>
        /// Where an arrow sits for a screen direction: on the border of the inset box around the screen's centre, as a
        /// share of the screen (0..1), so it is the same rule in landscape and in portrait.
        /// </summary>
        public static Vector2 EdgePoint(Vector2 direction)
        {
            var centre = new Vector2(0.5f, (InsetBottom + 1f - InsetTop) * 0.5f);
            float halfX = 0.5f - InsetX, halfY = (1f - InsetTop - InsetBottom) * 0.5f;
            float scale = float.MaxValue;
            if (Mathf.Abs(direction.x) > 1e-5f) scale = Mathf.Min(scale, halfX / Mathf.Abs(direction.x));
            if (Mathf.Abs(direction.y) > 1e-5f) scale = Mathf.Min(scale, halfY / Mathf.Abs(direction.y));
            if (scale == float.MaxValue) scale = 0f;
            return centre + direction * scale;
        }

        /// <summary>The map's letter for a kind without a picture (the same letters as the pause map's marks).</summary>
        public static string LetterFor(MapMarkerKind kind) =>
            kind == MapMarkerKind.StageItem ? "S" : kind == MapMarkerKind.Relic ? "!" : "?";
    }
}
