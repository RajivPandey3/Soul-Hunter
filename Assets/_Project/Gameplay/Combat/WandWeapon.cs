using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ka Magic Wand weapon.
    /// Ye har X seconds mein sabse kareebi dushman (closest enemy) ko dhoondhta hai,
    /// aur uski taraf ek Projectile shoot karta hai.
    /// </summary>
    public class WandWeapon : MonoBehaviour
    {
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _fireRate = 1f;
        [SerializeField] private float _attackRange = 15f;
        [SerializeField] private LayerMask _enemyLayer;

        public float FireRate 
        { 
            get { return _fireRate; } 
            set { _fireRate = Mathf.Max(0.1f, value); } // Cap minimum fire rate to 0.1s
        }

        private float _timer;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                FireAtClosestEnemy();
                _timer = _fireRate;
            }
        }

        private void FireAtClosestEnemy()
        {
            if (_projectilePrefab == null) return;

            // Dushmano ko dhoondho range mein
            Collider[] hits = UnityEngine.Physics.OverlapSphere(transform.position, _attackRange, _enemyLayer);
            if (hits.Length == 0) return;

            // Sabse kareebi dushman nikalo
            Transform closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                float dist = (hit.transform.position - transform.position).sqrMagnitude;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = hit.transform;
                }
            }

            if (closestEnemy != null)
            {
                // Dushman ki taraf goli (projectile) spawn karo
                Vector3 direction = (closestEnemy.position - transform.position).normalized;
                direction.y = 0; // Ground parallel
                Quaternion rotation = Quaternion.LookRotation(direction);
                
                Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
                Instantiate(_projectilePrefab, spawnPos, rotation);
                
                Debug.Log("[WandWeapon] Fired projectile at enemy!");
            }
        }
    }
}
