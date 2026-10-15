using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15): the Soul Reliquary chest (PickupV2Importer.BuildChest, from
    /// ArtSource/Generators/build_pickups_v2.py chest()) is 43 single-material parts under "Visual/SH10_Pickup_Chest",
    /// one renderer each. Merged per material and per side of the lid seam it draws 13 times.
    ///
    /// What reads the chest's parts, and why it keeps working:
    /// - ChestPreviewStage (the opening screen) copies "Visual" and measures it. Its up axis is the mean centre of every
    ///   renderer whose name starts with "Lid" (the shell and both LidBand rings) minus the whole chest's centre; its front
    ///   is "LockPlate*"; its seam is the lowest point of the "Lid" shell (not "LidBand"); then every renderer whose bounds
    ///   centre is above the seam hangs on the hinge. So every "Lid*" renderer and "LockPlate*" are kept as they are, and
    ///   the up axis, the front and the seam come out exactly as before.
    /// - ChestPreviewPlayModeTests finds the skull as a renderer named "Cranium*" on the hinge: "Cranium*" is kept too.
    /// - ChestLook, SoulCasket and the preview's seam dimming work per material name ("Chest_Iron", "Chest_Soul") on every
    ///   renderer: a merged renderer has exactly one material, so they find it as before.
    /// - PickupV2Tests looks for the "SH10_Pickup_Chest" object: the kept parts stay under it, so it stays.
    ///
    /// The split ("lid" or "body") is the preview's own rule, computed in the "Visual" group's space: the part's bounds
    /// centre along the chest's up axis against the seam. The preview measures world bounds after turning the copy
    /// upright, but "Visual" is not rotated inside the chest (identity in Chest.prefab) and the preview's turn only swaps
    /// axes, so "above the seam along up" in group space is the same comparison the preview makes after its turn.
    /// A part closer to the seam than 1% of the chest's height is not merged at all (the LidBand rings sit exactly on it:
    /// the preview's float rounding decides their side, and it must stay its decision).
    ///
    /// Then the merge is simulated (the same candidates, keys and drop rule as PropMeshMerger.MergeGroup) and each merged
    /// mesh is checked: its centre after RecalculateBounds must land on its own side of the seam, with the 1% margin. That
    /// centre is not known without the vertices, but every vertex of a piece lies inside that piece's transformed bounds
    /// box, so the merged mesh's lowest point lies between the lowest box bottom and the lowest box top, and its highest
    /// point between the highest box bottom and the highest box top. The check uses that whole range. A merged mesh that
    /// could land on the wrong side is not built: its pieces are kept as they are, and the simulation runs again.
    /// So the hinge takes exactly the geometry it took before, and the body keeps the rest.
    /// </summary>
    public static class ChestMergeTarget
    {
        /// <summary>Parts closer to the seam than this share of the chest's height are left alone.</summary>
        public const float SeamMargin = 0.01f;

        public static readonly PropMeshMerger.Target Target = new PropMeshMerger.Target
        {
            PrefabPath = PickupV2Importer.ChestPrefab,
            Groups = root => One(root.transform.Find("Visual")),
            Keep = renderer => KeepByName(renderer) || AnalysisFor(GroupOf(renderer)).Keep.Contains(renderer),
            Split = (renderer, group) => AnalysisFor(group).Lid.Contains(renderer) ? "lid" : "body",
        };

        /// <summary>Names a script or test looks up: the lid shell and its rings, the lock plate, the skull, any ground glow.</summary>
        public static bool KeepByName(Renderer r) =>
            PropMeshMerger.KeepByName(r) || r.name.StartsWith("Lid") || r.name.StartsWith("LockPlate") || r.name.StartsWith("Cranium");

        private static IEnumerable<Transform> One(Transform t) { if (t != null) yield return t; }

        private sealed class Analysis
        {
            public readonly HashSet<Renderer> Keep = new HashSet<Renderer>();
            public readonly HashSet<Renderer> Lid = new HashSet<Renderer>();
        }

        private static readonly Dictionary<Transform, Analysis> Cache = new Dictionary<Transform, Analysis>();

        /// <summary>The group a renderer belongs to: the "Visual" child of the opened prefab's root.</summary>
        private static Transform GroupOf(Renderer r)
        {
            for (var t = r.transform; t != null; t = t.parent)
                if (t.name == "Visual" && t.parent != null && t.parent.parent == null) return t;
            return r.transform.root;
        }

        private static Analysis AnalysisFor(Transform group)
        {
            var stale = new List<Transform>();
            foreach (var key in Cache.Keys) if (key == null) stale.Add(key);
            foreach (var key in stale) Cache.Remove(key);
            if (!Cache.TryGetValue(group, out var analysis)) { analysis = Analyse(group); Cache[group] = analysis; }
            return analysis;
        }

        /// <summary>A mesh's bounds box carried into the group's space (the box around the turned box, as Unity's renderer bounds).</summary>
        public static Bounds BoxIn(Transform group, MeshRenderer r)
        {
            var mesh = r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            var local = mesh != null ? mesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
            Matrix4x4 m = group.worldToLocalMatrix * r.transform.localToWorldMatrix;
            Vector3 c = m.MultiplyPoint3x4(local.center), e = local.extents;
            Vector3 ext = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return new Bounds(c, ext * 2f);
        }

        /// <summary>ChestPreviewStage.SnapToAxis.</summary>
        private static Vector3 SnapToAxis(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static float Low(Bounds b, Vector3 up) => Vector3.Dot(up, b.center) - Vector3.Dot(Abs(up), b.extents);
        private static float High(Bounds b, Vector3 up) => Vector3.Dot(up, b.center) + Vector3.Dot(Abs(up), b.extents);
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static Analysis Analyse(Transform group)
        {
            var analysis = new Analysis();
            var renderers = group.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) return analysis;

            var box = new Dictionary<MeshRenderer, Bounds>();
            foreach (var r in renderers) box[r] = BoxIn(group, r);
            var whole = box[renderers[0]];
            foreach (var r in renderers) whole.Encapsulate(box[r]);

            // The preview's up axis (mean centre of the "Lid*" renderers) and seam (the "Lid" shell's lowest point).
            Vector3 lidSum = Vector3.zero;
            int lids = 0;
            foreach (var r in renderers) if (r.name.StartsWith("Lid")) { lidSum += box[r].center; lids++; }
            Vector3 up = lids > 0 ? SnapToAxis(lidSum / lids - whole.center) : Vector3.up;
            float seam = float.MaxValue;
            foreach (var r in renderers)
                if (r.name.StartsWith("Lid") && !r.name.StartsWith("LidBand")) seam = Mathf.Min(seam, Low(box[r], up));
            if (seam == float.MaxValue) seam = Vector3.Dot(up, whole.center) + Vector3.Dot(Abs(up), whole.extents) * 0.25f;
            float margin = SeamMargin * Mathf.Max(1e-4f, High(whole, up) - Low(whole, up));

            foreach (var r in renderers)
            {
                if (KeepByName(r)) continue;
                float centre = Vector3.Dot(up, box[r].center);
                if (Mathf.Abs(centre - seam) < margin) analysis.Keep.Add(r);
                else if (centre > seam) analysis.Lid.Add(r);
            }

            // Simulate PropMeshMerger.MergeGroup and keep the pieces of any merged mesh that could land on the wrong side.
            while (true)
            {
                var candidates = new Dictionary<MeshRenderer, List<string>>();
                foreach (var r in renderers)
                {
                    if (KeepByName(r) || analysis.Keep.Contains(r)) continue;
                    if (!r.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
                    var materials = r.sharedMaterials;
                    if (materials.Length != filter.sharedMesh.subMeshCount) continue;
                    string split = analysis.Lid.Contains(r) ? "lid" : "body";
                    var keys = new List<string>();
                    bool ok = true;
                    foreach (var m in materials)
                    {
                        if (m == null) { ok = false; break; }
                        keys.Add($"{m.GetInstanceID()}|{r.shadowCastingMode}|{r.receiveShadows}|{r.gameObject.layer}|{split}");
                    }
                    if (ok) candidates.Add(r, keys);
                }
                while (true)
                {
                    var count = new Dictionary<string, int>();
                    foreach (var keys in candidates.Values)
                        foreach (var key in keys) count[key] = count.TryGetValue(key, out int c) ? c + 1 : 1;
                    var drop = new List<MeshRenderer>();
                    foreach (var pair in candidates)
                        if (pair.Value.Exists(k => count[k] < 2)) drop.Add(pair.Key);
                    if (drop.Count == 0) break;
                    foreach (var r in drop) candidates.Remove(r);
                }

                var members = new Dictionary<string, List<MeshRenderer>>();
                foreach (var pair in candidates)
                    foreach (var key in pair.Value)
                    {
                        if (!members.TryGetValue(key, out var list)) members[key] = list = new List<MeshRenderer>();
                        if (!list.Contains(pair.Key)) list.Add(pair.Key);
                    }

                bool unsafeFound = false;
                foreach (var pair in members)
                {
                    float lowMin = float.MaxValue, lowMax = float.MaxValue, highMin = float.MinValue, highMax = float.MinValue;
                    foreach (var r in pair.Value)
                    {
                        float lo = Low(box[r], up), hi = High(box[r], up);
                        lowMin = Mathf.Min(lowMin, lo); lowMax = Mathf.Min(lowMax, hi);
                        highMin = Mathf.Max(highMin, lo); highMax = Mathf.Max(highMax, hi);
                    }
                    float centreMin = (lowMin + highMin) * 0.5f, centreMax = (lowMax + highMax) * 0.5f;
                    bool lid = pair.Key.EndsWith("|lid");
                    bool safe = lid ? centreMin > seam + margin : centreMax < seam - margin;
                    if (safe) continue;
                    foreach (var r in pair.Value) analysis.Keep.Add(r);
                    unsafeFound = true;
                }
                if (!unsafeFound) break;
            }
            return analysis;
        }
    }
}
