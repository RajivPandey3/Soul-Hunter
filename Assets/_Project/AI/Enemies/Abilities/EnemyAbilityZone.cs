using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// The ground circle of an enemy ability: a hazard zone that hurts the player periodically (VS The Ender's zones,
    /// vampire.survivors.wiki/w/The_Ender; The Drowner's flood, /w/The_Drowner), a one-off pulse (VS Cosmic Egg,
    /// /w/Cosmic_Egg) or a marker that only shows a self-destruct's blast area while it follows the enemy.
    /// Visual: the Soul Meteor marker (GameContentCatalog.HazardMarker, the approved VFX v2 magic circle), tinted; no new
    /// art. Runs on game time: a pause holds it.
    /// </summary>
    public class EnemyAbilityZone : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        /// <summary>A pulse's circle stays visible this long after it struck (visual only).</summary>
        public const float LingerSeconds = 0.4f;

        public float Radius { get; private set; }
        public int Damage { get; private set; }
        public bool HalvesHealth { get; private set; }
        public bool Strikes { get; private set; }
        public float TickSeconds { get; private set; }
        /// <summary>Seconds the zone keeps striking after its first strike (0 = one strike).</summary>
        public float DurationSeconds { get; private set; }
        public Transform Follow { get; set; }
        public int Ticks { get; private set; }
        public int PlayerHits { get; private set; }

        private Transform _visual;
        private Color _tint = Color.white;
        private MaterialPropertyBlock _block;
        private float _elapsed;
        private float _nextTick;

        /// <summary>A zone that strikes: first at <paramref name="delaySeconds"/>, then every tick for the duration.</summary>
        public static EnemyAbilityZone CreateStriking(Vector3 position, float radius, Color tint, int damage, bool halvesHealth,
            float delaySeconds, float tickSeconds, float durationSeconds)
        {
            var zone = Create(position, radius, tint);
            zone.Strikes = true;
            zone.Damage = Mathf.Max(0, damage);
            zone.HalvesHealth = halvesHealth;
            zone.TickSeconds = Mathf.Max(0.05f, tickSeconds);
            zone.DurationSeconds = Mathf.Max(0f, durationSeconds);
            zone._nextTick = Mathf.Max(0f, delaySeconds);
            zone._firstStrikeAt = zone._nextTick;
            return zone;
        }

        /// <summary>A marker only (a self-destruct's blast area); the owner destroys it.</summary>
        public static EnemyAbilityZone CreateMarker(Vector3 position, float radius, Color tint, Transform follow)
        {
            var zone = Create(position, radius, tint);
            zone.Strikes = false;
            zone.Follow = follow;
            return zone;
        }

        private static EnemyAbilityZone Create(Vector3 position, float radius, Color tint)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            GameObject go;
            if (catalog != null && catalog.HazardMarker != null)
            {
                go = Instantiate(catalog.HazardMarker, position, Quaternion.identity);
                // The marker prefab may carry a SoulMeteor (it is the meteor's marker): only the circle is wanted here.
                foreach (var meteor in go.GetComponentsInChildren<SoulHunter.Gameplay.Hazards.SoulMeteor>(true))
                    DestroyImmediate(meteor);
            }
            else
            {
                go = new GameObject("EnemyAbilityZone");
                go.transform.position = position;
                var quad = new GameObject("Circle");
                quad.transform.SetParent(go.transform, false);
                quad.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var shader = Shader.Find("SoulHunter/VFX/Particle");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                quad.AddComponent<MeshRenderer>().sharedMaterial = new Material(shader);
            }
            go.name = "EnemyAbilityZone";
            var zone = go.AddComponent<EnemyAbilityZone>();
            zone.Radius = Mathf.Max(0.1f, radius);
            zone._tint = tint;
            zone._visual = go.transform.childCount > 0 ? go.transform.GetChild(0) : null;
            // The marker is a flat quad 1 unit across; its diameter matches the zone.
            if (zone._visual != null) zone._visual.localScale = new Vector3(zone.Radius * 2f, zone.Radius * 2f, 1f);
            zone.Refresh(0.5f);
            return zone;
        }

        private void Update()
        {
            if (SoulHunter.Core.Services.GameTime.IsPaused) return;
            float dt = Time.deltaTime;
            if (Follow != null)
            {
                if (!Follow.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
                transform.position = Follow.position;
            }
            if (_visual != null) _visual.Rotate(0f, 0f, 60f * dt, Space.Self);
            if (!Strikes) { Refresh(0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 8f))); return; }

            _elapsed += dt;
            if (_nextTick >= 0f && _elapsed >= _nextTick)
            {
                Strike();
                _nextTick += TickSeconds;
                // Strikes at first, first + tick, ... up to first + duration (duration 0: one strike, a pulse).
                if (_nextTick > _firstStrikeAt + DurationSeconds + 0.0001f) _nextTick = -1f;
            }
            Refresh(_nextTick >= 0f ? 0.7f : Mathf.Clamp01(1f - (_elapsed - _lastStrikeAt) / LingerSeconds));
            if (_nextTick < 0f && _elapsed - _lastStrikeAt >= LingerSeconds) Destroy(gameObject);
        }

        private float _lastStrikeAt;
        private float _firstStrikeAt;

        private void Strike()
        {
            Ticks++;
            _lastStrikeAt = _elapsed;
            if (DamagePlayerInside(transform.position, Radius, Damage, HalvesHealth)) PlayerHits++;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(HalvesHealth ? SoulHunter.Gameplay.VFX.VfxKind.LevelUp
                : SoulHunter.Gameplay.VFX.VfxKind.HitSpark, transform.position + Vector3.up * 0.3f);
        }

        private void Refresh(float alpha)
        {
            if (_visual == null) return;
            var r = _visual.GetComponent<Renderer>();
            if (r == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            Color c = _tint;
            c.a = Mathf.Clamp01(alpha);
            _block.SetColor(ColorId, c);
            r.SetPropertyBlock(_block);
        }

        /// <summary>VS Cosmic Egg halving: half of the current health (rounded down; 1 HP is left alone).</summary>
        public static int HalvingDamage(int currentHealth) => Mathf.Max(0, currentHealth) / 2;

        /// <summary>
        /// Hurts the player when it stands within radius of centre (flat distance, plus the player's body). A halving
        /// strike takes half the current health: Armor is added back so the net loss is exactly half, and it is not
        /// blocked by the post-hit invulnerability window (it is a pulse, not a hit). Returns true when it hurt.
        /// </summary>
        public static bool DamagePlayerInside(Vector3 centre, float radius, int damage, bool halvesHealth)
        {
            var player = PlayerController.Instance;
            if (player == null) return false;
            Vector3 d = player.transform.position - centre;
            d.y = 0f;
            if (d.magnitude > radius + 0.4f) return false;
            var health = player.GetComponent<HealthController>();
            if (health == null || health.CurrentHealth <= 0) return false;
            int amount = damage;
            var packet = new DamagePacket(0, centre, Vector3.zero);
            if (halvesHealth)
            {
                int half = HalvingDamage(health.CurrentHealth);
                if (half <= 0) return false;
                amount = half + Mathf.Max(0, health.Armor);
                packet.IgnoresHitInvulnerability = true;
            }
            if (amount <= 0) return false;
            packet.Amount = amount;
            int before = health.CurrentHealth;
            health.TakeDamage(packet);
            return health.CurrentHealth < before;
        }
    }
}
