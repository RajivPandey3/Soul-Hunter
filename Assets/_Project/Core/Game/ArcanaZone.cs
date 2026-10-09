using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// A VS Blood Astronomia (XXI) blood zone (wiki Blood_Astronomia_(XXI), raw, fetched 2026-10-09): a circle that hurts
    /// (or, from Pentagram and Gorgeous Moon, erases) the enemies inside it a set number of times, then goes. A planet
    /// zone (Song of Mana, Mannajja) orbits "in an ellipse around the character and the center point stays in place".
    /// Each enemy is hit at most once per <see cref="RehitSeconds"/> (Soul Hunter's balance number: the wiki gives no hit
    /// interval for the zones). Its look is the existing hit spark on every hit; card art and a zone look are the owner's.
    /// </summary>
    public sealed class ArcanaZone : MonoBehaviour
    {
        public const float RehitSeconds = 0.5f;

        public float Radius;
        public int Damage;
        public bool Erases;
        public int TriggersLeft;
        public float SecondsLeft;
        /// <summary>Planet zones: the fixed centre, the ellipse's radii and the angle speed (degrees per second).</summary>
        public bool Orbits;
        public Vector3 Centre;
        public float OrbitX, OrbitZ, Angle, AngleSpeed;
        public string SourceName = "Crimson Orbit";

        private readonly System.Collections.Generic.Dictionary<EnemyController, float> _lastHit = new System.Collections.Generic.Dictionary<EnemyController, float>();

        public static ArcanaZone Spawn(Vector3 at, float radius, int damage, int triggers, float seconds, bool erases)
        {
            var go = new GameObject(erases ? "ArcanaZone (erase)" : "ArcanaZone");
            go.transform.position = at;
            var zone = go.AddComponent<ArcanaZone>();
            zone.Radius = Mathf.Max(0.1f, radius);
            zone.Damage = Mathf.Max(1, damage);
            zone.TriggersLeft = Mathf.Max(1, triggers);
            zone.SecondsLeft = Mathf.Max(0.1f, seconds);
            zone.Erases = erases;
            return zone;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused
            SecondsLeft -= dt;
            if (SecondsLeft <= 0f || TriggersLeft <= 0) { Destroy(gameObject); return; }
            if (Orbits)
            {
                Angle += AngleSpeed * dt;
                float rad = Angle * Mathf.Deg2Rad;
                transform.position = Centre + new Vector3(Mathf.Cos(rad) * OrbitX, 0f, Mathf.Sin(rad) * OrbitZ);
            }

            Vector3 at = transform.position;
            float sqr = Radius * Radius;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0 && TriggersLeft > 0; i--)
            {
                var enemy = i < enemies.Count ? enemies[i] : null;
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - at;
                offset.y = 0f;
                if (offset.sqrMagnitude > sqr) continue;
                if (_lastHit.TryGetValue(enemy, out float last) && Time.time - last < RehitSeconds) continue;
                _lastHit[enemy] = Time.time;
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                using (DamageSource.From(null, SourceName))
                {
                    if (Erases)
                    {
                        // "which can erase enemies similar to a Rosary. Enemies with instant kill resistance are still immune".
                        if (enemy.ResistsInstantKill) continue;
                        health.EraseByInstantKill();
                    }
                    else health.TakeDamage(new DamagePacket(Damage, enemy.transform.position, offset.normalized, DamageType.Magic).WithKnockback(0f));
                }
                TriggersLeft--;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, enemy.transform.position + Vector3.up * 0.8f);
            }
        }
    }

    /// <summary>
    /// VS Twilight Requiem (II): put on a listed weapon's spawned shot (AutoAttackWeapon.AimAtHostiles); when the shot
    /// expires (switched off: lifetime over, pool return) it explodes there. Pooled shots are re-armed on every spawn.
    /// </summary>
    public sealed class ArcanaShotHook : MonoBehaviour
    {
        [System.NonSerialized] public AutoAttackWeapon Weapon;

        private void OnDisable()
        {
            var weapon = Weapon;
            Weapon = null;
            if (weapon == null || !Application.isPlaying || !gameObject.scene.isLoaded || Time.timeScale <= 0f) return;
            ArcanaManager.OnShotExpired(weapon, transform.position);
        }
    }
}
