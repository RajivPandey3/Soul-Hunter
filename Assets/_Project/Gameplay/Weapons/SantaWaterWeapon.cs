using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Expansion 1: Santa Water (VS Style).
    /// Asman se pani (ya aag) ki botlein girti hain aur zameen par area of effect (AoE) zone banati hain.
    /// Dushman us zone me aane se jalte hain.
    /// </summary>
    public class SantaWaterWeapon : AutoAttackWeapon
    {
        [Header("Santa Water Settings")]
        public GameObject WaterZonePrefab; // Zone jo zameen pe fire/water damage dega
        public float DropRadius = 8f; // Kitne area me randomly bottle giregi
        
        private int _zonesPerAttack = 2; 

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 5f; 
            
            if (CurrentLevel == 4 || CurrentLevel == 8) 
            {
                _zonesPerAttack++; 
            }
        }

        protected override void Attack()
        {
            if (WaterZonePrefab == null) return;

            for (int i = 0; i < _zonesPerAttack + ExtraAmount; i++)
            {
                // Random position near player
                // Owner decision: aim at the nearest enemy; drop on it (extra flasks land close by), random around the player when none.
                Transform nearest = FindNearestEnemy(DropRadius * 2f);
                Vector2 randomOffset = Random.insideUnitCircle * (nearest != null ? (i == 0 ? 0f : 1.5f) : DropRadius);
                Vector3 centre = nearest != null ? new Vector3(nearest.position.x, transform.position.y, nearest.position.z) : transform.position;
                Vector3 dropPosition = centre + new Vector3(randomOffset.x, 0f, randomOffset.y);
                
                SpawnWaterZone(dropPosition);
            }
        }

        private void SpawnWaterZone(Vector3 position)
        {
            GameObject zone = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WaterZonePrefab, position, Quaternion.identity)
                : Instantiate(WaterZonePrefab, position, Quaternion.identity);
            if (zone == null) return;
            ApplyArea(zone, WaterZonePrefab);
            
            // TouchDamage laga denge taake zone me jo bhi aaye use lagatar damage ho
            var damageDealer = zone.GetComponent<TouchDamage>();
            if (damageDealer == null) damageDealer = zone.AddComponent<TouchDamage>();
            
            damageDealer.DamageAmount = ScaledDamage(DamageAmount);
            damageDealer.DamageInterval = 0.5f; // Har adhe second baad dubara damage
            damageDealer.SourceWeaponName = "Santa Water";
            // Learning Comment:
            // Santa Water ground zone Enemy tag ko target karta hai aur DamageType.Holy emit karta hai
            // taaki Level 7 ke spectral ghosts isse damage le sakein (GDD & AGENTS.md Section 5).
            damageDealer.TargetTag = HostileTag;
            damageDealer.DamageType = DamageType.Holy;
            
            // Zone 3 seconds tak zameen pe rahega
            var lifetime = zone.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(3f * DurationMultiplier);
            else if (Application.isPlaying) Destroy(zone, 3f * DurationMultiplier);
        }
    }
}
