using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Bich bich mein achanak Elites (glowing enemies) aur Chamgadaron (Bats) ke jhund aate hain.
    /// Ye Director har kuch waqt baad ek random event (Bat Swarm ya Elite Enemy) trigger karta hai.
    /// </summary>
    public class GameEventDirector : MonoBehaviour
    {
        [SerializeField] private GameObject _batPrefab; // The enemy to use for swarms
        [SerializeField] private GameObject _elitePrefab; // Elite enemy

        private float _eventTimer = 0f;
        private float _eventInterval = 45f; // Har 45 seconds baad ek event hoga

        private Transform _player;
        private float _nextPlayerLookupTime;
        private readonly Dictionary<GameObject, Stack<GameObject>> _eventPools = new Dictionary<GameObject, Stack<GameObject>>();

        private void Start()
        {
            var p = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            if (p != null) _player = p.transform;
            _nextPlayerLookupTime = Time.time + 0.25f;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _eventTimer = 0f;
        }

        private void Update()
        {
            // Match EnemySpawner's throttled lookup for late-spawned players.
            if (_player == null && Time.time >= _nextPlayerLookupTime)
            {
                var p = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
                if (p != null) _player = p.transform;
                _nextPlayerLookupTime = Time.time + 0.25f;
            }
            if (_player == null) return;

            _eventTimer += Time.deltaTime;
            if (_eventTimer >= _eventInterval)
            {
                _eventTimer = 0f;
                TriggerRandomEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            int roll = Random.Range(0, 2);
            if (roll == 0)
            {
                Debug.Log("[GameEventDirector] EVENT: Elite Enemy Spawned!");
                SpawnElite();
            }
            else
            {
                Debug.Log("[GameEventDirector] EVENT: Bat Swarm!");
                StartCoroutine(SpawnBatSwarmCoroutine());
            }
        }

        private void SpawnElite()
        {
            if (_elitePrefab == null) return;

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = _player.position + new Vector3(randomDir.x * 25f, 0f, randomDir.y * 25f);
            
            GameObject elite = SpawnPooled(_elitePrefab, spawnPos);
            if (elite == null) return;
            elite.transform.localScale = Vector3.one * 1.5f; // Thora bada
            
            var health = elite.GetComponent<HealthController>();
            if (health != null) health.Initialize(500); // Mota health

            var drop = elite.GetComponent<EnemyDrop>();
            if (drop == null) drop = elite.AddComponent<EnemyDrop>();
            drop.DropsChest = true; // Elite hamesha chest girayega!
        }

        private IEnumerator SpawnBatSwarmCoroutine()
        {
            if (_batPrefab == null) yield break;

            // Decide direction: Left to Right, or Right to Left
            bool leftToRight = Random.value > 0.5f;
            Vector3 startOffset = leftToRight ? new Vector3(-30f, 0, 0) : new Vector3(30f, 0, 0);
            Vector3 flyDirection = leftToRight ? Vector3.right : Vector3.left;

            // Spawn 20 bats in a line/wave
            for (int i = 0; i < 20; i++)
            {
                Vector3 spawnPos = _player.position + startOffset;
                // Add some vertical spread
                spawnPos.z += Random.Range(-10f, 10f);

                GameObject bat = SpawnPooled(_batPrefab, spawnPos);
                if (bat == null) continue;
                var controller = bat.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.StartFlyingMode(flyDirection);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private GameObject SpawnPooled(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;
            if (!_eventPools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                _eventPools[prefab] = pool;
            }

            GameObject instance = pool.Count > 0 ? pool.Pop() : Instantiate(prefab, position, Quaternion.identity);
            if (instance.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>() == null)
            {
                var autoReturn = instance.AddComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>();
                autoReturn.TargetPool = pool;
            }
            instance.transform.position = position;
            var controller = instance.GetComponent<EnemyController>();
            if (controller != null) controller.Target = _player;
            instance.SetActive(true);
            return instance;
        }
    }
}
