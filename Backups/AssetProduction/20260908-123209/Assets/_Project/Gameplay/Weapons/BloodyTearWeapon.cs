using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class BloodyTearWeapon : AutoAttackWeapon
    {
        [Header("Bloody Tear Settings (Evolved Whip)")]
        public GameObject WhipVisualPrefab; 
        public float HealAmount = 8f; 
        private int _whipsPerAttack = 2; // Always hits both sides
        
        private HealthController _playerHealth;

        private void Start()
        {
            _playerHealth = GetComponentInParent<HealthController>();
        }

        public override void LevelUp()
        {
            // Evolved weapons don't usually level up, but we keep the override
        }

        protected override void Attack()
        {
            if (WhipVisualPrefab == null) return;
            
            // Whip logic but with lifesteal
            FireWhip(Vector3.right);
            FireWhip(Vector3.left);
        }

        private void FireWhip(Vector3 direction)
        {
            Vector3 spawnPosition = transform.position + direction * 2f;
            GameObject whip = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WhipVisualPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(WhipVisualPrefab, spawnPosition, Quaternion.identity, transform);
            if (whip == null) return;
            whip.transform.SetParent(transform);
            
            var damageDealer = whip.GetComponent<TouchDamage>();
            if (damageDealer == null) damageDealer = whip.AddComponent<TouchDamage>();
            
            damageDealer.DamageAmount = 50f; // High base damage
            damageDealer.SourceWeaponName = "Bloody Tear";
            
            // Critical hit & Heal logic simulation
            var allEnemies = EnemyController.ActiveEnemies;
            bool hitEnemy = false;
            foreach(var enemy in allEnemies)
            {
                if (Vector3.Distance(whip.transform.position, enemy.transform.position) < 3f)
                {
                    hitEnemy = true;
                    // Usually this is handled by collision, we are just hooking the heal here
                }
            }
            
            if (hitEnemy && _playerHealth != null)
            {
                _playerHealth.Heal(Mathf.RoundToInt(HealAmount)); // Heal on hit! (VS signature mechanic)
            }
            
            var lifetime = whip.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(0.3f);
            else Destroy(whip, 0.3f);
        }
    }
}
