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
            if (_chunkPrefab == null)
            {
                Debug.LogWarning("[InfiniteMap] Chunk prefab is not assigned; map tiling is disabled.", this);
                return;
            }

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 spawnPos = new Vector3(x * _chunkSize, 0, z * _chunkSize);
                    GameObject newChunk = Instantiate(_chunkPrefab, spawnPos, Quaternion.identity, transform);
                    newChunk.SetActive(true);
                    _chunks[x + 1, z + 1] = newChunk;
                }
            }
        }

        private void Update()
        {
            if (_player == null) return;

            // Naya Smooth Treadmill Algorithm:
            // Sirf us chunk ko aage shift karo jo player se bohat peechay reh gaya ho.
            // Current tile/chunk jahan player khara hai, wo bilkul bhi move nahi hoga!
            RepositionChunks();
        }

        private void RepositionChunks()
        {
            float viewDist = _chunkSize * 1.5f;

            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    if (_chunks[x, z] == null) continue;
                    Transform chunk = _chunks[x, z].transform;
                    Vector3 pos = chunk.position;

                    // X-Axis check
                    if (_player.position.x - pos.x > viewDist)
                        pos.x += _chunkSize * 3;
                    else if (pos.x - _player.position.x > viewDist)
                        pos.x -= _chunkSize * 3;

                    // Z-Axis check
                    if (_player.position.z - pos.z > viewDist)
                        pos.z += _chunkSize * 3;
                    else if (pos.z - _player.position.z > viewDist)
                        pos.z -= _chunkSize * 3;

                    chunk.position = pos;
                }
            }
        }
    }
}
