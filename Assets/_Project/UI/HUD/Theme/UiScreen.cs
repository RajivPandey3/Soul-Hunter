using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The one rule for how big the UI is on every screen (owner, 2026-10-10: "play in both landscape and portrait ... reuse
    /// the exact same screens and make it properly responsive ... 0 duplicacy or parallel implementation").
    ///
    /// - A canvas unit is the 1920x1080 layout's unit: the screen's short side is about <see cref="ReferenceShortSide"/>
    ///   units, in landscape and in portrait alike, so a 1080p monitor is unchanged and a portrait phone gets a tall canvas.
    /// - On a touch screen a unit is never smaller than what makes the theme's body text (<see cref="BodyTextUnits"/>
    ///   units) <see cref="TouchBodyTextSp"/> sp, a phone's body size (ui-ux-pro-max "Readable Font Size": 16 on mobile).
    ///   Text and icons are then phone-sized, not PC-sized (owner, 2026-10-11: "mobile ka mobile ke anusar or pc ka pc ke
    ///   anusar"). The first rule made a 68-unit button 48 dp, which drew body text at 20 sp and everything else as big.
    /// - A control is never smaller than <see cref="TouchTargetDp"/> dp (Android's 48 dp touch target; ui-ux-pro-max
    ///   "Touch Target Size"): <see cref="MinTouchUnits"/> is that size in units, and buttons, tabs and cells grow to it.
    /// - Every canvas takes its scale from here through <see cref="ResponsiveCanvas"/>; no screen sets up its own scaler.
    ///
    /// Desktop testing of the phone rule: <c>-sh-ui-touch</c> treats the screen as touch and <c>-sh-ui-dpi N</c> sets the
    /// density (a portrait window, for example <c>-screen-width 540 -screen-height 1200 -sh-ui-touch -sh-ui-dpi 240</c>).
    /// </summary>
    public static class UiScreen
    {
        public const float ReferenceShortSide = 1080f;
        public const float TouchTargetDp = 48f;
        /// <summary>The theme's body text size in canvas units (rows, descriptions), and the size it is drawn at on a phone.</summary>
        public const float BodyTextUnits = 28f, TouchBodyTextSp = 16f;

        private static bool? _argTouch;
        private static float _argDpi = -1f;

        /// <summary>Tests and tools: a screen size, density and touch flag to use instead of the real ones (null = real).</summary>
        public static Vector2? TestPixels;
        public static float? TestDpi;
        public static bool? TestTouch;

        public static Vector2 Pixels => TestPixels ?? new Vector2(Screen.width, Screen.height);

        public static bool IsPortrait => Pixels.y > Pixels.x;

        public static bool IsTouch
        {
            get
            {
                if (TestTouch.HasValue) return TestTouch.Value;
                if (!_argTouch.HasValue) _argTouch = HasArg("-sh-ui-touch");
                return Application.isMobilePlatform || _argTouch.Value;
            }
        }

        public static float Dpi
        {
            get
            {
                if (TestDpi.HasValue) return TestDpi.Value;
                if (_argDpi < 0f) _argDpi = ArgFloat("-sh-ui-dpi", 0f);
                if (_argDpi > 0f) return _argDpi;
                return Screen.dpi > 0f ? Screen.dpi : 96f;
            }
        }

        /// <summary>Screen pixels per canvas unit.</summary>
        public static float ScaleFactor => ScaleFor(Pixels, Dpi, IsTouch, BodyTextUnits);

        /// <summary>The rule, for any screen (pure, so tests can check it).</summary>
        public static float ScaleFor(Vector2 pixels, float dpi, bool touch, float bodyTextUnits)
        {
            float shortSide = Mathf.Max(1f, Mathf.Min(pixels.x, pixels.y));
            float scale = shortSide / ReferenceShortSide;
            if (touch && bodyTextUnits > 0f) scale = Mathf.Max(scale, DpToPixels(TouchBodyTextSp, dpi) / bodyTextUnits);
            return Mathf.Max(0.01f, scale);
        }

        /// <summary>The canvas size in units for the current screen.</summary>
        public static Vector2 CanvasUnits => Pixels / ScaleFactor;

        public static float DpToPixels(float dp, float dpi) => dp * Mathf.Max(1f, dpi) / 160f;

        /// <summary>The smallest touch target, in canvas units (0 without touch): no control is drawn smaller than this.</summary>
        public static float MinTouchUnits => IsTouch ? DpToPixels(TouchTargetDp, Dpi) / ScaleFactor : 0f;

        /// <summary>The smallest text, in canvas units: 12 sp on a touch screen (readable on a phone), 8 units otherwise.</summary>
        public static float MinTextUnits => IsTouch ? DpToPixels(12f, Dpi) / ScaleFactor : 8f;

        /// <summary>The safe area (notch, rounded corners, gesture bar) as a share of the screen, 0..1 on each axis.</summary>
        public static Rect SafeAreaNormalized
        {
            get
            {
                Vector2 px = Pixels;
                if (TestPixels.HasValue || px.x <= 0f || px.y <= 0f) return new Rect(0f, 0f, 1f, 1f);
                Rect s = Screen.safeArea;
                return new Rect(s.x / px.x, s.y / px.y, s.width / px.x, s.height / px.y);
            }
        }

        /// <summary>The columns that fit <paramref name="width"/> with cells at least <paramref name="minCell"/> wide (1..max).</summary>
        public static int ColumnsFor(float width, float minCell, float gap, int maxColumns)
        {
            if (minCell <= 0f) return Mathf.Max(1, maxColumns);
            int fit = Mathf.FloorToInt((width + gap) / (minCell + gap));
            return Mathf.Clamp(fit, 1, Mathf.Max(1, maxColumns));
        }

        /// <summary>Raised when the screen size, orientation or scale changes (a rotation, a window resize).</summary>
        public static event Action Changed;

        /// <summary>Tells every responsive layout the screen changed (ResponsiveCanvas does this; tests that simulate a screen too).</summary>
        public static void RaiseChanged() => Changed?.Invoke();

        private static bool HasArg(string name)
        {
            foreach (string a in System.Environment.GetCommandLineArgs()) if (a == name) return true;
            return false;
        }

        private static float ArgFloat(string name, float fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v)) return v;
            return fallback;
        }
    }
}
