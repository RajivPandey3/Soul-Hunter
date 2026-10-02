using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Orologion (Time Freeze) Pickup.
    /// Jaise hi player isay uthayega, screen ke tamaam dushman (EnemyController)
    /// 10 seconds ke liye barf (Freeze) ho jayenge.
    /// </summary>
    public class TimeFreezePickup : Pickup
    {
        public override string ResultsName => PickupNames.TimeFreeze;
        [SerializeField] private float _freezeDuration = 10f;

        /// <summary>Game time at which the Orologion's freeze ends (new spawns freeze until then; VS wiki Orologion).</summary>
        public static float WorldFreezeEndsAt { get; private set; } = -1f;
        /// <summary>Seconds of Orologion freeze left (0 when none).</summary>
        public static float WorldFreezeRemaining => Mathf.Max(0f, WorldFreezeEndsAt - Time.time);
        public static bool WorldFrozen => WorldFreezeRemaining > 0f;
        /// <summary>Starts (or extends) the Orologion's world freeze.</summary>
        public static void BeginWorldFreeze(float seconds) => WorldFreezeEndsAt = Mathf.Max(WorldFreezeEndsAt, Time.time + Mathf.Max(0f, seconds));

        protected override void OnPickedUp(Collider player)
        {
            Debug.Log($"[TimeFreezePickup] THE WORLD! Freezing all enemies for {_freezeDuration} seconds!");
            BeginWorldFreeze(_freezeDuration);

            var allEnemies = EnemyController.ActiveEnemies;
            for (int i = allEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = allEnemies[i];
                if (enemy == null) continue;
                // VS (vampire.survivors.wiki/w/Enemies, "Resistances"): "Freeze from Orologion goes through freeze
                // resistance", so bosses freeze too; weapon freezes use EnemyController.TryWeaponFreeze instead.
                enemy.Freeze(_freezeDuration);
            }
            
            // Add a screen flash or shake if we want juice
            if (SoulHunter.Gameplay.VFX.CameraShake.Instance != null)
            {
                SoulHunter.Gameplay.VFX.CameraShake.Instance.TriggerShake(0.3f, 0.4f);
            }
        }
    }
}
