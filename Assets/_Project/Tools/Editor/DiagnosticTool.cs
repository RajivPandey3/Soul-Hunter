using UnityEngine;
using UnityEditor;
using System.Linq;

public class DiagnosticTool
{
    public static void Run()
    {
        var players = Object.FindObjectsByType<SoulHunter.Gameplay.Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("DIAGNOSTIC: Found " + players.Length + " Players.");
        foreach (var p in players) Debug.Log("Player: " + GetPath(p.gameObject));

        var spawners = Object.FindObjectsByType<SoulHunter.Gameplay.AI.EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("DIAGNOSTIC: Found " + spawners.Length + " EnemySpawners.");
        foreach (var s in spawners) Debug.Log("Spawner: " + GetPath(s.gameObject));
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
