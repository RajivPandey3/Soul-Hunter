using System;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the four extra light-source pickups (2026-09-27) as PLACEHOLDER prefabs from Unity primitives with emissive
    /// URP Lit materials, each with its own colour and shape, bobbing and turning like the other pickups
    /// (PickupIdleMotion). None of this is final art; replace the "Visual" child when owner-approved models exist.
    ///   Soul Toll (screen wipe)     pale soul-white medallion with a cyan core
    ///   Hellfire Breath (fire)      tilted ember capsule with a flame tip
    ///   Luck Charm (+10% Luck)      three green leaves on a stem
    ///   Gilded Charm (Gold Fever)   four gold leaves around a gold core
    /// Writes Assets/Prefabs/Items/SH10_Pickup_*.prefab and fills GameContentCatalog (ScreenWipeItem, FireBreathItem,
    /// LuckCharmItem, GildedCharmItem, and FireBreathProjectile = the Fire Wand's SH10_Proj_Fireball).
    /// Menu: Soul Hunter/Content/Build Extra Pickups, or
    /// -executeMethod SoulHunter.Editor.Content.ExtraPickupBuilder.BuildAll -quit
    /// </summary>
    public static class ExtraPickupBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs/Items";
        public const string MaterialFolder = "Assets/Art/Materials/ExtraPickups";
        public const string FireballPath = "Assets/Prefabs/Weapons/Projectiles/SH10_Proj_Fireball.prefab";

        public const string ScreenWipePath = PrefabFolder + "/SH10_Pickup_SoulToll.prefab";
        public const string FireBreathPath = PrefabFolder + "/SH10_Pickup_HellfireBreath.prefab";
        public const string LuckCharmPath = PrefabFolder + "/SH10_Pickup_LuckCharm.prefab";
        public const string GildedCharmPath = PrefabFolder + "/SH10_Pickup_GildedCharm.prefab";

        /// <summary>Height of the visual pivot above the pickup's root (like the Soul Gem).</summary>
        private const float VisualHeight = 0.45f;

        [MenuItem("Soul Hunter/Content/Build Extra Pickups")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "ExtraPickups");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Items");

            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new InvalidOperationException("GameContentCatalog (Resources/GameContent) is missing.");

            catalog.ScreenWipeItem = Build<ScreenWipePickup>("SH10_Pickup_SoulToll", ScreenWipePath, BuildSoulToll);
            catalog.FireBreathItem = Build<FireBreathPickup>("SH10_Pickup_HellfireBreath", FireBreathPath, BuildHellfire);
            catalog.LuckCharmItem = Build<LuckCharmPickup>("SH10_Pickup_LuckCharm", LuckCharmPath, BuildLuckCharm);
            catalog.GildedCharmItem = Build<GildedCharmPickup>("SH10_Pickup_GildedCharm", GildedCharmPath, BuildGildedCharm);

            var fireball = AssetDatabase.LoadAssetAtPath<GameObject>(FireballPath);
            if (fireball == null) Debug.LogWarning($"[ExtraPickupBuilder] {FireballPath} is missing (Soul Hunter/Content/Build Weapon VFX); Hellfire Breath falls back to a sphere.");
            catalog.FireBreathProjectile = fireball;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[ExtraPickupBuilder] Soul Toll, Hellfire Breath, Luck Charm and Gilded Charm (placeholder art) are in the game.");
        }

        private static GameObject Build<T>(string name, string path, Action<Transform> visual) where T : Pickup
        {
            var root = new GameObject(name);
            try
            {
                var trigger = root.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.5f;
                trigger.center = new Vector3(0f, VisualHeight, 0f);
                root.AddComponent<T>();

                var pivot = new GameObject("Visual").transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(0f, VisualHeight, 0f);
                visual(pivot);

                var motion = root.AddComponent<PickupIdleMotion>();
                motion.Visual = pivot;
                motion.BobHeight = 0.15f;
                motion.SpinDegreesPerSecond = 90f;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---- Placeholder shapes -------------------------------------------------------------------------------

        private static void BuildSoulToll(Transform pivot)
        {
            var bone = Mat("SoulToll_Bone", new Color(0.92f, 0.95f, 1f), new Color(0.55f, 0.75f, 0.9f));
            var core = Mat("SoulToll_Core", new Color(0.35f, 0.95f, 1f), new Color(0.3f, 0.9f, 1f));
            // Upright medallion (a flattened cylinder faces the camera edge-on otherwise, so turn it to stand up).
            Part(pivot, PrimitiveType.Cylinder, bone, new Vector3(0.62f, 0.05f, 0.62f), new Vector3(90f, 0f, 0f), Vector3.zero);
            Part(pivot, PrimitiveType.Sphere, core, Vector3.one * 0.26f, Vector3.zero, Vector3.zero);
            Part(pivot, PrimitiveType.Cube, bone, new Vector3(0.1f, 0.3f, 0.1f), Vector3.zero, new Vector3(0f, 0.42f, 0f));
        }

        private static void BuildHellfire(Transform pivot)
        {
            var ember = Mat("Hellfire_Ember", new Color(0.85f, 0.15f, 0.05f), new Color(0.9f, 0.2f, 0.05f));
            var flame = Mat("Hellfire_Flame", new Color(1f, 0.75f, 0.2f), new Color(1f, 0.6f, 0.15f));
            Part(pivot, PrimitiveType.Capsule, ember, new Vector3(0.28f, 0.34f, 0.28f), new Vector3(0f, 0f, 35f), Vector3.zero);
            Part(pivot, PrimitiveType.Sphere, flame, Vector3.one * 0.2f, Vector3.zero, new Vector3(-0.2f, 0.28f, 0f));
            Part(pivot, PrimitiveType.Cube, Mat("Hellfire_Stem", new Color(0.2f, 0.55f, 0.15f), Color.black), new Vector3(0.06f, 0.14f, 0.06f), new Vector3(0f, 0f, 35f), new Vector3(0.22f, -0.3f, 0f));
        }

        private static void BuildLuckCharm(Transform pivot)
        {
            var leaf = Mat("LuckCharm_Leaf", new Color(0.25f, 0.85f, 0.3f), new Color(0.15f, 0.6f, 0.2f));
            Leaves(pivot, leaf, 3, 0.17f, 0.24f);
            Part(pivot, PrimitiveType.Cube, Mat("LuckCharm_Stem", new Color(0.18f, 0.5f, 0.15f), Color.black), new Vector3(0.05f, 0.3f, 0.05f), new Vector3(0f, 0f, -15f), new Vector3(0.04f, -0.25f, 0f));
        }

        private static void BuildGildedCharm(Transform pivot)
        {
            var gold = Mat("GildedCharm_Gold", new Color(1f, 0.78f, 0.2f), new Color(0.9f, 0.6f, 0.1f));
            Leaves(pivot, gold, 4, 0.18f, 0.25f);
            Part(pivot, PrimitiveType.Sphere, Mat("GildedCharm_Core", new Color(1f, 0.95f, 0.6f), new Color(1f, 0.85f, 0.4f)), Vector3.one * 0.16f, Vector3.zero, new Vector3(0f, 0f, -0.05f));
            Part(pivot, PrimitiveType.Cube, gold, new Vector3(0.05f, 0.3f, 0.05f), new Vector3(0f, 0f, -15f), new Vector3(0.04f, -0.27f, 0f));
        }

        /// <summary>Round leaves in an upright ring (the charm faces the tilted camera).</summary>
        private static void Leaves(Transform pivot, Material mat, int count, float distance, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = 90f + i * 360f / count;
                var offset = Quaternion.Euler(0f, 0f, angle) * Vector3.right * distance;
                Part(pivot, PrimitiveType.Sphere, mat, new Vector3(size, size, size * 0.4f), new Vector3(0f, 0f, angle), offset);
            }
        }

        private static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 scale, Vector3 euler, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>()); // the root's trigger is the only collider
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>URP Lit, matte, with emission so the placeholder reads on dark floors (no HDR bloom in the game).</summary>
        private static Material Mat(string name, Color color, Color emission)
        {
            string path = $"{MaterialFolder}/SH10_ExtraPickup_{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.35f);
            if (emission.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * 0.6f);
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
