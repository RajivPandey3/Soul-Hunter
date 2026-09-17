using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    public sealed class CampaignSceneConnector : MonoBehaviour
    {
        private Scene _loadedScene;
        private bool _hasLoadedScene;
        private Coroutine _loadRoutine;
        private string _requestedSceneName;

        private void OnEnable()
        {
            var progression = LevelProgressionManager.Instance;
            progression.OnStageStarted += HandleStageStarted;
            HandleStageStarted(progression.CurrentStage);
        }

        private void OnDisable()
        {
            var progression = FindFirstObjectByType<LevelProgressionManager>();
            if (progression != null) progression.OnStageStarted -= HandleStageStarted;
            if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        }

        private void HandleStageStarted(int stage)
        {
            string sceneName = CampaignLevelCatalog.Get(stage).ShowcaseSceneName;
            if (_requestedSceneName == sceneName && ((_loadRoutine != null) || (_hasLoadedScene && _loadedScene.IsValid() && _loadedScene.isLoaded)))
                return;
            if (_loadRoutine != null) StopCoroutine(_loadRoutine);
            _requestedSceneName = sceneName;
            _loadRoutine = StartCoroutine(LoadShowcase(sceneName));
        }

        private IEnumerator LoadShowcase(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) yield break;
            if (_hasLoadedScene && _loadedScene.IsValid() && _loadedScene.isLoaded) yield return SceneManager.UnloadSceneAsync(_loadedScene);
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null) { Debug.LogError($"[CampaignSceneConnector] Could not load {sceneName}"); yield break; }
            yield return operation;
            _loadedScene = SceneManager.GetSceneByName(sceneName);
            _hasLoadedScene = _loadedScene.IsValid() && _loadedScene.isLoaded;
            if (!_hasLoadedScene) yield break;
            foreach (var root in _loadedScene.GetRootGameObjects())
                if (root.name == "Showcase Camera" || root.name == "Asset pack title") root.SetActive(false);
            // Playable campaign always uses Main_Gameplay's player-follow camera.
            // Disable every camera imported by a level scene, even if its object
            // was renamed or nested and therefore missed the legacy name check.
            foreach (var levelCamera in _loadedScene.GetRootGameObjects())
                foreach (var camera in levelCamera.GetComponentsInChildren<Camera>(true))
                    camera.enabled = false;
            Debug.Log($"[CampaignSceneConnector] Connected {_loadedScene.name} to playable campaign.");
        }
    }
}
