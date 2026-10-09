using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Shows what Blind Fate (VS Random LevelUp; release item P3-05) picked: the item's icon and name over the hero's head
    /// for a moment. VS shows the picked item's sprite overhead (wiki Level_up, 1 April 2026: "Fixed DLC weapon sprites not
    /// appearing overhead for random level ups and limit breaks"). The look is the item's own icon on the HUD's plate with the
    /// HUD's text (theme only). It never pauses and never takes input. The HUD adds it.
    /// </summary>
    public sealed class BlindFatePopup : MonoBehaviour
    {
        /// <summary>How long the pick shows (game seconds). The wiki gives none; 1.6 s is long enough to read a name.</summary>
        public const float ShowSeconds = 1.6f;
        /// <summary>How far above the hero's centre (world units): clear of the head on every hero model.</summary>
        public const float AboveHead = 2.4f;

        private RectTransform _root, _canvasRect;
        private Canvas _canvas;
        private Image _icon;
        private TextMeshProUGUI _label;
        private LevelUpManager _levelUps;
        private float _left;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public string Text => _label != null ? _label.text : "";
        public Sprite Icon => _icon != null ? _icon.sprite : null;

        public void Setup()
        {
            if (_root != null) return;
            Build();
        }

        private void OnDestroy()
        {
            if (_levelUps != null) _levelUps.OnRandomPick -= Show;
        }

        /// <summary>Shows a pick (the level-up manager's OnRandomPick, and tests).</summary>
        public void Show(UpgradeData pick)
        {
            if (_root == null || pick == null) return;
            _icon.sprite = pick.Icon;
            _icon.enabled = pick.Icon != null;
            _label.text = UpgradeText.DisplayName(pick);
            _left = ShowSeconds;
            _root.gameObject.SetActive(true);
            Follow();
        }

        private void Update()
        {
            if (_levelUps == null)
            {
                _levelUps = FindFirstObjectByType<LevelUpManager>();
                if (_levelUps != null) _levelUps.OnRandomPick += Show;
            }
            if (!IsShowing) return;
            _left -= Time.deltaTime;
            if (_left <= 0f) _root.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (IsShowing) Follow();
        }

        private void Follow()
        {
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var cam = Camera.main;
            if (player == null || cam == null || _canvasRect == null) return;
            Vector3 screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * AboveHead);
            if (screen.z < 0f) return;
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, eventCamera, out var local))
                _root.anchoredPosition = local;
        }

        private void Build()
        {
            _canvas = GetComponentInParent<Canvas>() != null ? GetComponentInParent<Canvas>().rootCanvas : null;
            if (_canvas == null) return;
            _canvasRect = (RectTransform)_canvas.transform;
            var go = new GameObject("BlindFatePick", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _root = (RectTransform)go.transform;
            _root.SetParent(_canvasRect, false);
            _root.SetAsFirstSibling(); // under the menus
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0f);
            _root.sizeDelta = new Vector2(280f, 64f);
            var group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var plate = go.GetComponent<Image>();
            UITheme.ApplySurface(plate, ThemedElement.SurfaceRole.HudBacking);
            plate.raycastTarget = false;

            var iconRect = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            iconRect.SetParent(_root, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            var labelRect = new GameObject("Name", typeof(RectTransform)).GetComponent<RectTransform>();
            labelRect.SetParent(_root, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(64f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);
            _label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Left;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            _label.fontSize = 28f;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 14f;
            _label.fontSizeMax = 28f;
            _label.raycastTarget = false;
            UITheme.ApplyText(_label, ThemedElement.TextRole.HudLabel);
            go.SetActive(false);
        }
    }
}
