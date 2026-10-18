using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Marks an enemy bullet (HazardBullets) for EnemyBulletDrawer: its core mesh and its halo particle system. Put on the
    /// bullet template, so every pooled copy has it with references to its own children.
    /// </summary>
    public sealed class EnemyBulletLook : MonoBehaviour
    {
        [SerializeField] internal MeshRenderer Core;
        [SerializeField] internal GameObject Halo;

        internal static readonly List<EnemyBulletLook> Active = new List<EnemyBulletLook>();
        /// <summary>Enemy bullets in flight now, drawn by the drawer or not (the bot's A/B).</summary>
        public static int ActiveCount => Active.Count;
        private int _index = -1;
        internal bool Handed;
        // Phone profile (2026-10-18): about 10 engine calls per bullet per frame (GetComponent, sharedMaterial, transforms)
        // cost the drawer 1.2 ms at Level 5's ~500 bullets. They never change for a pooled bullet, so the drawer keeps them here.
        internal EnemyBulletDrawer CachedBy;
        internal EnemyBulletDrawer.Batch CoreBatch, HaloBatch;
        internal Transform CoreTransform, HaloTransform;

        internal void Setup(MeshRenderer core, GameObject halo) { Core = core; Halo = halo; }

        private void OnEnable()
        {
            if (!(_index >= 0 && _index < Active.Count && Active[_index] == this)) { _index = Active.Count; Active.Add(this); }
            EnemyBulletDrawer.Ensure();
        }

        private void OnDisable()
        {
            int i = _index;
            _index = -1;
            if (i < 0 || i >= Active.Count || Active[i] != this) { Active.Remove(this); return; }
            int last = Active.Count - 1;
            Active[i] = Active[last];
            if (Active[i] != null) Active[i]._index = i;
            Active.RemoveAt(last);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();
    }

    /// <summary>
    /// Draw calls (owner, 2026-10-15: "goliyon par kaam karo"): Level 5's Lost Heads keep about 550 enemy bullets in the
    /// air, and each drew twice (its core mesh and its halo particle system), about 1,100 draw calls a frame. Every enemy
    /// bullet on screen is now drawn here: all cores in one instanced draw, all halos as one soft camera-facing quad each in
    /// a second one. The bullet's core renderer stays enabled but is not drawn (forceRenderingOff) and its halo particle
    /// system is switched off, so flight, hits and pooling are unchanged. The halo quad stands in for the 2-3 overlapping
    /// particles the system kept alive (20 a second, 0.12 s each): same size, colour and texture, without the short smear
    /// behind a moving bullet. Kept only if an A/B in one run shows a gain (gems, 2026-10-15, did not).
    /// </summary>
    public sealed class EnemyBulletDrawer : MonoBehaviour
    {
        /// <summary>False leaves every bullet to its own renderers (A/B measurement and tests).</summary>
        public static bool Enabled = true;
        /// <summary>Measurement only (AutoplayBot -sh-autoplay-bulletlook-ab): 2 draws cores and halos, 1 cores only, 0 nothing.</summary>
        public static int DrawLevel = 2;

        /// <summary>Bullets drawn and draws issued in the last frame (tests and the bot).</summary>
        public static int LastBullets { get; private set; }
        public static int LastDraws { get; private set; }

        /// <summary>The halo particles' size and start colour (SH10_Proj_MagicBolt "Halo"): 0.9 units, (0.45, 0.95, 1).</summary>
        public const float HaloSize = 0.9f;
        public static readonly Color HaloColour = new Color(0.45f, 0.95f, 1f, 0.9f);

        private static EnemyBulletDrawer _instance;
        public static bool Exists => _instance != null;

        private const int ChunkSize = 1023;
        private const float ViewMargin = 2f;

        internal sealed class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public int Layer;
            public int Count;
            public Matrix4x4[] Matrices = new Matrix4x4[64];
        }

        private readonly Dictionary<Material, Batch> _cores = new Dictionary<Material, Batch>();
        private readonly Dictionary<Material, Batch> _halos = new Dictionary<Material, Batch>();
        private readonly List<Batch> _batchList = new List<Batch>();
        private readonly HashSet<EnemyBulletLook> _handed = new HashSet<EnemyBulletLook>();
        private readonly Matrix4x4[] _chunk = new Matrix4x4[ChunkSize];
        private Mesh _quad;
        private bool _wasEnabled = true;

        internal static void Ensure()
        {
            if (_instance != null || !Application.isPlaying || !SystemInfo.supportsInstancing) return;
            var go = new GameObject("EnemyBulletDrawer") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnemyBulletDrawer>();
        }

        private void LateUpdate()
        {
            LastBullets = 0;
            LastDraws = 0;
            if (!Enabled)
            {
                if (_wasEnabled) HandBackAll();
                _wasEnabled = false;
                return;
            }
            _wasEnabled = true;

            var cam = Camera.main;
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            Vector2 min = default, max = default;
            bool cull = player != null && SoulHunter.Gameplay.VFX.PickupIdleMotionRunner.ViewOnGround(
                cam, player.transform.position.y, out min, out max);
            if (cull) { min -= Vector2.one * ViewMargin; max += Vector2.one * ViewMargin; }
            Quaternion facing = cam != null ? cam.transform.rotation : Quaternion.identity;
            var haloScale = Vector3.one * HaloSize;

            foreach (var b in _batchList) b.Count = 0;
            var bullets = EnemyBulletLook.Active;
            for (int i = 0; i < bullets.Count; i++)
            {
                var look = bullets[i];
                if (look == null) continue;
                if (!ReferenceEquals(look.CachedBy, this))
                {
                    if (look.Core == null) continue;
                    look.CoreBatch = CoreBatch(look);
                    look.HaloBatch = HaloBatch(look);
                    look.CoreTransform = look.Core.transform;
                    look.HaloTransform = look.Halo != null ? look.Halo.transform : null;
                    look.CachedBy = this;
                }
                var coreBatch = look.CoreBatch;
                if (coreBatch.Mesh == null) continue; // nothing to draw it with: it keeps drawing itself
                if (!look.Handed) Hand(look, true);
                Matrix4x4 m = look.CoreTransform.localToWorldMatrix;
                if (cull && (m.m03 < min.x || m.m03 > max.x || m.m23 < min.y || m.m23 > max.y)) continue;
                if (DrawLevel >= 1) Add(coreBatch, m);
                var halo = look.HaloBatch;
                if (halo != null && DrawLevel >= 2) Add(halo, Matrix4x4.TRS(look.HaloTransform.position, facing, haloScale));
                LastBullets++;
            }

            Vector3 centre = player != null ? player.transform.position : Vector3.zero;
            var bounds = cull
                ? new Bounds(new Vector3((min.x + max.x) * 0.5f, centre.y, (min.y + max.y) * 0.5f), new Vector3(max.x - min.x, 30f, max.y - min.y))
                : new Bounds(centre, new Vector3(2000f, 200f, 2000f));
            foreach (var b in _batchList)
            {
                if (b.Count == 0) continue;
                var rp = new RenderParams(b.Material)
                {
                    worldBounds = bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    layer = b.Layer,
                };
                for (int start = 0; start < b.Count; start += ChunkSize)
                {
                    int count = Mathf.Min(ChunkSize, b.Count - start);
                    System.Array.Copy(b.Matrices, start, _chunk, 0, count);
                    Graphics.RenderMeshInstanced(rp, b.Mesh, 0, _chunk, count);
                    LastDraws++;
                }
            }
        }

        private static void Add(Batch b, Matrix4x4 m)
        {
            if (b.Count == b.Matrices.Length) System.Array.Resize(ref b.Matrices, b.Matrices.Length * 2);
            b.Matrices[b.Count++] = m;
        }

        private Batch CoreBatch(EnemyBulletLook look)
        {
            var material = look.Core.sharedMaterial;
            if (_cores.TryGetValue(material, out var b)) return b;
            material.enableInstancing = true; // the bullets' tinted runtime copy (HazardBullets.Template), shared by every bullet
            var filter = look.Core.GetComponent<MeshFilter>();
            b = new Batch { Mesh = filter != null ? filter.sharedMesh : null, Material = material, Layer = look.Core.gameObject.layer };
            _cores.Add(material, b);
            _batchList.Add(b);
            return b;
        }

        private Batch HaloBatch(EnemyBulletLook look)
        {
            if (look.Halo == null) return null;
            var particles = look.Halo.GetComponent<ParticleSystemRenderer>();
            var source = particles != null ? particles.sharedMaterial : null;
            if (source == null) return null;
            if (_halos.TryGetValue(source, out var b)) return b;
            // The particle colour (start colour x the tinted material colour) moves into one material copy for all halos.
            var material = new Material(source) { name = source.name + " (bullet halo)", enableInstancing = true };
            if (material.HasProperty("_Color")) material.SetColor("_Color", source.GetColor("_Color") * HaloColour);
            b = new Batch { Mesh = Quad(), Material = material, Layer = look.Halo.layer };
            _halos.Add(source, b);
            _batchList.Add(b);
            return b;
        }

        private Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "BulletHaloQuad" };
            _quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) });
            _quad.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            _quad.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            _quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            _quad.RecalculateBounds();
            return _quad;
        }

        private void Hand(EnemyBulletLook look, bool toDrawer)
        {
            if (look.Core != null) look.Core.forceRenderingOff = toDrawer;
            if (look.Halo != null) look.Halo.SetActive(!toDrawer);
            look.Handed = toDrawer;
            if (toDrawer) _handed.Add(look);
        }

        private void HandBackAll()
        {
            foreach (var look in _handed)
                if (look != null && look.Handed) Hand(look, false);
            _handed.Clear();
        }

        private void OnDestroy()
        {
            HandBackAll();
            foreach (var b in _halos.Values) if (b.Material != null) Destroy(b.Material);
            if (_quad != null) Destroy(_quad);
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Enabled = true;
            DrawLevel = 2;
            LastBullets = 0;
            LastDraws = 0;
        }
    }
}
