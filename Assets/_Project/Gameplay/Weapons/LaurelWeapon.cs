using System;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class LaurelWeapon : AutoAttackWeapon
    {
        public GameObject ShieldVisualPrefab;
        [SerializeField, Min(0f)] private float _blockGraceSeconds = 0.2f;
        private GameObject _activeShield;
        private Renderer[] _shieldRenderers;
        private MaterialPropertyBlock _shieldColor;
        protected HealthController _playerHealth;
        protected PlayerStats _stats;
        private int _currentCharges = 1;
        private float _rechargeRemaining;
        private float _graceRemaining;
        public override int MaxLevel => 7;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        private int TimingUpgrades => (Level >= 2 ? 1 : 0) + (Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 6 ? 1 : 0);
        public int CurrentCharges => _currentCharges;
        public virtual int MaxCharges => 1 + (Level >= 4 ? 1 : 0) + (Level >= 7 ? 1 : 0);
        public virtual float BaseRechargeCooldown => Mathf.Max(0.1f, AttackCooldown - TimingUpgrades * 0.5f);
        public float EffectiveRechargeCooldown => BaseRechargeCooldown * CooldownMultiplier;
        public virtual float BlockGraceSeconds => _blockGraceSeconds + TimingUpgrades * 0.2f;
        private float CooldownMultiplier => _stats != null ? Mathf.Max(0.1f, _stats.Cooldown) : 1f;
        public event Action<DamagePacket> OnBlocked;

        protected override void Awake()
        {
            base.Awake();
            _currentCharges = MaxCharges;
            _rechargeRemaining = BaseRechargeCooldown;
        }

        protected virtual void OnEnable()
        {
            _playerHealth = GetComponentInParent<HealthController>();
            _stats = GetComponentInParent<PlayerStats>();
            if (_playerHealth != null) _playerHealth.DamageInterceptor = TryBlock;
            UpdateVisual();
        }

        protected virtual void OnDisable()
        {
            if (_playerHealth != null && _playerHealth.DamageInterceptor == TryBlock)
                _playerHealth.DamageInterceptor = null;
            _playerHealth = null;
            _graceRemaining = 0f;
            if (_activeShield != null) _activeShield.SetActive(false);
        }

        private bool TryBlock(DamagePacket packet)
        {
            if (!isActiveAndEnabled || packet.Amount <= 0) return false;
            if (_graceRemaining > 0f) return true;
            if (_currentCharges <= 0) return false;
            if (_currentCharges == MaxCharges) _rechargeRemaining = BaseRechargeCooldown;
            _currentCharges--;
            _graceRemaining = BlockGraceSeconds;
            UpdateVisual();
            OnBlocked?.Invoke(packet);
            return true;
        }

        protected override void Update() { AdvanceTime(Time.deltaTime); }

        private void AdvanceTime(float scaledDeltaTime)
        {
            if (scaledDeltaTime <= 0f) return;
            _graceRemaining = Mathf.Max(0f, _graceRemaining - scaledDeltaTime);
            if (_currentCharges >= MaxCharges) return;
            // Remaining time is in base-cooldown units: a passive acquired
            // during recharge changes its rate without resetting progress.
            _rechargeRemaining -= scaledDeltaTime / CooldownMultiplier;
            if (_rechargeRemaining > 0f) return;
            do {
                _currentCharges++;
                _rechargeRemaining += BaseRechargeCooldown;
            } while (_currentCharges < MaxCharges && _rechargeRemaining <= 0f);
            UpdateVisual();
        }

        public override void LevelUp()
        {
            if (CurrentLevel >= MaxLevel) return;
            float oldCooldown = BaseRechargeCooldown;
            bool wasFull = _currentCharges >= MaxCharges;
            CurrentLevel++;
            _rechargeRemaining = wasFull ? BaseRechargeCooldown : _rechargeRemaining * BaseRechargeCooldown / oldCooldown;
            UpdateVisual();
        }

        protected virtual Color ChargeColor(int charges) => charges >= 3 ? Color.yellow : charges == 2 ? Color.green : Color.blue;

        private void UpdateVisual()
        {
            if (_currentCharges > 0 && _activeShield == null && ShieldVisualPrefab != null)
            {
                _activeShield = Instantiate(ShieldVisualPrefab, transform);
                _shieldRenderers = _activeShield.GetComponentsInChildren<Renderer>(true);
                _shieldColor = new MaterialPropertyBlock();
            }
            if (_activeShield == null) return;
            _activeShield.SetActive(_currentCharges > 0 && isActiveAndEnabled);
            Color color = ChargeColor(_currentCharges);
            if (_shieldRenderers == null) return;
            foreach (var renderer in _shieldRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_shieldColor);
                _shieldColor.SetColor("_Color", color);
                _shieldColor.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(_shieldColor);
            }
        }
    }
}
