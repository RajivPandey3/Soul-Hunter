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
        [SerializeField] private float _freezeDuration = 10f;

        protected override void OnPickedUp(Collider player)
        {
            Debug.Log($"[TimeFreezePickup] THE WORLD! Freezing all enemies for {_freezeDuration} seconds!");

            var allEnemies = EnemyController.ActiveEnemies;
            for (int i = allEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = allEnemies[i];
                if (enemy == null) continue;
                // Bosses ko shayad freeze nahi hona chahiye, par VS mein bosses bhi slow ya freeze ho jate hain.
                enemy.ChangeState(new EnemyFrozenState(enemy, _freezeDuration));
            }
            
            // Add a screen flash or shake if we want juice
            if (SoulHunter.Gameplay.VFX.CameraShake.Instance != null)
            {
                SoulHunter.Gameplay.VFX.CameraShake.Instance.TriggerShake(0.3f, 0.4f);
            }
        }
    }
}
