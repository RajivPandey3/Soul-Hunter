using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class PentagramWeapon : AutoAttackWeapon
    {
        public GameObject PentagramVFXPrefab; 
        
        private float _chanceToKeepGems = 0.1f; // Lv 1: deletes everything, barely keeps gems

        public override void LevelUp()
        {
            CurrentLevel++;
            AttackCooldown -= 5f; // Faster cooldown
            _chanceToKeepGems += 0.1f; // Higher chance to keep items
        }

        protected override void Attack()
        {
            if (PentagramVFXPrefab != null)
            {
                GameObject vfx = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(PentagramVFXPrefab, transform.position, Quaternion.identity)
                    : Instantiate(PentagramVFXPrefab, transform.position, Quaternion.identity);
                if (vfx != null)
                {
                    var lifetime = vfx.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(2f);
                    else Destroy(vfx, 2f);
                }
            }

            // Wipe all enemies on screen
            // KillSilently disables enemies immediately, and OnDisable removes
            // them from ActiveEnemies. Iterate backwards so that removal during
            // this wipe cannot invalidate an enumerator or skip the remaining
            // lower indices.
            var allEnemies = EnemyController.ActiveEnemies;
            for (int i = allEnemies.Count - 1; i >= 0; i--)
            {
                if (i >= allEnemies.Count) continue;
                var enemy = allEnemies[i];
                if (enemy == null) continue;
                // Check chance to leave gem
                bool keepDrop = Random.value <= _chanceToKeepGems;
                if (!keepDrop)
                {
                    // Disable drop script to simulate vaporizing the drop too
                    var drop = enemy.GetComponent<EnemyDrop>();
                    if (drop != null) drop.enabled = false;
                }
                
                var health = enemy.GetComponent<HealthController>();
                if (health != null) health.KillSilently();
            }
        }
    }
}
