using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Axe weapon umeed se uthta hai (Parabola arc) aur wapas neeche girti hai.
    /// Ye script simply ek Axe Projectile banati hai aur usay upar ki taraf dhakka (force) deti hai.
    /// Level table (1-8): L2/L5 ek aur axe, L3/L6/L8 damage, L4/L7 area.
    /// </summary>
    public class AxeWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [Tooltip("Axe ki tasveer/mesh jisme Rigidbody laga ho")]
        [SerializeField] private GameObject _axePrefab;

        [Tooltip("Kitni der baad agla axe fainka jayega")]
        [SerializeField] private float _cooldown = 2f;

        [Tooltip("Hawa mein kitna upar aur aage jayega")]
        [SerializeField] private float _upwardForce = 15f;
        [SerializeField] private float _forwardForce = 5f;

        [SerializeField] private int _damage = 25;

        public override int MaxLevel => 8;
        /// <summary>Axes per attack from levels (before the Amount stat).</summary>
        public int LevelAxeCount { get; private set; } = 1;
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;

        protected override void Awake()
        {
            // Learning Comment:
            // Infinite Recursion Safeguard:
            // Agar ye script kisi spawned axe projectile par lag gayi ho, toh turant disable karein.
            if (transform.parent != null && transform.parent.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null)
            {
                enabled = false;
                return;
            }

            base.Awake();
            // Damage and cooldown live in the base fields so levelling and
            // Shadow Kael's mirroring can scale them.
            DamageAmount = _damage;
            AttackCooldown = _cooldown;
        }

        public override void LevelUp()
        {
            if (CurrentLevel >= MaxLevel) return;
            CurrentLevel++;
            switch (CurrentLevel)
            {
                case 2:
                case 5: LevelAxeCount++; break;
                case 4:
                case 7: LevelAreaMultiplier += 0.2f; break;
                default: DamageAmount += 15f; break; // 3, 6, 8
            }
        }

        protected override void Attack()
        {
            ThrowAxe();
        }

        private void ThrowAxe()
        {
            if (_axePrefab == null || WeaponPoolManager.Instance == null) return;

            // Learning Comment:
            // Self-Reference Guard:
            if (_axePrefab == gameObject || _axePrefab.GetComponent<AxeWeapon>() != null)
            {
                Debug.LogError("[AxeWeapon] Infinite loop blocked! _axePrefab cannot be AxeWeapon itself.");
                return;
            }

            // VS rule: Amount throws extra axes, each a little further forward.
            int amount = LevelAxeCount + ExtraAmount;
            for (int i = 0; i < amount; i++) ThrowSingleAxe(1f + i * 0.35f);
        }

        private void ThrowSingleAxe(float forwardScale)
        {
            // Axe banao O(1) performance ke sath
            GameObject axeObj = WeaponPoolManager.Instance.GetFromPool(_axePrefab, transform.position, Quaternion.identity);
            if (axeObj == null) return;
            axeObj.transform.localScale = _axePrefab.transform.localScale * (AreaMultiplier * LevelAreaMultiplier);

            int actualDamage = Mathf.RoundToInt(RollDamage(DamageAmount));

            var projectileDamage = axeObj.GetComponent<ProjectileDamage>();
            if (projectileDamage == null)
            {
                projectileDamage = axeObj.AddComponent<ProjectileDamage>();
            }
            projectileDamage.DamageAmount = actualDamage;

            // Rigidbody ko dhakka do
            Rigidbody rb = axeObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Reset velocity kyunki ye pool se wapas aayi hai
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Owner decision: throw toward the nearest enemy (facing side when none), still in an upward arc.
                float sign = Mathf.Sign(transform.root.localScale.x);
                Vector3 aim = AimDirection(new Vector3(sign, 0f, 0f));
                Vector3 throwDirection = (aim * (_forwardForce * forwardScale) + Vector3.up * _upwardForce) * SpeedMultiplier;

                rb.AddForce(throwDirection, ForceMode.VelocityChange);
                rb.AddTorque(Vector3.Cross(aim, Vector3.up) * 10f, ForceMode.VelocityChange); // tumble end over end along the throw
            }

            // 5 second baad axe wapas pool mein chala jayega (Destroy nahi hoga)
            StartCoroutine(DisableAxeRoutine(axeObj));
        }

        private System.Collections.IEnumerator DisableAxeRoutine(GameObject axe)
        {
            yield return new WaitForSeconds(5f);
            if (axe != null && axe.activeSelf) axe.SetActive(false);
        }
    }
}
