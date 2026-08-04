using UnityEngine;
using SoulHunter.Core.Services;

namespace SoulHunter.Core.Bootstrap
{
    /// <summary>
    /// Entry point of the Soul Hunter Framework.
    ///
    /// Responsibility:
    /// - Receive Unity startup.
    /// - Create root framework objects.
    /// - Start the bootstrap installation process.
    ///
    /// This class must never contain gameplay logic,
    /// service registration, scene loading, or business logic.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            StartFramework();
        }

        /// <summary>
        /// Starts the Soul Hunter Framework.
        /// </summary>
        private void StartFramework()
        {
            // ============================================================
            // LEARNING COMMENT:
            // GameServices is only a container.
            // It stores framework services.
            // It never initializes or manages their lifecycle.
            // ============================================================

            GameServices services = new GameServices();

            // ============================================================
            // LEARNING COMMENT:
            // BootstrapInstaller is responsible for installing
            // the entire framework.
            // ============================================================

            BootstrapInstaller installer = new BootstrapInstaller(services);

            installer.Install();
        }
    }
}