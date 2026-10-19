using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Hellmaw Comet: Soul Hunter's evolution of the Fire Wand (owner's name, 2026-10-01), built on VS Hellfire
    /// (vampire.survivors.wiki/w/Hellfire, fetched 2026-10-01): Fire Wand at max level + Spinach. 100 damage, 3.0 s
    /// cooldown, 2 meteors 0.2 s apart, speed 100% (the Fire Wand's base is 75%), infinite pierce, passes through walls.
    /// "Ignores Duration": the meteors' flight time is fixed. The wiki gives no Area change, so the meteors are drawn at
    /// twice the fireball's size (Soul Hunter's look). Evolutions do not level.
    /// </summary>
    public class HellmawCometWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;

        public GameObject MeteorPrefab;

        public const float VsDamage = 100f;
        public const float VsCooldown = 3f;
        public const int VsAmount = 2;
        public const float VsInterval = 0.2f;
        /// <summary>The Fire Wand's base speed (5) is VS 75%; Hellfire's 100% is 5 / 0.75.</summary>
        public const float MeteorSpeed = 5f / 0.75f;
        public const float FlightSeconds = 4f;
        public const float MeteorScale = 2f;
        public const int InfinitePierce = 9999;

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            if (MeteorPrefab == null) return;
            int count = VsAmount + ExtraAmount;
            FireInSuccession(count, VsInterval, _meteorShot ??= MeteorShot);
        }

        private SuccessionShot _meteorShot;

        private void MeteorShot(int i, int count, Vector3 arg)
        {
            Vector3 aim = AimDirection(RandomFlatDirection());
            Vector3 dir = Quaternion.Euler(0f, (i - (count - 1) * 0.5f) * 10f, 0f) * aim;
            GameObject meteor = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(MeteorPrefab, transform.position, Quaternion.identity)
                : Instantiate(MeteorPrefab, transform.position, Quaternion.identity);
            if (meteor == null) return;
            meteor.transform.localScale = MeteorPrefab.transform.localScale * MeteorScale * AreaMultiplier;
            var proj = meteor.GetComponent<Projectile>();
            if (proj == null) proj = meteor.AddComponent<Projectile>();
            proj.Initialize(dir, MeteorSpeed * SpeedMultiplier, RollDamage(DamageAmount), FlightSeconds, DamageType.Magic, "Hellmaw Comet", InfinitePierce);
            proj.ConfigureContacts(InfinitePierce, ExtraBounces, false); // passes through walls (VS)
            proj.AddScreenBounces(ArcanaScreenBounces); // Waltz of Pearls (XI): bounces off the screen edge once
            var damage = meteor.GetComponent<ProjectileDamage>();
            if (damage == null) damage = meteor.AddComponent<ProjectileDamage>();
            damage.SourceWeaponName = "Hellmaw Comet";
            AimAtHostiles(meteor);
        }
    }
}
