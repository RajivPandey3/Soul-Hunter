using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>Restarts a pooled object's particle systems cleanly when it is reused.</summary>
    public sealed class ParticleReset : MonoBehaviour
    {
        private ParticleSystem[] _systems;

        private void OnEnable()
        {
            if (_systems == null) _systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in _systems)
            {
                ps.Clear(false);
                ps.Play(false);
            }
        }
    }
}
