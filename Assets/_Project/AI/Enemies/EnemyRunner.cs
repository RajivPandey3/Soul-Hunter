using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Steps the whole horde from one Update and one FixedUpdate (created on first use, one per loaded game).
    /// 60 FPS pass (2026-10-14): about 300 enemies with their own Update and FixedUpdate cost about 2.5 ms a frame on
    /// the reference laptop, most of it engine calls each enemy repeated (the clock, the player's position).
    /// </summary>
    public sealed class EnemyRunner : MonoBehaviour
    {
        private static EnemyRunner _instance;

        internal static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("EnemyRunner") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnemyRunner>();
        }

        private void Update() => EnemyController.StepAll(false);

        private void FixedUpdate() => EnemyController.StepAll(true);

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so the runner of the last play session must not stay referenced.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;
    }
}
