using System;
using System.Collections.Generic;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Ordered registry and lifecycle owner for core services.
    /// Instance is a compatibility bridge for existing scene clients, not dependency injection.
    /// </summary>
    public sealed class GameServices : IDisposable
    {
        public static GameServices Instance { get; private set; }
        private readonly Dictionary<Type, IGameService> services = new();
        private readonly List<IGameService> orderedServices = new();
        private bool initialized;
        private bool disposed;

        public GameServices()
        {
            // Learning Comment: Restart must release old input actions before replacing services.
            Instance?.Dispose();
            Instance = this;
        }

        public void Register<T>(T service) where T : IGameService
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameServices));
            if (initialized) throw new InvalidOperationException("Register services before initialization.");
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (services.ContainsKey(typeof(T))) throw new InvalidOperationException("Service already registered: " + typeof(T).Name);
            services.Add(typeof(T), service);
            orderedServices.Add(service);
        }

        public T Get<T>() where T : IGameService
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameServices));
            return (T)services[typeof(T)];
        }

        public bool TryGet<T>(out T service) where T : IGameService
        {
            if (disposed) { service = default; return false; }
            if (services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }
            service = default;
            return false;
        }

        public void InitializeAll()
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameServices));
            if (initialized) return;
            try
            {
                foreach (var service in orderedServices) service.Initialize();
                initialized = true;
            }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Reverse dependency order: consumers stop before their providers.
            List<Exception> errors = null;
            try
            {
                for (int i = orderedServices.Count - 1; i >= 0; i--)
                {
                    try { if (orderedServices[i] is IDisposable resource) resource.Dispose(); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                }
            }
            finally
            {
                orderedServices.Clear();
                services.Clear();
                if (ReferenceEquals(Instance, this)) Instance = null;
            }
            if (errors != null) throw new AggregateException("Service cleanup failed.", errors);
        }
    }
}
