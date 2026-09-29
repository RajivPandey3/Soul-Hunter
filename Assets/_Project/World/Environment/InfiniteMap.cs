using UnityEngine;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Environment
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors wali asali Infinite Map logic (Chunk Repositioning).
    /// Isme hum 5x5 (Total 25) zameen ke tukdo (chunks) ka ek grid banate hain.
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
        /// <summary>
        /// Tiles per side. Owner report (2026-09-30): "ground late se spawn ho raha hai". A 3 x 3 grid of 20-unit tiles
        /// only reaches 20 units past the hero near a tile edge, while the 55-degree camera shows about 21 units to each
        /// side at 16:9 (more in a wider window), so the screen edge showed bare background until the far tile jumped
        /// over. 5 x 5 reaches at least 40 units in every direction.
        /// </summary>
        public const int GridSize = 5;
        private const int Mid = GridSize / 2;

        private GameObject[,] _chunks = new GameObject[GridSize, GridSize];
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
            if (floorPrefab == CurrentFloorPrefab && _chunks[Mid, Mid] != null) return;

            _originalMaterials.Clear(); // every tile is replaced below
            // Keep the grid where it is (around the player) and only swap the tiles.
            Vector3 center = _chunks[Mid, Mid] != null ? _chunks[Mid, Mid].transform.position : Vector3.zero;
            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    Vector3 spawnPos = _chunks[x, z] != null
                        ? _chunks[x, z].transform.position
                        : center + new Vector3((x - Mid) * _chunkSize, 0, (z - Mid) * _chunkSize);
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
            // A tile more than half the grid away from the hero wraps to the other side.
            float viewDist = _chunkSize * GridSize * 0.5f;
            float wrap = _chunkSize * GridSize;

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    if (_chunks[x, z] == null) continue;
                    Transform chunk = _chunks[x, z].transform;
                    Vector3 pos = chunk.position;

                    // X-Axis check
                    if (_player.position.x - pos.x > viewDist)
                        pos.x += wrap;
                    else if (pos.x - _player.position.x > viewDist)
                        pos.x -= wrap;

                    // Z-Axis check
                    if (_player.position.z - pos.z > viewDist)
                        pos.z += wrap;
                    else if (pos.z - _player.position.z > viewDist)
                        pos.z -= wrap;

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
