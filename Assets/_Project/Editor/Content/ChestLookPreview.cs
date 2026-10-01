using System.IO;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Renders the bronze, silver and Arcana chests (ChestLook on the approved Soul Reliquary) side by side into one PNG,
    /// for the owner's approval. Menu Soul Hunter/Content/Render Chest Looks, or
    /// -executeMethod SoulHunter.Editor.Content.ChestLookPreview.RunFromCommandLine -sh-chest-preview &lt;file.png&gt;.
    /// </summary>
    public static class ChestLookPreview
    {
        private const string ChestPrefab = "Assets/Prefabs/Items/Chest.prefab";

        [MenuItem("Soul Hunter/Content/Render Chest Looks")]
        public static void RenderFromMenu()
        {
            string path = EditorUtility.SaveFilePanel("Chest looks", "", "chest-looks.png", "png");
            if (!string.IsNullOrEmpty(path)) Render(path);
        }

        public static void RunFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-sh-chest-preview");
            string path = i >= 0 && i + 1 < args.Length ? args[i + 1] : "chest-looks.png";
            int code = 0;
            try { Render(path); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Render(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefab);
            var visual = prefab != null ? prefab.transform.Find("Visual") : null;
            if (visual == null) throw new FileNotFoundException("Chest visual not found", ChestPrefab);

            var root = new GameObject("ChestLookPreview") { hideFlags = HideFlags.HideAndDontSave };
            var kinds = new[] { ChestKind.Bronze, ChestKind.Silver, ChestKind.Arcana };
            const float spacing = 3.2f;
            for (int k = 0; k < kinds.Length; k++)
            {
                // The whole prefab, as it lies on the ground in play (its own rotations kept); behaviours do not run in Edit Mode.
                var copy = Object.Instantiate(prefab, root.transform);
                copy.transform.localPosition = new Vector3((k - 1) * spacing, 0f, 0f);
                foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
                    if (r.name.Contains("Glow") || r.name.Contains("Cube")) r.enabled = false;
                ChestLook.Apply(copy.transform, kinds[k]);
            }
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            var lightGo = new GameObject("Key") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f);
            cam.fieldOfView = 24f;
            // The game camera's 55-degree tilt, from the side the chest faces the player (-Z, see PickupV2Importer).
            Vector3 dir = new Vector3(0f, Mathf.Sin(55f * Mathf.Deg2Rad), -Mathf.Cos(55f * Mathf.Deg2Rad));
            float distance = bounds.extents.x / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad * 1.7f) + bounds.extents.z * 2f;
            cam.transform.position = bounds.center + dir * distance;
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = distance * 4f;

            var rt = new RenderTexture(1800, 700, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[ChestLookPreview] Wrote " + path);

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(root);
        }
    }
}
