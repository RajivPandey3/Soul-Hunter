using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>Plays a feedback effect where a pooled object ends (Cherry Bomb explodes when its flight ends).</summary>
    public sealed class SpawnVfxOnDisable : MonoBehaviour
    {
        public VfxKind Kind = VfxKind.Explosion;
        private bool _armed;

        private void OnEnable() => _armed = true;

        private void OnDisable()
        {
            if (!_armed || !Application.isPlaying || !gameObject.scene.isLoaded) return;
            _armed = false;
            // Never create the manager from OnDisable (scene teardown disables everything); play only when it exists.
            var manager = VFXPoolManager.Instance;
            if (manager == null || !manager.isActiveAndEnabled) return;
            VFXPoolManager.Play(Kind, transform.position);
        }
    }
}
