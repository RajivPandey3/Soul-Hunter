using UnityEngine;
using SoulHunter.Core.Services;
using SoulHunter.Core.Scenes;

namespace SoulHunter.Core.Bootstrap
{
    /// <summary>Sequences startup and owns service lifetime across scene transitions.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _loadMenuOnStartup = true;
        private static GameBootstrap owner;
        private GameServices services;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            GameServices.Instance?.Dispose();
            owner = null;
        }

        private void Awake()
        {
            // A return to Bootstrap reuses the live framework, rather than enabling input twice.
            if (owner != null && owner != this)
            {
                if (_loadMenuOnStartup) owner.services.Get<SceneService>().LoadSceneAsync("Loading");
                return;
            }

            owner = this;
            DontDestroyOnLoad(gameObject);
            services = new GameServices();
            var installer = new BootstrapInstaller(services);
            installer.Install();
            if (_loadMenuOnStartup) services.Get<SceneService>().LoadSceneAsync("Loading");
        }

        private void OnApplicationQuit() { services?.Dispose(); }
        private void OnDestroy()
        {
            if (owner != this) return;
            services?.Dispose();
            owner = null;
        }
    }
}
