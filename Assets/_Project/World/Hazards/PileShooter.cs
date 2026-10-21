using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// One member of a Pile Assault (vampire.survivors.wiki, Gallo Tower: "spawns a group of enemies around the player,
    /// shooting bullets at them"; Dairy Plant: "shoot bullets in an ellipsoid ring around the player"). The pooled stage
    /// enemy stands still where it was placed, faces the player and fires a slow bullet at the player every few seconds.
    /// It can be killed like any enemy (drops and kills as usual). When the assault ends, the survivors return to the pool
    /// without drops. Everything is put back when it leaves, so the next enemy from the pool is ordinary.
    /// </summary>
    [DisallowMultipleComponent]
    public class PileShooter : MonoBehaviour, IHoldsPlace
    {
        public float RemainingSeconds { get; set; }
        public float FireIntervalSeconds { get; set; } = StageHazardTable.PileFireIntervalSeconds;
        public int ShotsFired { get; private set; }
        public bool IsLeaving { get; private set; }

        private EnemyController _enemy;
        private float _nextShot;
        private bool _wasFrozen;

        public static PileShooter Begin(GameObject enemyObject, float durationSeconds, float firstShotDelay)
        {
            if (enemyObject == null) return null;
            var shooter = enemyObject.GetComponent<PileShooter>();
            if (shooter == null) shooter = enemyObject.AddComponent<PileShooter>();
            shooter._enemy = enemyObject.GetComponent<EnemyController>();
            shooter.RemainingSeconds = Mathf.Max(0.1f, durationSeconds);
            shooter._nextShot = Mathf.Max(0f, firstShotDelay);
            shooter.IsLeaving = false;
            shooter.Hold();
            // Bug MIS AI-06 (wiki Orologion: "new enemies will also become frozen"): a member spawned during an Orologion
            // was frozen by SpawnEnemy, and the hold replaced that; it stays frozen for the rest of it, then holds again.
            EnemySpawner.KeepWorldFreeze(shooter._enemy);
            return shooter;
        }

        private void Hold()
        {
            if (_enemy != null) _enemy.ChangeState(new EnemyHoldState(_enemy));
        }

        private void Update()
        {
            if (IsLeaving || _enemy == null) return;
            float dt = Time.deltaTime; // game time: nothing happens while paused
            if (dt <= 0f) return;

            // A Time Freeze or Clock Lancet holds it; afterwards it would chase, so it is put back on its spot.
            if (_enemy.IsFrozen) { _wasFrozen = true; }
            else if (_wasFrozen) { _wasFrozen = false; Hold(); }

            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f) { Leave(); return; }
            if (_enemy.IsFrozen) return;

            _nextShot -= dt;
            if (_nextShot > 0f) return;
            _nextShot = FireIntervalSeconds;
            var player = PlayerController.Instance;
            if (player != null) FireAt(player.transform.position);
        }

        /// <summary>Fires one slow bullet at a point on the ground plane (also used by tests).</summary>
        public Component FireAt(Vector3 target)
        {
            Vector3 from = transform.position + Vector3.up * 0.5f; // inside the player's capsule height (it spans root y -1..+1)
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
            var bullet = HazardBullets.Fire(from, direction.normalized, StageHazardTable.PileBulletSpeed,
                StageHazardTable.PileBulletDamage, StageHazardTable.PileBulletLifetime);
            if (bullet != null) ShotsFired++;
            return bullet;
        }

        /// <summary>The assault is over: back to the pool without drops.</summary>
        public void Leave()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up * 0.8f);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            // EnemyController.OnDisable already cleared the state; a reused enemy starts idle and chases again.
            _enemy = null;
            Destroy(this);
        }
    }

    /// <summary>An enemy that stays where it is (Pile Assault). It still faces its target (EnemyController does that).</summary>
    public class EnemyHoldState : IEnemyState
    {
        private readonly EnemyController _controller;

        public EnemyHoldState(EnemyController controller) { _controller = controller; }

        public void Enter()
        {
            if (_controller.Rigidbody != null) _controller.Rigidbody.linearVelocity = Vector3.zero;
            if (_controller.Animator != null) _controller.Animator.UpdateSpeed(0f);
        }

        public void UpdateLogic() { }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            if (_controller.Rigidbody != null)
                _controller.Rigidbody.linearVelocity = new Vector3(0f, _controller.Rigidbody.linearVelocity.y, 0f);
        }

        public void Exit() { }
    }

    /// <summary>
    /// Enemy bullets for stage hazards: Projectiles with TargetsPlayer (like Shadow Kael's mirrored shots), so they hurt
    /// only the player. They reuse the Magic Wand bolt (GameContentCatalog.HazardBullet) as a scene-local template tinted
    /// crimson, so the player's own Magic Wand bolts are untouched.
    /// </summary>
    public static class HazardBullets
    {
        public const string SourceName = "Pile Assault";
        private static readonly Color BulletTint = new Color(1f, 0.25f, 0.3f, 1f);
        private static GameObject _holder;
        private static GameObject _template;
        private static GameObject _lightTemplate;

        /// <summary>
        /// Phone profile (2026-10-18): true fires EnemyBullets (no Rigidbody, no Collider; they hit the player by distance).
        /// False fires the old physics Projectiles (A/B measurement: AutoplayBot -sh-autoplay-lightbullet-ab; tests).
        /// </summary>
        public static bool LightBullets = true;

        /// <summary>Fires one enemy bullet: an EnemyBullet, or a Projectile while LightBullets is off. Null if none could be made.</summary>
        public static Component Fire(Vector3 from, Vector3 direction, float speed, int damage, float lifetime)
        {
            if (LightBullets) return FireLight(from, direction, speed, damage, lifetime);
            var template = Template();
            if (template == null) return null;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            GameObject bullet;
            if (WeaponPoolManager.Instance != null) bullet = WeaponPoolManager.Instance.GetFromPool(template, from, rotation);
            else
            {
                bullet = Object.Instantiate(template, from, rotation);
                bullet.SetActive(true);
                Object.Destroy(bullet, lifetime + 0.5f);
            }
            if (bullet == null) return null;
            var projectile = bullet.GetComponent<Projectile>();
            if (projectile == null) return null;
            projectile.Initialize(direction, speed, damage, lifetime, DamageType.Normal, SourceName, 1);
            projectile.ConfigureContacts(1, 0, false); // the player's Arcana bounces are not for enemy bullets
            projectile.TargetsPlayer = true;           // after OnEnable, which resets it
            PhysicsShots.Add(projectile);              // one entry per pooled object (EndAll)
            return projectile;
        }

        /// <summary>The physics bullets this class has fired (pooled objects of its own template; EndAll).</summary>
        private static readonly HashSet<Projectile> PhysicsShots = new HashSet<Projectile>();

        /// <summary>
        /// Bug MIS AI-11. Ends every enemy bullet in flight: the stage clear at 30:00 ("All enemies on the screen are
        /// cleared", wiki Mad_Forest; a bullet is the enemies' attack and goes with them) and the stage change. Before, a
        /// Lost Head's bullets kept hitting through the 5 s transition and into the next level. Not a per-frame call.
        /// </summary>
        public static int EndAll()
        {
            int ended = 0;
            var light = EnemyBullet.Active;
            for (int i = light.Count - 1; i >= 0; i--)
            {
                if (i >= light.Count) continue;
                var bullet = light[i];
                if (bullet == null) { light.RemoveAt(i); continue; }
                bullet.gameObject.SetActive(false); // back to its pool (it swap-removes itself from Active)
                ended++;
            }
            PhysicsShots.RemoveWhere(p => p == null);
            foreach (var projectile in PhysicsShots)
                if (projectile.isActiveAndEnabled && projectile.TargetsPlayer) { projectile.gameObject.SetActive(false); ended++; }
            return ended;
        }

        private static Component FireLight(Vector3 from, Vector3 direction, float speed, int damage, float lifetime)
        {
            var template = LightTemplate();
            if (template == null) return null;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            GameObject go;
            if (WeaponPoolManager.Instance != null) go = WeaponPoolManager.Instance.GetFromPool(template, from, rotation);
            else
            {
                go = Object.Instantiate(template, from, rotation);
                go.SetActive(true);
                Object.Destroy(go, lifetime + 0.5f);
            }
            if (go == null) return null;
            var bullet = go.GetComponent<EnemyBullet>();
            if (bullet == null) return null;
            bullet.Launch(from, direction, speed, damage, lifetime, SourceName);
            return bullet;
        }

        /// <summary>The physics template with its Projectile, Rigidbody and Collider taken off and an EnemyBullet in their place.</summary>
        private static GameObject LightTemplate()
        {
            if (_lightTemplate != null) return _lightTemplate;
            var source = Template();
            if (source == null) return null;
            var template = Object.Instantiate(source, _holder.transform);
            template.name = "HazardBullet_Light";
            float radius = 0.15f;
            var sphere = template.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                var scale = template.transform.lossyScale;
                radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
            foreach (var projectile in template.GetComponents<Projectile>()) Object.DestroyImmediate(projectile);
            foreach (var body in template.GetComponents<Rigidbody>()) Object.DestroyImmediate(body);
            foreach (var collider in template.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            int shotLayer = Projectile.EnemyShotLayer;
            if (shotLayer >= 0) template.layer = shotLayer;
            template.AddComponent<EnemyBullet>().HitRadius = radius;
            _lightTemplate = template;
            return _lightTemplate;
        }

        private static GameObject Template()
        {
            if (_template != null) return _template;
            if (_holder == null)
            {
                _holder = new GameObject("HazardBullet_Templates");
                _holder.SetActive(false);
            }
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var source = catalog != null ? catalog.HazardBullet : null;
            GameObject template;
            if (source != null)
            {
                template = Object.Instantiate(source, _holder.transform);
                // 60 FPS pass (2026-10-15): enemy bullets keep the bolt's core and halo but not its trail. Level 5's Lost
                // Heads keep about 350 bullets in the air, and their trails were 350 of the 800 particle systems running
                // (1.7 ms a frame). The player's own Magic Wand bolt keeps its trail.
                foreach (var fx in template.GetComponentsInChildren<ParticleSystem>(true))
                    if (fx != null && fx.gameObject.name == "Trail") Object.DestroyImmediate(fx.gameObject);
            }
            else
            {
                template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                template.transform.SetParent(_holder.transform, false);
                template.transform.localScale = Vector3.one * 0.4f;
                template.GetComponent<Collider>().isTrigger = true;
                var body = template.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }
            template.name = "HazardBullet";
            if (template.GetComponent<Projectile>() == null) template.AddComponent<Projectile>();
            // Tinted material copies on the template (clones share them; property blocks would not be copied).
            foreach (var r in template.GetComponentsInChildren<Renderer>(true))
            {
                var materials = r.materials;
                foreach (var m in materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", BulletTint);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", BulletTint);
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", BulletTint);
                }
                r.materials = materials;
            }
            // Draw calls (2026-10-15): EnemyBulletDrawer draws every enemy bullet's core and halo in two instanced draws.
            var halo = template.transform.Find("Halo");
            template.AddComponent<EnemyBulletLook>().Setup(template.GetComponentInChildren<MeshRenderer>(true),
                halo != null && halo.GetComponent<ParticleSystem>() != null ? halo.gameObject : null);
            _template = template;
            return _template;
        }
    }
}
