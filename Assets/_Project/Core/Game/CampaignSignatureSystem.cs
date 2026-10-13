using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    public sealed class CampaignSignatureSystem : MonoBehaviour
    {
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

        // ---- Level 9, the Closing Arena (GDD: "The safe zone continuously shrinks. The player must survive in extremely
        // tight spaces."). Fixed on 2026-10-13 after a bot run died in 1.6 s at an edge nobody could see:
        // - the zone was centred on the world's origin, wherever the hero stood when the level began; it is now centred on
        //   the hero's place at the start of the level;
        // - nothing showed it; a ring is drawn on the ground at its edge;
        // - it was a wall that also hurt: the hero was put back on the edge every frame and took at least 1 damage per
        //   frame (60 a second at 60 FPS, for a setting of 10). The story says "safe zone", so it is a zone, not a wall:
        //   the hero may leave it (the level's stage items lie 45 to 100 units out) and loses the set 10 health a second
        //   out there, in ticks of the game's contact interval.
        /// <summary>Seconds between two ticks outside the zone (TouchDamage's contact interval).</summary>
        public const float ArenaTickSeconds = 0.5f;
        private Vector3 _arenaCentre;
        private float _arenaTick;
        private LineRenderer _arenaRing;
        private const int ArenaRingPoints = 96;
        private static readonly Color ArenaRingColour = new Color(0.95f, 0.2f, 0.25f, 0.85f);

        /// <summary>True on Level 9 while its closing arena runs.</summary>
        public bool ArenaActive => _level.Signature == CampaignSignature.ThroneOfDeath;
        public Vector3 ArenaCentre => _arenaCentre;
        /// <summary>The safe zone's radius now: from the start radius down to the end radius over the level's 30 minutes.</summary>
        public float ArenaRadius => ArenaRadiusAt(_stageTime, _level.RunDurationSeconds, _arenaStartRadius, _arenaEndRadius);
        public static float ArenaRadiusAt(float stageSeconds, float runSeconds, float startRadius, float endRadius) =>
            Mathf.Lerp(startRadius, endRadius, Mathf.Clamp01(stageSeconds / Mathf.Max(1f, runSeconds)));
        /// <summary>The damage of one tick outside the zone: the per-second setting over the tick (10 a second: 5 per tick).</summary>
        public int ArenaTickDamage => Mathf.Max(1, Mathf.RoundToInt(_boundaryDamagePerSecond * ArenaTickSeconds));
        /// <summary>True when a point on the ground is outside the safe zone.</summary>
        public bool OutsideArena(Vector3 point)
        {
            Vector3 flat = point - _arenaCentre;
            flat.y = 0f;
            return ArenaActive && flat.magnitude > ArenaRadius;
        }
        /// <summary>The ring drawn at the zone's edge (null off Level 9).</summary>
        public LineRenderer ArenaRing => _arenaRing;

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
            _stageTime = 0f; _slowTimer = 0f; _slowDuration = 0f;
            _soulTideTimer = _soulTideInterval;
            _corePulseTimer = _corePulseInterval;
            // Har stage start par stale modifier clear. Sirf Frozen Peaks ka
            // authored signature Update() mein 0.55 multiplier lagata hai.
            if (_player != null) _player.SetCampaignMovementMultiplier(1f);
            ClearHazards(); RestoreEnvironment();
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            _arenaCentre = _player != null ? new Vector3(_player.transform.position.x, 0f, _player.transform.position.z) : Vector3.zero;
            _arenaTick = 0f;
            if (_level.Signature == CampaignSignature.ThroneOfDeath) BuildArenaRing();
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
                // The Ruined Castle and Crimson Swamp used to ring the start with damage discs; VS floors never hurt
                // the player, so they were removed (owner, 2026-09-26). VS pressure plates start events instead.
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
        }

        private void ApplySoulCorePulse()
        {
            _corePulseTimer -= Time.deltaTime;
            if (_corePulseTimer > 0f || _playerHealth == null) return;
            _corePulseTimer = Mathf.Max(1f, _corePulseInterval);
            _playerHealth.TakeDamage(new DamagePacket(Mathf.Max(1, _corePulseDamage), _player.transform.position, Vector3.zero));
        }

        private void EnforceContractingArena()
        {
            DrawArenaRing();
            if (!OutsideArena(_player.transform.position)) { _arenaTick = 0f; return; }
            _arenaTick -= Time.deltaTime; // game time: a pause holds it
            if (_arenaTick > 0f || _playerHealth == null) return;
            _arenaTick = ArenaTickSeconds;
            // A zone, not a hit: an enemy's hit a moment earlier must not swallow it, and it starts no immunity itself.
            // Armor takes 1 off every hit; the zone's 10 a second is the zone's own (a hero with 4 Armor lost 2 a second in
            // the first build run), so the Armor is added back.
            var tick = new DamagePacket(ArenaTickDamage + Mathf.Max(0, _playerHealth.Armor), _player.transform.position, Vector3.zero);
            tick.IgnoresHitInvulnerability = true;
            _playerHealth.TakeDamage(tick);
        }

        private void BuildArenaRing()
        {
            var go = new GameObject("ClosingArena_Ring");
            _arenaRing = go.AddComponent<LineRenderer>();
            _arenaRing.useWorldSpace = true;
            _arenaRing.loop = true;
            _arenaRing.positionCount = ArenaRingPoints;
            _arenaRing.widthMultiplier = 0.35f;
            _arenaRing.numCornerVertices = 2;
            _arenaRing.alignment = LineAlignment.TransformZ; // flat on the ground (the object looks down)
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _arenaRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arenaRing.receiveShadows = false;
            var shader = Shader.Find("SoulHunter/VFX/Particle");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            if (material.HasProperty("_Color")) material.SetColor("_Color", ArenaRingColour);
            _arenaRing.sharedMaterial = material;
            _arenaRing.startColor = _arenaRing.endColor = ArenaRingColour;
            _runtimeHazards.Add(go); // removed with the level's other things
            DrawArenaRing();
        }

        private void DrawArenaRing()
        {
            if (_arenaRing == null) return;
            float radius = ArenaRadius;
            float y = _player != null ? _player.transform.position.y - 0.85f : 0.15f; // just above the floor
            for (int i = 0; i < ArenaRingPoints; i++)
            {
                float angle = Mathf.PI * 2f * i / ArenaRingPoints;
                _arenaRing.SetPosition(i, new Vector3(_arenaCentre.x + Mathf.Cos(angle) * radius, y, _arenaCentre.z + Mathf.Sin(angle) * radius));
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
