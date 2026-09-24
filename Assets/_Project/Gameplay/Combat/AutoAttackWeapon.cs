using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors jaisa auto-attack system.
    /// Ye script ek timer maintain karti hai aur har X seconds ke baad DamageCaster ko trigger karti hai.
    /// Player par attach karne se player ke aas-paas automatically attack hoga.
    /// </summary>
    public class AutoAttackWeapon : MonoBehaviour
    {
        public int CurrentLevel = 1;
        public virtual int MaxLevel => int.MaxValue;
        public float DamageAmount = 10f;
        public float AttackCooldown = 1.5f;

        [System.NonSerialized] private float _timer;
        private DamageCaster _damageCaster;

        protected virtual void Awake()
        {
            _damageCaster = GetComponent<DamageCaster>();
        }

        protected virtual void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                Attack();
                _timer = AttackCooldown;
            }
        }

        public virtual void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 2f;
        }

        protected virtual void Attack()
        {
            if (_damageCaster != null)
            {
                _damageCaster.CastDamage();
            }
        }
    }
}
