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
        public override float LimitBreakBaseDuration => PoolDuration;
        public override float Knockback => 0f; // VS wiki Santa Water: knockback N/A (none)
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;
        [Header("Santa Water Settings")]
        public GameObject WaterZonePrefab; // Zone jo zameen pe fire/water damage dega
        public float DropRadius = 8f; // Kitne area me randomly bottle giregi
        
        // VS wiki (Santa Water): 10 damage, 4.5 s cooldown, 1 flask, 2.0 s pool. Levels: L2 +1 flask, Area +20%;
        // L3 Dur +0.5, +10; L4 +1 flask, Area +20%; L5 Dur +0.25, +10; L6 +1 flask, Area +20%; L7 Dur +0.25, +5;
        // L8 Area +20%, +5. Area bonuses scale our own pool size.
        public const float VsBaseDamage = 10f;
        public const float VsCooldown = 4.5f;
        public const int VsAmount = 1;
        public const float VsDuration = 2f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 3 ? 10f : 0f) + (Level >= 5 ? 10f : 0f) + (Level >= 7 ? 5f : 0f) + (Level >= 8 ? 5f : 0f);
        public int FlaskCount => VsAmount + (Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0);
        public float LevelArea => 1f + 0.2f * ((Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        public float PoolDuration => VsDuration + (Level >= 3 ? 0.5f : 0f) + (Level >= 5 ? 0.25f : 0f) + (Level >= 7 ? 0.25f : 0f);

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = VsCooldown;
        }

        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        public const float VsInterval = 0.3f;

        protected override void Attack()
        {
            if (WaterZonePrefab == null) return;

            // VS interval (wiki Santa_Water): the Amount of shots leave 0.3s apart.
            FireInSuccession(FlaskCount + ExtraAmount, VsInterval, _flaskShot ??= FlaskShot);
        }

        private SuccessionShot _flaskShot;

        private void FlaskShot(int i, int count, Vector3 arg)
        {
            // Random position near player
            // Owner decision: aim at the nearest enemy; drop on it (extra flasks land close by), random around the player when none.
            Transform nearest = FindNearestEnemy(DropRadius * 2f);
            Vector2 randomOffset = Random.insideUnitCircle * (nearest != null ? (i == 0 ? 0f : 1.5f) : DropRadius);
            Vector3 centre = nearest != null ? new Vector3(nearest.position.x, transform.position.y, nearest.position.z) : transform.position;
            Vector3 dropPosition = centre + new Vector3(randomOffset.x, 0f, randomOffset.y);
            
            SpawnWaterZone(dropPosition);
        }

        private void SpawnWaterZone(Vector3 position)
        {
            GameObject zone = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WaterZonePrefab, position, Quaternion.identity)
                : Instantiate(WaterZonePrefab, position, Quaternion.identity);
            if (zone == null) return;
            zone.transform.localScale = WaterZonePrefab.transform.localScale * AreaMultiplier * LevelArea;
            
            // TouchDamage laga denge taake zone me jo bhi aaye use lagatar damage ho
            var damageDealer = zone.GetComponent<TouchDamage>();
            if (damageDealer == null) damageDealer = zone.AddComponent<TouchDamage>();
            
            damageDealer.DamageAmount = ScaledDamage(BaseDamage);
            damageDealer.DamageInterval = 0.5f; // Har adhe second baad dubara damage
            damageDealer.SourceWeaponName = "Holy Water";
            // Learning Comment:
            // Santa Water ground zone Enemy tag ko target karta hai aur DamageType.Holy emit karta hai
            // taaki Level 7 ke spectral ghosts isse damage le sakein (GDD & AGENTS.md Section 5).
            damageDealer.TargetTag = HostileTag;
            damageDealer.DamageType = DamageType.Holy;
            AimAtHostiles(zone);
            
            // The pool stays for its VS duration, scaled by Duration.
            var lifetime = zone.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(PoolDuration * DurationMultiplier);
            else if (Application.isPlaying) Destroy(zone, PoolDuration * DurationMultiplier);
        }
    }
}
