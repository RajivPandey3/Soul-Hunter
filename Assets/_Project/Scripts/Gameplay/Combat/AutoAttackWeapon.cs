using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors jaisa auto-attack system.
    /// Ye script ek timer maintain karti hai aur har X seconds ke baad DamageCaster ko trigger karti hai.
    /// Player par attach karne se player ke aas-paas automatically attack hoga.
    /// </summary>
    [RequireComponent(typeof(DamageCaster))]
    public class AutoAttackWeapon : MonoBehaviour
    {
        [SerializeField] private float _attackInterval = 1.5f;
        private float _timer;
        private DamageCaster _damageCaster;

        private void Awake()
        {
            _damageCaster = GetComponent<DamageCaster>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                Attack();
                _timer = _attackInterval;
            }
        }

        private void Attack()
        {
            if (_damageCaster != null)
            {
                _damageCaster.CastDamage();
                // Future: Yahan par attack animation ya sound trigger kar sakte hain
                Debug.Log("[AutoAttackWeapon] Auto Attack Fired!");
            }
        }
    }
}
