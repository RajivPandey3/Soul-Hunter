using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// VS-style death (owner, 2026-09-29: "enemy marne per gayab ho jata hai per blood dikhta rahta hai"; chose "Body
    /// briefly stays"). VS enemies have a death animation where the body breaks up and fades over about 2 s (wiki enemy
    /// pages, e.g. Skeleton 18 x 140 ms); Soul Hunter's killed enemy used to vanish at once while its neck spray went on.
    ///
    /// The enemy itself still returns to the pool at once, so kills, spawning and pooling are unchanged. A corpse, a still
    /// copy of the enemy's drawn meshes in their last pose (skinned meshes are baked), stays where it died and sinks into
    /// the ground and shrinks for LingerSeconds, ending with the blood jet (its last spurt starts at 0.5 s). At most
    /// MaxCorpses exist at once; beyond that an enemy vanishes as before, so a horde of kills cannot flood the frame.
    /// Timings and sizes are provisional.
    /// </summary>
    public sealed class EnemyCorpses : MonoBehaviour
    {
        public const float LingerSeconds = 0.7f;
        public const int MaxCorpses = 32, MaxPartsPerCorpse = 8;
        /// <summary>How far below its start a corpse ends, as a share of its height (it sinks under the floor).</summary>
        public const float SinkShare = 1.1f;
        public const float EndScale = 0.6f;

        private sealed class Part
        {
            public GameObject Go;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Mesh Baked;
        }

        private sealed class Corpse
        {
            public GameObject Root;
            public readonly List<Part> Parts = new List<Part>();
            public float Started;
            public float Sink;
            public Vector3 StartPosition;
            public bool Busy;
        }

        private static EnemyCorpses _instance;
        private readonly List<Corpse> _corpses = new List<Corpse>();

        /// <summary>Corpses sinking now (tests).</summary>
        public static int Active
        {
            get
            {
                if (_instance == null) return 0;
                int n = 0;
                foreach (var c in _instance._corpses) if (c.Busy) n++;
                return n;
            }
        }

        /// <summary>The root of the corpse started last (tests).</summary>
        public static Transform LastStarted { get; private set; }

        /// <summary>
        /// Leaves a sinking copy of <paramref name="renderers"/> (the enemy's drawn body, still shown) at its place.
        /// Returns false when the corpse budget is used up or there is nothing to copy.
        /// </summary>
        public static bool Leave(IReadOnlyList<Renderer> renderers, Bounds body)
        {
            if (!Application.isPlaying || renderers == null || renderers.Count == 0) return false;
            if (_instance == null)
            {
                var go = new GameObject("EnemyCorpses");
                _instance = go.AddComponent<EnemyCorpses>();
            }
            return _instance.Begin(renderers, body);
        }

        private bool Begin(IReadOnlyList<Renderer> renderers, Bounds body)
        {
            Corpse corpse = null;
            foreach (var c in _corpses) if (!c.Busy) { corpse = c; break; }
            if (corpse == null)
            {
                if (_corpses.Count >= MaxCorpses) return false;
                corpse = new Corpse { Root = new GameObject("Corpse") };
                corpse.Root.transform.SetParent(transform, false);
                _corpses.Add(corpse);
            }

            var root = corpse.Root.transform;
            root.SetPositionAndRotation(new Vector3(body.center.x, body.min.y, body.center.z), Quaternion.identity);
            root.localScale = Vector3.one;
            int used = 0;
            for (int i = 0; i < renderers.Count && used < MaxPartsPerCorpse; i++)
            {
                var source = renderers[i];
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy) continue;
                Mesh mesh = null;
                Part part = used < corpse.Parts.Count ? corpse.Parts[used] : null;
                if (part == null)
                {
                    part = new Part { Go = new GameObject("Part") };
                    part.Go.transform.SetParent(root, false);
                    part.Filter = part.Go.AddComponent<MeshFilter>();
                    part.Renderer = part.Go.AddComponent<MeshRenderer>();
                    part.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    corpse.Parts.Add(part);
                }
                var t = source.transform;
                if (source is SkinnedMeshRenderer skinned)
                {
                    if (part.Baked == null) part.Baked = new Mesh { name = "CorpseBaked" };
                    skinned.BakeMesh(part.Baked, true); // the pose now, with the renderer's scale
                    mesh = part.Baked;
                    part.Go.transform.SetPositionAndRotation(t.position, t.rotation);
                    part.Go.transform.localScale = Vector3.one;
                }
                else if (source is MeshRenderer && source.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    mesh = filter.sharedMesh;
                    part.Go.transform.SetPositionAndRotation(t.position, t.rotation);
                    SetWorldScale(part.Go.transform, t.lossyScale);
                }
                if (mesh == null) continue;
                part.Filter.sharedMesh = mesh;
                var rest = source.GetComponentInParent<DamageFlash>()?.RestMaterial(source); // not the killing hit's flash
                if (rest != null) part.Renderer.sharedMaterial = rest;
                else part.Renderer.sharedMaterials = source.sharedMaterials;
                part.Renderer.receiveShadows = source.receiveShadows;
                part.Go.SetActive(true);
                used++;
            }
            for (int i = used; i < corpse.Parts.Count; i++) corpse.Parts[i].Go.SetActive(false);
            if (used == 0) return false;

            corpse.Busy = true;
            corpse.Started = Time.time;
            corpse.StartPosition = root.position;
            corpse.Sink = Mathf.Max(0.5f, body.size.y) * SinkShare;
            corpse.Root.SetActive(true);
            LastStarted = root;
            return true;
        }

        private static void SetWorldScale(Transform part, Vector3 world)
        {
            var parent = part.parent != null ? part.parent.lossyScale : Vector3.one;
            part.localScale = new Vector3(world.x / Mathf.Max(1e-4f, parent.x), world.y / Mathf.Max(1e-4f, parent.y), world.z / Mathf.Max(1e-4f, parent.z));
        }

        private void Update()
        {
            float now = Time.time;
            foreach (var corpse in _corpses)
            {
                if (!corpse.Busy) continue;
                float t = (now - corpse.Started) / LingerSeconds;
                if (t >= 1f)
                {
                    corpse.Busy = false;
                    corpse.Root.SetActive(false);
                    continue;
                }
                // Holds still for a moment, then sinks faster and shrinks: the body goes down into the ground.
                float ease = t * t;
                var root = corpse.Root.transform;
                root.position = corpse.StartPosition + Vector3.down * corpse.Sink * ease;
                root.localScale = Vector3.one * Mathf.Lerp(1f, EndScale, ease);
            }
        }

        private void OnDestroy()
        {
            foreach (var corpse in _corpses)
                foreach (var part in corpse.Parts)
                    if (part.Baked != null) Destroy(part.Baked);
            if (_instance == this) _instance = null;
        }
    }
}
