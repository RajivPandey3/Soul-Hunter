var scenes = UnityEditor.EditorBuildSettings.scenes.ToList();
for (int i = 1; i <= 10; i++)
{
    string path = $"Assets/Scenes/Levels/SH10_L{i:00}_Showcase.unity";
    if (!scenes.Any(s => s.path == path)) scenes.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
}
UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
return new { total = UnityEditor.EditorBuildSettings.scenes.Length, added = 10 };
