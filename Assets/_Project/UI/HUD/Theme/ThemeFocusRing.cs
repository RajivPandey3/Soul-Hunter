using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The ring drawn around the chosen (focused) button, in the theme's focus colour (<see cref="ThemedMotion"/>). Its own
    /// type, so it is never mistaken for the theme's edge line (<see cref="UITheme.Edge"/>).
    /// </summary>
    public sealed class ThemeFocusRing : Outline { }
}
