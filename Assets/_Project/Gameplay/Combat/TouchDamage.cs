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
        /// <summary>The knockback of the weapon that spawned this (VS infobox "knockback"); negative = the default 1.</summary>
        [System.NonSerialized] public float Knockback = -1f;
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
        /// <summary>The weapon that spawned this (set by AutoAttackWeapon.AimAtHostiles), credited on the Results screen.</summary>
        [System.NonSerialized] public AutoAttackWeapon SourceWeapon;
        public string TargetTag = "Player";

        private class ContactInfo
        {
            public Transform TargetTransform;
            public float Timer;
            // CMB-03: the body's HealthController.Life when the contact began (null for targets without one).
            public HealthController Health;
            public int Life;
        }

        private Dictionary<IDamageable, ContactInfo> _targets = new Dictionary<IDamageable, ContactInfo>();
        private readonly List<IDamageable> _targetKeysSnapshot = new List<IDamageable>();

        private void OnEnable() { _targets.Clear(); _targetKeysSnapshot.Clear(); }

        // 60 FPS pass (2026-10-14): every enemy carries one of these, and about 300 Update calls a frame did nothing
        // (no contact). Only a TouchDamage that is touching something is ticked, by TouchDamageTicker.
        internal int TickIndex = -1;
        private int _tickedFrame = -1;

        private void OnCollisionEnter(Collision collision)
        {
            RegisterContact(collision.collider);
        }

        private void OnTriggerEnter(Collider other) => RegisterContact(other);

        private void RegisterContact(Collider other)
        {
            if (other == null || !other.CompareTag(TargetTag)) return;
            // Unity sends trigger messages to a switched-off component too; it deals no damage and keeps no contact.
            if (!enabled) return;
            var damageable = DamageableLookup.Of(other);
            if (damageable == null) return;
            
            var health = damageable as HealthController;
            if (!_targets.TryGetValue(damageable, out var known))
            {
                _targets[damageable] = new ContactInfo { TargetTransform = other.transform, Timer = 0f, Health = health, Life = health != null ? health.Life : 0 };
            }
            else if (known.Health != null && known.Life != known.Health.Life)
            {
                // The pool reused a body this zone still listed from its last life: a new contact.
                known.Life = known.Health.Life;
                known.Timer = 0f;
            }
            TouchDamageTicker.Add(this);
        }

        private void OnDisable() { _targets.Clear(); _targetKeysSnapshot.Clear(); TouchDamageTicker.Remove(this); } 
        
        private void OnCollisionExit(Collision collision)
        {
            ClearContact(collision.collider);
        }

        private void OnTriggerExit(Collider other) => ClearContact(other);

        private void ClearContact(Collider other)
        {
            if (other != null && other.CompareTag(TargetTag))
            {
                var damageable = DamageableLookup.Of(other);
                if (damageable != null && _targets.ContainsKey(damageable))
                {
                    _targets.Remove(damageable);
                }
            }
        }

        private SoulHunter.Gameplay.AI.EnemyController _owner;
        private bool _ownerLooked;

        /// <summary>One frame of contact damage. Called by TouchDamageTicker while something is being touched.</summary>
        internal void Tick()
        {
            if (_targets.Count == 0) { TouchDamageTicker.Remove(this); return; }
            // Once a frame, whatever the ticker's list does while targets die.
            if (Application.isPlaying)
            {
                int frame = Time.frameCount;
                if (_tickedFrame == frame) return;
                _tickedFrame = frame;
            }
            // VS Defang: a defanged enemy cannot deal damage to the player.
            if (!_ownerLooked) { _owner = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>(); _ownerLooked = true; }
            if (_owner != null && _owner.IsDefanged) return;

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
                    // CMB-03: a killed enemy is switched off without OnTriggerExit, and the pool may have reused it
                    // somewhere else since. Its life changed, so the contact is over (checked only when a hit is due).
                    if (info.Health != null && info.Life != info.Health.Life)
                    {
                        _targets.Remove(target);
                        continue;
                    }
                    // Target ko damage do
                    // Learning Comment: _damageType pass kiya gaya hai taaki Level 7 spectral enemies Holy damage wagera sahi se receive karein
                    Vector3 knockback = (transform.position - info.TargetTransform.position).normalized;
                    using (DamageSource.From(SourceWeapon, SourceWeaponName != "Unknown" ? SourceWeaponName : null))
                        target.TakeDamage(new DamagePacket(Mathf.RoundToInt(_damageAmount), transform.position, -knockback, _damageType).WithKnockback(Knockback));
                    
                    // Learning Comment: TakeDamage() ke baad check karein ke target abhi bhi _targets mein hai ya OnTriggerExit se remove ho chuka hai
                    if (_targets.TryGetValue(target, out var stillActiveInfo))
                    {
                        stillActiveInfo.Timer = _damageInterval;
                    }
                }
            }
        }
    }

    /// <summary>Ticks every TouchDamage that is touching something, once a frame (created on first use).</summary>
    public sealed class TouchDamageTicker : MonoBehaviour
    {
        private static readonly List<TouchDamage> Touching = new List<TouchDamage>();
        private static TouchDamageTicker _instance;

        /// <summary>TouchDamage components being ticked now (tests).</summary>
        public static int Count => Touching.Count;

        internal static void Add(TouchDamage touch)
        {
            // The stored index is only trusted when the list agrees: a pooled copy can carry its template's index.
            int listed = touch.TickIndex;
            if (listed >= 0 && listed < Touching.Count && ReferenceEquals(Touching[listed], touch)) return;
            touch.TickIndex = Touching.Count;
            Touching.Add(touch);
            if (_instance == null && Application.isPlaying)
            {
                var go = new GameObject("TouchDamageTicker") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<TouchDamageTicker>();
            }
        }

        internal static void Remove(TouchDamage touch)
        {
            int i = touch.TickIndex;
            touch.TickIndex = -1;
            if (i < 0 || i >= Touching.Count || !ReferenceEquals(Touching[i], touch)) return;
            int last = Touching.Count - 1;
            Touching[i] = Touching[last];
            Touching[i].TickIndex = i;
            Touching.RemoveAt(last);
            touch.TickIndex = -1;
        }

        private void Update()
        {
            for (int i = Touching.Count - 1; i >= 0; i--)
            {
                if (i >= Touching.Count) continue;
                var touch = Touching[i];
                if (touch == null)
                {
                    // Destroyed without OnDisable (never expected): swap-remove so the others keep their indices.
                    int last = Touching.Count - 1;
                    Touching[i] = Touching[last];
                    if (Touching[i] != null) Touching[i].TickIndex = i;
                    Touching.RemoveAt(last);
                    continue;
                }
                try { touch.Tick(); }
                catch (System.Exception e) { Debug.LogException(e, touch); }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so contacts of the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Touching.Clear();
            _instance = null;
        }
    }
}
