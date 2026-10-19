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
            // One after another (VS interval 0.1 s, like the Whip): extra lashes used to land on top of the first ones.
            FireInSuccession(whips, VsInterval, _lashShot ??= LashShot, aim);
        }

        private SuccessionShot _lashShot;

        private void LashShot(int i, int count, Vector3 arg)
        {
            FireWhip(i % 2 == 0 ? arg : -arg);
        }

        /// <summary>vampire.survivors.wiki/w/Bloody_Tear: "interval = 0.1 seconds" between lashes.</summary>
        public const float VsInterval = 0.1f;

        /// <summary>
        /// VS (vampire.survivors.wiki/w/Bloody_Tear, raw, fetched 2026-10-19): "Restores 8 HP/attack if it hits any enemies
        /// with a crit"; "The player can heal 8 points of health if an enemy was damaged by a critical hit"; "This lifesteal
        /// effect has a one second delay before it can be triggered again"; "It is not affected by Recovery".
        /// </summary>
        public const float VsHealDelay = 1f;

        // Lashes whose damage roll was a critical hit; only their damaging hits heal.
        private readonly System.Collections.Generic.HashSet<TouchDamage> _critLashes = new System.Collections.Generic.HashSet<TouchDamage>();
        private System.Action<TouchDamage> _onLashDamaged;
        private float _nextHealAt = float.NegativeInfinity;

        private void OnLashDamaged(TouchDamage lash)
        {
            if (!_critLashes.Contains(lash)) return; // not a crit: no heal
            if (Time.time < _nextHealAt) return;      // the 1 s lifesteal delay (game time: a pause holds it)
            _nextHealAt = Time.time + VsHealDelay;
            if (_playerHealth != null) _playerHealth.Heal(Mathf.RoundToInt(HealAmount)); // 8, not scaled by Recovery
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
            bool crit = LastRollWasCrit;
            damageDealer.SourceWeaponName = "Bloody Tear";
            damageDealer.SourceWeapon = this; // VS Results
            damageDealer.TargetTag = HostileTag; // TouchDamage defaults to "Player": the whip hurt its own holder
            
            // CMB-08: the lash heals only when one of its hits really damages a body and its roll was a crit, at most once
            // a second (VS, see VsHealDelay). It healed when any enemy stood near the lash, so Level 7 ghosts (immune to
            // Normal damage) healed the player 8 per lash. Shadow Kael's copy heals the boss by the same rule.
            if (crit) _critLashes.Add(damageDealer); else _critLashes.Remove(damageDealer);
            damageDealer.DamagedTarget = _onLashDamaged ??= OnLashDamaged;

            var lifetime = whip.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(0.3f);
            else Destroy(whip, 0.3f);
        }
    }
}
