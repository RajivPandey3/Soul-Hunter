using System.Collections.Generic;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Learning Comment:
    /// Architecture Blueprint (service-locator.md) ke mutabiq ye GameServices class hai.
    /// Ye class ek "Service Locator" ki tarah kaam karti hai jo saari core services (jaise EventBus) ko hold karti hai.
    /// Iska fayda ye hai ki humein Singleton pattern (static classes) use nahi karna padta, 
    /// jisse system decoupled aur Hybrid-ready rehta hai.
    /// </summary>
    public class GameServices
    {
        public static GameServices Instance { get; private set; }

        public GameServices()
        {
            Instance = this;
        }

        // Dictionary jo sabhi services ko unke Type ke hisaab se store karti hai
        private readonly Dictionary<System.Type, IGameService> services = new();

        /// <summary>
        /// Nayi service ko register karna.
        /// (e.g., gameServices.Register(new EventBus());)
        /// </summary>
        public void Register<T>(T service) where T : IGameService
        {
            services[typeof(T)] = service;
        }

        /// <summary>
        /// Kisi registered service ko uske Type se fetch karna.
        /// </summary>
        public T Get<T>() where T : IGameService
        {
            return (T)services[typeof(T)];
        }

        /// <summary>
        /// Bootstrap ke time par saari registered services ko ek sath initialize karna.
        /// </summary>
        public void InitializeAll()
        {
            foreach (var service in services.Values)
            {
                service.Initialize();
            }
        }
    }
}