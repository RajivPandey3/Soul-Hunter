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
        /// <summary>Rune parts of a merged plate (PropMeshMerger, one SoulHunter/PropMerged material): the mask A part,
        /// with its own material's colour and glow. Only that part is dimmed; the stone baked into the same mesh is not.</summary>
        private readonly List<(SoulHunter.Gameplay.VFX.PropMergedLook.Part part, Color emission, Color baseColor)> _mergedRunes = new();
        private MaterialPropertyBlock _block;
        private float _readyAt;
        private bool _dimmed;

        /// <summary>
        /// VS (wiki Enemies, Traps): "The trap events have a global cooldown, which depends on Luck: effectiveCooldown =
        /// trapCooldown x totalLuck". One cooldown for every plate on the stage; it starts again when a stage lays its plates.
        /// </summary>
        public static float GlobalReadyAt { get; private set; }
        public static void ResetGlobalCooldown() => GlobalReadyAt = 0f;
        /// <summary>The trap cooldown for a player with <paramref name="totalLuck"/> (1 = no Luck bonus).</summary>
        public static float EffectiveCooldown(float trapCooldown, float totalLuck) => Mathf.Max(0f, trapCooldown) * Mathf.Max(0f, totalLuck);

        public bool IsReady => Time.time >= Mathf.Max(_readyAt, GlobalReadyAt);
        public int TimesTriggered { get; private set; }

        /// <summary>Raised when the plate fires, with the event it started.</summary>
        public event System.Action<PressurePlate, SoulHunter.Gameplay.Data.StageEventType> Triggered;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.TryGetComponent(out SoulHunter.Gameplay.VFX.PropMergedLook _))
                {
                    for (int i = 0; SoulHunter.Gameplay.VFX.PropMergedLook.TryFind(r, "Rune", out var part, i); i++)
                    {
                        var src = part.Source;
                        _mergedRunes.Add((part, src.HasProperty(EmissionId) ? src.GetColor(EmissionId) : Color.black,
                                                src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : Color.white));
                    }
                    continue;
                }
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
            else if (!_dimmed && !IsReady) SetDimmed(true); // another plate fired: all wait together
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
            var stats = PlayerController.Instance != null ? PlayerController.Instance.Stats : null;
            GlobalReadyAt = Time.time + EffectiveCooldown(CooldownSeconds, stats != null ? stats.Luck : 1f);
            _readyAt = GlobalReadyAt;
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
            foreach (var (part, emission, baseColor) in _mergedRunes)
                SoulHunter.Gameplay.VFX.PropMergedLook.Set(part, _block,
                    dimmed ? baseColor * 0.35f : baseColor, dimmed ? emission * 0.08f : emission);
        }

        /// <summary>Rune parts found on this plate (ordinary rune renderers plus merged rune masks); for tests.</summary>
        public int RunePartCount => _runes.Count + _mergedRunes.Count;
    }
}
