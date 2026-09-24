using System;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Weapons
{
    public class MagicWandWeapon : AutoAttackWeapon
    {
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _cooldown = 1.2f;
        [SerializeField] private float _range = 15f;
        [SerializeField] private float _projectileSpeed = 20f;
        [SerializeField] private int _damage = 10;
        private const float ShotInterval = 0.1f;
        // Learning Comment:
        // Base class AutoAttackWeapon mein pehle se `_timer` field majood hai.
        // Derived class mein same field name hone se Unity serialization conflict error deta hai:
        // "The same field name is serialized multiple times in the class or its parent class: Base(MagicWandWeapon) _timer"
        // Isliye isko `_cooldownTimer` rename kiya aur [System.NonSerialized] mark kiya.
        [System.NonSerialized] private float _cooldownTimer;
        private float _shotTimer;
        private int _remainingShots;
        private PlayerStats _stats;
        private readonly System.Collections.Generic.HashSet<Projectile> _activeProjectiles = new();
        private Action<Projectile> _releaseHandler;
        public event Action<Projectile> OnProjectileFired;

        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public override int MaxLevel => 8;
        public int BaseDamage => _damage + (Level >= 5 ? 10 : 0) + (Level >= 8 ? 10 : 0);
        public int ShotCount => Mathf.Max(1, 1 + (Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (_stats != null ? _stats.Amount : 0));
        public int Pierce => Level >= 7 ? 2 : 1;
        public float EffectiveCooldown => Mathf.Max(0.01f, (_cooldown - (Level >= 3 ? 0.2f : 0f)) * (_stats != null ? _stats.Cooldown : 1f));

        protected override void Awake()
        {
            base.Awake();
            _stats = GetComponentInParent<PlayerStats>();
            _releaseHandler = ReleaseProjectile;
        }

        private void OnDisable() { _remainingShots = 0; _cooldownTimer = 0f; _shotTimer = 0f; }
        private void ReleaseProjectile(Projectile projectile) { _activeProjectiles.Remove(projectile); }
        private void OnDestroy()
        {
            foreach (var projectile in _activeProjectiles)
                if (projectile != null) projectile.Deactivated -= _releaseHandler;
            _activeProjectiles.Clear();
        }

        public override void LevelUp() { CurrentLevel = Mathf.Min(8, CurrentLevel + 1); }

        protected override void Update()
        {
            if (Time.deltaTime <= 0f) return;
            if (_remainingShots == 0)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer > 0f || FindClosestEnemy() == null || _projectilePrefab == null) return;
                _remainingShots = ShotCount;
                _shotTimer = 0f;
            }
            else _shotTimer -= Time.deltaTime;

            // Retain overdue shots across slow frames; bound work per frame.
            int emittedThisFrame = 0;
            while (_remainingShots > 0 && _shotTimer <= 0f && emittedThisFrame < 8)
            {
                _remainingShots--;
                FireWand();
                emittedThisFrame++;
                _shotTimer += ShotInterval;
            }
            if (_remainingShots == 0) _cooldownTimer = EffectiveCooldown;
        }

        private void FireWand()
        {
            if (_activeProjectiles.Count >= 60) return;
            Transform target = FindClosestEnemy();
            if (target == null || _projectilePrefab == null) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            var shot = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(_projectilePrefab, transform.position, Quaternion.LookRotation(direction))
                : Instantiate(_projectilePrefab, transform.position, Quaternion.LookRotation(direction));
            if (shot == null) return;
            shot.SetActive(true);
            var projectile = shot.GetComponent<Projectile>();
            if (projectile == null) projectile = shot.AddComponent<Projectile>();
            int damage = Mathf.RoundToInt(BaseDamage * (_stats != null ? _stats.Might : 1f));
            float speed = _projectileSpeed * (_stats != null ? _stats.ProjectileSpeed : 1f);
            shot.transform.localScale = _projectilePrefab.transform.localScale * (_stats != null ? _stats.Area : 1f);
            // Learning Comment:
            // AGENTS.md Section 5: Spectral enemies ignore Normal damage and require Holy, Magic, or Spectral.
            // Magic Wand projectiles use DamageType.Magic to damage Level 7 spectral enemies.
            projectile.Initialize(direction, speed, damage, 3f, DamageType.Magic, "Magic Wand");
            var arcana = ArcanaManager.Instance;
            int bounces = arcana != null && arcana.HasArcana(ArcanaData.ArcanaType.WaltzOfPearls) ? 3 : 0;
            projectile.ConfigureContacts(Pierce, bounces, true);
            _activeProjectiles.Add(projectile);
            projectile.Deactivated += _releaseHandler;
            // Gemini and Iron Blue Will do not apply to Magic Wand.
            OnProjectileFired?.Invoke(projectile);
        }

        private Transform FindClosestEnemy()
        {
            Transform closest = null;
            float nearestSquared = _range * _range;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance < nearestSquared)
                {
                    nearestSquared = distance;
                    closest = enemy.transform;
                }
            }
            return closest;
        }
    }
}
