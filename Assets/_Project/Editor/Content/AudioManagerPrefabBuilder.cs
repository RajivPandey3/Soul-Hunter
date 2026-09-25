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
                audio.EnemyDeathSound = Clip("EnemyDeath");
                audio.PlayerHurtSound = Clip("PlayerHurt");
                audio.GameOverSound = Clip("GameOver");
                audio.StageClearSound = Clip("StageClear");
                audio.BossWarningSound = Clip("BossWarning");
                audio.UIClickSound = Clip("UIClick");
                audio.UIConfirmSound = Clip("UIConfirm");
                audio.UnlockSound = Clip("Unlock");
                audio.ShootSound = Clip("Shoot");
                audio.SlashSound = Clip("Slash");
                audio.MagicSound = Clip("Magic");
                audio.ExplosionSound = Clip("Explosion");
                audio.MenuMusic = Music("Music_Menu");
                audio.StageMusic = new AudioClip[10];
                for (int stage = 1; stage <= 10; stage++) audio.StageMusic[stage - 1] = Music($"Music_Stage{stage:00}");
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

        /// <summary>CC0 recordings (install_cc0_audio.py) win over the older synthesised clips, which stay as a fallback.</summary>
        public const string Cc0Folder = "Assets/_Project/Audio/CC0";

        /// <summary>Music streams (a few MB each); effects are decompressed on load so a horde can play them at once.</summary>
        private static void Configure(string path, bool music)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            var settings = importer.defaultSampleSettings;
            var load = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            if (settings.loadType == load && importer.loadInBackground == music) return;
            settings.loadType = load;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.6f : 0.8f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
        }

        private static AudioClip Music(string name)
        {
            Configure($"{Cc0Folder}/Music/{name}.ogg", music: true);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Cc0Folder}/Music/{name}.ogg")
                       ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/Music/{name}.wav");
            if (clip == null) Debug.LogError($"[AudioManagerPrefabBuilder] Missing music {name}.wav (run Generate Retro Audio first)");
            return clip;
        }

        private static AudioClip Clip(string name)
        {
            Configure($"{Cc0Folder}/SFX/{name}.ogg", music: false);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Cc0Folder}/SFX/{name}.ogg")
                       ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{name}.wav");
            if (clip == null) Debug.LogError($"[AudioManagerPrefabBuilder] Missing {SfxFolder}/{name}.wav");
            return clip;
        }
    }
}
#endif
