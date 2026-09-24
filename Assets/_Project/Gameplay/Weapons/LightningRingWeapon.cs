using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Expansion 1: Lightning Ring (VS Style).
    /// Har kuch seconds baad random dushmanon par asman se bijli (lightning) girti hai.
    /// Ise aim karne ki zaroorat nahi hoti.
    /// </summary>
    public class LightningRingWeapon : AutoAttackWeapon
    {
        [Header("Lightning Ring Settings")]
        public GameObject LightningStrikePrefab; // Bijli ka visual aur damage hitbox
        public float StrikeRadius = 15f; // Kitni door tak bijli gir sakti hai
        
        private int _strikesPerAttack = 2; // Level 1 par 2 bijliyan girenge
        private readonly List<EnemyController> _targetBuffer = new List<EnemyController>(64);

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 15f; // Har level par damage badhega
            
            if (CurrentLevel % 2 == 0) 
            {
                _strikesPerAttack++; // Har 2nd level par ek extra bijli giregi
            }
            if (CurrentLevel == 4 || CurrentLevel == 8)
            {
                AttackCooldown -= 0.5f; // Speed badhegi
            }
        }

        protected override void Attack()
        {
            if (LightningStrikePrefab == null) return;

            // Screen par mojood dushman dhoondo
            var allEnemies = EnemyController.ActiveEnemies;
            if (allEnemies.Count == 0) return;

            // Random dushmanon ko target karo
            _targetBuffer.Clear();
            _targetBuffer.AddRange(allEnemies);
            List<EnemyController> validTargets = _targetBuffer;
            
            int strikesThisTurn = Mathf.Min(_strikesPerAttack + ExtraAmount, validTargets.Count);

            for (int i = 0; i < strikesThisTurn; i++)
            {
                int randomIndex = Random.Range(0, validTargets.Count);
                EnemyController target = validTargets[randomIndex];
                validTargets.RemoveAt(randomIndex); // Ek dushman pe 2 bijliyan na giren

                if (Vector3.Distance(transform.position, target.transform.position) <= StrikeRadius)
                {
                    SpawnLightning(target.transform.position);
                }
            }
        }

        private void SpawnLightning(Vector3 position)
        {
            GameObject strike = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(LightningStrikePrefab, position, Quaternion.identity)
                : Instantiate(LightningStrikePrefab, position, Quaternion.identity);
            if (strike == null) return;
            ApplyArea(strike, LightningStrikePrefab);
            
            var damageDealer = strike.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = strike.AddComponent<ProjectileDamage>();
            
            damageDealer.DamageAmount = ScaledDamage(DamageAmount);
            damageDealer.SourceWeaponName = "Lightning Ring";
            
            // Bijli 0.5 sec baad khud gayab ho jayegi
            var lifetime = strike.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(0.5f);
            else Destroy(strike, 0.5f);
        }
    }
}
