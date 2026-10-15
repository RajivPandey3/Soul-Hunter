using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Draw calls (2026-10-15): a Level 5 render census counted 23-25 damage numbers on screen, each its own TextMeshPro
    /// renderer and so its own draw (the TMP shader is not SRP Batcher compatible). Every number on screen is now drawn
    /// here in one draw: when a number gets its text (DamagePopup.Setup) its TextMeshPro mesh is generated once and copied,
    /// and each frame the copies are placed by their popup's transform (which still rises and faces the camera as before)
    /// into one shared mesh, with the same font material. The popup's own renderer stays enabled but is not drawn
    /// (forceRenderingOff). The fade is applied to the copy's vertex colours, so it is smooth and costs no TextMeshPro
    /// rebuild (it was shown in 10 steps to save those rebuilds). A number whose text needs more than one material (a
    /// fallback font) keeps drawing itself. Kept only if an A/B in one run shows a gain (gems, 2026-10-15, did not).
    /// </summary>
    public sealed class DamagePopupBatcher : MonoBehaviour
    {
        /// <summary>False leaves every number to its own renderer (A/B measurement and tests).</summary>
        public static bool Enabled = true;

        /// <summary>Numbers drawn and draws issued in the last frame (tests and the bot).</summary>
        public static int LastPopups { get; private set; }
        public static int LastDraws { get; private set; }

        private static readonly List<DamagePopup> Active = new List<DamagePopup>();
        private static DamagePopupBatcher _instance;
        public static bool Exists => _instance != null;

        private sealed class Copy
        {
            public Material Material;
            public int Layer;
            public int Count;
            public Vector3[] Vertices = new Vector3[0];
            public Vector3[] Normals = new Vector3[0];
            public Vector4[] Uv0 = new Vector4[0];
            public Vector2[] Uv1 = new Vector2[0];
            public Color32[] Colors = new Color32[0];
        }

        private sealed class Batch
        {
            public Material Material;
            public int Layer;
            public Mesh Mesh;
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector4> Uv0 = new List<Vector4>();
            public readonly List<Vector2> Uv1 = new List<Vector2>();
            public readonly List<Color32> Colors = new List<Color32>();
            public readonly List<int> Triangles = new List<int>();
            public bool Drew;
        }

        private readonly Dictionary<DamagePopup, Copy> _copies = new Dictionary<DamagePopup, Copy>();
        private readonly Dictionary<Material, Batch> _batches = new Dictionary<Material, Batch>();
        private readonly List<Batch> _batchList = new List<Batch>();
        private bool _wasEnabled = true;

        // Read buffers for one popup's mesh.
        private static readonly List<Vector3> ReadVertices = new List<Vector3>();
        private static readonly List<Vector3> ReadNormals = new List<Vector3>();
        private static readonly List<Vector4> ReadUv0 = new List<Vector4>();
        private static readonly List<Vector2> ReadUv1 = new List<Vector2>();
        private static readonly List<Color32> ReadColors = new List<Color32>();

        internal static void Register(DamagePopup popup)
        {
            if (!Active.Contains(popup)) Active.Add(popup);
            Ensure();
        }

        internal static void Unregister(DamagePopup popup)
        {
            Active.Remove(popup);
            if (_instance != null) _instance.Release(popup);
        }

        /// <summary>Copies a number's freshly set text (from DamagePopup.Setup).</summary>
        internal static void Capture(DamagePopup popup)
        {
            Ensure();
            if (_instance == null || !Enabled) return;
            _instance.CaptureInternal(popup);
        }

        private static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("DamagePopupBatcher") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DamagePopupBatcher>();
        }

        private void CaptureInternal(DamagePopup popup)
        {
            var text = popup != null ? popup.Text : null;
            if (text == null) return;
            text.ForceMeshUpdate(); // the text and size Setup just gave it, generated now rather than before the camera renders
            var info = text.textInfo;
            var mesh = text.mesh;
            var material = text.fontSharedMaterial;
            if (info == null || mesh == null || material == null || info.materialCount > 1 || info.meshInfo == null || info.meshInfo.Length == 0)
            {
                Release(popup); // draws itself
                return;
            }

            int count = Mathf.Min(info.meshInfo[0].vertexCount, mesh.vertexCount);
            mesh.GetVertices(ReadVertices);
            mesh.GetNormals(ReadNormals);
            mesh.GetUVs(0, ReadUv0);
            mesh.GetUVs(1, ReadUv1);
            mesh.GetColors(ReadColors);
            if (ReadUv0.Count < count || ReadColors.Count < count) { Release(popup); return; }

            if (!_copies.TryGetValue(popup, out var copy)) _copies[popup] = copy = new Copy();
            copy.Material = material;
            copy.Layer = text.gameObject.layer;
            copy.Count = count - count % 4; // whole glyph quads
            if (copy.Vertices.Length < copy.Count)
            {
                int size = Mathf.NextPowerOfTwo(copy.Count);
                copy.Vertices = new Vector3[size];
                copy.Normals = new Vector3[size];
                copy.Uv0 = new Vector4[size];
                copy.Uv1 = new Vector2[size];
                copy.Colors = new Color32[size];
            }
            for (int i = 0; i < copy.Count; i++)
            {
                copy.Vertices[i] = ReadVertices[i];
                copy.Normals[i] = i < ReadNormals.Count ? ReadNormals[i] : Vector3.back;
                copy.Uv0[i] = ReadUv0[i];
                copy.Uv1[i] = i < ReadUv1.Count ? ReadUv1[i] : Vector2.zero;
                var colour = ReadColors[i];
                colour.a = 255; // the fade is applied per frame from DamagePopup.Alpha
                copy.Colors[i] = colour;
            }

            if (!popup.Batched)
            {
                popup.Batched = true;
                var renderer = text.renderer;
                if (renderer != null) renderer.forceRenderingOff = true;
            }
        }

        private void Release(DamagePopup popup)
        {
            if (popup == null) return;
            _copies.Remove(popup);
            if (!popup.Batched) return;
            popup.Batched = false;
            var text = popup.Text;
            if (text != null && text.renderer != null) text.renderer.forceRenderingOff = false;
        }

        private void LateUpdate()
        {
            LastPopups = 0;
            LastDraws = 0;
            if (!Enabled)
            {
                if (_wasEnabled) HandBackAll();
                _wasEnabled = false;
                return;
            }
            if (!_wasEnabled)
            {
                // Switched on again: numbers already in the air join at their next Setup; take the ones up now too.
                for (int i = 0; i < Active.Count; i++) if (Active[i] != null && !Active[i].Batched) CaptureInternal(Active[i]);
            }
            _wasEnabled = true;

            foreach (var b in _batchList)
            {
                b.Vertices.Clear(); b.Normals.Clear(); b.Uv0.Clear(); b.Uv1.Clear(); b.Colors.Clear(); b.Triangles.Clear();
                b.Drew = false;
            }

            for (int p = 0; p < Active.Count; p++)
            {
                var popup = Active[p];
                if (popup == null || !popup.Batched || !_copies.TryGetValue(popup, out var copy) || copy.Count == 0) continue;
                var text = popup.Text;
                if (text == null || !text.enabled) continue;
                var batch = BatchFor(copy.Material, copy.Layer);
                Matrix4x4 m = text.transform.localToWorldMatrix;
                float alpha = popup.Alpha;
                int start = batch.Vertices.Count;
                for (int i = 0; i < copy.Count; i++)
                {
                    batch.Vertices.Add(m.MultiplyPoint3x4(copy.Vertices[i]));
                    batch.Normals.Add(m.MultiplyVector(copy.Normals[i]).normalized);
                    batch.Uv0.Add(copy.Uv0[i]);
                    batch.Uv1.Add(copy.Uv1[i]);
                    var c = copy.Colors[i];
                    c.a = (byte)Mathf.RoundToInt(c.a * alpha);
                    batch.Colors.Add(c);
                }
                // TextMeshPro's glyph quads: bottom-left, top-left, top-right, bottom-right.
                for (int q = start; q < start + copy.Count; q += 4)
                {
                    batch.Triangles.Add(q); batch.Triangles.Add(q + 1); batch.Triangles.Add(q + 2);
                    batch.Triangles.Add(q + 2); batch.Triangles.Add(q + 3); batch.Triangles.Add(q);
                }
                LastPopups++;
            }

            foreach (var b in _batchList)
            {
                if (b.Vertices.Count == 0) continue;
                var mesh = b.Mesh;
                mesh.Clear(false);
                mesh.indexFormat = b.Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(b.Vertices);
                mesh.SetNormals(b.Normals);
                mesh.SetUVs(0, b.Uv0);
                mesh.SetUVs(1, b.Uv1);
                mesh.SetColors(b.Colors);
                mesh.SetTriangles(b.Triangles, 0, true);
                var rp = new RenderParams(b.Material)
                {
                    worldBounds = mesh.bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    layer = b.Layer,
                };
                Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.identity);
                b.Drew = true;
                LastDraws++;
            }
        }

        private Batch BatchFor(Material material, int layer)
        {
            if (_batches.TryGetValue(material, out var b)) return b;
            var mesh = new Mesh { name = "DamageNumbers (batched)" };
            mesh.MarkDynamic();
            b = new Batch { Material = material, Layer = layer, Mesh = mesh };
            _batches.Add(material, b);
            _batchList.Add(b);
            return b;
        }

        private void HandBackAll()
        {
            for (int i = 0; i < Active.Count; i++) Release(Active[i]);
            _copies.Clear();
        }

        private void OnDestroy()
        {
            HandBackAll();
            foreach (var b in _batchList) if (b.Mesh != null) Destroy(b.Mesh);
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Enabled = true;
            LastPopups = 0;
            LastDraws = 0;
            Active.Clear();
        }
    }
}
