using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Axe weapon umeed se uthta hai (Parabola arc) aur wapas neeche girti hai.
    /// Ye script simply ek Axe Projectile banati hai aur usay upar ki taraf dhakka (force) deti hai.
    /// </summary>
    public class AxeWeapon : MonoBehaviour
    {
        [Tooltip("Axe ki tasveer/mesh jisme Rigidbody laga ho")]
        [SerializeField] private GameObject _axePrefab;
        
        [Tooltip("Kitni der baad agla axe fainka jayega")]
        [SerializeField] private float _cooldown = 2f;

        [Tooltip("Hawa mein kitna upar aur aage jayega")]
        [SerializeField] private float _upwardForce = 15f;
        [SerializeField] private float _forwardForce = 5f;
        
        [SerializeField] private int _damage = 25;

        private float _timer;
        private SoulHunter.Gameplay.Player.PlayerStats _stats;

        private void Awake()
        {
            // Learning Comment:
            // Infinite Recursion Safeguard:
            // Agar ye script kisi spawned axe projectile par lag gayi ho, toh turant disable karein.
            if (transform.parent != null && transform.parent.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null)
            {
                enabled = false;
                return;
            }

            _stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                ThrowAxe();
                float currentCooldown = _stats != null ? _cooldown * _stats.Cooldown : _cooldown;
                _timer = currentCooldown;
            }
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

            // Axe banao O(1) performance ke sath
            GameObject axeObj = WeaponPoolManager.Instance.GetFromPool(_axePrefab, transform.position, Quaternion.identity);
            
            int actualDamage = _stats != null ? Mathf.RoundToInt(_damage * _stats.Might) : _damage;

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

                float sign = Mathf.Sign(transform.root.localScale.x);
                Vector3 throwDirection = new Vector3(sign * _forwardForce, _upwardForce, 0);
                
                rb.AddForce(throwDirection, ForceMode.VelocityChange);
                rb.AddTorque(new Vector3(0, 0, -sign * 10f), ForceMode.VelocityChange);
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
