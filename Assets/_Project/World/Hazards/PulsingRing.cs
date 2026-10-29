using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// VS Jellyfish Wall (vampire.survivors.wiki/w/Jellyfish_Wall, raw, 2026-10-27): "summons 40 Wall Jellyfish by
    /// default, forming an ellipsoid ring around the player, lasting for 30 seconds. The ring of jellyfish periodically
    /// closes in on and moves away from the player, keeping the player and enemies contained within its perimeter."
    ///
    /// The members are the event's own unit (Level 3's Wall Jelly: Jellyfish (wall) statbox). They stand on an ellipse
    /// round the spot where it was summoned, which grows and shrinks between <see cref="StageHazardTable.JellyInnerScale"/>
    /// and 1 of its size every <see cref="StageHazardTable.JellyPulseSeconds"/>. Killing a member opens the ring there.
    /// When the time is up the survivors leave without drops.
    /// Soul Hunter's numbers (the wiki gives none): the ellipse size, the inner size and the pulse period.
    /// Not built: the ring does not stop the hero or other enemies (enemy bodies are triggers, the crowd rule); it holds
    /// them in only by its contact damage.
    /// </summary>
    public class PulsingRing : MonoBehaviour
    {
        private sealed class Member
        {
            public GameObject Enemy;
            public StillEnemy Still;
            public Rigidbody Body;
            public float Angle;
        }

        private readonly List<Member> _members = new List<Member>();
        private Vector3 _centre;
        private float _duration;
        private float _elapsed;

        public float Scale { get; private set; } = 1f;
        public float RemainingSeconds => Mathf.Max(0f, _duration - _elapsed);
        public Vector3 Centre => _centre;
        public int Standing
        {
            get
            {
                int n = 0;
                foreach (var m in _members) if (IsStanding(m)) n++;
                return n;
            }
        }
        public IEnumerable<EnemyController> Members
        {
            get
            {
                foreach (var m in _members)
                    if (m.Enemy != null && m.Enemy.TryGetComponent<EnemyController>(out var enemy)) yield return enemy;
            }
        }

        /// <summary>The scale of the ellipse <paramref name="elapsed"/> seconds in: 1 at the start, then in and out.</summary>
        public static float ScaleAt(float elapsed) =>
            Mathf.Lerp(StageHazardTable.JellyInnerScale, 1f,
                0.5f + 0.5f * Mathf.Cos(elapsed * Mathf.PI * 2f / StageHazardTable.JellyPulseSeconds));

        public static PulsingRing Begin(EnemySpawner spawner, GameObject memberPrefab, Vector3 centre, int count, float durationSeconds)
        {
            if (spawner == null || memberPrefab == null) return null;
            var go = new GameObject("PulsingRing");
            var ring = go.AddComponent<PulsingRing>();
            ring._centre = centre;
            ring._duration = Mathf.Max(1f, durationSeconds);
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                ring._pending++;
                // A few members a frame (EnemySpawner.SpawnSpread), as the Thorn Circle's.
                spawner.SpawnSpread(() => ring.AddMember(spawner, memberPrefab, angle));
            }
            if (ring._members.Count == 0 && ring._pending == 0) { Destroy(go); return null; }
            return ring;
        }

        private int _pending;
        private bool _left;

        private void AddMember(EnemySpawner spawner, GameObject memberPrefab, float angle)
        {
            _pending--;
            if (this == null || _left || spawner == null) return;
            var enemy = spawner.SpawnHazardEnemy(memberPrefab, SpotOf(angle));
            if (enemy == null) return;
            _members.Add(new Member { Enemy = enemy, Angle = angle, Body = enemy.GetComponent<Rigidbody>(), Still = StillEnemy.Begin(enemy) });
        }

        private Vector3 SpotOf(float angle) =>
            _centre + new Vector3(Mathf.Cos(angle) * StageHazardTable.JellyRadiusX, 0f, Mathf.Sin(angle) * StageHazardTable.JellyRadiusZ) * Scale;

        private static bool IsStanding(Member m) =>
            m.Enemy != null && m.Enemy.activeInHierarchy && m.Still != null && !m.Still.IsLeaving;

        private void Update()
        {
            float dt = Time.deltaTime; // game time: a pause holds the ring
            if (dt <= 0f) return;
            _elapsed += dt;
            if (_elapsed >= _duration) { Leave(); return; }
            Scale = ScaleAt(_elapsed);
            bool any = false;
            foreach (var m in _members)
            {
                if (!IsStanding(m)) continue;
                if (m.Enemy.TryGetComponent<EnemyController>(out var enemy) && enemy.IsFrozen) { any = true; continue; }
                any = true;
                Vector3 at = SpotOf(m.Angle);
                at.y = m.Enemy.transform.position.y;
                if (m.Body != null) m.Body.position = at;
                m.Enemy.transform.position = at;
            }
            if (!any && _pending <= 0) Destroy(gameObject);
        }

        /// <summary>The ring's time is up (or the stage changed): the survivors leave without drops.</summary>
        public void Leave()
        {
            _left = true; // members still waiting are not made
            foreach (var m in _members)
                if (IsStanding(m)) m.Still.Leave();
            _members.Clear();
            Destroy(gameObject);
        }
    }
}
