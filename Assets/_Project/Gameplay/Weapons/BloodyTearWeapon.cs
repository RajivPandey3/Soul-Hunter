using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class BloodyTearWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [Header("Bloody Tear Settings (Evolved Whip)")]
        public GameObject WhipVisualPrefab; 
        public float HealAmount = 8f; 
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
            Vector3 spawnPosition = transform.position + direction * (2f * AreaMultiplier);
            GameObject whip = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WhipVisualPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(WhipVisualPrefab, spawnPosition, Quaternion.identity, transform);
            if (whip == null) return;
            ApplyArea(whip, WhipVisualPrefab);
            whip.transform.SetParent(transform);

            // TouchDamage requires a Collider. Some generated SH10 visual
            // prefabs are mesh-only, so make the pooled hitbox explicit before
            // adding the damage component instead of logging an AddComponent
            // failure and dereferencing a missing component.
            var hitbox = whip.GetComponent<Collider>();
            if (hitbox == null)
            {
                var box = whip.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(2.5f, 1.5f, 1f);
            }
            
            var damageDealer = whip.GetComponent<TouchDamage>();
            if (damageDealer == null) damageDealer = whip.AddComponent<TouchDamage>();
            
            damageDealer.DamageAmount = RollDamage(50f); // High base damage
            damageDealer.SourceWeaponName = "Bloody Tear";
            damageDealer.TargetTag = HostileTag; // TouchDamage defaults to "Player": the whip hurt its own holder
            
            // Critical hit & Heal logic simulation
            var allEnemies = EnemyController.ActiveEnemies;
            bool hitEnemy = false;
            foreach(var enemy in allEnemies)
            {
                if (enemy == null) continue; // destroyed enemies can linger in the static list
                if (Vector3.Distance(whip.transform.position, enemy.transform.position) < 3f * AreaMultiplier)
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
