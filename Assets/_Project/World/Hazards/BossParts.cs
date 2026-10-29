using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// A stage boss's own parts, each an enemy of its own statbox that moves with the boss.
    /// - Level 4, Giant Enemy Crab (vampire.survivors.wiki/w/Giant_Enemy_Crab, raw 2026-10-28): "has 2 Pincers attached to
    ///   it, which grow upon receiving damage and can regenerate a short while after being destroyed"; "Each pincer has 17
    ///   lives" (Pincer: 5 HP x Level, damage 12, xp 10, res-debuff). "In Gallo Tower, it can also summon a Drowner, if the
    ///   player spends longer than 6 seconds below it" (The_Drowner: "The Drowner XL", 655,350 HP x Level, xp 50, Gallo
    ///   Tower; "Defeating it causes it to drop a chest").
    /// - Level 6, Orochimario (wiki Orochimario): "spawns with eight heads, the first head has double the health to be
    ///   1,000 [x Level] and each head adds its own health to Orochimario, which means its total health is actually 5,000
    ///   [x Level]. When they take damage, Orochimario itself also take that much, they can be individually defeated. The
    ///   heads can lunge at the player to attack them every 5 to 10 seconds" (head: 500 HP x Level, damage 20).
    /// Balance method (the wiki is silent): pincers 2.6 units either side of the crab, back 5 s after being destroyed, up
    /// to twice their size as they lose health; "below it" is south of the crab within 4 units across, for 6 s in a row,
    /// once per crab, a 60 s flood. The heads stand on an arc of 3.2 units facing the hero, and a lunge (Lunge ability's
    /// 0.6 s at 3x) runs at the body's speed, then the head goes back. When the boss dies, its parts go without drops.
    /// </summary>
    public class BossParts : MonoBehaviour
    {
        public enum Kind { CrabPincers, HydraHeads }

        public const int PincerLives = 17;
        public const float PincerRegrowSeconds = 5f;
        public const float PincerSide = 2.6f;
        public const float PincerMaxGrowth = 2f;
        public const float BelowSeconds = 6f;
        public const float BelowHalfWidth = 4f;
        public const int DrownerXlHealthPerLevel = 655350;
        public const float DrownerXlXp = 50f;
        public const float DrownerSeconds = 60f;
        public const int Heads = 8;
        public const float HeadArc = 3.2f;
        public const float LungeMinSeconds = 5f, LungeMaxSeconds = 10f, LungeSeconds = 0.6f, LungeSpeedMultiplier = 3f;

        private sealed class Part
        {
            public int Slot;
            public GameObject Object;
            public EnemyController Enemy;
            public HealthController Health;
            public int LastHealth;
            public float BaseScale;
            public int LivesLeft;
            public float RegrowAt = -1f;
            public float NextLunge;
            public float LungeLeft;
            public Vector3 LungeOffset;
            public System.Action<int, int> HealthWatch;
            public System.Action DeathWatch;
        }

        private readonly List<Part> _parts = new List<Part>();
        private EnemyController _boss;
        private HealthController _bossHealth;
        private EnemySpawner _spawner;
        private GameObject _partPrefab;
        private Kind _kind;
        private float _belowFor;
        private bool _active;

        public Kind PartKind => _kind;
        public bool DrownerSummoned { get; private set; }
        public int PartsAlive { get { int n = 0; foreach (var p in _parts) if (Alive(p)) n++; return n; } }
        public IEnumerable<GameObject> LiveParts { get { foreach (var p in _parts) if (Alive(p)) yield return p.Object; } }
        public int PincerLivesLeft(int slot) => slot >= 0 && slot < _parts.Count ? _parts[slot].LivesLeft : 0;

        /// <summary>The boss parts of <paramref name="stage"/>'s guardian, or none.</summary>
        public static Kind? KindFor(int stage) => stage == 4 ? Kind.CrabPincers : stage == 6 ? Kind.HydraHeads : (Kind?)null;
        public static string PartTypeId(Kind kind) => kind == Kind.CrabPincers ? "CrabPincer" : "HydraHead";

        public static BossParts Begin(GameObject bossObject, Kind kind, EnemySpawner spawner)
        {
            if (bossObject == null || spawner == null) return null;
            var prefab = spawner.FindStageType(PartTypeId(kind));
            if (prefab == null) return null;
            var parts = bossObject.GetComponent<BossParts>();
            if (parts == null) parts = bossObject.AddComponent<BossParts>();
            parts.Setup(kind, spawner, prefab);
            return parts;
        }

        private void Setup(Kind kind, EnemySpawner spawner, GameObject prefab)
        {
            RemoveAll();
            _kind = kind;
            _spawner = spawner;
            _partPrefab = prefab;
            _boss = GetComponent<EnemyController>();
            _bossHealth = GetComponent<HealthController>();
            _belowFor = 0f;
            DrownerSummoned = false;
            _active = true;
            if (_bossHealth != null) { _bossHealth.OnDied -= HandleBossDied; _bossHealth.OnDied += HandleBossDied; }
            int count = kind == Kind.CrabPincers ? 2 : Heads;
            for (int i = 0; i < count; i++)
            {
                var part = new Part { Slot = i, LivesLeft = kind == Kind.CrabPincers ? PincerLives : 1 };
                part.NextLunge = Random.Range(LungeMinSeconds, LungeMaxSeconds);
                _parts.Add(part);
                Grow(part);
            }
            if (kind == Kind.HydraHeads && _bossHealth != null)
            {
                // "each head adds its own health to Orochimario": the body's own health plus every head's (the first double).
                int heads = 0;
                foreach (var p in _parts) if (p.Health != null) heads += p.Health.MaxHealth;
                _bossHealth.Initialize(_bossHealth.MaxHealth + heads);
            }
        }

        private static bool Alive(Part p) => p.Object != null && p.Object.activeInHierarchy && p.Health != null && p.Health.CurrentHealth > 0;

        private void Grow(Part part)
        {
            if (!_active || part.LivesLeft <= 0 || _spawner == null) return;
            part.LivesLeft--;
            GameObject obj;
            using (SoulHunter.Core.Services.SlowSpan.Measure("BossParts.Grow")) obj = _spawner.SpawnHazardEnemy(_partPrefab, SlotPosition(part), ignoreHardCap: true);
            if (obj == null) return;
            part.Object = obj;
            part.Enemy = obj.GetComponent<EnemyController>();
            part.Health = obj.GetComponent<HealthController>();
            part.BaseScale = _partPrefab.transform.localScale.x;
            obj.transform.localScale = Vector3.one * part.BaseScale;
            StillEnemy.Begin(obj);
            if (_kind == Kind.HydraHeads && part.Slot == 0 && part.Health != null) part.Health.Initialize(part.Health.MaxHealth * 2);
            if (part.Health != null)
            {
                part.LastHealth = part.Health.CurrentHealth;
                part.HealthWatch = (current, max) => OnPartHealth(part, current);
                part.DeathWatch = () => OnPartDied(part);
                part.Health.OnHealthChanged += part.HealthWatch;
                part.Health.OnDied += part.DeathWatch;
            }
            part.RegrowAt = -1f;
        }

        private void OnPartHealth(Part part, int current)
        {
            int lost = part.LastHealth - current;
            part.LastHealth = current;
            if (lost > 0 && _kind == Kind.HydraHeads && _bossHealth != null) _bossHealth.TakeLinkedDamage(lost);
        }

        private void OnPartDied(Part part)
        {
            Unwatch(part);
            if (part.Object != null) part.Object.transform.localScale = Vector3.one * part.BaseScale; // the pool gets it back as it was
            if (_active && _kind == Kind.CrabPincers && part.LivesLeft > 0) part.RegrowAt = PincerRegrowSeconds;
        }

        private static void Unwatch(Part part)
        {
            if (part.Health == null) return;
            if (part.HealthWatch != null) part.Health.OnHealthChanged -= part.HealthWatch;
            if (part.DeathWatch != null) part.Health.OnDied -= part.DeathWatch;
            part.HealthWatch = null;
            part.DeathWatch = null;
        }

        private Vector3 TowardHero()
        {
            var player = PlayerController.Instance;
            Vector3 d = player != null ? player.transform.position - transform.position : Vector3.back;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.back;
        }

        private Vector3 SlotPosition(Part part)
        {
            Vector3 at = transform.position;
            if (_kind == Kind.CrabPincers) return at + Vector3.right * (part.Slot == 0 ? -PincerSide : PincerSide);
            float spread = 160f * Mathf.Deg2Rad;
            float angle = -spread / 2f + spread * part.Slot / Mathf.Max(1, Heads - 1);
            Vector3 toward = TowardHero();
            Vector3 dir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up) * toward;
            return at + dir * HeadArc;
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime; // game time: nothing moves under a pause
            if (dt <= 0f) return;
            if (_boss == null || !_boss.isActiveAndEnabled) { RemoveAll(); return; }

            foreach (var part in _parts)
            {
                if (part.RegrowAt >= 0f)
                {
                    part.RegrowAt -= dt;
                    if (part.RegrowAt <= 0f) Grow(part);
                    continue;
                }
                if (!Alive(part)) continue;
                Vector3 slot = SlotPosition(part);
                if (_kind == Kind.HydraHeads && !part.Enemy.IsFrozen) Lunge(part, dt);
                Vector3 at = slot + part.LungeOffset;
                at.y = part.Object.transform.position.y;
                part.Object.transform.position = at;
                if (part.Enemy.Rigidbody != null) part.Enemy.Rigidbody.position = at;
                if (_kind == Kind.CrabPincers)
                {
                    // "grow upon receiving damage": up to twice the size as health runs out.
                    float lost = part.Health.MaxHealth > 0 ? 1f - (float)part.Health.CurrentHealth / part.Health.MaxHealth : 0f;
                    part.Object.transform.localScale = Vector3.one * part.BaseScale * Mathf.Lerp(1f, PincerMaxGrowth, lost);
                }
            }
            if (_kind == Kind.CrabPincers) WatchBelow(dt);
        }

        private void Lunge(Part part, float dt)
        {
            if (part.LungeLeft > 0f)
            {
                // Out at 3x the body's speed for the first half, back for the second.
                part.LungeLeft -= dt;
                float speed = (_boss != null ? _boss.MoveSpeed : 5f) * LungeSpeedMultiplier;
                Vector3 toward = TowardHero();
                bool out_ = part.LungeLeft > LungeSeconds;
                part.LungeOffset += (out_ ? toward : -part.LungeOffset.normalized) * speed * dt;
                if (!out_ && part.LungeOffset.magnitude < speed * dt) part.LungeOffset = Vector3.zero;
                if (part.LungeLeft <= 0f) part.LungeOffset = Vector3.zero;
                return;
            }
            part.NextLunge -= dt;
            if (part.NextLunge > 0f) return;
            part.NextLunge = Random.Range(LungeMinSeconds, LungeMaxSeconds);
            part.LungeLeft = LungeSeconds * 2f;
            LungesStarted++;
        }

        public int LungesStarted { get; private set; }

        private void WatchBelow(float dt)
        {
            if (DrownerSummoned) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            bool below = d.z < -0.5f && Mathf.Abs(d.x) <= BelowHalfWidth;
            _belowFor = below ? _belowFor + dt : 0f;
            if (_belowFor < BelowSeconds) return;
            DrownerSummoned = true;
            var hazards = FindFirstObjectByType<StageHazards>();
            using (SoulHunter.Core.Services.SlowSpan.Measure("BossParts.Drowner"))
                if (hazards != null) hazards.StartRisingTide(DrownerSeconds, DrownerXlHealthPerLevel, DrownerXlXp);
        }

        /// <summary>For tests: how long the hero has stood below the crab.</summary>
        public float BelowFor => _belowFor;

        private void HandleBossDied() => RemoveAll();

        private void RemoveAll()
        {
            _active = false;
            foreach (var part in _parts)
            {
                Unwatch(part);
                if (part.Object != null) part.Object.transform.localScale = Vector3.one * part.BaseScale;
                if (Alive(part) && part.Object.TryGetComponent<StillEnemy>(out var still)) still.Leave(); // gone without drops
            }
            _parts.Clear();
        }

        private void OnDisable() => RemoveAll();
    }
}
