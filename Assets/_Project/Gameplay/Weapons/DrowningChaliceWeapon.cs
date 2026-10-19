using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Drowning Chalice: Soul Hunter's evolution of Santa Water (owner's name, 2026-10-01), built on VS La Borra
    /// (vampire.survivors.wiki/w/La_Borra, fetched 2026-10-01): Santa Water at max level + Attractorb. 40 damage, 4 pools,
    /// Area 200%, 4.0 s Duration, 4.0 s cooldown, 0.3 s interval, no knockback. Flasks fall "in a clockwise, roughly
    /// circular pattern" around the player; the pools "slowly creep towards the character, increase in size as they
    /// travel, and have a maximum size limit"; without Speed bonuses they "still travel slowly but they do not grow".
    /// Creep speed, growth rate, size limit and ring radius are Soul Hunter's numbers (the wiki gives none).
    /// </summary>
    public class DrowningChaliceWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        public override float Knockback => 0f; // VS wiki La Borra: knockback none
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;

        public GameObject WaterZonePrefab;

        public const float VsDamage = 40f;
        public const int VsAmount = 4;
        public const float VsArea = 2f;
        public const float VsDuration = 4f;
        public const float VsCooldown = 4f;
        public const float VsInterval = 0.3f;
        public const float RingRadius = 4f;
        public const float CreepSpeed = 0.8f;      // units per second toward the player
        public const float GrowthPerSecond = 0.5f; // per 100% Speed bonus
        public const float MaxGrowth = 2f;         // at most twice the dropped size

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        private float _angle;

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            if (WaterZonePrefab == null) return;
            int count = VsAmount + ExtraAmount;
            FireInSuccession(count, VsInterval * CooldownMultiplier, _poolShot ??= PoolShot);
        }

        private SuccessionShot _poolShot;

        private void PoolShot(int i, int count, Vector3 arg)
        {
            _angle -= 360f / Mathf.Max(1, count); // clockwise seen from above
            Vector3 around = Quaternion.Euler(0f, _angle, 0f) * Vector3.forward * RingRadius;
            SpawnPool(transform.position + around);
        }

        private void SpawnPool(Vector3 position)
        {
            GameObject zone = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WaterZonePrefab, position, Quaternion.identity)
                : Instantiate(WaterZonePrefab, position, Quaternion.identity);
            if (zone == null) return;
            Vector3 dropped = WaterZonePrefab.transform.localScale * AreaMultiplier * VsArea;
            zone.transform.localScale = dropped;

            var damage = zone.GetComponent<TouchDamage>();
            if (damage == null) damage = zone.AddComponent<TouchDamage>();
            damage.DamageAmount = ScaledDamage(VsDamage);
            damage.DamageInterval = 0.5f;
            damage.SourceWeaponName = "Drowning Chalice";
            damage.TargetTag = HostileTag;
            damage.DamageType = DamageType.Holy; // Santa Water's family is Holy (owner, Level 7)
            AimAtHostiles(zone);

            var creep = zone.GetComponent<PoolCreep>();
            if (creep == null) creep = zone.AddComponent<PoolCreep>();
            float speedBonus = Mathf.Max(0f, SpeedMultiplier - 1f);
            creep.Begin(transform, CreepSpeed * Mathf.Max(1f, SpeedMultiplier), GrowthPerSecond * speedBonus, dropped, MaxGrowth);

            float seconds = VsDuration * DurationMultiplier;
            var lifetime = zone.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(seconds);
            else if (Application.isPlaying) Destroy(zone, seconds);
        }
    }
}
