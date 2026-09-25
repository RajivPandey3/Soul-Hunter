#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Audio;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Rebuilds Assets/Resources/AudioManager.prefab with Unity's own prefab writer. The previous file was
    /// hand-written YAML that Unity imported as a DefaultAsset, so Resources.Load returned null, no
    /// AudioManager was ever created and the game had no sound. Keeps the asset's GUID.
    /// Menu: Soul Hunter/Content/Rebuild Audio Manager Prefab, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.AudioManagerPrefabBuilder.Build -quit
    /// </summary>
    public static class AudioManagerPrefabBuilder
    {
        public const string PrefabPath = "Assets/Resources/AudioManager.prefab";
        private const string SfxFolder = "Assets/_Project/Audio/SFX";

        [MenuItem("Soul Hunter/Content/Rebuild Audio Manager Prefab")]
        public static void Build()
        {
            var root = new GameObject("AudioManager");
            try
            {
                var audio = root.AddComponent<AudioManager>();
                audio.GemPickupSound = Clip("GemPickup");
                audio.LevelUpSound = Clip("LevelUp");
                audio.EnemyHitSound = Clip("EnemyHit");
                audio.ChestOpenSound = Clip("ChestOpen");
                audio.HealSound = Clip("Heal");
                audio.MasterVolume = 0.8f;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioManagerPrefabBuilder] Rebuilt {PrefabPath}: {AssetDatabase.GetMainAssetTypeAtPath(PrefabPath)?.Name}");
        }

        private static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{name}.wav");
            if (clip == null) Debug.LogError($"[AudioManagerPrefabBuilder] Missing {SfxFolder}/{name}.wav");
            return clip;
        }
    }
}
#endif
