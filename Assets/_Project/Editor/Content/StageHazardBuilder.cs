using System;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the stage-hazard assets (2026-09-27) and fills GameContentCatalog:
    ///   RailCart     Assets/Prefabs/Props/SH10_RailCart.prefab: a 24-unit rail (two iron rails on wooden sleepers) and
    ///                a cart loaded with soul crystals. PLACEHOLDER art from Unity primitives; replace the "Rails" and
    ///                "Cart/Visual" children when owner-approved models exist. The cart is tagged and layered "Enemy" with
    ///                a trigger, HealthController (RailCart.MaxHealth) and RailCart, like the Soul Lantern.
    ///   HazardMarker Assets/Prefabs/Props/SH10_HazardMarker.prefab: a flat quad with the approved VFX v2 magic circle
    ///                (additive SoulHunter/VFX/Particle; SoulMeteor tints it per stage). No new art.
    ///   HazardBullet the Magic Wand bolt (SH10_Proj_MagicBolt), tinted crimson at runtime by HazardBullets.
    /// Menu: Soul Hunter/Content/Build Stage Hazards, or
    /// -executeMethod SoulHunter.Editor.Content.StageHazardBuilder.BuildAll -quit
    /// </summary>
    public static class StageHazardBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs/Props";
        public const string MaterialFolder = "Assets/Art/Materials/Hazards";
        public const string RailCartPath = PrefabFolder + "/SH10_RailCart.prefab";
        public const string MarkerPath = PrefabFolder + "/SH10_HazardMarker.prefab";
        public const string MagicCircleTexture = "Assets/Art/Textures/VFX/SH10_Vfx2_magic_circle.png";
        public const string BulletPath = "Assets/Prefabs/Weapons/Projectiles/SH10_Proj_MagicBolt.prefab";
        private const string ParticleShader = "SoulHunter/VFX/Particle";

        [MenuItem("Soul Hunter/Content/Build Stage Hazards")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Materials")) AssetDatabase.CreateFolder("Assets/Art", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "Hazards");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Props");

            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new InvalidOperationException("GameContentCatalog (Resources/GameContent) is missing.");

            catalog.RailCart = BuildRailCart();
            catalog.HazardMarker = BuildMarker();
            var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath);
            if (bullet == null) Debug.LogWarning($"[StageHazardBuilder] {BulletPath} is missing (Soul Hunter/Content/Build Weapon VFX); Pile Assault bullets fall back to a sphere.");
            catalog.HazardBullet = bullet;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[StageHazardBuilder] Rail cart (placeholder art), meteor marker and hazard bullet are in the game.");
        }

        // ---------------------------------------------------------------- rail cart (placeholder)

        private static GameObject BuildRailCart()
        {
            var iron = Mat("Iron", new Color(0.32f, 0.33f, 0.36f), Color.black, 0.6f, 0.45f);
            var sleeperWood = Mat("SleeperWood", new Color(0.28f, 0.19f, 0.12f), Color.black, 0f, 0.2f);
            var cartWood = Mat("CartWood", new Color(0.45f, 0.3f, 0.18f), Color.black, 0f, 0.25f);
            var soul = Mat("SoulCrystal", new Color(0.45f, 0.95f, 1f), new Color(0.3f, 0.9f, 1f), 0f, 0.6f);

            var root = new GameObject("SH10_RailCart");
            try
            {
                root.AddComponent<RailCartTrack>();

                var rails = new GameObject("Rails").transform;
                rails.SetParent(root.transform, false);
                float length = RailCartTrack.Length;
                Part(rails, PrimitiveType.Cube, iron, new Vector3(length, 0.1f, 0.12f), Vector3.zero, new Vector3(0f, 0.08f, 0.55f), false);
                Part(rails, PrimitiveType.Cube, iron, new Vector3(length, 0.1f, 0.12f), Vector3.zero, new Vector3(0f, 0.08f, -0.55f), false);
                for (float x = -length * 0.5f + 0.5f; x <= length * 0.5f - 0.4f; x += 1.5f)
                    Part(rails, PrimitiveType.Cube, sleeperWood, new Vector3(0.35f, 0.06f, 1.6f), Vector3.zero, new Vector3(x, 0.03f, 0f), false);
                // End stops, so the rail reads as a rail from above.
                Part(rails, PrimitiveType.Cube, iron, new Vector3(0.25f, 0.4f, 1.4f), Vector3.zero, new Vector3(-length * 0.5f, 0.2f, 0f), true);
                Part(rails, PrimitiveType.Cube, iron, new Vector3(0.25f, 0.4f, 1.4f), Vector3.zero, new Vector3(length * 0.5f, 0.2f, 0f), true);

                var cart = new GameObject("Cart");
                cart.transform.SetParent(root.transform, false);
                cart.transform.localPosition = new Vector3(-RailCartTrack.HalfTravel, 0f, 0f);
                cart.tag = "Enemy";                            // Whip, Bible, Santa Water... hit the "Enemy" tag
                cart.layer = LayerMask.NameToLayer("Enemy");   // Garlic, Cross, Soul Burst query the Enemy layer
                var body = cart.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var trigger = cart.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.9f, 1.6f, 1.5f);
                trigger.center = new Vector3(0f, 0.8f, 0f);
                var health = cart.AddComponent<HealthController>();
                var so = new SerializedObject(health);
                so.FindProperty("_maxHealth").intValue = RailCart.MaxHealth;
                so.ApplyModifiedPropertiesWithoutUndo();
                cart.AddComponent<RailCart>();

                var visual = new GameObject("Visual").transform;
                visual.SetParent(cart.transform, false);
                Part(visual, PrimitiveType.Cube, cartWood, new Vector3(1.7f, 0.7f, 1.2f), Vector3.zero, new Vector3(0f, 0.8f, 0f), true);
                Part(visual, PrimitiveType.Cube, iron, new Vector3(1.78f, 0.1f, 1.28f), Vector3.zero, new Vector3(0f, 1.1f, 0f), true);
                Part(visual, PrimitiveType.Cube, iron, new Vector3(1.78f, 0.1f, 1.28f), Vector3.zero, new Vector3(0f, 0.55f, 0f), true);
                foreach (float x in new[] { -0.55f, 0.55f })
                foreach (float z in new[] { -0.6f, 0.6f })
                    Part(visual, PrimitiveType.Cylinder, iron, new Vector3(0.5f, 0.05f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(x, 0.3f, z), true);
                Part(visual, PrimitiveType.Sphere, soul, new Vector3(0.45f, 0.55f, 0.45f), new Vector3(0f, 0f, 15f), new Vector3(-0.35f, 1.25f, 0.15f), false);
                Part(visual, PrimitiveType.Sphere, soul, new Vector3(0.38f, 0.5f, 0.38f), new Vector3(0f, 0f, -20f), new Vector3(0.3f, 1.22f, -0.2f), false);
                Part(visual, PrimitiveType.Sphere, soul, new Vector3(0.3f, 0.4f, 0.3f), Vector3.zero, new Vector3(0.1f, 1.3f, 0.3f), false);

                return PrefabUtility.SaveAsPrefabAsset(root, RailCartPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- meteor marker

        private static GameObject BuildMarker()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MagicCircleTexture);
            if (texture == null) Debug.LogWarning($"[StageHazardBuilder] {MagicCircleTexture} is missing; the marker has no circle texture.");
            string path = $"{MaterialFolder}/SH10_Hazard_Marker.mat";
            var shader = Shader.Find(ParticleShader);
            if (shader == null) throw new InvalidOperationException("Shader " + ParticleShader + " is missing.");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_SrcBlend", 5f); // SrcAlpha
            mat.SetFloat("_DstBlend", 1f); // One: additive glow
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);

            var root = new GameObject("SH10_HazardMarker");
            try
            {
                var quad = new GameObject("Circle");
                quad.transform.SetParent(root.transform, false);
                quad.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var r = quad.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                return PrefabUtility.SaveAsPrefabAsset(root, MarkerPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 scale, Vector3 euler, Vector3 position, bool castShadows)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>()); // only the cart's trigger is a collider
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>URP Lit; emission for the soul crystals (no HDR bloom in the game).</summary>
        private static Material Mat(string name, Color color, Color emission, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/SH10_Hazard_{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * 0.7f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
