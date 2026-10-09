using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// VS enemy lives (vampire.survivors.wiki/w/Scarleton, raw, fetched 2026-10-09): "Scarleton has 3 lives, meaning that it
    /// can resurrect twice. However, if killed by Pentagram, Gorgeous Moon, Rosary or Heaven Sword, it will not resurrect.
    /// Each time it dies it drops an Experience Gem." and "It cannot drop a Treasure Chest" (EnemySpawner.GivePendingChest).
    /// Soul Hunter's instant kills are Pentagram, Eclipse Sigil (Gorgeous Moon), Soul Toll (Rosary), Arcana zones and the
    /// stage-clear wipe (HealthController.IsInstantKill), plus Heaven Sword's hits.
    /// The wiki is silent on the health and timing of a resurrection. Balance method (owner rule 2026-10-09): it rises at
    /// once with full health (the existing revival path, HealthController.TryPreventDeath), and only the last death counts
    /// as a kill, since only that one ends the enemy (Results, Bestiary).
    /// Armed by EnemySpawner.SpawnEnemy for EnemyData.Lives above 1; the prefab is the type's own, so nothing else uses
    /// its TryPreventDeath.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class EnemyLives : MonoBehaviour
    {
        private HealthController _health;
        private int _livesLeft = 1;

        public int LivesLeft => _livesLeft;

        public static EnemyLives Arm(GameObject enemy, int lives)
        {
            if (enemy == null) return null;
            var component = enemy.GetComponent<EnemyLives>();
            if (component == null) component = enemy.AddComponent<EnemyLives>();
            component._health = enemy.GetComponent<HealthController>();
            component._livesLeft = Mathf.Max(1, lives);
            if (component._health != null) component._health.TryPreventDeath = component.Resurrect;
            return component;
        }

        private bool Resurrect()
        {
            if (_health == null || _livesLeft <= 1) return false;
            if (HealthController.IsInstantKill || KilledByHeavenSword()) return false;
            _livesLeft--;
            var data = GetComponent<EnemyController>() != null ? GetComponent<EnemyController>().Data : null;
            if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position, data != null ? data.ExperienceValue : 10);
            _health.ResetHealth();
            return true;
        }

        private static bool KilledByHeavenSword()
        {
            var weapon = DamageSource.Weapon;
            return weapon != null && weapon.name.Contains("Heaven");
        }
    }
}
