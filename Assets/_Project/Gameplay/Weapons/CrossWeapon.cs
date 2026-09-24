using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Cross weapon (Boomerang) sabse nazdeek wale dushman ki taraf jata hai,
    /// thodi door aage jaa kar wapas ghoomta hai (boomerang effect), aur raste mein sabko katta hai.
    /// Is script ke do hisse hain: (1) Weapon Spawner, (2) Boomerang Projectile logic (jo aage chal kar hum projectile me shift kar sakte hain).
    /// </summary>
    public class CrossWeapon : MonoBehaviour
    {
        [Tooltip("Cross ka 3D model/prefab jisme Rigidbody aur TouchDamage laga ho")]
        [SerializeField] private GameObject _crossPrefab;
        
        [Tooltip("Kitni der baad agla cross fainka jayega")]
        [SerializeField] private float _cooldown = 3f;

        [Tooltip("Boomerang ki speed")]
        [SerializeField] private float _speed = 12f;

        [Tooltip("Cross ko palatne (wapas aane) mein kitna waqt lagega")]
        [SerializeField] private float _returnTime = 1f;

        [SerializeField] private int _damage = 20;

        private float _timer;
        private SoulHunter.Gameplay.Player.PlayerStats _stats;
        private Collider[] _hits = new Collider[50];
        private int _enemyLayerMask;

        private void Awake()
        {
            // Learning Comment:
            // Infinite Recursion Safeguard:
            // Agar ye script kisi spawned boomerang projectile par lag gayi ho, toh turant disable karein
            // taaki projectile ke andar se مزید weapons na paida hon.
            if (GetComponent<BoomerangProjectile>() != null || (transform.parent != null && transform.parent.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null))
            {
                enabled = false;
                return;
            }

            _stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                ThrowCross();
                float currentCooldown = _stats != null ? _cooldown * _stats.Cooldown : _cooldown;
                _timer = currentCooldown;
            }
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
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, 15f, _hits, _enemyLayerMask);
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

            Vector3 throwDirection = transform.root.localScale.x > 0 ? Vector3.right : Vector3.left;
            if (closestEnemy != null)
            {
                throwDirection = (closestEnemy.position - transform.position).normalized;
                throwDirection.y = 0;
            }

            // VS rule: Amount throws extra crosses fanned around the aim direction.
            int amount = 1 + (_stats != null ? Mathf.Max(0, _stats.Amount) : 0);
            for (int i = 0; i < amount; i++)
            {
                float spread = amount > 1 ? Mathf.Lerp(-15f, 15f, i / (float)(amount - 1)) : 0f;
                ThrowSingleCross(Quaternion.Euler(0f, spread, 0f) * throwDirection);
            }
        }

        private void ThrowSingleCross(Vector3 direction)
        {
            float area = _stats != null ? Mathf.Max(0.1f, _stats.Area) : 1f;
            float speed = _stats != null ? Mathf.Max(0.1f, _stats.ProjectileSpeed) : 1f;

            // Cross ko pool se nikalo
            GameObject crossObj = WeaponPoolManager.Instance.GetFromPool(_crossPrefab, transform.position, Quaternion.identity);
            if (crossObj == null) return;
            crossObj.transform.localScale = _crossPrefab.transform.localScale * area;

            // Cross par Damage set karo
            int actualDamage = _stats != null ? Mathf.RoundToInt(_damage * _stats.Might) : _damage;
            
            var projectileDamage = crossObj.GetComponent<ProjectileDamage>();
            if (projectileDamage == null)
            {
                projectileDamage = crossObj.AddComponent<ProjectileDamage>();
            }
            projectileDamage.DamageAmount = actualDamage;

            // Cross ko Boomerang ki tarah udao
            var boomerangLogic = crossObj.GetComponent<BoomerangProjectile>();
            if (boomerangLogic == null) boomerangLogic = crossObj.AddComponent<BoomerangProjectile>();
            
            boomerangLogic.Initialize(direction, _speed * speed, _returnTime);
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

        public void Initialize(Vector3 direction, float speed, float returnTime)
        {
            _direction = direction;
            _speed = speed;
            _returnTime = returnTime;
            _timer = 0f;
            _aliveTimer = 0f;
            
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
