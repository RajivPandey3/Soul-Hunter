#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Imports the Power Up shop icons made by ArtSource/Generators/build_powerup_icons.py (owner, 2026-10-07: "icons bhi
    /// banao") as sprites and lists them in GameContent.PowerUpIcons. Menu Soul Hunter/Content/Import Power Up Icons;
    /// batch: -executeMethod SoulHunter.Editor.Content.PowerUpIconImporter.ImportFromCommandLine.
    /// </summary>
    public static class PowerUpIconImporter
    {
        public static readonly PowerUpId[] Made =
        {
            PowerUpId.Area, PowerUpId.Greed, PowerUpId.Omni, PowerUpId.Charm, PowerUpId.Defang,
            PowerUpId.Reroll, PowerUpId.Skip, PowerUpId.Banish, PowerUpId.Preserve,
        };

        public static string IconPath(PowerUpId id) => $"Assets/Art/UI/SH10_PowerUp_{id}.png";

        [MenuItem("Soul Hunter/Content/Import Power Up Icons")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var entries = new List<GameContentCatalog.PowerUpIcon>();
            foreach (var id in Made)
            {
                string path = IconPath(id);
                if (!File.Exists(path)) { Debug.LogWarning($"[PowerUpIconImporter] {path} missing: run build_powerup_icons.py."); continue; }
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (sprite != null) entries.Add(new GameContentCatalog.PowerUpIcon { PowerUp = id, Icon = sprite });
            }
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
            if (catalog == null) { Debug.LogError("[PowerUpIconImporter] GameContent.asset not found."); return; }
            catalog.PowerUpIcons = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PowerUpIconImporter] {entries.Count} Power Up icons in GameContent.");
        }

        public static void ImportFromCommandLine()
        {
            int code = 0;
            try { Import(); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }
    }
}
#endif
