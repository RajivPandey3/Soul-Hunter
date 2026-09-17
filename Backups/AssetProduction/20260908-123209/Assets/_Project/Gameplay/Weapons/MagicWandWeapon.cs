using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// Magic Wand VS ki tarah sabse qareebi dushman (Closest Enemy) ko khud dhundh kar fire karta hai.
    /// Is mein OverlapSphereNonAlloc use hua hai taake har frame scan karne se GC spike na aaye.
    /// </summary>
    public class MagicWandWeapon : MonoBehaviour
    {
        [SerializeField] private GameObject _projectilePrefab; 
        [SerializeField] private float _cooldown = 1.2f;
        [SerializeField] private float _range = 15f;
        [SerializeField] private float _projectileSpeed = 20f;
        [SerializeField] private int _damage = 15;

        private float _timer;
        private SoulHunter.Gameplay.Player.PlayerStats _stats;
        private Collider[] _hits = new Collider[50];
        private int _enemyLayerMask;

        private void Awake()
        {
            _stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                FireWand();
                float currentCooldown = _stats != null ? _cooldown * _stats.Cooldown : _cooldown;
                _timer = currentCooldown;
            }
        }

        private void FireWand()
        {
            Transform closestEnemy = FindClosestEnemy();
            if (closestEnemy == null || _projectilePrefab == null) return; 

            GameObject wandObj = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(_projectilePrefab, transform.position, Quaternion.identity)
                : Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            if (wandObj == null) return;

            // REVERTED to 3D Rigidbody (Hybrid System)
            var rb = wandObj.GetComponent<Rigidbody>();
            Vector3 direction = (closestEnemy.position - transform.position).normalized;
            direction.y = 0;
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = direction * _projectileSpeed;
                wandObj.transform.forward = direction;
            }

            var projectile = wandObj.GetComponent<Projectile>();
            if (projectile == null) projectile = wandObj.AddComponent<Projectile>();
            projectile.Initialize(direction, _projectileSpeed, _damage, 3f);

            int actualDamage = _stats != null ? Mathf.RoundToInt(_damage * _stats.Might) : _damage;
            var damageComponent = wandObj.GetComponent<ProjectileDamage>();
            if (damageComponent != null) damageComponent.DamageAmount = actualDamage;

        }
        private Transform FindClosestEnemy()
        {
            // REVERTED to 3D Physics (Hybrid System)
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _range, _hits, _enemyLayerMask);
            if (count == 0) return null;

            Transform closest = null;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null) continue;

                float sqrDist = (_hits[i].transform.position - transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = _hits[i].transform;
                }
            }
            return closest;
        }

        private System.Collections.IEnumerator DisableProjectileRoutine(GameObject obj)
        {
            yield return new WaitForSeconds(3f);
            if (obj != null && obj.activeSelf) obj.SetActive(false);
        }
    }
}
