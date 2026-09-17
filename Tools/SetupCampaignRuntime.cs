using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

public static class SetupCampaignRuntime
{
    public static void Run()
    {
        const string mainPath = "Assets/Scenes/Gameplay/Main_Gameplay.unity";
        var scene = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);
        var progressionObject = GameObject.Find("LevelProgression_Manager");
        if (progressionObject == null) throw new InvalidOperationException("LevelProgression_Manager not found");

        if (progressionObject.GetComponent<CampaignSceneConnector>() == null) progressionObject.AddComponent<CampaignSceneConnector>();
        if (progressionObject.GetComponent<CampaignSignatureSystem>() == null) progressionObject.AddComponent<CampaignSignatureSystem>();

        var spawnerObject = GameObject.Find("EnemySpawner_Manager");
        if (spawnerObject == null) throw new InvalidOperationException("EnemySpawner_Manager not found");
        var spawner = spawnerObject.GetComponent<EnemySpawner>();
        if (spawner == null) throw new InvalidOperationException("EnemySpawner missing");

        string[] enemyPaths = new string[10];
        string[] elitePaths = new string[10];
        string[] bossPaths = new string[10];
        for (int i = 0; i < 10; i++)
        {
            string n = (i + 1).ToString("00");
            enemyPaths[i] = $"Assets/Prefabs/Enemies/SH10_L{n}_Enemy.prefab";
            elitePaths[i] = $"Assets/Prefabs/Enemies/SH10_L{n}_Elite.prefab";
            bossPaths[i] = $"Assets/Prefabs/Bosses/SH10_L{n}_Boss.prefab";
        }

        var so = new SerializedObject(spawner);
        SetObjectList(so, "_stageEnemyPrefabs", enemyPaths);
        SetObjectList(so, "_stageElitePrefabs", elitePaths);
        SetObjectList(so, "_stageBossPrefabs", bossPaths);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(progressionObject);
        EditorUtility.SetDirty(spawnerObject);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupCampaignRuntime] Runtime campaign connector, signatures and level-specific prefabs wired.");
    }

    private static void SetObjectList(SerializedObject so, string propertyName, string[] paths)
    {
        var property = so.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException($"Missing serialized field {propertyName}");
        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (asset == null) throw new InvalidOperationException($"Missing asset {paths[i]}");
            property.GetArrayElementAtIndex(i).objectReferenceValue = asset;
        }
    }
}
