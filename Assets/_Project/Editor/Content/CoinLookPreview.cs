using System.IO;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Renders the Soul Coin pickup as the game shows it (the 55-degree game camera, one sun, a mid-tone floor): its three
    /// tiers, each at four moments of its turn, close up and at about the size it has on screen. Owner (2026-10-13): "coin
    /// kisi stiker ke jaisa lag raha hai use asli gold coin jaisa banao pahle tum khud check karo".
    /// CLI: -executeMethod SoulHunter.Editor.Content.CoinLookPreview.RunFromCommandLine -sh-coin-preview &lt;file.png&gt;
    /// </summary>
    public static class CoinLookPreview
    {
        private const string CoinPrefab = "Assets/Prefabs/Items/SH10_Gold_Coin.prefab";
        private static readonly float[] Turns = { 0f, 50f, 90f, 140f };

        [MenuItem("Soul Hunter/Content/Render Coin Looks")]
        public static void RenderFromMenu()
        {
            string path = EditorUtility.SaveFilePanel("Coin looks", "", "coin-looks.png", "png");
            if (!string.IsNullOrEmpty(path)) Render(path);
        }

        public static void RunFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-sh-coin-preview");
            string path = i >= 0 && i + 1 < args.Length ? args[i + 1] : "coin-looks.png";
            int code = 0;
            try { Render(path); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Render(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefab);
            if (prefab == null) throw new FileNotFoundException("Coin prefab not found", CoinPrefab);
            var root = new GameObject("CoinLookPreview") { hideFlags = HideFlags.HideAndDontSave };

            // A floor in the stage floors' comfort band, so the coin is judged against what it lies on in play.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = Vector3.one * 4f;
            var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMaterial.SetColor("_BaseColor", new Color(0.36f, 0.42f, 0.30f));
            floorMaterial.SetFloat("_Smoothness", 0.05f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            const float spacing = 1.7f;
            for (int tier = 0; tier < 3; tier++)
            for (int t = 0; t < Turns.Length; t++)
            {
                var copy = Object.Instantiate(prefab, root.transform);
                copy.transform.localPosition = new Vector3((t - (Turns.Length - 1) * 0.5f) * spacing, 0f, (1 - tier) * spacing);
                var tiered = copy.GetComponent<TieredVisual>();
                if (tiered != null && tiered.Tiers != null)
                    for (int k = 0; k < tiered.Tiers.Length; k++) if (tiered.Tiers[k] != null) tiered.Tiers[k].SetActive(k == tier);
                // The idle motion turns the visual about the world's up axis in play.
                var motion = copy.GetComponent<PickupIdleMotion>();
                if (motion != null && motion.Visual != null) motion.Visual.Rotate(Vector3.up, Turns[t], Space.World);
            }

            var lightGo = new GameObject("Sun") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Hard;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f);
            cam.orthographic = true; // the game camera is orthographic, pitched 55 degrees
            Vector3 dir = new Vector3(0f, Mathf.Sin(55f * Mathf.Deg2Rad), -Mathf.Cos(55f * Mathf.Deg2Rad));
            Vector3 centre = new Vector3(0f, 0.4f, 0f);
            cam.transform.position = centre + dir * 20f;
            cam.transform.LookAt(centre);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;

            // The first render of a batch-mode editor has no lighting set up yet (it came out white): throw one away.
            Object.DestroyImmediate(Shoot(cam, 64, 64, 2.6f));
            // Left: close up. Right: about the size on screen (the game's view is 24 units tall on 768 pixels).
            var close = Shoot(cam, 1280, 900, 2.6f);
            var small = Shoot(cam, 220, 154, 2.6f);
            var sheet = new Texture2D(close.width + 440 + 30, close.height, TextureFormat.RGB24, false);
            var dark = new Color32(18, 18, 22, 255);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = dark;
            sheet.SetPixels32(fill);
            sheet.SetPixels(0, 0, close.width, close.height, close.GetPixels());
            // The small one twice as large as real (nearest neighbour), so its pixels can be read.
            for (int y = 0; y < small.height * 2; y++)
            for (int x = 0; x < small.width * 2; x++)
                sheet.SetPixel(close.width + 30 + x, (sheet.height - small.height * 2) / 2 + y, small.GetPixel(x / 2, y / 2));
            sheet.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Debug.Log("[CoinLookPreview] Wrote " + path);

            Object.DestroyImmediate(close);
            Object.DestroyImmediate(small);
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(floorMaterial);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(root);
        }

        private static Texture2D Shoot(Camera cam, int width, int height, float halfHeightUnits)
        {
            cam.orthographicSize = halfHeightUnits;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            return tex;
        }
    }
}
