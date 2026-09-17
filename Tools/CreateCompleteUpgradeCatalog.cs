var folder = "Assets/Resources/Upgrades";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Upgrades")) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");
var weaponTypes = new SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType[]
{
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MagicWand, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Whip, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Garlic,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Axe, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Bible, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Cross,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.LightningRing, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.FireWand, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SantaWater,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Knife, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Runetracer, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Pentagram,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.ClockLancet, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Bone,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Peachone, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SongOfMana, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.CherryBomb,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Gun
};
var passiveTypes = new SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType[]
{
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.PlayerSpeed, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MaxHealth,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.EmptyTome, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Spinach, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Bracer,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Candelabrador, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Spellbinder, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Duplicator,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Armor, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Pummarola, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Attractorb,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Clover, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Crown, SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SkullOManiac,
    SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Tiragisu
};
var all = new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.UpgradeData>();
var names = new System.Collections.Generic.Dictionary<SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType,string>();
foreach (var type in weaponTypes) names[type] = type.ToString();
foreach (var type in passiveTypes) names[type] = type.ToString();
foreach (var type in weaponTypes.Concat(passiveTypes))
{
    for (int level = 1; level <= 8; level++)
    {
        string path = $"{folder}/{type}_Lv{level}.asset";
        var data = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.UpgradeData>(path);
        if (data == null) { data = UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Combat.UpgradeData>(); UnityEditor.AssetDatabase.CreateAsset(data, path); }
        data.UpgradeName = $"{names[type]} Lv.{level}"; data.Description = $"{names[type]} upgrade level {level}."; data.Type = type; data.Level = level;
        UnityEditor.EditorUtility.SetDirty(data); all.Add(data);
    }
}
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.GameContentCatalog>("Assets/Resources/GameContent.asset");
if (catalog == null) throw new System.Exception("GameContent.asset not found");
catalog.Upgrades = all.ToArray(); UnityEditor.EditorUtility.SetDirty(catalog); UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh();
return new { upgrades = all.Count, weaponTypes = weaponTypes.Length, passiveTypes = passiveTypes.Length };
