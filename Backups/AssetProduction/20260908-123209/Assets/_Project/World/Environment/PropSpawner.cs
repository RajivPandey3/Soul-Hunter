using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Prop Spawner
    /// Yeh script map par mashaalein (torches) randomly spawn karti hai taake player 
    /// inhe tor kar apni health maintain kar sake. Player se screen-edge ki doori par spawn hote hain.
    /// </summary>
    public class PropSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _destructiblePrefab;
        [SerializeField] private int _maxPropsInWorld = 15;
        [SerializeField] private float _spawnRadius = 25f; // Offscreen radius
        [SerializeField] private float _spawnInterval = 3f;

        private Transform _playerTransform;
        private float _timer;
        private List<GameObject> _activeProps = new List<GameObject>();
        private Stack<GameObject> _propPool = new Stack<GameObject>();

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void Update()
        {
            if (_playerTransform == null || _destructiblePrefab == null) return;

            // Cleanup inactive/destroyed props
            _activeProps.RemoveAll(p => p == null || !p.activeInHierarchy);

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _spawnInterval;
                
                if (_activeProps.Count < _maxPropsInWorld)
                {
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    Vector3 spawnPos = _playerTransform.position + new Vector3(randomDir.x, 0, randomDir.y) * _spawnRadius;
                    
                    GameObject prop = _propPool.Count > 0
                        ? _propPool.Pop()
                        : Instantiate(_destructiblePrefab, spawnPos, Quaternion.identity);
                    if (prop.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>() == null)
                    {
                        var autoReturn = prop.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                        autoReturn.TargetPool = _propPool;
                    }
                    prop.transform.position = spawnPos;
                    prop.SetActive(true);
                    _activeProps.Add(prop);
                }
            }
        }
    }
}
