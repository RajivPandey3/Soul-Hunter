using System.Collections.Generic;
using System.IO;
using System.Text;
using SoulHunter.Gameplay.Pickups;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Proof that PropMeshMerger's one-material merge keeps the look (owner, 2026-10-15: "apna skill use karoge to fark
    /// kyun parega"): renders each prop as it was (a copy of the prefab saved before the merge) and as it is now, from the
    /// same game-like camera under the same light, writes both side by side into one PNG per prop, and reports how far
    /// apart the two pictures are (mean and 99th-percentile colour difference over the pixels either one covers, 0-255).
    /// -executeMethod SoulHunter.Editor.Content.PropLookCompare.RunFromCommandLine -sh-before &lt;folder of old prefabs&gt;
    /// -sh-out &lt;folder&gt;; each old prefab has the same file name as its merged one.
    /// </summary>
    public static class PropLookCompare
    {
        private const int Size = 512;

        public static void RunFromCommandLine()
        {
            string before = Arg("-sh-before"), output = Arg("-sh-out");
            int code = 0;
            try { File.WriteAllText(Path.Combine(output, "look-compare.txt"), Compare(before, output)); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static string Arg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        public static string Compare(string beforeFolder, string output)
        {
            Directory.CreateDirectory(output);
            var report = new StringBuilder();
            // The first Camera.Render of a batch session can come out without the main light set up (the lantern, first
            // in the list, rendered dark and unlit while every later prop matched to the pixel), so one picture is
            // rendered and thrown away first.
            var first = AssetDatabase.LoadAssetAtPath<GameObject>(PropMeshMerger.Targets[0].PrefabPath);
            if (first != null) { Bounds warm = default; Object.DestroyImmediate(Render(first, 0, ref warm, out _)); }
            foreach (var target in PropMeshMerger.Targets)
            {
                string name = Path.GetFileName(target.PrefabPath);
                var now = AssetDatabase.LoadAssetAtPath<GameObject>(target.PrefabPath);
                var old = AssetDatabase.LoadAssetAtPath<GameObject>(beforeFolder.TrimEnd('/') + "/" + name);
                if (now == null || old == null) { report.AppendLine($"{name}: missing ({(now == null ? "merged" : "before")})"); continue; }
                int tiers = Mathf.Max(1, now.GetComponentInChildren<TieredVisual>(true)?.Tiers.Length ?? 1);
                for (int tier = 0; tier < tiers; tier++)
                {
                    // Both from the old prefab's framing, so a tighter merged bounds box does not move the camera.
                    Bounds frame = default;
                    var a = Render(old, tier, ref frame, out int drawnA);
                    var b = Render(now, tier, ref frame, out int drawnB);
                    var (mean, p99, covered) = Difference(a, b);
                    var sheet = new Texture2D(Size * 2, Size, TextureFormat.RGB24, false);
                    sheet.SetPixels(0, 0, Size, Size, a.GetPixels());
                    sheet.SetPixels(Size, 0, Size, Size, b.GetPixels());
                    sheet.Apply();
                    string file = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(name)}{(tiers > 1 ? "_tier" + tier : "")}.png");
                    File.WriteAllBytes(file, sheet.EncodeToPNG());
                    report.AppendLine($"{name}{(tiers > 1 ? " tier " + tier : "")}: renderers {drawnA} -> {drawnB}; pixels {covered}; mean difference {mean:0.00}, 99th percentile {p99:0.0} (of 255)");
                    Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(sheet);
                }
            }
            Debug.Log("[PropLookCompare]\n" + report);
            return report.ToString();
        }

        private static Texture2D Render(GameObject prefab, int tier, ref Bounds frame, out int drawn)
        {
            var root = new GameObject("PropLookCompare") { hideFlags = HideFlags.HideAndDontSave };
            var copy = Object.Instantiate(prefab, root.transform);
            copy.transform.position = Vector3.zero;
            var tiered = copy.GetComponentInChildren<TieredVisual>(true);
            if (tiered != null) for (int i = 0; i < tiered.Tiers.Length; i++) if (tiered.Tiers[i] != null) tiered.Tiers[i].SetActive(i == tier);
            var bounds = new Bounds(copy.transform.position, Vector3.zero);
            bool any = false;
            drawn = 0;
            foreach (var r in copy.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || !r.enabled) continue;
                drawn++;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            if (frame.size == Vector3.zero) frame = bounds; else bounds = frame;

            var lightGo = new GameObject("Key") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sh-no-shadows") >= 0 ? LightShadows.None : LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var camGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 30f;
            // The game camera's 55-degree tilt.
            Vector3 dir = new Vector3(0f, Mathf.Sin(55f * Mathf.Deg2Rad), -Mathf.Cos(55f * Mathf.Deg2Rad));
            float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
            float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position = bounds.center + dir * distance;
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = distance * 4f;

            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(root);
            return tex;
        }

        private static (float mean, float p99, int covered) Difference(Texture2D a, Texture2D b)
        {
            var pa = a.GetPixels32();
            var pb = b.GetPixels32();
            var diffs = new List<int>();
            for (int i = 0; i < pa.Length; i++)
            {
                bool inA = pa[i].r + pa[i].g + pa[i].b > 6, inB = pb[i].r + pb[i].g + pb[i].b > 6;
                if (!inA && !inB) continue;
                diffs.Add(Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r), Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].b - pb[i].b)));
            }
            if (diffs.Count == 0) return (0f, 0f, 0);
            diffs.Sort();
            float sum = 0f;
            foreach (int d in diffs) sum += d;
            return (sum / diffs.Count, diffs[Mathf.Min(diffs.Count - 1, (int)(diffs.Count * 0.99f))], diffs.Count);
        }
    }
}
