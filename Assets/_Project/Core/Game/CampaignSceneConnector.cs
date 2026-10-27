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

        /// <summary>
        /// Off since 2026-10-27 (owner: "jhatka bhi theek karo, 60 constant"): every object a showcase scene brings in is
        /// hidden in play (its SH10_L* models, its camera and its title; an additive scene's lighting never applies), so
        /// loading one at each stage start only cost a frame of 30-85 ms (scene activation, physics and GPU uploads) for
        /// nothing on screen. True loads them again (the old behaviour).
        /// </summary>
        public static bool LoadShowcaseScenes;

        private void HandleStageStarted(int stage)
        {
            if (!LoadShowcaseScenes)
            {
                if (_hasLoadedScene && _loadedScene.IsValid() && _loadedScene.isLoaded) SceneManager.UnloadSceneAsync(_loadedScene);
                _hasLoadedScene = false;
                return;
            }
            string sceneName = CampaignLevelCatalog.Get(stage).ShowcaseSceneName;
            if (_requestedSceneName == sceneName && ((_loadRoutine != null) || (_hasLoadedScene && _loadedScene.IsValid() && _loadedScene.isLoaded)))
                return;
            if (_loadRoutine != null) StopCoroutine(_loadRoutine);
            _requestedSceneName = sceneName;
            _loadRoutine = StartCoroutine(LoadShowcase(sceneName));
        }

        /// <summary>
        /// Showcase objects that must not appear in the playable campaign: all of its SH10_Lxx_* models (props
        /// and the still Enemy/Elite/Boss display models, which stood at the spawn point like enemies that never
        /// move or die). Each stage floor carries its own obstacles (owner, 2026-09-25: remove the old decor).
        /// </summary>
        public static bool IsHiddenInPlay(string objectName) => objectName.StartsWith("SH10_L");

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
            // InfiniteMap lays the stage floor, with its obstacles, everywhere; the showcase's
            // props and display models would stand on top of it without blocking anything.
            foreach (var root in _loadedScene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    if (IsHiddenInPlay(node.name)) node.gameObject.SetActive(false);
            Debug.Log($"[CampaignSceneConnector] Connected {_loadedScene.name} to playable campaign.");
        }
    }
}
