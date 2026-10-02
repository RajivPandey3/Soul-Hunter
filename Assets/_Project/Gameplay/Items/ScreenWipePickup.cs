using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Soul Toll: the VS screen-wipe pickup (vampire.survivors.wiki, Rosary, fetched 2026-09-27). Picking it up kills
    /// every enemy on screen. Stage bosses are immune (VS: some enemies resist the instant kill; Soul Hunter uses the
    /// same rule as Pentagram). Killed enemies die normally: EnemyDrop drops their gems/coins/chests and counts the kill.
    /// Drops from light sources (weight 1, from player level 8). Name and placeholder art are Soul Hunter's own.
    /// </summary>
    public class ScreenWipePickup : Pickup
    {
        public override string ResultsName => DisplayName;
        public const string DisplayName = "Soul Toll";

        protected override void OnPickedUp(Collider player)
        {
            int killed = Wipe(Camera.main);
            Debug.Log($"[{DisplayName}] {killed} enemies wiped.");
            var at = player != null ? player.transform.position : transform.position;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, at + Vector3.up * 0.5f);
            if (SoulHunter.Gameplay.VFX.CameraShake.Instance != null) SoulHunter.Gameplay.VFX.CameraShake.Instance.TriggerShake(0.35f, 0.5f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.ExplosionSound);
        }

        /// <summary>Kills every non-boss enemy the camera sees (all of them when there is no camera). Returns the count.</summary>
        public static int Wipe(Camera camera)
        {
            int killed = 0;
            var enemies = EnemyController.ActiveEnemies;
            // Killing disables the enemy, which removes it from the list: walk backwards.
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                if (enemy.IsStageBoss) continue; // immune to instant kills
                // VS instant kill resistance (vampire.survivors.wiki/w/Enemies, "Resistances": immunity to Rosary).
                if (enemy.ResistsInstantKill) continue;
                if (!IsOnScreen(camera, enemy.transform.position)) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null || health.CurrentHealth <= 0) continue;
                health.KillSilently(); // Die() -> OnDied: EnemyDrop drops normally and counts the kill
                killed++;
            }
            return killed;
        }

        /// <summary>True when the point is inside the camera's view (always true without a camera).</summary>
        public static bool IsOnScreen(Camera camera, Vector3 position)
        {
            if (camera == null) return true;
            Vector3 view = camera.WorldToViewportPoint(position);
            return view.z > 0f && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f;
        }
    }
}
