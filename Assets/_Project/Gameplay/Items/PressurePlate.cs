using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS pressure plate (vampire.survivors.wiki, Gallo Tower / Dairy Plant / Boss Rash): a flat plate on the ground;
    /// stepping on it starts a map event, then it waits at least 20 s before it can fire again. It never blocks the
    /// player. Soul Hunter's Rune Plate starts one of this stage's formations (swarm, wall or closing ring) with the
    /// stage's own enemies; its runes glow when it is ready and dim while it recharges.
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        public const float DefaultCooldownSeconds = 20f;
        public const int EventCount = 16;

        /// <summary>Seconds before the plate can fire again (VS: 20 s or more).</summary>
        public float CooldownSeconds = DefaultCooldownSeconds;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly List<(Renderer renderer, Color emission, Color baseColor)> _runes = new();
        private MaterialPropertyBlock _block;
        private float _readyAt;
        private bool _dimmed;

        public bool IsReady => Time.time >= _readyAt;
        public int TimesTriggered { get; private set; }

        /// <summary>Raised when the plate fires, with the event it started.</summary>
        public event System.Action<PressurePlate, SoulHunter.Gameplay.Data.StageEventType> Triggered;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var m = r.sharedMaterial;
                if (m == null || !m.name.Contains("Rune")) continue;
                _runes.Add((r, m.HasProperty(EmissionId) ? m.GetColor(EmissionId) : Color.black,
                               m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white));
            }
        }

        private void OnEnable()
        {
            _readyAt = 0f;
            SetDimmed(false);
        }

        private void Update()
        {
            if (_dimmed && IsReady) SetDimmed(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            Step();
        }

        /// <summary>The player steps on it: fires if ready. Returns whether an event started.</summary>
        public bool Step()
        {
            if (!IsReady) return false;
            var spawner = FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
            if (spawner == null) return false;
            var type = (SoulHunter.Gameplay.Data.StageEventType)Random.Range(0, 3);
            var spawned = spawner.TriggerStageEvent(type, EventCount);
            if (spawned.Count == 0) return false; // boss fight or stage transition: nothing to start
            _readyAt = Time.time + Mathf.Max(0f, CooldownSeconds);
            TimesTriggered++;
            SetDimmed(true);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, transform.position + Vector3.up * 0.2f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.BossWarningSound, 0.55f);
            Triggered?.Invoke(this, type);
            return true;
        }

        private void SetDimmed(bool dimmed)
        {
            _dimmed = dimmed;
            foreach (var (r, emission, baseColor) in _runes)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, dimmed ? emission * 0.08f : emission);
                _block.SetColor(BaseColorId, dimmed ? baseColor * 0.35f : baseColor);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
