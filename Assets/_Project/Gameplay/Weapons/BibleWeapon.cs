using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Bible weapon player ke gird (orbit) mein ghoomti hai.
    /// Ye continuously rotate karti hai, aur jo dushman beech mein aaye usey damage karti hai.
    /// Level table (1-8): L2/L5/L8 ek aur bible, L3/L6 area + speed, L4/L7 damage.
    /// </summary>
    public class BibleWeapon : AutoAttackWeapon
    {
        [Tooltip("Bible ka asli chhota 3D model (prefab) yahan assign karein")]
        [SerializeField] private GameObject _biblePrefab;

        [Tooltip("Kitni bibles ghoomengi")]
        [SerializeField] private int _bibleCount = 2;

        [Tooltip("Player se kitni door ghoomengi (Orbit Radius)")]
        [SerializeField] private float _radius = 3f;

        [Tooltip("Kitni tezi se ghoomengi")]
        [SerializeField] private float _rotationSpeed = 180f;

        [SerializeField] private int _damage = 8;

        private float _currentAngle = 0f;
        private List<GameObject> _activeBibles = new List<GameObject>();

        public override int MaxLevel => 8;
        /// <summary>Extra bibles from levels (before the Amount stat).</summary>
        public int LevelBookCount { get; private set; }
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;
        /// <summary>Spin-speed bonus from levels (multiplies the projectile Speed stat).</summary>
        public float LevelSpeedMultiplier { get; private set; } = 1f;

        // VS rule: Amount adds bibles, Area widens the orbit and book size,
        // projectile Speed spins faster and Might raises damage.
        private int _spawnedCount;
        private float _spawnedDamage;
        private float _spawnedArea;
        private float _statsCheckTimer;

        private int DesiredCount => _bibleCount + LevelBookCount + ExtraAmount;
        private float BibleArea => AreaMultiplier * LevelAreaMultiplier;
        private float BibleSpeed => SpeedMultiplier * LevelSpeedMultiplier;
        private float BibleDamage => ScaledDamage(DamageAmount);

        protected override void Awake()
        {
            base.Awake();
            // Damage lives in the base field so levelling and Shadow Kael's
            // mirroring can scale it.
            DamageAmount = _damage;
        }

        public override void LevelUp()
        {
            if (CurrentLevel >= MaxLevel) return;
            CurrentLevel++;
            switch (CurrentLevel)
            {
                case 3:
                case 6: LevelAreaMultiplier += 0.25f; LevelSpeedMultiplier += 0.3f; break;
                case 4:
                case 7: DamageAmount += 6f; break;
                default: LevelBookCount++; break; // 2, 5, 8
            }
        }

        private void OnEnable()
        {
            SpawnBibles();
        }

        private void OnDisable()
        {
            ClearBibles();
        }

        private void SpawnBibles()
        {
            if (_biblePrefab == null) return;

            ClearBibles(); // Purani bibles delete karo

            _spawnedCount = DesiredCount;
            _spawnedDamage = BibleDamage;
            _spawnedArea = BibleArea;
            for (int i = 0; i < _spawnedCount; i++)
            {
                // Instantiate at our position initially
                GameObject newBible = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(_biblePrefab, transform.position, Quaternion.identity)
                    : Instantiate(_biblePrefab, transform.position, Quaternion.identity);
                if (newBible == null) continue;
                newBible.transform.SetParent(transform); // Child bana lo taaki apne aap clean ho jaye
                SetWorldScale(newBible.transform, _biblePrefab.transform.localScale * _spawnedArea);
                
                // Bible ke andar ek TouchDamage script laga dete hain taaki wo chhoote hi damage de (agar pehle se nahi lagi toh)
                var touchDamage = newBible.GetComponent<TouchDamage>();
                if (touchDamage == null)
                {
                    touchDamage = newBible.AddComponent<TouchDamage>();
                    touchDamage.DamageInterval = 0.5f; // Har aadhe second baad dobara damage de sakta hai
                }
                
                touchDamage.DamageAmount = _spawnedDamage;
                touchDamage.TargetTag = HostileTag;
                touchDamage.SourceWeaponName = "Bible";

                _activeBibles.Add(newBible);
            }
            
            UpdateBiblePositions();
        }

        // Keep the book's world size independent of the player's (possibly flipped) scale,
        // matching the previous world-scale-preserving SetParent behaviour.
        private void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = transform.lossyScale;
            target.localScale = new Vector3(
                Mathf.Approximately(parent.x, 0f) ? worldScale.x : worldScale.x / parent.x,
                Mathf.Approximately(parent.y, 0f) ? worldScale.y : worldScale.y / parent.y,
                Mathf.Approximately(parent.z, 0f) ? worldScale.z : worldScale.z / parent.z);
        }

        private void ClearBibles()
        {
            foreach (var bible in _activeBibles)
            {
                if (bible != null)
                {
                    bible.transform.SetParent(null);
                    bible.SetActive(false);
                }
            }
            _activeBibles.Clear();
        }

        // Bibles orbit continuously, so this replaces the base timed attack.
        protected override void Update()
        {
            if (_activeBibles.Count == 0) return;

            // Stats rarely change, so only re-check a few times per second.
            _statsCheckTimer -= Time.deltaTime;
            if (_statsCheckTimer <= 0f)
            {
                _statsCheckTimer = 0.25f;
                if (DesiredCount != _spawnedCount ||
                    !Mathf.Approximately(BibleDamage, _spawnedDamage) ||
                    !Mathf.Approximately(BibleArea, _spawnedArea))
                {
                    SpawnBibles();
                    if (_activeBibles.Count == 0) return;
                }
            }

            // Angle badhao
            _currentAngle += _rotationSpeed * BibleSpeed * Time.deltaTime;
            
            // Bibles ki position update karo (Trigonometry: Sin/Cos)
            UpdateBiblePositions();
        }

        private void UpdateBiblePositions()
        {
            float angleStep = 360f / _activeBibles.Count;

            for (int i = 0; i < _activeBibles.Count; i++)
            {
                if (_activeBibles[i] != null)
                {
                    float angleInDegrees = _currentAngle + (i * angleStep);
                    float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

                    // X aur Z par circle banayenge (top-down view)
                    float x = Mathf.Cos(angleInRadians) * _radius * _spawnedArea;
                    float z = Mathf.Sin(angleInRadians) * _radius * _spawnedArea;

                    // Position update karo relative to the player
                    _activeBibles[i].transform.localPosition = new Vector3(x, 0, z);

                    // Bible ko uski disha (direction) mein ghumao
                    _activeBibles[i].transform.localRotation = Quaternion.Euler(0, -angleInDegrees, 0);
                }
            }
        }
    }
}
