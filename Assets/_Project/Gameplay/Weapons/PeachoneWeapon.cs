using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.Combat
{
    public class PeachoneWeapon : AutoAttackWeapon
    {
        protected override bool ShowsAttackPose => false; // an orbit/aura/area weapon: no swing toward enemies

        public GameObject BirdVisual; // Bird circling the player
        public GameObject BombPrefab; // The projectile dropped
        public float CircleRadius = 3f;
        
        private float _angle = 0f;

        // VS wiki (Peachone): 10 damage, 1.0 s cooldown, 4 bombs. Levels: +1 bomb every level; L2 Area +40%; L3 +10;
        // L4 CD -0.3; L5 Area +40%; L6 +10; L7 CD -0.3; L8 Area +40%. Area bonuses scale our own blast size.
        public const float VsBaseDamage = 10f;
        public const float VsCooldown = 1f;
        public const int VsAmount = 4;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 3 ? 10f : 0f) + (Level >= 6 ? 10f : 0f);
        public int BombCount => VsAmount + (Level - 1);
        public float LevelArea => 1f + 0.4f * ((Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        public float CooldownForLevel => VsCooldown - (Level >= 4 ? 0.3f : 0f) - (Level >= 7 ? 0.3f : 0f);

        public const float BirdHeight = 1.6f;

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = CooldownForLevel;
            // BirdVisual was never assigned, so Attack() returned at once and Peachone never dropped a bomb.
            // The prefab now names a bird prefab; the weapon flies its own instance of it.
            if (BirdVisual != null && !BirdVisual.scene.IsValid())
                BirdVisual = Instantiate(BirdVisual, transform.position, Quaternion.identity, transform);
        }

        protected override void Update()
        {
            base.Update();
            if (BirdVisual != null)
            {
                // Circle around the player on the ground plane (it used to circle vertically, in X-Y).
                _angle += Time.deltaTime * 50f;
                float rad = _angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * CircleRadius;
                BirdVisual.transform.position = transform.position + offset + Vector3.up * BirdHeight;
                BirdVisual.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)));
            }
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            AttackCooldown = CooldownForLevel;
        }

        protected override void Attack()
        {
            if (BombPrefab == null || BirdVisual == null) return;
            StartCoroutine(BombingRun());
        }

        /// <summary>VS interval (wiki Peachone): 0.025 s between bombs (it was 0.1 s).</summary>
        public const float VsInterval = 0.025f;

        private IEnumerator BombingRun()
        {
            for (int i = 0; i < BombCount + ExtraAmount; i++)
            {
                Vector3 dropPos = BirdVisual.transform.position - Vector3.up * BirdHeight; // blast on the ground under the bird
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, dropPos, Quaternion.identity)
                    : Instantiate(BombPrefab, dropPos, Quaternion.identity);
                if (bomb == null) continue;
                bomb.transform.localScale = BombPrefab.transform.localScale * AreaMultiplier * LevelArea;
                
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(BaseDamage);
                damageDealer.DamageType = DamageType.Magic; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
                damageDealer.SourceWeaponName = "Peachone";
                AimAtHostiles(bomb);
                
                var lifetime = bomb.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(1.5f);
                else Destroy(bomb, 1.5f);
                yield return new WaitForSeconds(VsInterval);
            }
        }
    }
}
