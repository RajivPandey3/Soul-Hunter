using System;
using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// One enemy model baked into vertex animation textures (2026-10-15, horde drawing groundwork). The horde (~300 on
    /// screen) costs skinning and an Animator per body; a VAT body is a static mesh whose vertex shader reads each
    /// vertex's place from a texture, so one RenderMeshInstanced call can draw every body of a model.
    /// Written by HordeVatBaker (Editor); read by the instanced horde drawer (not written yet).
    /// Texture layout: frame f of the bake holds RowsPerFrame rows; vertex v of frame f is at
    /// x = v % TextureWidth, y = f * RowsPerFrame + v / TextureWidth. Positions and normals are in the model root's space,
    /// so the visual child's local transform in a gameplay prefab still applies on top.
    /// </summary>
    public sealed class HordeVatAsset : ScriptableObject
    {
        [Serializable]
        public struct Clip
        {
            public string Name;
            [Tooltip("First baked frame of this clip (frame index, not texture row)")]
            public int StartFrame;
            [Tooltip("First texture row of this clip: StartFrame x RowsPerFrame")]
            public int StartRow;
            public int FrameCount;
            [Tooltip("Frames per second the clip was sampled at")]
            public float Fps;
            [Tooltip("A looping clip has no repeated last frame; playback wraps to its first frame")]
            public bool Loop;

            /// <summary>Seconds the clip lasts at speed 1.</summary>
            public float Length => Fps > 0f ? (Loop ? FrameCount : Mathf.Max(1, FrameCount - 1)) / Fps : 0f;

            /// <summary>What the shader's per-instance _VatClip takes: start frame, frame count, fps, loop.</summary>
            public Vector4 ShaderVector => new Vector4(StartFrame, FrameCount, Fps, Loop ? 1f : 0f);
        }

        [Header("Drawing")]
        [Tooltip("Static rest mesh: TEXCOORD3.x holds each vertex's index; submeshes follow Materials")]
        public Mesh Mesh;
        [Tooltip("RGBAHalf positions in the model root's space")]
        public Texture2D Positions;
        [Tooltip("RGBAHalf normals in the model root's space")]
        public Texture2D Normals;
        [Tooltip("SoulHunter/HordeVat materials, one per submesh, instancing on")]
        public Material[] Materials = Array.Empty<Material>();

        [Header("Layout")]
        public int VertexCount;
        public int TextureWidth;
        public int RowsPerFrame = 1;
        public int TotalFrames;
        public Clip[] Clips = Array.Empty<Clip>();
        [Tooltip("Covers every baked frame, in the model root's space")]
        public Bounds Bounds;

        [Header("Source")]
        [Tooltip("The single skinned mesh the body draws today; the horde drawer finds a body's VAT by it (null with several renderers)")]
        public Mesh SourceMesh;
        [Tooltip("Model the mesh and materials were read from (the model prefab when it exists, else the FBX)")]
        public GameObject SourceModel;
        public string SourceModelPath;
        [Tooltip("FBX the animation clips were read from")]
        public string SourceClipsPath;
        [Tooltip("The source renderers' materials, one per submesh, in the same order as Materials")]
        public Material[] SourceMaterials = Array.Empty<Material>();
        [Tooltip("Renderers that were baked, as paths under the model root")]
        public string[] SourceRenderers = Array.Empty<string>();

        /// <summary>Index of the clip with this name (case-insensitive), or -1.</summary>
        public int IndexOf(string clipName)
        {
            for (int i = 0; i < Clips.Length; i++)
                if (string.Equals(Clips[i].Name, clipName, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        public bool TryGetClip(string clipName, out Clip clip)
        {
            int index = IndexOf(clipName);
            clip = index >= 0 ? Clips[index] : default;
            return index >= 0;
        }

        /// <summary>
        /// A still body for corpses, which today bake the skinned mesh at death: Mesh itself is the first Idle frame with
        /// real positions, the model's vertex colours and its UV1 glow, so it draws as it is with SoulHunter/HordeMerged.
        /// </summary>
        public Mesh IdlePoseMesh => Mesh;

        /// <summary>
        /// A new still mesh in the pose of one baked frame (for example Death's last frame), read from the textures
        /// (they are saved readable). The caller owns the mesh and destroys it. Null when the frame is out of range.
        /// </summary>
        public Mesh BuildFrameMesh(int clipIndex, int frame)
        {
            if (Mesh == null || Positions == null || Normals == null || clipIndex < 0 || clipIndex >= Clips.Length) return null;
            var clip = Clips[clipIndex];
            if (frame < 0 || frame >= clip.FrameCount || !Positions.isReadable || !Normals.isReadable) return null;
            var positionData = Positions.GetPixelData<ushort>(0);
            var normalData = Normals.GetPixelData<ushort>(0);
            var vertices = new Vector3[VertexCount];
            var normals = new Vector3[VertexCount];
            int baseRow = (clip.StartFrame + frame) * RowsPerFrame;
            for (int v = 0; v < VertexCount; v++)
            {
                int texel = ((baseRow + v / TextureWidth) * TextureWidth + v % TextureWidth) * 4;
                vertices[v] = new Vector3(Mathf.HalfToFloat(positionData[texel]), Mathf.HalfToFloat(positionData[texel + 1]), Mathf.HalfToFloat(positionData[texel + 2]));
                normals[v] = new Vector3(Mathf.HalfToFloat(normalData[texel]), Mathf.HalfToFloat(normalData[texel + 1]), Mathf.HalfToFloat(normalData[texel + 2]));
            }
            var mesh = Instantiate(Mesh);
            mesh.name = $"{Mesh.name}_{clip.Name}_{frame}";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.bounds = Bounds;
            return mesh;
        }
    }
}
