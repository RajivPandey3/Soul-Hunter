#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Switches the pipeline from the URP 2D Renderer to the 3D Universal (Forward) Renderer (2026-09-25). The game is
    /// 3D (hero, enemy and stage models, a directional light), but the 2D Renderer ignores 3D lights and shadows, so
    /// the stage light's shadows never showed and URP Lit materials could not be lit. Only the unused URP 2D template
    /// test scene has a 2D light. Renderer2D.asset stays on disk, unused.
    /// CLI: -executeMethod SoulHunter.Editor.Content.RendererSetup.UseUniversalRenderer -quit
    /// </summary>
    public static class RendererSetup
    {
        public const string PipelinePath = "Assets/_Project/Settings/Graphics/UniversalRP.asset";
        public const string RendererPath = "Assets/_Project/Settings/Graphics/UniversalRenderer.asset";
        /// <summary>Covers the whole tilted view (the camera sits 30 units back); farther shadows are never seen.</summary>
        public const float ShadowDistance = 45f;

        [MenuItem("Soul Hunter/Content/Use 3D Universal Renderer")]
        public static void UseUniversalRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null) throw new System.IO.FileNotFoundException("URP asset missing", PipelinePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            renderer.renderingMode = RenderingMode.Forward;
            // No post-processing is used, so skip the always-on intermediate texture and the HDR buffer: both cost
            // bandwidth the low-end reference GPU (UHD 620) needs for the horde.
            renderer.intermediateTextureMode = IntermediateTextureMode.Auto;
            EditorUtility.SetDirty(renderer);
            pipeline.supportsHDR = false;
            pipeline.shadowDistance = ShadowDistance;

            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RendererSetup] {PipelinePath} now renders with {RendererPath} ({renderer.renderingMode}).");
        }
    }
}
#endif
