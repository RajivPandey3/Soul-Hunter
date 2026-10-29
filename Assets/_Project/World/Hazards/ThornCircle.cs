using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// The Thorn Circle (Omens; VS Random Event "Flower Wall", vampire.survivors.wiki/w/Random_Events, raw, 2026-10-13:
    /// "Summons a circular Flower Wall around the player that slowly closes in on them. Disappears after ~50 seconds";
    /// Flower_Wall_(event): "a ring of durable Flower Wall units around the player, which slowly creeps inward. The Flower
    /// Walls trap both the player and enemies inside until they are defeated or expire"; its tip: "it's often better to
    /// break through their encirclement").
    ///
    /// The members are Level 1's Thorn Wall type on every level (the wiki's own Flower Wall statbox: 30 x Level health, 1
    /// damage). They stand in a ring that shrinks around the spot where it was summoned. Each living member carries a solid
    /// post on the Obstacle layer, which stops the hero only; killing a member removes its post and opens the ring there.
    ///
    /// Owner (2026-10-13, "kuch bhi nahi bachna chahiye"): this is the one place where enemy bodies stop the hero. The
    /// crowd rule of 2026-09-26 ("the horde never walls the player in") stays for every other enemy.
    /// "trap both the player and enemies inside" (owner 2026-10-29: the wiki's answer stands): an enemy on the inside of the
    /// ring stays inside and one outside stays out, unless the member in its way is down (the ring is open there). Fliers,
    /// the ring's own members and enemies that hold a place (IHoldsPlace) are left alone. Enemy bodies still pass through
    /// each other everywhere else.
    /// Soul Hunter's numbers (the wiki gives none): <see cref="StageHazardTable.ThornCount"/> members, from
    /// <see cref="StageHazardTable.ThornStartRadius"/> down to <see cref="StageHazardTable.ThornEndRadius"/> over its time.
    /// </summary>
    public class ThornCircle : MonoBehaviour
    {
        private sealed class Member
        {
            public GameObject Enemy;
            public StillEnemy Still;
            public Rigidbody Body;
            public GameObject Post;
            public float Angle;
        }

        private readonly List<Member> _members = new List<Member>();
        private Vector3 _centre;
        private float _duration;
        private float _elapsed;

        public float Radius { get; private set; }
        public float RemainingSeconds => Mathf.Max(0f, _duration - _elapsed);
        public Vector3 Centre => _centre;
        /// <summary>Members still standing (a defeated one has left the ring and its post is gone).</summary>
        public int Standing
        {
            get
            {
                int n = 0;
                foreach (var m in _members) if (IsStanding(m)) n++;
                return n;
            }
        }
        /// <summary>The ring's members (for map events that return what they spawned).</summary>
        public IEnumerable<SoulHunter.Gameplay.AI.EnemyController> Members
        {
            get
            {
                foreach (var m in _members)
                    if (m.Enemy != null && m.Enemy.TryGetComponent<SoulHunter.Gameplay.AI.EnemyController>(out var enemy)) yield return enemy;
            }
        }
        /// <summary>The solid posts of the members still standing (tests).</summary>
        public int Posts
        {
            get
            {
                int n = 0;
                foreach (var m in _members) if (m.Post != null) n++;
                return n;
            }
        }

        public static ThornCircle Begin(EnemySpawner spawner, GameObject memberPrefab, Vector3 centre, float durationSeconds) =>
            Begin(spawner, memberPrefab, centre, durationSeconds, StageHazardTable.ThornCount);

        /// <summary>A ring of <paramref name="count"/> members (a map event row's amount; the Omen uses ThornCount).</summary>
        public static ThornCircle Begin(EnemySpawner spawner, GameObject memberPrefab, Vector3 centre, float durationSeconds, int count)
        {
            count = Mathf.Max(1, count);
            if (spawner == null || memberPrefab == null) return null;
            var go = new GameObject("ThornCircle");
            var circle = go.AddComponent<ThornCircle>();
            circle._centre = centre;
            circle._duration = Mathf.Max(1f, durationSeconds);
            circle.Radius = StageHazardTable.ThornStartRadius;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                circle._pending++;
                // A few members a frame (EnemySpawner.SpawnSpread): 100 in one frame made a 29-37 ms jolt (2026-10-29).
                spawner.SpawnSpread(() => circle.AddMember(spawner, memberPrefab, angle));
            }
            if (circle._members.Count == 0 && circle._pending == 0) { Destroy(go); return null; }
            return circle;
        }

        private int _pending;
        private bool _left;

        private void AddMember(EnemySpawner spawner, GameObject memberPrefab, float angle)
        {
            _pending--;
            if (this == null || _left || spawner == null) return;
            var enemy = spawner.SpawnHazardEnemy(memberPrefab, SpotOf(angle));
            if (enemy == null) return;
            var member = new Member { Enemy = enemy, Angle = angle, Body = enemy.GetComponent<Rigidbody>(), Still = StillEnemy.Begin(enemy) };
            // The post is its own object, not a child: a solid collider under the enemy's body would let the hero
            // shove the enemy about, and the pool would hand the collider on with it.
            var post = new GameObject("ThornCircle_Post");
            post.transform.SetParent(transform, false);
            post.transform.position = enemy.transform.position;
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer >= 0) post.layer = obstacleLayer;
            var collider = post.AddComponent<CapsuleCollider>();
            collider.radius = StageHazardTable.ThornPostRadius;
            collider.height = 4f;
            collider.direction = 1;
            member.Post = post;
            _members.Add(member);
        }

        private Vector3 SpotOf(float angle) => _centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Radius;

        private static bool IsStanding(Member m) =>
            m.Enemy != null && m.Enemy.activeInHierarchy && m.Still != null && !m.Still.IsLeaving;

        private void Update()
        {
            float dt = Time.deltaTime; // game time: a pause holds the ring
            if (dt <= 0f) return;
            _elapsed += dt;
            if (_elapsed >= _duration) { Leave(); return; }
            Radius = Mathf.Lerp(StageHazardTable.ThornStartRadius, StageHazardTable.ThornEndRadius, _elapsed / _duration);
            bool any = false;
            foreach (var m in _members)
            {
                if (!IsStanding(m))
                {
                    if (m.Post != null) { Destroy(m.Post); m.Post = null; } // defeated: the ring is open here
                    continue;
                }
                any = true;
                Vector3 at = SpotOf(m.Angle);
                at.y = m.Enemy.transform.position.y;
                if (m.Body != null) m.Body.position = at;
                m.Enemy.transform.position = at;
                if (m.Post != null) m.Post.transform.position = at;
            }
            if (!any && _pending <= 0) { Destroy(gameObject); return; }
            HoldEnemiesOnTheirSide();
        }

        /// <summary>
        /// Which side of the ring each enemy was on (true = inside) in the life it was seen in, and whether it is left alone
        /// (it holds a place). A pooled body comes back with a new life (HealthController.Life), so a reused body starts on
        /// the side it now stands on instead of being pulled to the side its last life was on.
        /// </summary>
        private struct Seen { public int Life; public bool Inside; public bool Skip; public int Frame; }
        private readonly Dictionary<EnemyController, Seen> _side = new Dictionary<EnemyController, Seen>();
        private readonly List<EnemyController> _stale = new List<EnemyController>();
        private readonly HashSet<GameObject> _memberObjects = new HashSet<GameObject>();
        public const float TrapMargin = 0.45f;

        private void HoldEnemiesOnTheirSide()
        {
            if (_memberObjects.Count != _members.Count)
            {
                _memberObjects.Clear();
                foreach (var m in _members) if (m.Enemy != null) _memberObjects.Add(m.Enemy);
            }
            int frame = Time.frameCount;
            float step = _members.Count > 0 ? Mathf.PI * 2f / _members.Count : Mathf.PI * 2f;
            var live = EnemyController.ActiveEnemies;
            for (int i = 0; i < live.Count; i++)
            {
                var enemy = live[i];
                if (enemy == null || enemy.IsFlying || enemy.IsDying) continue;
                var health = enemy.Health;
                int life = health != null ? health.Life : 0;
                Vector3 position = enemy.CachedTransform.position;
                Vector3 offset = position - _centre; offset.y = 0f;
                float distance = offset.magnitude;
                bool inside = distance < Radius;
                if (!_side.TryGetValue(enemy, out var seen) || seen.Life != life)
                {
                    var holds = enemy.GetComponent<IHoldsPlace>();
                    bool skip = _memberObjects.Contains(enemy.gameObject) ||
                                (holds != null && !(holds is ILeavesWithItsLife life0 && life0.Doomed)); // a doomed one is last life's
                    _side[enemy] = new Seen { Life = life, Inside = inside, Skip = skip, Frame = frame };
                    continue;
                }
                seen.Frame = frame;
                if (seen.Skip || inside == seen.Inside) { _side[enemy] = seen; continue; }
                if (!WallStandsAt(Mathf.Atan2(offset.z, offset.x), step)) { seen.Inside = inside; _side[enemy] = seen; continue; } // through a gap
                _side[enemy] = seen;
                float keep = seen.Inside ? Radius - TrapMargin : Radius + TrapMargin;
                Vector3 direction = distance > 0.001f ? offset / distance : Vector3.forward;
                Vector3 held = _centre + direction * Mathf.Max(0.1f, keep);
                held.y = position.y;
                if (enemy.Rigidbody != null) enemy.Rigidbody.position = held;
                enemy.CachedTransform.position = held;
            }
            // Forget bodies that left the live list (so the table does not grow over the ring's life).
            if ((frame & 63) == 0)
            {
                _stale.Clear();
                foreach (var pair in _side) if (pair.Key == null || pair.Value.Frame != frame) _stale.Add(pair.Key);
                foreach (var key in _stale) _side.Remove(key);
            }
        }

        /// <summary>True when the member nearest <paramref name="angle"/> is standing (the ring is closed there).</summary>
        private bool WallStandsAt(float angle, float step)
        {
            Member nearest = null;
            float best = float.MaxValue;
            foreach (var m in _members)
            {
                float gap = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, m.Angle * Mathf.Rad2Deg));
                if (gap < best) { best = gap; nearest = m; }
            }
            return nearest != null && best <= step * Mathf.Rad2Deg && IsStanding(nearest);
        }

        /// <summary>The ring's time is up (or the stage changed): the survivors leave without drops.</summary>
        public void Leave()
        {
            _left = true; // members still waiting are not made
            foreach (var m in _members)
                if (IsStanding(m)) m.Still.Leave();
            _members.Clear();
            Destroy(gameObject); // the posts are its children
        }
    }
}
