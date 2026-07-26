using UnityEngine;

/// <summary>
/// Main entry point of the game framework.
/// Responsible for initializing core systems.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        InitializeFramework();
    }


    private void InitializeFramework()
    {
        Debug.Log("Soul Hunter Initialized");
    }
}