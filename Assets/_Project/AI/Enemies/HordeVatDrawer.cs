using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// 60 FPS, horde on the GPU (owner, 2026-10-15: "sab kadam shuru karo"): every enemy body whose model is baked into a
    /// vertex animation texture (HordeVatAsset, HordeVatLibrary) is drawn here, all bodies of a model in one instanced
    /// draw, in place of a SkinnedMeshRenderer and an Animator each. The body's own renderer stays enabled but is not drawn
    /// (forceRenderingOff), so Unity neither skins it nor runs its Animator (CullCompletely), while every renderer check,
    /// the corpse bake and the hit flash keep working on it. The colour comes each frame from the material DamageFlash has
    /// put on the body (type tint, flash, Defang green, Death's Shadow shade), so that system is unchanged.
    /// Bodies that cast shadows (bosses, Death's Shadow) and bodies with no baked model keep drawing themselves.
    /// The horde today plays only Idle (no enemy prefab drives its Animator), so each body loops Idle at its own phase.
    /// </summary>
    public sealed class HordeVatDrawer : MonoBehaviour
    {
        /// <summary>False leaves every body to its own renderer (A/B measurement and tests).</summary>
        public static bool Enabled = true;

        /// <summary>Bodies drawn and draws issued in the last frame (tests and the bot).</summary>
        public static int LastBodies { get; private set; }
        /// <summary>Last frame: bodies whose own renderer this drawer switched off, and those of them left out as off view.
        /// On the owner's phone (2026-10-18) the horde was invisible with LastBodies 0, so both are counted.</summary>
        public static int LastHanded { get; private set; }
        public static int LastCulled { get; private set; }
        /// <summary>Last frame's ground view (x, z) used for culling, and whether culling was on.</summary>
        public static Vector2 LastViewMin, LastViewMax;
        public static bool LastCull;
        public static int LastDraws { get; private set; }

        private static HordeVatDrawer _instance;
        public static bool Exists => _instance != null;

        private const int ChunkSize = 511;
        private const float ViewMargin = 4f; // a 3x boss-sized body near the edge still shows

        private static readonly int TimeId = Shader.PropertyToID("_HordeVatTime");
        private static readonly int ClipId = Shader.PropertyToID("_VatClip");
        private static readonly int PlayId = Shader.PropertyToID("_VatPlay");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        /// <summary>What the drawer knows about one pooled enemy body (found once, kept for its reuses).</summary>
        private sealed class Body
        {
            public SkinnedMeshRenderer Renderer;
            public Transform Visual;
            public HordeVatAsset Asset;
            public float Phase;
            public bool Handed; // its own renderer is switched off because this drawer draws it
        }

        private sealed class Batch
        {
            public HordeVatAsset Asset;
            public Vector4 Clip;
            public int Layer;
            public int Count;
            public Matrix4x4[] Matrices = new Matrix4x4[64];
            public Vector4[] Play = new Vector4[64];
            public Vector4[] Colors = new Vector4[64];
            public float[] Tints = new float[64];
        }

        private readonly Dictionary<EnemyController, Body> _bodies = new Dictionary<EnemyController, Body>();
        private readonly Dictionary<HordeVatAsset, Batch> _batches = new Dictionary<HordeVatAsset, Batch>();
        private readonly List<Batch> _batchList = new List<Batch>();
        private readonly Dictionary<Material, Vector4> _colorOf = new Dictionary<Material, Vector4>();
        private readonly Dictionary<Material, float> _tintOf = new Dictionary<Material, float>();
        private readonly Matrix4x4[] _chunkMatrices = new Matrix4x4[ChunkSize];
        private readonly Vector4[] _chunkPlay = new Vector4[ChunkSize];
        private readonly Vector4[] _chunkColors = new Vector4[ChunkSize];
        private readonly float[] _chunkTints = new float[ChunkSize];
        private readonly float[] _chunkFlash = new float[ChunkSize];
        private readonly Vector4[] _chunkClips = new Vector4[ChunkSize];
        private MaterialPropertyBlock _props;
        private HordeVatLibrary _library;
        private bool _wasEnabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            // Made again after any scene load, since something (a test boot) may have cleared the persistent objects.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Create();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Create();
            RequestPrune(); // the old scene's bodies are gone
        }

        // Bug MIS AI-12: the drawer lives across scenes (DontDestroyOnLoad) and kept every body it had ever seen, and every
        // material it had read a colour from, after they were destroyed (scene changes, idle pools trimmed), so it grew over
        // long Endless sessions and repeated runs. Destroyed entries are forgotten, not every frame: after a scene load and
        // after EnemySpawner frees idle pooled bodies, one frame later (Destroy completes at the end of a frame).
        private static bool _pruneDue;
        private static int _pruneAfterFrame;
        private readonly List<EnemyController> _deadBodies = new List<EnemyController>();
        private readonly List<Material> _deadMaterials = new List<Material>();

        /// <summary>Forget destroyed bodies and materials on a later frame (scene loads, EnemySpawner.TrimIdlePools).</summary>
        public static void RequestPrune()
        {
            _pruneDue = true;
            _pruneAfterFrame = Time.frameCount;
        }

        /// <summary>Bodies the drawer remembers (tests).</summary>
        public static int KnownBodies => _instance != null ? _instance._bodies.Count : 0;

        /// <summary>Forgets every destroyed body and material now (also called by tests).</summary>
        public static void PruneNow()
        {
            _pruneDue = false;
            if (_instance != null) _instance.Prune();
        }

        private void Prune()
        {
            _deadBodies.Clear();
            foreach (var entry in _bodies)
                if (entry.Key == null || (entry.Value != null && (entry.Value.Renderer == null || entry.Value.Visual == null))) _deadBodies.Add(entry.Key);
            foreach (var key in _deadBodies) _bodies.Remove(key);
            _deadBodies.Clear();
            _deadMaterials.Clear();
            foreach (var key in _colorOf.Keys) if (key == null) _deadMaterials.Add(key);
            foreach (var key in _deadMaterials) _colorOf.Remove(key);
            _deadMaterials.Clear();
            foreach (var key in _tintOf.Keys) if (key == null) _deadMaterials.Add(key);
            foreach (var key in _deadMaterials) _tintOf.Remove(key);
            _deadMaterials.Clear();
        }

        private static void Create()
        {
            if (_instance != null || !Application.isPlaying || !SystemInfo.supportsInstancing) return;
            var library = HordeVatLibrary.Load();
            if (library == null || library.Assets.Length == 0) return;
            var go = new GameObject("HordeVatDrawer") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HordeVatDrawer>();
            _instance._library = library;
        }

        private void LateUpdate()
        {
            if (_pruneDue && Time.frameCount > _pruneAfterFrame) PruneNow();
            LastBodies = 0;
            LastDraws = 0;
            LastHanded = LastCulled = 0;
            var enemies = EnemyController.ActiveEnemies;
            if (!Enabled)
            {
                if (_wasEnabled) HandBackAll();
                _wasEnabled = false;
                return;
            }
            _wasEnabled = true;
            Shader.SetGlobalFloat(TimeId, Time.time); // game time: a pause holds the horde's pose, as the Animator did

            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            Vector2 min = default, max = default;
            bool cull = player != null && SoulHunter.Gameplay.VFX.PickupIdleMotionRunner.ViewOnGround(
                Camera.main, player.transform.position.y, out min, out max);
            if (cull) { min -= Vector2.one * ViewMargin; max += Vector2.one * ViewMargin; }
            LastCull = cull; LastViewMin = min; LastViewMax = max;

            foreach (var batch in _batchList) batch.Count = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                var body = BodyOf(enemy);
                if (body == null) continue;
                var r = body.Renderer;
                if (r == null || body.Visual == null) continue;
                if (r.shadowCastingMode != ShadowCastingMode.Off || !SoulHunter.Gameplay.VFX.HordeMaterials.IsHorde(r.sharedMaterial))
                {
                    // A boss, Death's Shadow or a body with another look draws itself, skinned, as before.
                    if (body.Handed) { r.forceRenderingOff = false; body.Handed = false; }
                    continue;
                }
                if (!body.Handed) { r.forceRenderingOff = true; body.Handed = true; }
                LastHanded++;
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (cull)
                {
                    Vector3 p = body.Visual.position;
                    if (p.x < min.x || p.x > max.x || p.z < min.y || p.z > max.y) { LastCulled++; continue; }
                }
                if (!_batches.TryGetValue(body.Asset, out var b))
                {
                    int idle = body.Asset.IndexOf("Idle");
                    b = new Batch { Asset = body.Asset, Clip = body.Asset.Clips[Mathf.Max(0, idle)].ShaderVector, Layer = r.gameObject.layer };
                    _batches.Add(body.Asset, b);
                    _batchList.Add(b);
                }
                if (b.Count == b.Matrices.Length) Grow(b);
                var material = r.sharedMaterial;
                b.Matrices[b.Count] = body.Visual.localToWorldMatrix;
                b.Play[b.Count] = new Vector4(0f, 1f, body.Phase, 0f);
                b.Colors[b.Count] = ColorOf(material);
                b.Tints[b.Count] = TintOf(material);
                b.Count++;
            }

            Vector3 centre = player != null ? player.transform.position : Vector3.zero;
            var bounds = cull
                ? new Bounds(new Vector3((min.x + max.x) * 0.5f, centre.y, (min.y + max.y) * 0.5f), new Vector3(max.x - min.x, 30f, max.y - min.y))
                : new Bounds(centre, new Vector3(2000f, 200f, 2000f));
            _props ??= new MaterialPropertyBlock();
            foreach (var b in _batchList)
            {
                if (b.Count == 0) continue;
                for (int start = 0; start < b.Count; start += ChunkSize)
                {
                    int count = Mathf.Min(ChunkSize, b.Count - start);
                    System.Array.Copy(b.Matrices, start, _chunkMatrices, 0, count);
                    System.Array.Copy(b.Play, start, _chunkPlay, 0, count);
                    System.Array.Copy(b.Colors, start, _chunkColors, 0, count);
                    System.Array.Copy(b.Tints, start, _chunkTints, 0, count);
                    for (int k = 0; k < count; k++) _chunkClips[k] = b.Clip;
                    _props.Clear();
                    _props.SetVectorArray(ClipId, _chunkClips);
                    _props.SetVectorArray(PlayId, _chunkPlay);
                    _props.SetVectorArray(ColorId, _chunkColors);
                    _props.SetFloatArray(TintAmountId, _chunkTints);
                    _props.SetFloatArray(FlashAmountId, _chunkFlash);
                    for (int s = 0; s < b.Asset.Materials.Length && s < b.Asset.Mesh.subMeshCount; s++)
                    {
                        var rp = new RenderParams(b.Asset.Materials[s])
                        {
                            worldBounds = bounds,
                            shadowCastingMode = ShadowCastingMode.Off,
                            receiveShadows = true,
                            layer = b.Layer, // the body's own layer, so the camera draws it as before
                            matProps = _props,
                        };
                        Graphics.RenderMeshInstanced(rp, b.Asset.Mesh, s, _chunkMatrices, count);
                        LastDraws++;
                    }
                }
                LastBodies += b.Count;
            }
        }

        private Body BodyOf(EnemyController enemy)
        {
            if (_bodies.TryGetValue(enemy, out var body)) return body;
            body = null;
            var visual = enemy.transform.Find("SH10_Visual");
            if (visual != null)
            {
                var skinned = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skinned.Length == 1)
                {
                    var asset = _library.For(skinned[0].sharedMesh);
                    if (asset != null)
                        body = new Body
                        {
                            Renderer = skinned[0], Visual = visual, Asset = asset,
                            Phase = (enemy.GetInstanceID() * 0.6180339f) % 1f,
                        };
                }
            }
            if (body != null && body.Phase < 0f) body.Phase += 1f;
            _bodies[enemy] = body; // null too: a body with no baked model is not looked up again
            return body;
        }

        private Vector4 ColorOf(Material material)
        {
            if (material == null) return Vector4.one;
            if (!_colorOf.TryGetValue(material, out var c)) { c = material.GetColor(ColorId); _colorOf[material] = c; }
            return c;
        }

        private float TintOf(Material material)
        {
            if (material == null) return 0f;
            if (!_tintOf.TryGetValue(material, out var t)) { t = material.GetFloat(TintAmountId); _tintOf[material] = t; }
            return t;
        }

        private static void Grow(Batch b)
        {
            int size = b.Matrices.Length * 2;
            System.Array.Resize(ref b.Matrices, size);
            System.Array.Resize(ref b.Play, size);
            System.Array.Resize(ref b.Colors, size);
            System.Array.Resize(ref b.Tints, size);
        }

        private void HandBackAll()
        {
            foreach (var body in _bodies.Values)
                if (body != null && body.Handed && body.Renderer != null) { body.Renderer.forceRenderingOff = false; body.Handed = false; }
        }

        private void OnDestroy()
        {
            HandBackAll();
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Enabled = true;
            _pruneDue = false;
            LastBodies = 0;
            LastDraws = 0;
        }
    }
}
