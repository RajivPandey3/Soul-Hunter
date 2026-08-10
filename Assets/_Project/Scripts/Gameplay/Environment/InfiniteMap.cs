using UnityEngine;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors wali asali Infinite Map logic (Chunk Repositioning).
    /// Isme hum 3x3 (Total 9) zameen ke tukdo (chunks) ka ek grid banate hain. 
    /// Jab player aage badhta hai, toh jo zameen piche chhoot jati hai,
    /// game usko uthakar chup-chap player ke aage rakh deta hai. 
    /// Yahi exact trick Vampire Survivors mein anant map banane ke liye use hoti hai!
    /// </summary>
    public class InfiniteMap : MonoBehaviour
    {
        [Tooltip("Aapka ground plane ya tile prefab jisse zameen banegi")]
        [SerializeField] private GameObject _chunkPrefab;
        
        [Tooltip("Ek chunk ka size. (Unity ke default Plane ka size 10 hota hai)")]
        [SerializeField] private float _chunkSize = 10f;
        
        private Transform _player;
        private GameObject[,] _chunks = new GameObject[3, 3];
        private Vector2Int _currentCenterTile;

        private void Start()
        {
            // Player ko tag se dhundhna
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;

            // Shuru mein 3x3 ka grid (9 planes) paida (spawn) karein
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 spawnPos = new Vector3(x * _chunkSize, 0, z * _chunkSize);
                    _chunks[x + 1, z + 1] = Instantiate(_chunkPrefab, spawnPos, Quaternion.identity, transform);
                }
            }
        }

        private void Update()
        {
            if (_player == null) return;

            // Player ab kis tile/chunk me khada hai?
            int playerTileX = Mathf.RoundToInt(_player.position.x / _chunkSize);
            int playerTileZ = Mathf.RoundToInt(_player.position.z / _chunkSize);
            Vector2Int newCenterTile = new Vector2Int(playerTileX, playerTileZ);

            // Agar player purani tile se nikal kar nayi tile me gaya hai, toh zameen ko shift karo!
            if (newCenterTile != _currentCenterTile)
            {
                _currentCenterTile = newCenterTile;
                RepositionChunks();
            }
        }

        private void RepositionChunks()
        {
            // Piche chhoote hue planes ko utha kar aage laga do
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 targetPos = new Vector3((_currentCenterTile.x + x) * _chunkSize, 0, (_currentCenterTile.y + z) * _chunkSize);
                    _chunks[x + 1, z + 1].transform.position = targetPos;
                }
            }
        }
    }
}
