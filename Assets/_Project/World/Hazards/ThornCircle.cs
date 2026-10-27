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
    /// Not built: the ring does not hold other enemies inside (enemy bodies pass through each other and through
    /// obstacles everywhere in Soul Hunter).
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

        public static ThornCircle Begin(EnemySpawner spawner, GameObject memberPrefab, Vector3 centre, float durationSeconds)
        {
            if (spawner == null || memberPrefab == null) return null;
            var go = new GameObject("ThornCircle");
            var circle = go.AddComponent<ThornCircle>();
            circle._centre = centre;
            circle._duration = Mathf.Max(1f, durationSeconds);
            circle.Radius = StageHazardTable.ThornStartRadius;
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            for (int i = 0; i < StageHazardTable.ThornCount; i++)
            {
                float angle = Mathf.PI * 2f * i / StageHazardTable.ThornCount;
                Vector3 at = circle.SpotOf(angle);
                var enemy = spawner.SpawnHazardEnemy(memberPrefab, at);
                if (enemy == null) continue;
                var member = new Member { Enemy = enemy, Angle = angle, Body = enemy.GetComponent<Rigidbody>(), Still = StillEnemy.Begin(enemy) };
                // The post is its own object, not a child: a solid collider under the enemy's body would let the hero
                // shove the enemy about, and the pool would hand the collider on with it.
                var post = new GameObject("ThornCircle_Post");
                post.transform.SetParent(go.transform, false);
                post.transform.position = enemy.transform.position;
                if (obstacleLayer >= 0) post.layer = obstacleLayer;
                var collider = post.AddComponent<CapsuleCollider>();
                collider.radius = StageHazardTable.ThornPostRadius;
                collider.height = 4f;
                collider.direction = 1;
                member.Post = post;
                circle._members.Add(member);
            }
            if (circle._members.Count == 0) { Destroy(go); return null; }
            return circle;
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
            if (!any) Destroy(gameObject);
        }

        /// <summary>The ring's time is up (or the stage changed): the survivors leave without drops.</summary>
        public void Leave()
        {
            foreach (var m in _members)
                if (IsStanding(m)) m.Still.Leave();
            _members.Clear();
            Destroy(gameObject); // the posts are its children
        }
    }
}
