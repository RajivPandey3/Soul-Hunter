using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Death's Shadow, Soul Hunter's version of VS "The Stalker" (vampire.survivors.wiki, Dairy Plant: "spawns a green
    /// reaper to follow the player for 2 minutes"). It is this stage's boss model, drawn 1.5x and dark, spawned through the
    /// enemy pool. It chases at the player's base speed, hits hard on contact, cannot be hurt, knocked back or wiped
    /// (Soul Toll, Pentagram and the stage-clear wipe leave it standing), never drops anything, never counts as a kill
    /// and is not a stage boss. After 120 s (game time) it fades back into the pool. Everything it changed on the pooled
    /// enemy is put back when it leaves, so the next enemy from that pool is ordinary.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeathsShadow : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color ShadowTint = new Color(0.16f, 0.1f, 0.22f, 1f);

        /// <summary>Seconds (game time) before it leaves. Tests may shorten it.</summary>
        public float RemainingSeconds { get; set; }
        public bool IsLeaving { get; private set; }

        private EnemyController _enemy;
        private HealthController _health;
        private TouchDamage _touch;
        private float _touchDamageBefore;
        private float _knockbackResistanceBefore;
        private System.Func<bool> _preventDeathBefore;
        private MaterialPropertyBlock _block;

        /// <summary>Turns a freshly spawned pooled enemy into Death's Shadow.</summary>
        public static DeathsShadow Begin(GameObject enemyObject, float lifetimeSeconds = StageHazardTable.ShadowLifetimeSeconds)
        {
            if (enemyObject == null) return null;
            var shadow = enemyObject.GetComponent<DeathsShadow>();
            if (shadow == null) shadow = enemyObject.AddComponent<DeathsShadow>();
            shadow.Setup(lifetimeSeconds);
            return shadow;
        }

        private void Setup(float lifetimeSeconds)
        {
            _enemy = GetComponent<EnemyController>();
            _health = GetComponent<HealthController>();
            _touch = GetComponent<TouchDamage>();
            RemainingSeconds = Mathf.Max(0.1f, lifetimeSeconds);
            IsLeaving = false;

            transform.localScale *= StageHazardTable.ShadowScale; // the spawner puts the pool's scale back on reuse
            if (_enemy != null)
            {
                float baseSpeed = _enemy.MoveSpeed / Mathf.Max(0.1f, _enemy.CampaignSpeedMultiplier);
                _enemy.ApplyCampaignSpeed(StageHazardTable.ShadowSpeedMultiplier(baseSpeed));
                _enemy.SetCastsShadows(true);
            }
            if (_touch != null)
            {
                _touchDamageBefore = _touch.DamageAmount;
                _touch.DamageAmount = StageHazardTable.ShadowContactDamage;
            }
            if (_health != null)
            {
                _knockbackResistanceBefore = _health.KnockbackResistance;
                _preventDeathBefore = _health.TryPreventDeath;
                _health.KnockbackResistance = 1f;
                _health.IsInvincible = true;
                // Wipes (KillSilently) skip IsInvincible; refusing the death keeps it standing without OnDied, so there
                // are no drops and no kill.
                _health.TryPreventDeath = RefuseDeath;
            }
            Tint(true);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.BossWarningSound, 0.7f);
        }

        private bool RefuseDeath()
        {
            _health.ResetHealth();
            return true;
        }

        private void Update()
        {
            if (IsLeaving) return;
            if (_health != null) _health.IsInvincible = true; // a refused death's revival timer would clear it
            RemainingSeconds -= Time.deltaTime;                // game time: stops while paused
            if (RemainingSeconds <= 0f) Leave();
        }

        /// <summary>Fades back into the pool: no drops, no kill.</summary>
        public void Leave()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up);
            gameObject.SetActive(false); // AutoReturnToPool takes it back; OnDisable restores the enemy
        }

        private void OnDisable()
        {
            Restore();
            Destroy(this);
        }

        private void Restore()
        {
            if (_touch != null) _touch.DamageAmount = _touchDamageBefore;
            if (_health != null)
            {
                _health.IsInvincible = false;
                _health.KnockbackResistance = _knockbackResistanceBefore;
                if (_health.TryPreventDeath == (System.Func<bool>)RefuseDeath) _health.TryPreventDeath = _preventDeathBefore;
            }
            if (_enemy != null) _enemy.SetCastsShadows(false);
            Tint(false);
            _touch = null;
            _health = null;
            _enemy = null;
        }

        private void Tint(bool on)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!on) { r.SetPropertyBlock(null); continue; }
                var m = r.sharedMaterial;
                if (m == null) continue;
                r.GetPropertyBlock(_block);
                if (m.HasProperty(BaseColorId)) _block.SetColor(BaseColorId, m.GetColor(BaseColorId) * ShadowTint);
                if (m.HasProperty(ColorId)) _block.SetColor(ColorId, m.GetColor(ColorId) * ShadowTint);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
