using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors style me dushman touch hone par lagatar (continuous) damage deta hai.
    /// Ye script Dushman (Enemy) par lagegi aur jab tak Player touch mein rahega, usko nuksaan pahunchayegi.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TouchDamage : MonoBehaviour
    {
        [SerializeField] private float _damageAmount = 5f;
        [SerializeField] private float _damageInterval = 0.5f; // Har 0.5 sec mein damage dega

        /// <summary>
        /// Learning Comment:
        /// DamageType batata hai ke damage physical hai ya elemental/holy.
        /// Spectral enemies (Level 7) Normal damage discard karte hain aur Holy/Magic/Spectral damage qubool karte hain.
        /// Default DamageType.Normal rakha gaya hai taake existing enemy touch damage affect na ho.
        /// </summary>
        [SerializeField] private DamageType _damageType = DamageType.Normal;

        public float DamageAmount { get => _damageAmount; set => _damageAmount = value; }
        public float DamageInterval { get => _damageInterval; set => _damageInterval = value; }
        public DamageType DamageType { get => _damageType; set => _damageType = value; }
        public string SourceWeaponName = "Unknown";
        public string TargetTag = "Player";

        private class ContactInfo
        {
            public Transform TargetTransform;
            public float Timer;
        }

        private Dictionary<IDamageable, ContactInfo> _targets = new Dictionary<IDamageable, ContactInfo>();
        private readonly List<IDamageable> _targetKeysSnapshot = new List<IDamageable>();

        private void OnEnable() { _targets.Clear(); _targetKeysSnapshot.Clear(); }

        private void OnCollisionEnter(Collision collision)
        {
            RegisterContact(collision.collider);
        }

        private void OnTriggerEnter(Collider other) => RegisterContact(other);

        private void RegisterContact(Collider other)
        {
            if (other == null || !other.CompareTag(TargetTag)) return;
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
            
            if (!_targets.ContainsKey(damageable))
            {
                _targets[damageable] = new ContactInfo { TargetTransform = other.transform, Timer = 0f };
            }
        }

        private void OnDisable() { _targets.Clear(); _targetKeysSnapshot.Clear(); } 
        
        private void OnCollisionExit(Collision collision)
        {
            ClearContact(collision.collider);
        }

        private void OnTriggerExit(Collider other) => ClearContact(other);

        private void ClearContact(Collider other)
        {
            if (other != null && other.CompareTag(TargetTag))
            {
                var damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null && _targets.ContainsKey(damageable))
                {
                    _targets.Remove(damageable);
                }
            }
        }

        private void Update()
        {
            if (_targets.Count == 0) return;

            // Learning Comment:
            // Dictionary ko direct foreach mein iterate karte waqt agar TakeDamage() ke natijay mein
            // target mar jaye ya OnTriggerExit/ClearContact call ho jaye,
            // toh "InvalidOperationException: Collection was modified" crash trigger hota hai.
            // Is liye pehle keys ka pre-allocated snapshot banate hain aur safe indexed for-loop chalate hain.
            _targetKeysSnapshot.Clear();
            foreach (var key in _targets.Keys)
            {
                _targetKeysSnapshot.Add(key);
            }

            for (int i = 0; i < _targetKeysSnapshot.Count; i++)
            {
                var target = _targetKeysSnapshot[i];

                // Agar pichle kisi target ke damage callback mein ye target remove ho chuka ho toh safe skip karein
                if (!_targets.TryGetValue(target, out var info)) continue;

                if (target == null || target.Equals(null) || info.TargetTransform == null)
                {
                    _targets.Remove(target);
                    continue;
                }

                info.Timer -= Time.deltaTime;
                if (info.Timer <= 0f)
                {
                    // Target ko damage do
                    // Learning Comment: _damageType pass kiya gaya hai taaki Level 7 spectral enemies Holy damage wagera sahi se receive karein
                    Vector3 knockback = (transform.position - info.TargetTransform.position).normalized;
                    target.TakeDamage(new DamagePacket(Mathf.RoundToInt(_damageAmount), transform.position, -knockback, _damageType));
                    
                    if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null && SourceWeaponName != "Unknown")
                    {
                        SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordDamage(SourceWeaponName, Mathf.RoundToInt(_damageAmount));
                    }
                    
                    // Learning Comment: TakeDamage() ke baad check karein ke target abhi bhi _targets mein hai ya OnTriggerExit se remove ho chuka hai
                    if (_targets.TryGetValue(target, out var stillActiveInfo))
                    {
                        stillActiveInfo.Timer = _damageInterval;
                    }
                }
            }
        }
    }
}
