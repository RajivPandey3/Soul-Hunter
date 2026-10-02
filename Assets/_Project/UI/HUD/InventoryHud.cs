using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The HUD's inventory row: 6 weapon slots and 6 passive slots (VS wiki, Passive items: "The player has 6 slots for
    /// passive items"; weapons likewise), filled in the order the items were gained. Before this, HUD_Canvas had no slots
    /// wired and its WeaponManager field was empty, so the HUD never showed an item.
    /// The wiki does not describe the HUD layout. The rows sit at the top left under the Kills and Gold labels, and an
    /// evolved weapon keeps its base weapon's icon with a gold frame (evolutions have no icons yet; Art section). Both are
    /// provisional Soul Hunter choices.
    /// </summary>
    public sealed class InventoryHud : MonoBehaviour
    {
        public const int SlotsPerRow = 6;
        /// <summary>Slot size and spacing, in canvas units (1920x1080 reference).</summary>
        public const float SlotSize = 44f, Spacing = 6f;
        /// <summary>Top-left corner of the weapon row when there is no label to follow (canvas units from the top left).</summary>
        public static readonly Vector2 Origin = new Vector2(110f, -262f);
        /// <summary>Gap under the label the row follows (canvas units).</summary>
        public const float GapBelowLabel = 22f;

        // Bug MIS v2 C09/C10 (2026-10-02): empty slots were see-through (a lantern or gravestone behind one looked like an
        // item), and an evolved weapon's slot was a solid yellow square. Slots are now near-opaque, and an evolution
        // keeps the dark slot inside a gold border.
        public static readonly Color EmptyFrame = new Color(0.05f, 0.04f, 0.07f, 0.9f);
        public static readonly Color FilledFrame = new Color(0.08f, 0.05f, 0.1f, 0.92f);
        public static readonly Color EvolvedBorder = new Color(1f, 0.78f, 0.2f, 1f);
        private static readonly Color PlainBorder = new Color(0.35f, 0.3f, 0.45f, 0.6f);

        private WeaponManager _weapons;
        private RectTransform _rect, _canvasRect, _followLabel;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Image[] _weaponFrames = new Image[SlotsPerRow], _weaponIcons = new Image[SlotsPerRow];
        private readonly Image[] _passiveFrames = new Image[SlotsPerRow], _passiveIcons = new Image[SlotsPerRow];
        private readonly List<WeaponManager.InventorySlot> _shownWeapons = new List<WeaponManager.InventorySlot>();
        private readonly List<WeaponManager.InventorySlot> _shownPassives = new List<WeaponManager.InventorySlot>();

        /// <summary>What the rows show now, left to right.</summary>
        public IReadOnlyList<WeaponManager.InventorySlot> ShownWeapons => _shownWeapons;
        public IReadOnlyList<WeaponManager.InventorySlot> ShownPassives => _shownPassives;
        public Image WeaponIcon(int index) => _weaponIcons[index];
        public Image WeaponFrame(int index) => _weaponFrames[index];

        /// <param name="followLabel">
        /// The row sits under this label (the Gold label). The HUD labels hang from a fixed box at the screen centre, so
        /// their distance from the top edge changes with the aspect ratio; a fixed position overlapped them off 16:9.
        /// </param>
        public static InventoryHud Attach(Canvas canvas, WeaponManager weapons, RectTransform followLabel = null)
        {
            if (canvas == null) return null;
            var root = canvas.rootCanvas.transform;
            var existing = root.GetComponentInChildren<InventoryHud>(true);
            if (existing != null) { existing._followLabel = followLabel; existing.Bind(weapons); return existing; }
            var go = new GameObject("InventoryHud", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.SetAsFirstSibling(); // under the menus
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Origin;
            rect.sizeDelta = new Vector2(SlotsPerRow * (SlotSize + Spacing), 2f * (SlotSize + Spacing));
            var hud = go.AddComponent<InventoryHud>();
            hud._rect = rect;
            hud._canvasRect = (RectTransform)root;
            hud._followLabel = followLabel;
            hud.Build();
            hud.FollowLabel();
            hud.Bind(weapons);
            return hud;
        }

        private void Build()
        {
            for (int i = 0; i < SlotsPerRow; i++)
            {
                (_weaponFrames[i], _weaponIcons[i]) = MakeSlot("Weapon", i, 0);
                (_passiveFrames[i], _passiveIcons[i]) = MakeSlot("Passive", i, 1);
            }
        }

        private (Image frame, Image icon) MakeSlot(string kind, int column, int row)
        {
            var frameGo = new GameObject($"{kind}Slot{column}", typeof(RectTransform), typeof(Image));
            var frameRect = (RectTransform)frameGo.transform;
            frameRect.SetParent(transform, false);
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0f, 1f);
            frameRect.anchoredPosition = new Vector2(column * (SlotSize + Spacing), -row * (SlotSize + Spacing));
            frameRect.sizeDelta = new Vector2(SlotSize, SlotSize);
            var frame = frameGo.GetComponent<Image>();
            frame.raycastTarget = false;
            frame.color = EmptyFrame;
            var border = frameGo.AddComponent<Outline>();
            border.effectColor = PlainBorder;
            border.effectDistance = new Vector2(2f, -2f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(frameRect, false);
            iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(3f, 3f); iconRect.offsetMax = new Vector2(-3f, -3f);
            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.enabled = false;
            return (frame, icon);
        }

        public void Bind(WeaponManager weapons)
        {
            if (_weapons == weapons && weapons != null) { Refresh(); return; }
            if (_weapons != null) _weapons.OnInventoryChanged -= Refresh;
            _weapons = weapons;
            if (_weapons != null) _weapons.OnInventoryChanged += Refresh;
            Refresh();
        }

        private void LateUpdate() => FollowLabel();

        private void FollowLabel()
        {
            if (_followLabel == null || _rect == null || _canvasRect == null || !_followLabel.gameObject.activeInHierarchy) return;
            _followLabel.GetWorldCorners(_corners);
            // Bottom-left of the label, in the canvas's local space; the row hangs from there.
            Vector3 local = _canvasRect.InverseTransformPoint(_corners[0]);
            Vector2 canvasSize = _canvasRect.rect.size;
            var topLeft = new Vector2(local.x + canvasSize.x * 0.5f, local.y - canvasSize.y * 0.5f - GapBelowLabel);
            if ((_rect.anchoredPosition - topLeft).sqrMagnitude > 0.01f) _rect.anchoredPosition = topLeft;
        }

        private void OnDestroy()
        {
            if (_weapons != null) _weapons.OnInventoryChanged -= Refresh;
        }

        public void Refresh()
        {
            _shownWeapons.Clear();
            _shownPassives.Clear();
            if (_weapons != null)
            {
                _shownWeapons.AddRange(_weapons.GetInventory(passives: false));
                _shownPassives.AddRange(_weapons.GetInventory(passives: true));
            }
            Fill(_shownWeapons, _weaponFrames, _weaponIcons);
            Fill(_shownPassives, _passiveFrames, _passiveIcons);
        }

        private static void Fill(List<WeaponManager.InventorySlot> items, Image[] frames, Image[] icons)
        {
            for (int i = 0; i < SlotsPerRow; i++)
            {
                bool filled = i < items.Count;
                frames[i].color = filled ? FilledFrame : EmptyFrame;
                var border = frames[i].GetComponent<Outline>();
                if (border != null)
                {
                    bool evolved = filled && items[i].Evolved;
                    border.effectColor = evolved ? EvolvedBorder : PlainBorder;
                    border.effectDistance = evolved ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
                }
                icons[i].sprite = filled ? items[i].Icon : null;
                icons[i].enabled = filled && items[i].Icon != null;
            }
        }
    }
}
