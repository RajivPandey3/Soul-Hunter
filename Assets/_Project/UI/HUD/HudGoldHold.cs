namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Gold the HUD holds back while the chest screen counts it up. The chest's gold is granted when the chest opens, so
    /// the HUD jumped to the new total before the "+759" count-up had begun (Bug MIS B08, 2026-09-30). The chest screen
    /// sets how much is still to be counted; the HUD shows the run's gold minus that amount, so both rise together.
    /// </summary>
    public static class HudGoldHold
    {
        public static int Hidden { get; private set; }
        public static event System.Action Changed;

        public static void Set(int amount)
        {
            amount = System.Math.Max(0, amount);
            if (amount == Hidden) return;
            Hidden = amount;
            Changed?.Invoke();
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() { Hidden = 0; Changed = null; }
    }
}
