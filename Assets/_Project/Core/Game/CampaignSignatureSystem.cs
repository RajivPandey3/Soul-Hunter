using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    public sealed class CampaignSignatureSystem : MonoBehaviour
    {
        [SerializeField] private float _hazardRadius = 10f;
        [SerializeField] private float _arenaStartRadius = 35f;
        [SerializeField] private float _arenaEndRadius = 10f;
        [SerializeField] private float _boundaryDamagePerSecond = 10f;
        [SerializeField] private float _soulTideInterval = 15f;
        [SerializeField] private int _soulTideDamage = 3;
        [SerializeField] private float _corePulseInterval = 20f;
        [SerializeField] private int _corePulseDamage = 5;
        private readonly List<GameObject> _runtimeHazards = new();
        private PlayerController _player;
        private HealthController _playerHealth;
        private CampaignLevelDefinition _level;
        private float _stageTime;
        private float _slowTimer;
        private float _slowDuration;
        private float _originalAmbientIntensity;
        private bool _originalFog;
        private Color _originalFogColor;
        private float _originalFogDensity;
        private float _soulTideTimer;
        private float _corePulseTimer;
        private bool _subscribed;

        private void Awake()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _playerHealth = _player != null ? _player.GetComponent<HealthController>() : null;
            _originalAmbientIntensity = RenderSettings.ambientIntensity;
            _originalFog = RenderSettings.fog;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;
        }

        private void OnEnable()
{
    TrySubscribeToProgression();
}

private void Start()
{
    TrySubscribeToProgression();
}

private void TrySubscribeToProgression()
{
    if (_subscribed) return;

    var progression = FindFirstObjectByType<LevelProgressionManager>();
    if (progression == null) return;

    progression.OnStageStarted += HandleStageStarted;
    _subscribed = true;

    HandleStageStarted(progression.CurrentStage);
}

        private void OnDisable()
{
    var progression = FindFirstObjectByType<LevelProgressionManager>();

    if (_subscribed && progression != null)
        progression.OnStageStarted -= HandleStageStarted;

    _subscribed = false;

    RestoreEnvironment();
    ClearHazards();
}

        private void HandleStageStarted(int stage)
        {
            _level = CampaignLevelCatalog.Get(stage);
            // Diagnostic: confirm HandleStageStarted called
            Debug.Log("[Diagnostic] HandleStageStarted invoked for stage " + stage);
            _stageTime = 0f; _slowTimer = 0f; _slowDuration = 0f;
            _soulTideTimer = _soulTideInterval;
            _corePulseTimer = _corePulseInterval;
            // Har stage start par stale modifier clear. Sirf Frozen Peaks ka
            // authored signature Update() mein 0.55 multiplier lagata hai.
            if (_player != null) _player.SetCampaignMovementMultiplier(1f);
            ClearHazards(); RestoreEnvironment();
            if (_level.Signature == CampaignSignature.DarkForest)
            {
                RenderSettings.fog = true; RenderSettings.fogColor = Color.black;
                RenderSettings.fogDensity = 0.045f; RenderSettings.ambientIntensity = 0.22f;
            }
            switch (_level.Signature)
            {
                case CampaignSignature.ForgottenVillage:
                    Debug.Log("[CampaignSignatureSystem] Spawning Wandering Merchant");
                    StartCoroutine(SpawnMerchantOnNewGround());
                    break;
                case CampaignSignature.RuinedCastle: CreateHazards(CampaignHazard.HazardType.Trap, 14, 7); break;
                case CampaignSignature.CrimsonSwamp: CreateHazards(CampaignHazard.HazardType.Poison, 6, 9); break;
            }
            Debug.Log($"[CampaignSignatureSystem] Active signature: {_level.Signature}");
        }

        /// <summary>
        /// VS rule: the merchant waits close to where the stage starts. The first spot around the player
        /// that no stage obstacle covers is used, so the player can always walk up to it.
        /// </summary>
        public static Vector3 FindMerchantSpot(Vector3 playerPosition, float distance = 7f)
        {
            int obstacles = LayerMask.GetMask("Obstacle");
            for (int i = 0; i < 16; i++)
            {
                float angle = 45f + i * 22.5f;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                Vector3 spot = playerPosition + offset;
                if (obstacles == 0 || !UnityEngine.Physics.CheckSphere(spot + Vector3.up, 1.5f, obstacles, QueryTriggerInteraction.Collide))
                    return spot;
            }
            return playerPosition + new Vector3(distance, 0f, 0f);
        }

        /// <summary>
        /// InfiniteMap swaps the stage floor on the same OnStageStarted event, in no fixed order. Wait one frame and
        /// sync physics so the merchant's spot is checked against this stage's obstacles, not the last stage's.
        /// </summary>
        private System.Collections.IEnumerator SpawnMerchantOnNewGround()
        {
            yield return null;
            UnityEngine.Physics.SyncTransforms();
            SpawnMerchant();
        }

        private void SpawnMerchant()
        {
            if (_player == null) return;
            Vector3 position = FindMerchantSpot(_player.transform.position);
            // Same height as the hero and the horde: their models stand at their pivot, on the player's plane.
            position.y = _player.transform.position.y;
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            GameObject merchant;
            if (catalog != null && catalog.WanderingMerchant != null)
            {
                // Owner-approved model (2026-09-26) with its trigger and shop, facing the player.
                Vector3 toPlayer = _player.transform.position - position;
                toPlayer.y = 0f;
                // The model's front is -Z (like the hero models).
                var facing = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-toPlayer) : Quaternion.identity;
                merchant = Instantiate(catalog.WanderingMerchant, position, facing);
            }
            else
            {
                merchant = GameObject.CreatePrimitive(PrimitiveType.Capsule); // fallback if the model is missing
                merchant.transform.position = position + Vector3.up;
                merchant.GetComponent<Collider>().isTrigger = true;
                merchant.AddComponent<SoulHunter.Gameplay.Core.WanderingMerchant>();
            }
            merchant.name = "WanderingMerchant";
            _runtimeHazards.Add(merchant); // Add to hazards list so it gets cleaned up properly
        }

        private void Update()
        {
            if (_player == null) { _player = FindFirstObjectByType<PlayerController>(); _playerHealth = _player != null ? _player.GetComponent<HealthController>() : null; }
            _stageTime += Time.deltaTime;
            if (_level.Signature == CampaignSignature.DarkForest) RenderSettings.ambientIntensity = 0.18f + Mathf.Sin(Time.time * 0.7f) * 0.04f;
            if (_level.Signature == CampaignSignature.FrozenPeaks)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowDuration <= 0f && _slowTimer <= 0f) { _slowDuration = 4f; _slowTimer = 12f; }
                if (_player != null) _player.SetCampaignMovementMultiplier(_slowDuration > 0f ? 0.55f : 1f);
                _slowDuration -= Time.deltaTime;
            }
            else if (_player != null) _player.SetCampaignMovementMultiplier(1f);
            if (_level.Signature == CampaignSignature.ThroneOfDeath && _player != null) EnforceContractingArena();
            if (_level.Signature == CampaignSignature.SeaOfLostSouls) ApplySoulTide();
            if (_level.Signature == CampaignSignature.SoulCore) ApplySoulCorePulse();
        }

        private void ApplySoulTide()
        {
            _soulTideTimer -= Time.deltaTime;
            if (_soulTideTimer > 0f || _playerHealth == null) return;
            _soulTideTimer = Mathf.Max(1f, _soulTideInterval);
            _playerHealth.TakeDamage(new DamagePacket(Mathf.Max(1, _soulTideDamage), _player.transform.position, Vector3.zero));
            Debug.Log("[CampaignSignatureSystem] Sea of Lost Souls: Soul Tide hit Kael.");
        }

        private void ApplySoulCorePulse()
        {
            _corePulseTimer -= Time.deltaTime;
            if (_corePulseTimer > 0f || _playerHealth == null) return;
            _corePulseTimer = Mathf.Max(1f, _corePulseInterval);
            _playerHealth.TakeDamage(new DamagePacket(Mathf.Max(1, _corePulseDamage), _player.transform.position, Vector3.zero));
            Debug.Log("[CampaignSignatureSystem] Soul Core: Death Pulse released.");
        }

        private void EnforceContractingArena()
        {
            float t = Mathf.Clamp01(_stageTime / Mathf.Max(1f, _level.RunDurationSeconds));
            float radius = Mathf.Lerp(_arenaStartRadius, _arenaEndRadius, t);
            Vector3 flat = _player.transform.position; flat.y = 0f;
            if (flat.magnitude <= radius) return;
            Vector3 safe = flat.normalized * radius;
            _player.transform.position = new Vector3(safe.x, _player.transform.position.y, safe.z);
            if (_playerHealth != null)
            {
                // Applied every frame outside the arena, so it must not be swallowed by the post-hit window.
                var boundaryHit = new DamagePacket(Mathf.Max(1, Mathf.CeilToInt(_boundaryDamagePerSecond * Time.deltaTime)), _player.transform.position, Vector3.zero);
                boundaryHit.IgnoresHitInvulnerability = true;
                _playerHealth.TakeDamage(boundaryHit);
            }
        }

        private void CreateHazards(CampaignHazard.HazardType type, int damage, int count)
        {
            if (_player == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count);
                Vector3 position = _player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _hazardRadius;
                var hazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hazard.name = $"Campaign_{type}_Hazard_{i + 1}";
                hazard.transform.position = position; hazard.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
                hazard.GetComponent<Collider>().isTrigger = true;
                hazard.GetComponent<Renderer>().material.color = type == CampaignHazard.HazardType.Fire ? new Color(1f, .15f, .02f, .65f) : type == CampaignHazard.HazardType.Poison ? new Color(.1f, .8f, .1f, .65f) : new Color(.3f, .3f, .35f, .7f);
                hazard.AddComponent<CampaignHazard>().Configure(type, damage);
                _runtimeHazards.Add(hazard);
            }
        }

        private void ClearHazards()
        {
            for (int i = _runtimeHazards.Count - 1; i >= 0; i--) if (_runtimeHazards[i] != null) Destroy(_runtimeHazards[i]);
            _runtimeHazards.Clear();
        }

        private void RestoreEnvironment()
        {
            RenderSettings.ambientIntensity = _originalAmbientIntensity; RenderSettings.fog = _originalFog;
            RenderSettings.fogColor = _originalFogColor; RenderSettings.fogDensity = _originalFogDensity;
        }
    }
}
