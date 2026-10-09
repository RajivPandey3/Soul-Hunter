using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Cross weapon (Boomerang) sabse nazdeek wale dushman ki taraf jata hai,
    /// thodi door aage jaa kar wapas ghoomta hai (boomerang effect), aur raste mein sabko katta hai.
    /// Is script ke do hisse hain: (1) Weapon Spawner, (2) Boomerang Projectile logic (jo aage chal kar hum projectile me shift kar sakte hain).
    /// Level table (1-8): L2/L5/L8 damage, L3/L6 area + speed, L4/L7 ek aur cross.
    /// VS wiki (Cross): 5 damage, 2 s, 1 cross; L2/L5/L8 +10 damage; L3/L6 Area +10%, Speed +25%; L4/L7 +1 cross.
    /// VS wiki (Heaven Sword, uses this script): 77 damage, 3.3 s, 1 sword (HeavenSword.prefab).
    /// </summary>
    public class CrossWeapon : AutoAttackWeapon
    {
        // VS wiki: Cross knockback 1; Heaven Sword (this script, HeavenSword.prefab) 6.
        [SerializeField, Min(0f)] private float _knockback = 1f;
        public override float Knockback => _knockback;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Slash;
        // VS wiki Critical_Hit: the Cross cannot crit; Heaven Sword (this script, HeavenSword.prefab) 10%, 2.5x.
        [SerializeField, Range(0f, 1f)] private float _critChance = 0f;
        [SerializeField, Min(1f)] private float _critMultiplier = 2f;
        protected override float BaseCritChance => _critChance;
        protected override float WeaponCritMultiplier => _critMultiplier;
        [Tooltip("Cross ka 3D model/prefab jisme Rigidbody aur TouchDamage laga ho")]
        [SerializeField] private GameObject _crossPrefab;
        
        [Tooltip("Kitni der baad agla cross fainka jayega")]
        [SerializeField] private float _cooldown = 2f;

        [Tooltip("Boomerang ki speed")]
        [SerializeField] private float _speed = 12f;

        [Tooltip("Cross ko palatne (wapas aane) mein kitna waqt lagega")]
        [SerializeField] private float _returnTime = 1f;

        [SerializeField] private int _damage = 5;

        public override int MaxLevel => 8;
        /// <summary>Crosses per attack from levels (before the Amount stat).</summary>
        public int LevelCrossCount { get; private set; } = 1;
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;
        /// <summary>Speed bonus from levels (multiplies the projectile Speed stat).</summary>
        public float LevelSpeedMultiplier { get; private set; } = 1f;

        private Collider[] _hits = new Collider[50];
        private int _enemyLayerMask;

        protected override void Awake()
        {
            // Learning Comment:
            // Infinite Recursion Safeguard:
            // Agar ye script kisi spawned boomerang projectile par lag gayi ho, toh turant disable karein
            // taaki projectile ke andar se مزید weapons na paida hon.
            if (GetComponent<BoomerangProjectile>() != null || (transform.parent != null && transform.parent.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null &&
                                                                     transform.parent.GetComponent<SoulHunter.Gameplay.AI.EnemyController>() == null))
            {
                enabled = false;
                return;
            }

            base.Awake();
            _enemyLayerMask = LayerMask.GetMask("Enemy");
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
                case 3:
                case 6: LevelAreaMultiplier += 0.1f; LevelSpeedMultiplier += 0.25f; break;
                case 4:
                case 7: LevelCrossCount++; break;
                default: DamageAmount += 10f; break; // 2, 5, 8
            }
        }

        protected override void Attack()
        {
            ThrowCross();
        }

        private void ThrowCross()
        {
            if (_crossPrefab == null || WeaponPoolManager.Instance == null) return;

            // Learning Comment:
            // Self-Reference Guard:
            // Agar prefab mein galti se weapon khud ko hi projectile assign kar de,
            // to ye condition infinite instantiation aur Unity freeze ko 100% block kar deti hai.
            if (_crossPrefab == gameObject || _crossPrefab.GetComponent<CrossWeapon>() != null)
            {
                Debug.LogError("[CrossWeapon] Infinite loop blocked! _crossPrefab cannot be CrossWeapon itself.");
                return;
            }

            // Sabse nazdeek ka dushman dhoondho O(1) performance ke sath
            // Shadow Kael's copy aims at the player, not at its own horde.
            int count = HeldByEnemy ? 0 : UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, 15f, _hits, _enemyLayerMask);
            Transform closestEnemy = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null) continue;
                float dist = (_hits[i].transform.position - transform.position).sqrMagnitude;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = _hits[i].transform;
                }
            }

            if (HeldByEnemy) closestEnemy = FindNearestEnemy(15f);
            Vector3 throwDirection = HolderFacing;
            if (closestEnemy != null)
            {
                throwDirection = (closestEnemy.position - transform.position).normalized;
                throwDirection.y = 0;
            }

            // VS (wiki Cross): every cross flies at the closest enemy; the Amount of crosses leave 0.1 s apart (interval).
            // Soul Hunter used to fan them +/-15 degrees in one frame instead.
            FireInSuccession(LevelCrossCount + ExtraAmount, VsInterval, i => ThrowSingleCross(throwDirection));
        }

        public const float VsInterval = 0.1f;

        private void ThrowSingleCross(Vector3 direction)
        {
            float area = AreaMultiplier * LevelAreaMultiplier;
            float speed = SpeedMultiplier * LevelSpeedMultiplier;

            // Cross ko pool se nikalo
            GameObject crossObj = WeaponPoolManager.Instance.GetFromPool(_crossPrefab, transform.position, Quaternion.identity);
            if (crossObj == null) return;
            crossObj.transform.localScale = _crossPrefab.transform.localScale * area;

            // Cross par Damage set karo
            int actualDamage = Mathf.RoundToInt(RollDamage(DamageAmount));
            
            var projectileDamage = crossObj.GetComponent<ProjectileDamage>();
            if (projectileDamage == null)
            {
                projectileDamage = crossObj.AddComponent<ProjectileDamage>();
            }
            projectileDamage.DamageAmount = actualDamage;
            projectileDamage.DamageType = DamageType.Holy; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
            AimAtHostiles(crossObj);

            // Cross ko Boomerang ki tarah udao
            var boomerangLogic = crossObj.GetComponent<BoomerangProjectile>();
            if (boomerangLogic == null) boomerangLogic = crossObj.AddComponent<BoomerangProjectile>();
            
            // Waltz of Pearls (XI): the Cross and Heaven Sword bounce off the screen edges twice.
            boomerangLogic.Initialize(direction, _speed * speed, _returnTime, ArcanaScreenBounces);
        }
    }

    /// <summary>
    /// Boomerang logic: Seedha jata hai, phir rukh badal kar wapas ulta bhagta hai.
    /// </summary>
    public class BoomerangProjectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _timer;
        private float _returnTime;
        private float _aliveTimer;
        private int _screenBouncesLeft;

        /// <summary>Screen-edge bounces left (VS Waltz of Pearls; tests).</summary>
        public int ScreenBouncesLeft => _screenBouncesLeft;

        public void Initialize(Vector3 direction, float speed, float returnTime, int screenBounces = 0)
        {
            _direction = direction;
            _speed = speed;
            _returnTime = returnTime;
            _timer = 0f;
            _aliveTimer = 0f;
            _screenBouncesLeft = Mathf.Max(0, screenBounces);
            
            // Spinning effect
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Position manually handle karenge
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            _aliveTimer += Time.deltaTime;
            
            // Agar return time pura ho gaya toh disha (direction) ulti kardo
            if (_timer > _returnTime)
            {
                _direction = -_direction;
                _timer = -999f; // Ek hi baar ulta karna hai
            }

            transform.position += _direction * (_speed * Time.deltaTime);
            if (_screenBouncesLeft > 0 && ScreenEdge.Reflect(transform.position, ref _direction)) _screenBouncesLeft--;
            
            // Ghoomne (spinning) ka effect
            transform.Rotate(0, 1000f * Time.deltaTime, 0);

            // Pool mein wapas bhej do
            if (_aliveTimer > 5f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
