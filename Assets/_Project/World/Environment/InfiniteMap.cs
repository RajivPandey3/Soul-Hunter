using UnityEngine;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors wali asali Infinite Map logic (Chunk Repositioning).
    /// Isme hum 3x3 (Total 9) zameen ke tukdo (chunks) ka ek grid banate hain.
    /// Jab player aage badhta hai, toh jo zameen piche chhoot jati hai,
    /// game usko uthakar chup-chap player ke aage rakh deta hai.
    /// Yahi exact trick Vampire Survivors mein anant map banane ke liye use hoti hai!
    /// VS mein har stage ki apni zameen hoti hai: yahan har stage ka floor tile
    /// GameContentCatalog.StageFloors se aata hai aur stage badalte hi zameen badal jati hai.
    /// Har tile random 90-degree ghumaya jata hai taake repetition kam dikhe.
    /// </summary>
    public class InfiniteMap : MonoBehaviour
    {
        [Tooltip("Fallback ground tile when the catalog has no floor for the current stage")]
        [SerializeField] private GameObject _chunkPrefab;

        [Tooltip("Ek chunk ka size. Stage floors are 20x20, so this must stay 20 or tiles leave gaps.")]
        [SerializeField] private float _chunkSize = 20f;

        private Transform _player;
        private GameObject[,] _chunks = new GameObject[3, 3];
        private LevelProgressionManager _progression;

        public GameObject CurrentFloorPrefab { get; private set; }
        public float ChunkSize => _chunkSize;
        public GameObject ChunkAt(int x, int z) => _chunks[x, z];

        /// <summary>
        /// Assign the active player when spawned; pass null to pause map tracking.
        /// </summary>
        public void SetPlayer(Transform player)
        {
            _player = player;
        }

        private void Start()
        {
            SoulHunter.Core.Persistence.GameSettings.Changed += HandleSettingsChanged;
            _progression = FindFirstObjectByType<LevelProgressionManager>();
            if (_progression != null) _progression.OnStageStarted += HandleStageStarted;
            BuildChunks(FloorForStage(_progression != null ? _progression.CurrentStage : 1));
        }

        private void OnDestroy()
        {
            SoulHunter.Core.Persistence.GameSettings.Changed -= HandleSettingsChanged;
            if (_progression != null) _progression.OnStageStarted -= HandleStageStarted;
        }

        private void HandleStageStarted(int stage) => BuildChunks(FloorForStage(stage));

        /// <summary>The stage's floor tile from the content catalog, else the scene's fallback tile.</summary>
        public GameObject FloorForStage(int stage)
        {
            var catalog = GameContentCatalog.Load();
            var floors = catalog != null ? catalog.StageFloors : null;
            if (floors != null && stage >= 1 && stage <= floors.Length && floors[stage - 1] != null) return floors[stage - 1];
            return _chunkPrefab;
        }

        private void BuildChunks(GameObject floorPrefab)
        {
            if (floorPrefab == null)
            {
                Debug.LogWarning("[InfiniteMap] Chunk prefab is not assigned; map tiling is disabled.", this);
                return;
            }
            if (floorPrefab == CurrentFloorPrefab && _chunks[1, 1] != null) return;

            _originalMaterials.Clear(); // every tile is replaced below
            // Keep the grid where it is (around the player) and only swap the tiles.
            Vector3 center = _chunks[1, 1] != null ? _chunks[1, 1].transform.position : Vector3.zero;
            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    Vector3 spawnPos = _chunks[x, z] != null
                        ? _chunks[x, z].transform.position
                        : center + new Vector3((x - 1) * _chunkSize, 0, (z - 1) * _chunkSize);
                    if (_chunks[x, z] != null) Destroy(_chunks[x, z]);
                    GameObject newChunk = Instantiate(floorPrefab, spawnPos, RandomQuarterTurn(), transform);
                    newChunk.SetActive(true);
                    ApplyBloodSetting(newChunk);
                    _chunks[x, z] = newChunk;
                }
            }
            CurrentFloorPrefab = floorPrefab;
        }

        // VS "Disable blood": the Blood Arenas floor draws its stains with a *_Blood material; with the option on,
        // those parts use the floor's *_Base material instead (and switch back when it is turned off).
        private readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _originalMaterials =
            new System.Collections.Generic.Dictionary<Renderer, Material[]>();

        private void HandleSettingsChanged()
        {
            foreach (var chunk in _chunks) if (chunk != null) ApplyBloodSetting(chunk);
        }

        public void ApplyBloodSetting(GameObject chunk)
        {
            bool hide = SoulHunter.Core.Persistence.GameSettings.DisableBlood;
            foreach (var renderer in chunk.GetComponentsInChildren<Renderer>())
            {
                if (!_originalMaterials.TryGetValue(renderer, out var original))
                    _originalMaterials[renderer] = original = renderer.sharedMaterials;
                Material plain = System.Array.Find(original, m => m != null && m.name.EndsWith("_Base"));
                if (plain == null || !System.Array.Exists(original, m => m != null && m.name.EndsWith("_Blood"))) continue;
                var materials = (Material[])original.Clone();
                if (hide)
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] != null && materials[i].name.EndsWith("_Blood")) materials[i] = plain;
                renderer.sharedMaterials = materials;
            }
        }

        private static Quaternion RandomQuarterTurn() => Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

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

                    if (pos != chunk.position)
                    {
                        chunk.position = pos;
                        chunk.rotation = RandomQuarterTurn(); // a moved tile looks new
                    }
                }
            }
        }
    }
}
