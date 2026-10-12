using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Lays a real screen out at another screen size inside a test (owner, 2026-10-10: the same screens in landscape and
    /// portrait). The test runner's window cannot change shape, so the screen's canvas is turned into a world-space canvas
    /// of exactly the canvas size UiScreen gives that screen; anchors, layout groups and the responsive pieces then lay the
    /// screen out as they would on that device. UiScreen is told the size, density and touch flag too, so the touch rules
    /// apply. Measurements are in canvas units.
    /// </summary>
    public static class ScreenSimulation
    {
        /// <summary>The screens every responsive screen is checked on.</summary>
        public static readonly (string Name, Vector2 Pixels, float Dpi, bool Touch)[] Devices =
        {
            ("PC 1920x1080", new Vector2(1920, 1080), 96f, false),
            ("PC 21:9", new Vector2(2560, 1080), 96f, false),
            ("PC 5:4", new Vector2(1280, 1024), 96f, false),
            ("Phone portrait", new Vector2(1080, 2400), 480f, true),
            ("Phone landscape", new Vector2(2400, 1080), 480f, true),
        };

        private static readonly Dictionary<Canvas, (RenderMode Mode, Vector2 Size, Vector3 Scale)> Saved = new Dictionary<Canvas, (RenderMode, Vector2, Vector3)>();

        /// <summary>Lays <paramref name="canvases"/> out as on a <paramref name="pixels"/> screen at <paramref name="dpi"/>.</summary>
        public static IEnumerator At(Vector2 pixels, float dpi, bool touch, params Canvas[] canvases)
        {
            UiScreen.TestPixels = pixels;
            UiScreen.TestDpi = dpi;
            UiScreen.TestTouch = touch;
            Vector2 units = UiScreen.CanvasUnits;
            foreach (var canvas in canvases.Where(c => c != null).Select(c => c.rootCanvas).Distinct())
            {
                var rect = (RectTransform)canvas.transform;
                if (!Saved.ContainsKey(canvas)) Saved[canvas] = (canvas.renderMode, rect.sizeDelta, rect.localScale);
                canvas.renderMode = RenderMode.WorldSpace;
                rect.localScale = Vector3.one;
                rect.sizeDelta = units;
            }
            UiThemeAsset.NotifyChanged(); // standard sizes follow the touch rule
            UiScreen.RaiseChanged();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Puts the canvases back on the real screen.</summary>
        public static void Restore()
        {
            UiScreen.TestPixels = null;
            UiScreen.TestDpi = null;
            UiScreen.TestTouch = null;
            foreach (var pair in Saved)
            {
                if (pair.Key == null) continue;
                pair.Key.renderMode = pair.Value.Mode;
                var rect = (RectTransform)pair.Key.transform;
                rect.localScale = pair.Value.Scale;
                rect.sizeDelta = pair.Value.Size;
            }
            Saved.Clear();
            UiThemeAsset.NotifyChanged();
            UiScreen.RaiseChanged();
        }

        /// <summary>A rect in its root canvas's units (origin at the canvas's bottom-left).</summary>
        public static Rect UnitsRect(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var root = (RectTransform)canvas.transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = root.InverseTransformPoint(corners[0]), b = root.InverseTransformPoint(corners[2]);
            Vector2 origin = root.rect.min;
            return Rect.MinMaxRect(a.x - origin.x, a.y - origin.y, b.x - origin.x, b.y - origin.y);
        }

        /// <summary>
        /// Every shown control inside the canvas, none overlapping another, and (touch) none under 48 dp. Returns the
        /// problems found, empty when the screen is right.
        /// </summary>
        public static List<string> Problems(string device, IEnumerable<Selectable> controls)
        {
            var problems = new List<string>();
            var shown = controls.Where(c => c != null && c.isActiveAndEnabled && c.transform is RectTransform && !ScrolledOutOfView((RectTransform)c.transform)).ToList();
            var boxes = shown.Select(c => (c.name, UnitsRect((RectTransform)c.transform), ((RectTransform)c.GetComponentInParent<Canvas>().rootCanvas.transform).rect.size)).ToList();
            float minUnits = UiScreen.MinTouchUnits;
            foreach (var (name, box, size) in boxes)
            {
                if (box.xMin < -1f || box.yMin < -1f || box.xMax > size.x + 1f || box.yMax > size.y + 1f)
                    problems.Add($"{device}: {name} {box} is off the {size} canvas");
                if (minUnits > 0f && Mathf.Min(box.width, box.height) < minUnits - 0.5f)
                    problems.Add($"{device}: {name} is {box.size} units, under the 48 dp touch target ({minUnits:0.#} units)");
            }
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Rect a = boxes[i].Item2, b = boxes[j].Item2;
                    if (Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1f)
                        problems.Add($"{device}: {boxes[i].Item1} and {boxes[j].Item1} overlap");
                }
            problems.AddRange(LabelProblems(device, shown));
            return problems;
        }

        /// <summary>
        /// Owner (2026-10-11: "tab se text bahar ja raha hai"): the drawn label of every standard button and tab stays
        /// inside it, and is not cut short. On a phone a tab grows to its label (text cannot shrink under 12 sp).
        /// </summary>
        public static List<string> LabelProblems(string device, IEnumerable<Selectable> controls)
        {
            var problems = new List<string>();
            foreach (var control in controls)
            {
                var element = control != null ? control.GetComponent<ThemedElement>() : null;
                if (control != null && control.isActiveAndEnabled) problems.AddRange(TextsOnTopOfEachOther(device, control));
                if (element == null || element.Size == ThemedElement.SizeRole.Free || !control.isActiveAndEnabled) continue;
                var box = ((RectTransform)control.transform).rect;
                foreach (var label in control.GetComponentsInChildren<TMPro.TMP_Text>())
                {
                    if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text) || label.GetComponentInParent<Selectable>() != control) continue;
                    label.ForceMeshUpdate();
                    Bounds drawn = label.textBounds;
                    if (drawn.size.x <= 0f) continue;
                    Vector2 lo = control.transform.InverseTransformPoint(label.transform.TransformPoint(drawn.min));
                    Vector2 hi = control.transform.InverseTransformPoint(label.transform.TransformPoint(drawn.max));
                    float over = Mathf.Max(Mathf.Max(box.xMin - lo.x, hi.x - box.xMax), Mathf.Max(box.yMin - lo.y, hi.y - box.yMax));
                    if (over > 1f) problems.Add($"{device}: {control.name} '{label.text}' runs {over:0.#} units out of its {box.size} box");
                    if (label.isTextTruncated) problems.Add($"{device}: {control.name} '{label.text}' is cut short");
                }
            }
            return problems;
        }

        /// <summary>
        /// Two texts of one control (a card's name, tag and description) drawn over each other: found on a touch screen on
        /// 2026-10-12, where a refit had centred every text of a level-up card on the same spot.
        /// </summary>
        public static List<string> TextsOnTopOfEachOther(string device, Selectable control)
        {
            var problems = new List<string>();
            var drawn = new List<(TMPro.TMP_Text text, Rect box)>();
            foreach (var label in control.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text) || label.color.a <= 0.01f
                    || label.GetComponentInParent<Selectable>() != control) continue;
                label.ForceMeshUpdate();
                Bounds b = label.textBounds;
                if (b.size.x <= 0f) continue;
                Vector2 lo = control.transform.InverseTransformPoint(label.transform.TransformPoint(b.min));
                Vector2 hi = control.transform.InverseTransformPoint(label.transform.TransformPoint(b.max));
                drawn.Add((label, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y)));
            }
            for (int i = 0; i < drawn.Count; i++)
                for (int j = i + 1; j < drawn.Count; j++)
                {
                    Rect a = drawn[i].box, c = drawn[j].box;
                    float x = Mathf.Min(a.xMax, c.xMax) - Mathf.Max(a.xMin, c.xMin), y = Mathf.Min(a.yMax, c.yMax) - Mathf.Max(a.yMin, c.yMin);
                    if (x > 3f && y > 3f)
                        problems.Add($"{device}: {control.name} '{drawn[i].text.text}' is drawn over '{drawn[j].text.text}' ({x:0.#} x {y:0.#} units)");
                }
            return problems;
        }

        /// <summary>
        /// True when the control lies outside the view of the list it scrolls in (a RectMask2D or Mask parent): it is hidden
        /// and takes no taps until scrolled to, so it is not judged as off screen or overlapping (2026-10-10).
        /// </summary>
        public static bool ScrolledOutOfView(RectTransform rect)
        {
            Rect box = UnitsRect(rect);
            for (var t = rect.parent; t != null; t = t.parent)
            {
                if (t.GetComponent<RectMask2D>() == null && t.GetComponent<Mask>() == null) continue;
                Rect view = UnitsRect((RectTransform)t); // every list it sits in counts (a grid inside a scrolling page)
                if (box.xMin < view.xMin - 1f || box.yMin < view.yMin - 1f || box.xMax > view.xMax + 1f || box.yMax > view.yMax + 1f) return true;
            }
            return false;
        }

        /// <summary>Asserts a screen is right on every device in <see cref="Devices"/>, restoring the real screen after.</summary>
        public static IEnumerator AssertFitsEveryDevice(System.Func<IEnumerable<Selectable>> controls, params Canvas[] canvases)
        {
            var all = new List<string>();
            try
            {
                foreach (var d in Devices)
                {
                    yield return At(d.Pixels, d.Dpi, d.Touch, canvases);
                    all.AddRange(Problems(d.Name, controls()));
                }
            }
            finally { Restore(); }
            Assert.That(all, Is.Empty, string.Join("\n", all));
        }
    }
}
