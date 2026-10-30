using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Phone profile (2026-10-18, Level 5 at Ultra): Lost Heads keep 800-1,050 enemy bullets in the air. As Projectiles each
    /// was a kinematic Rigidbody with a trigger that PhysX read, swept and wrote back every step (physics 9-11 ms of a 25 ms
    /// frame). An enemy bullet only ever needs to touch the one player, so this one has no Rigidbody and no Collider: it
    /// flies straight, asks PlayerReach whether it is touching the player (the same answer as an overlap with the player's
    /// colliders) and hits through the same damage path a Projectile used (DamageableLookup, DamageSource, TakeDamage), then
    /// ends. Shadow Kael's mirrored shots stay Projectiles.
    /// </summary>
    public sealed class EnemyBullet : MonoBehaviour
    {
        internal static readonly List<EnemyBullet> Active = new List<EnemyBullet>();
        /// <summary>Enemy bullets in flight (tests and tools).</summary>
        public static int ActiveCount => Active.Count;

        [SerializeField] private float _hitRadius = 0.15f;
        private Transform _transform;
        private Vector3 _position;
        private Vector3 _velocity;
        private float _lifeLeft;
        private int _damage;
        private string _source;
        private int _index = -1;

        /// <summary>The radius it touches the player within (from the bolt's own collider when the template is made).</summary>
        public float HitRadius { get => _hitRadius; internal set => _hitRadius = Mathf.Max(0.01f, value); }
        public int Damage => _damage;

        internal void Launch(Vector3 position, Vector3 direction, float speed, int damage, float lifetime, string source)
        {
            if (_transform == null) _transform = transform;
            _position = position;
            _velocity = direction * speed;
            _damage = damage;
            _lifeLeft = lifetime;
            _source = source;
        }

        private void OnEnable()
        {
            if (!(_index >= 0 && _index < Active.Count && Active[_index] == this)) { _index = Active.Count; Active.Add(this); }
            if (_transform == null) _transform = transform;
            _position = _transform.position;
            EnemyBulletRunner.Ensure();
        }

        private void OnDisable()
        {
            int i = _index;
            _index = -1;
            if (i < 0 || i >= Active.Count || Active[i] != this) { Active.Remove(this); return; }
            int last = Active.Count - 1;
            Active[i] = Active[last];
            if (Active[i] != null) Active[i]._index = i;
            Active.RemoveAt(last);
        }

        /// <summary>One frame of flight, from EnemyBulletRunner.</summary>
        internal void Tick(float dt)
        {
            _position += _velocity * dt;
            _transform.position = _position;
            var player = PlayerReach.Within(_position, _hitRadius);
            if (player != null)
            {
                var target = DamageableLookup.Of(player);
                if (target != null)
                {
                    using (DamageSource.From(null, _source))
                        target.TakeDamage(new DamagePacket(_damage, _position, _velocity.normalized, DamageType.Normal));
                    gameObject.SetActive(false); // pierce 1: it ends on its first hit, as the Projectile did
                    return;
                }
            }
            _lifeLeft -= dt;
            if (_lifeLeft <= 0f) gameObject.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();
    }

    /// <summary>Moves every enemy bullet from one Update (as ProjectileRunner does for shots). One per loaded game.</summary>
    public sealed class EnemyBulletRunner : MonoBehaviour
    {
        private static EnemyBulletRunner _instance;

        internal static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("EnemyBulletRunner") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnemyBulletRunner>();
        }

        private void Update()
        {
            var bullets = EnemyBullet.Active;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            // Backwards: a bullet that ends during its tick is swap-removed, which only moves an already-ticked bullet.
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                if (i >= bullets.Count) continue;
                var bullet = bullets[i];
                if (bullet == null) { bullets.RemoveAt(i); continue; }
                try { bullet.Tick(dt); }
                catch (System.Exception e) { Debug.LogException(e, bullet); } // one bullet's error never stops the rest
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;
    }
}
