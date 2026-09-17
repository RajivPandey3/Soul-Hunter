using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Core.Services;

namespace SoulHunter.Core.Scenes
{
    /// <summary>Single owner of asynchronous scene transitions and loading progress.</summary>
    public class SceneService : IGameService, IDisposable
    {
        public event Action<float> OnLoadProgress;
        public event Action OnLoadComplete;
        public bool IsLoading { get; private set; }
        private bool disposed;
        private string activeScene;
        private string pendingScene;
        public void Initialize() { disposed = false; }

        public async void LoadSceneAsync(string sceneName)
        {
            // Unity button callbacks require void; keep exceptions at this boundary.
            if (disposed) return;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneService] Scene is not available in Build Settings: " + sceneName);
                return;
            }
            if (IsLoading)
            {
                // Bootstrap.Awake may request Loading before its own activation has completed.
                if (sceneName != activeScene) pendingScene = sceneName;
                return;
            }
            IsLoading = true;
            activeScene = sceneName;
            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null) return;
                while (!operation.isDone)
                {
                    if (disposed) return;
                    OnLoadProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                    await Task.Yield();
                }
                if (!disposed)
                {
                    OnLoadProgress?.Invoke(1f);
                    OnLoadComplete?.Invoke();
                }
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                IsLoading = false;
                activeScene = null;
                string next = pendingScene;
                pendingScene = null;
                if (!disposed && next != null) LoadSceneAsync(next);
            }
        }

        public void Dispose()
        {
            disposed = true;
            pendingScene = null;
            OnLoadProgress = null;
            OnLoadComplete = null;
        }
    }
}
