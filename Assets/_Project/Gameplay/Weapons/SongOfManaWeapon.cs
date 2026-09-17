using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class SongOfManaWeapon : AutoAttackWeapon
    {
        public GameObject VerticalBeamPrefab; 
        private float _beamDuration = 1f;

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 10f; 
            if (CurrentLevel == 4 || CurrentLevel == 8) _beamDuration += 0.5f; 
            if (CurrentLevel % 2 == 0) AttackCooldown -= 0.2f;
        }

        protected override void Attack()
        {
            if (VerticalBeamPrefab == null) return;

            // Spawns a tall vertical pillar of light on the player
            GameObject beam = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(VerticalBeamPrefab, transform.position, Quaternion.identity)
                : Instantiate(VerticalBeamPrefab, transform.position, Quaternion.identity);
            if (beam == null) return;
            beam.transform.SetParent(transform); // Moves with player
            
            var damageDealer = beam.GetComponent<TouchDamage>();
            if (damageDealer == null) damageDealer = beam.AddComponent<TouchDamage>();
            damageDealer.DamageAmount = DamageAmount;
            damageDealer.DamageInterval = 0.3f; // Hits multiple times
            damageDealer.SourceWeaponName = "Song Of Mana";
            
            var lifetime = beam.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(_beamDuration);
            else Destroy(beam, _beamDuration);
        }
    }
}
