using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class CherryBombWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        public override bool HasChance => true;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;
        public GameObject BombPrefab; 
        public GameObject ExplosionPrefab; // What spawns when it hits
        
        // VS wiki (Cherry Bomb): 10 damage, 3.0 s cooldown, 1 bomb, 2.0 s duration. Levels: L2 Area +25%, Spd +30%;
        // L3 50% explode chance; L4 +1 bomb; L5 Area +25%, +5; L6 60% explode chance; L7 Area +25%, Spd +30%;
        // L8 Area +25%, +5. Area and Speed bonuses scale our own bomb size and throw speed (7).
        public const float VsBaseDamage = 10f;
        public const float VsCooldown = 3f;
        public const int VsAmount = 1;
        public const float VsDuration = 2f;
        public const float ThrowSpeed = 7f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 5 ? 5f : 0f) + (Level >= 8 ? 5f : 0f);
        public int BombCount => VsAmount + (Level >= 4 ? 1 : 0);
        public float LevelArea => 1f + 0.25f * ((Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        public float LevelSpeed => 1f + 0.3f * ((Level >= 2 ? 1 : 0) + (Level >= 7 ? 1 : 0));

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = VsCooldown;
        }

        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        public const float VsInterval = 0.3f;

        /// <summary>
        /// VS (wiki Cherry_Bomb): "Depending on its level and the Luck stat, there is a chance for the bomb to explode":
        /// 40% at level 1 (infobox), 50% from level 3, 60% from level 6, times Luck. Might and Area affect the
        /// explosion; the wiki gives no separate explosion damage, so it deals the bomb's damage.
        /// </summary>
        public float ExplodeChance => (Level >= 6 ? 0.6f : Level >= 3 ? 0.5f : 0.4f) + LimitBreak.ChanceBonus(this); // + Limit Break Chance
        public const float ExplosionSeconds = 0.4f;
        /// <summary>Explosions and fizzles so far (tests).</summary>
        public int Explosions { get; private set; }
        public int Fizzles { get; private set; }

        public static bool Explodes(float chance, float luck, float roll01) => roll01 < Mathf.Clamp01(chance * Mathf.Max(0f, luck));

        private void OnBombDone(Projectile bomb)
        {
            if (bomb == null || this == null || !isActiveAndEnabled || !bomb.gameObject.scene.isLoaded) return; // not on scene teardown
            float luck = OwnerStats != null ? OwnerStats.Luck : 1f;
            if (!Explodes(ExplodeChance, luck, Random.value)) { Fizzles++; return; }
            Explosions++;
            Vector3 at = bomb.transform.position;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, at);
            if (ExplosionPrefab == null) return;
            GameObject blast = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(ExplosionPrefab, at, Quaternion.identity)
                : Instantiate(ExplosionPrefab, at, Quaternion.identity);
            if (blast == null) return;
            blast.transform.localScale = ExplosionPrefab.transform.localScale * AreaMultiplier * LevelArea;
            var damage = blast.GetComponent<ProjectileDamage>();
            if (damage == null) damage = blast.AddComponent<ProjectileDamage>();
            damage.DamageAmount = ScaledDamage(BaseDamage);
            damage.SourceWeaponName = "Cherry Bomb";
            AimAtHostiles(blast);
            var lifetime = blast.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(ExplosionSeconds);
            else Destroy(blast, ExplosionSeconds);
        }

        protected override void Attack()
        {
            if (BombPrefab == null) return;

            // VS interval (wiki Cherry_Bomb): the Amount of shots leave 0.3s apart.
            FireInSuccession(BombCount + ExtraAmount, VsInterval, _bombShot ??= BombShot);
        }

        private SuccessionShot _bombShot;

        private void BombShot(int i, int count, Vector3 arg)
        {
            // Owner decision: aim at the nearest enemy; random when none. On the ground plane (a Vector2 here sent bombs vertically).
            Vector3 randomDir = Quaternion.Euler(0f, (i - (BombCount + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
            GameObject bomb = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, transform.position, Quaternion.identity)
                : Instantiate(BombPrefab, transform.position, Quaternion.identity);
            if (bomb == null) return;
            bomb.transform.localScale = BombPrefab.transform.localScale * AreaMultiplier * LevelArea;
            
            var proj = bomb.GetComponent<Projectile>();
            if (proj == null) proj = bomb.AddComponent<Projectile>();
            
            proj.Initialize(randomDir, ThrowSpeed * LevelSpeed * SpeedMultiplier, ScaledDamage(BaseDamage), VsDuration * DurationMultiplier);
            proj.AddBounces(ExtraBounces);
            // VS: only some bombs explode (the chance roll); the prefab's always-on explosion effect is off.
            var alwaysBoom = bomb.GetComponent<SoulHunter.Gameplay.VFX.SpawnVfxOnDisable>();
            if (alwaysBoom != null) alwaysBoom.enabled = false;
            proj.Deactivated += OnBombDone;
            
            var damageDealer = bomb.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Cherry Bomb";
            AimAtHostiles(bomb);
        }
    }
}
