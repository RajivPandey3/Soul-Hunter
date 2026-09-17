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
            var allEnemies = EnemyController.ActiveEnemies;
            foreach (var enemy in allEnemies)
            {
                // Check chance to leave gem
                bool keepDrop = Random.value <= _chanceToKeepGems;
                if (!keepDrop)
                {
                    // Disable drop script to simulate vaporizing the drop too
                    var drop = enemy.GetComponent<EnemyDrop>();
                    if (drop != null) drop.enabled = false;
                }
                
                var health = enemy.GetComponent<HealthController>();
                if (health != null) health.TakeDamage(new DamagePacket(999999, enemy.transform.position, Vector3.zero));
            }
        }
    }
}
