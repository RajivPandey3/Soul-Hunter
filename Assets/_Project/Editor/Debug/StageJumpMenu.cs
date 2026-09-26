using SoulHunter.Gameplay.Core;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Debugging
{
    /// <summary>
    /// Owner play-testing: jump the running campaign to a stage (e.g. Stage 3 to meet the Wandering Merchant)
    /// without playing the stages before it. Play Mode only; nothing is saved by the jump itself.
    /// </summary>
    public static class StageJumpMenu
    {
        private const string Menu = "Soul Hunter/Debug/Jump To Stage ";

        [MenuItem(Menu + "3 (Forgotten Village, Merchant)")] private static void Stage3() => Jump(3);
        [MenuItem(Menu + "7 (Sea of Lost Souls)")] private static void Stage7() => Jump(7);
        [MenuItem(Menu + "10 (Soul Core, Shadow Kael)")] private static void Stage10() => Jump(10);

        [MenuItem(Menu + "3 (Forgotten Village, Merchant)", true)]
        [MenuItem(Menu + "7 (Sea of Lost Souls)", true)]
        [MenuItem(Menu + "10 (Soul Core, Shadow Kael)", true)]
        private static bool CanJump() => Application.isPlaying && LevelProgressionManager.Instance != null;

        private static void Jump(int stage)
        {
            LevelProgressionManager.Instance.StartStage(stage);
            Debug.Log($"[StageJumpMenu] Jumped to stage {stage}.");
        }
    }
}
