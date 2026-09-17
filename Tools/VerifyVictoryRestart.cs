var ui=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.UI.GameOverUI>();
var button=(UnityEngine.UI.Button)ui.GetType().GetField("_restartButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ui);
button.onClick.Invoke(); return UnityEngine.Time.timeScale;
