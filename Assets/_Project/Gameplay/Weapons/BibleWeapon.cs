using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Bible weapon player ke gird (orbit) mein ghoomti hai.
    /// Ye continuously rotate karti hai, aur jo dushman beech mein aaye usey damage karti hai.
    /// </summary>
    public class BibleWeapon : MonoBehaviour
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

            for (int i = 0; i < _bibleCount; i++)
            {
                // Instantiate at our position initially
                GameObject newBible = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(_biblePrefab, transform.position, Quaternion.identity)
                    : Instantiate(_biblePrefab, transform.position, Quaternion.identity);
                if (newBible == null) continue;
                newBible.transform.SetParent(transform); // Child bana lo taaki apne aap clean ho jaye
                
                // Bible ke andar ek TouchDamage script laga dete hain taaki wo chhoote hi damage de (agar pehle se nahi lagi toh)
                var touchDamage = newBible.GetComponent<TouchDamage>();
                if (touchDamage == null)
                {
                    touchDamage = newBible.AddComponent<TouchDamage>();
                    touchDamage.DamageInterval = 0.5f; // Har aadhe second baad dobara damage de sakta hai
                }
                
                touchDamage.DamageAmount = _damage;
                touchDamage.TargetTag = "Enemy";
                touchDamage.SourceWeaponName = "Bible";

                _activeBibles.Add(newBible);
            }
            
            UpdateBiblePositions();
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

        private void Update()
        {
            if (_activeBibles.Count == 0) return;

            // Angle badhao
            _currentAngle += _rotationSpeed * Time.deltaTime;
            
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
                    float x = Mathf.Cos(angleInRadians) * _radius;
                    float z = Mathf.Sin(angleInRadians) * _radius;

                    // Position update karo relative to the player
                    _activeBibles[i].transform.localPosition = new Vector3(x, 0, z);

                    // Bible ko uski disha (direction) mein ghumao
                    _activeBibles[i].transform.localRotation = Quaternion.Euler(0, -angleInDegrees, 0);
                }
            }
        }
    }
}
