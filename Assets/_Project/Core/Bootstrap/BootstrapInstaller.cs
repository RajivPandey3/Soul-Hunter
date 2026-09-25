using SoulHunter.Core.Services;
using UnityEngine;

namespace SoulHunter.Core.Bootstrap
{
    /// <summary>
    /// Installs and initializes the core framework.
    /// </summary>
    public sealed class BootstrapInstaller
    {
        private readonly GameServices gameServices;

        /// <summary>
        /// Creates a new installer.
        /// </summary>
        /// <param name="gameServices">
        /// Service container that stores all framework services.
        /// </param>
        public BootstrapInstaller(GameServices gameServices)
        {
            this.gameServices = gameServices;
        }

        /// <summary>
        /// Registers and initializes every core service.
        /// </summary>
        public void Install()
        {
            RegisterServices();

            InitializeServices();

            // Remapped controls (Options) are stored in the save, which loads after InputService starts.
            if (gameServices.TryGet(out SoulHunter.Core.Input.InputService input) && gameServices.TryGet(out SoulHunter.Core.Persistence.SaveService save))
                input.LoadBindingOverrides(save.CurrentData != null ? save.CurrentData.InputBindingOverrides : null);

            Debug.Log("[BootstrapInstaller] Framework Installed.");
        }

        /// <summary>
        /// Registers all core framework services.
        /// </summary>
        private void RegisterServices()
        {
            // Register Event Bus first
            var eventBus = new SoulHunter.Core.Events.EventBus();
            gameServices.Register(eventBus);
            
            // Register Input Service with injected Event Bus
            gameServices.Register(new SoulHunter.Core.Input.InputService(eventBus));
            
            // Register Save Service
            gameServices.Register(new SoulHunter.Core.Persistence.SaveService());
            
            // Register Scene Service
            gameServices.Register(new SoulHunter.Core.Scenes.SceneService());
            
            // Register Economy Service
            gameServices.Register(new SoulHunter.Core.Services.EconomyService());
            
            // Future:
            // gameServices.Register(new LoggingService());
        }

        /// <summary>
        /// Initializes every registered service.
        /// </summary>
        private void InitializeServices()
        {
            gameServices.InitializeAll();
        }
    }
}
