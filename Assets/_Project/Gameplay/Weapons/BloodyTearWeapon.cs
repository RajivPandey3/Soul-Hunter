using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class BloodyTearWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Slash;
        protected override float BaseCritChance => 0.1f; // VS wiki Critical_Hit: Bloody Tear 10%, 2x
        [Header("Bloody Tear Settings (Evolved Whip)")]
        public GameObject WhipVisualPrefab; 
        public float HealAmount = 8f; 
        private HealthController _playerHealth;

        // VS wiki (Bloody Tear): 40 damage, 1.35 s, 2 whips (+Amount). Evolutions do not level.
        public const float BaseDamage = 40f;
        public const float BaseCooldown = 1.35f;
        public const int BaseWhipCount = 2;

        protected override void Awake()
        {
            base.Awake();
            // Damage and cooldown live in the base fields so Shadow Kael's mirroring can scale them.
            DamageAmount = BaseDamage;
            AttackCooldown = BaseCooldown;
        }

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
            // Owner decision: aim at the nearest enemy; the second lash covers the opposite side.
            // Extra whips (Amount) alternate sides like the Whip.
            Vector3 aim = AimDirection(Vector3.right);
            int whips = BaseWhipCount + ExtraAmount;
            for (int i = 0; i < whips; i++) FireWhip(i % 2 == 0 ? aim : -aim);
        }

        private void FireWhip(Vector3 direction)
        {
            Vector3 spawnPosition = transform.position + direction * (2f * AreaMultiplier);
            GameObject whip = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(WhipVisualPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(WhipVisualPrefab, spawnPosition, Quaternion.identity, transform);
            if (whip == null) return;
            ApplyArea(whip, WhipVisualPrefab);
            // The whip's hitbox is long along local X; turn it to face the aim direction.
            whip.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
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
            
            damageDealer.DamageAmount = RollDamage(DamageAmount);
            damageDealer.SourceWeaponName = "Bloody Tear";
            damageDealer.SourceWeapon = this; // VS Results
            damageDealer.TargetTag = HostileTag; // TouchDamage defaults to "Player": the whip hurt its own holder
            
            // Critical hit & Heal logic simulation
            var allEnemies = EnemyController.ActiveEnemies;
            bool hitEnemy = false;
            // Shadow Kael's copy heals when it lashes the player, not when its own horde is near.
            if (HeldByEnemy) hitEnemy = FindNearestEnemy(3f * AreaMultiplier) != null;
            else foreach(var enemy in allEnemies)
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
