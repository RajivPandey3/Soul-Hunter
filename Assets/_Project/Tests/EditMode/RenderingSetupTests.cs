using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: Game 3D hai, isliye 3D Universal renderer chahiye (2D renderer 3D light/shadow nahi dikhata),
    /// models shadow daalen, aur har stage ki zameen textured, lit material se bane (owner request 2026-09-25:
    /// "ground attractive nahi hai").
    /// </summary>
    public class RenderingSetupTests
    {
        [Test]
        public void EveryDevice_StartsOnUltra()
        {
            // Owner decision (2026-10-18): "quality should be default ultra in all devices". Android, iPhone, tvOS and WebGL
            // started on Medium or lower before.
            int ultra = System.Array.IndexOf(QualitySettings.names, "Ultra");
            Assert.That(ultra, Is.GreaterThanOrEqualTo(0), "The project has an Ultra level.");
            string text = System.IO.File.ReadAllText("ProjectSettings/QualitySettings.asset");
            int start = text.IndexOf("m_PerPlatformDefaultQuality:", System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            var platforms = text.Substring(start).Split('\n').Select(line => line.TrimEnd('\r'))
                .Skip(1).TakeWhile(line => line.StartsWith("    ")).ToList();
            Assert.That(platforms, Is.Not.Empty);
            foreach (string line in platforms)
                Assert.That(line.Trim(), Does.EndWith(": " + ultra), "Every device starts on Ultra: " + line.Trim());
        }

        [Test]
        public void Pipeline_UsesThe3DUniversalRenderer()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.IsNotNull(pipeline, "No render pipeline asset.");
            var list = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            int index = new SerializedObject(pipeline).FindProperty("m_DefaultRendererIndex").intValue;
            var renderer = list.GetArrayElementAtIndex(index).objectReferenceValue;
            Assert.That(renderer.GetType().Name, Is.EqualTo("UniversalRendererData"),
                "The 2D Renderer ignores 3D lights and shadows.");
        }

        [Test]
        public void FacetedShader_IsLit_AndCastsShadows()
        {
            var shader = Shader.Find("SoulHunter/Review/FacetedPreview");
            Assert.IsNotNull(shader);
            var material = new Material(shader);
            try
            {
                Assert.That(material.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0), "Models must cast shadows.");
                Assert.That(material.FindPass("ForwardLit"), Is.GreaterThanOrEqualTo(0), "Models must be lit by the stage light.");
                Assert.That(material.HasProperty("_Color"), "Existing materials keep their colour property.");
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void StageFloors_AreTexturedLitMaterials_AndObstaclesCastShadows()
        {
            foreach (var floor in GameContentCatalog.Load().StageFloors)
            {
                var renderers = floor.GetComponentsInChildren<MeshRenderer>(true);
                var ground = renderers.Single(r => r.name == floor.name);
                var obstacles = renderers.Single(r => r.name == floor.name + "_Obstacles");
                // Draw calls (2026-10-15): one merged material (FloorMeshMerger), lit like URP Lit, its detail textures in
                // one texture array and each part's slice baked into UV1.w.
                foreach (var material in ground.sharedMaterials.Concat(obstacles.sharedMaterials))
                {
                    Assert.That(material.shader.name, Is.EqualTo("SoulHunter/FloorMerged"), $"{floor.name}: {material.name}");
                    Assert.That(material.GetTexture("_GroundArray"), Is.InstanceOf<Texture2DArray>(), $"{floor.name}: {material.name}");
                }
                var parts = new System.Collections.Generic.List<Vector4>();
                ground.GetComponent<MeshFilter>().sharedMesh.GetUVs(1, parts);
                Assert.That(parts.Select(p => Mathf.RoundToInt(p.w)).Where(slice => slice > 0).Distinct().Count(), Is.GreaterThanOrEqualTo(1),
                    $"{floor.name}: the ground and its paths use detail textures.");
                Assert.That(ground.receiveShadows, $"{floor.name} ground receives shadows.");
                Assert.That(obstacles.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On), $"{floor.name} obstacles cast shadows.");
                var mesh = ground.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.uv, Is.Not.Empty, $"{floor.name} ground has UVs for its textures.");
            }
        }

        [Test]
        public void GroundTextures_Tile()
        {
            foreach (string guid in AssetDatabase.FindAssets("SH10_Tex_ t:Texture2D", new[] { "Assets/Art/Textures/Ground" }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), importer.assetPath);
                Assert.That(importer.mipmapEnabled, importer.assetPath);
            }
            Assert.That(AssetDatabase.FindAssets("SH10_Tex_ t:Texture2D", new[] { "Assets/Art/Textures/Ground" }).Length, Is.GreaterThanOrEqualTo(12));
        }
    }
}
