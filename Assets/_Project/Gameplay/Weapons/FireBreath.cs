using System.Collections;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Hellfire Breath, Soul Hunter's version of the VS Nduja Fritta Tanto temporary weapon (vampire.survivors.wiki,
    /// fetched 2026-09-27: damage 30, cooldown 0.5 s, amount 12, interval 0.04 s, flame duration 1 s, area 200%, speed
    /// 300%, pierces as an area of effect, scales with every stat). For 10 s x Duration the holder breathes a cone of 12
    /// (+ Amount) fire flames every 0.5 s x Cooldown. Owner aiming rule: the cone points at the nearest enemy, else
    /// where the hero faces. Flames reuse the Fire Wand's fireball (GameContentCatalog.FireBreathProjectile), grow as
    /// they fly, pierce everything and deal Magic damage x Might (as the Fire Wand, so they hurt Level 7 ghosts; owner, 2026-09-26).
    /// It is a temporary child of the player, not a WeaponManager slot: it takes no weapon slot, has no levels, and
    /// Shadow Kael does not mirror it. Flame speed and cone width are Soul Hunter numbers (provisional).
    /// </summary>
    public class FireBreath : AutoAttackWeapon
    {
        public const string DisplayName = "Hellfire Breath";
        public const float BaseDurationSeconds = 10f;
        public const float BaseDamage = 30f;
        public const float BaseCooldown = 0.5f;
        public const int FlamesPerBurst = 12;
        public const float FlameInterval = 0.04f;
        public const float FlameLifetime = 1f;
        public const float FlameSpeed = 9f;        // provisional: about 9 units of reach at Speed 1
        public const float FlameEndScale = 2f;     // VS area 200%: the flame doubles as it travels
        public const float ConeDegrees = 30f;      // provisional
        public const int Pierce = 9999;            // an area of effect: hits everything it passes

        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;

        /// <summary>Seconds of breathing left (game time). The component removes itself at 0.</summary>
        public float RemainingSeconds { get; private set; }
        public bool IsBreathing => RemainingSeconds > 0f;

        private static GameObject _templateHolder;
        private static GameObject _flameTemplate;

        /// <summary>Starts the breath on the player, or refills it to full length if it is already breathing.</summary>
        public static FireBreath Begin(Transform player)
        {
            if (player == null) return null;
            var breath = player.GetComponentInChildren<FireBreath>();
            if (breath == null)
            {
                var go = new GameObject("HellfireBreath");
                go.transform.SetParent(player, false);
                breath = go.AddComponent<FireBreath>();
            }
            breath.RemainingSeconds = BaseDurationSeconds * breath.DurationMultiplier;
            return breath;
        }

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = BaseDamage;
            AttackCooldown = BaseCooldown;
        }

        protected override void Update()
        {
            if (!IsBreathing) return;
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            if (!IsBreathing)
            {
                StopAllCoroutines();
                Destroy(gameObject);
                return;
            }
            base.Update(); // cooldown timer -> Attack()
        }

        public override void LevelUp() { } // a temporary pickup weapon has no levels

        protected override void Attack()
        {
            StartCoroutine(Burst(FlamesPerBurst + ExtraAmount));
        }

        private IEnumerator Burst(int count)
        {
            var wait = new WaitForSeconds(FlameInterval);
            for (int i = 0; i < count; i++)
            {
                if (!IsBreathing) yield break;
                SpawnFlame();
                yield return wait;
            }
        }

        private void SpawnFlame()
        {
            Vector3 aim = AimDirection(HolderFacing);
            SpawnFlame(aim);
            // VS Gemini (I) (wiki Gemini_(I), fetched 2026-10-09): "Nduja Fritta Tanto ... create[s] another stream of flame
            // ... from the back side of the character." Only for the player (Shadow Kael never holds the breath).
            if (!IsHeldByEnemy && SoulHunter.Gameplay.Core.ArcanaManager.GeminiActive) SpawnFlame(-aim);
        }

        private void SpawnFlame(Vector3 aim)
        {
            var template = FlameTemplate();
            if (template == null) return;
            float half = ConeDegrees * 0.5f;
            Vector3 direction = Quaternion.Euler(0f, Random.Range(-half, half), 0f) * aim;
            Vector3 origin = transform.position + direction * 0.5f;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject flame;
            if (WeaponPoolManager.Instance != null) flame = WeaponPoolManager.Instance.GetFromPool(template, origin, rotation);
            else
            {
                flame = Instantiate(template, origin, rotation);
                flame.SetActive(true);
            }
            if (flame == null) return;

            float life = FlameLifetime * DurationMultiplier;
            var projectile = flame.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(direction, FlameSpeed * SpeedMultiplier, ScaledDamage(BaseDamage), life, DamageType.Magic, DisplayName, Pierce); // Magic like the Fire Wand (owner, Level 7 "type by theme"), so it reaches ghosts
            var grow = flame.GetComponent<FireBreathFlame>();
            Vector3 baseScale = template.transform.localScale * AreaMultiplier;
            if (grow != null) grow.Begin(baseScale, baseScale * FlameEndScale, life);
            AimAtHostiles(flame);
            if (WeaponPoolManager.Instance == null) Destroy(flame, life + 0.5f);
        }

        /// <summary>
        /// A scene-local copy of the flame prefab with FireBreathFlame added, kept under an inactive holder. Pooling the
        /// copy (not the shared Fire Wand prefab) keeps the grow component off the Fire Wand's own fireballs. Rebuilt
        /// when a scene change destroys it.
        /// </summary>
        private static GameObject FlameTemplate()
        {
            if (_flameTemplate != null) return _flameTemplate;
            if (_templateHolder == null)
            {
                _templateHolder = new GameObject("HellfireBreath_Templates");
                _templateHolder.SetActive(false);
            }
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var source = catalog != null ? catalog.FireBreathProjectile : null;
            GameObject template;
            if (source != null)
            {
                template = Instantiate(source, _templateHolder.transform);
            }
            else
            {
                // Placeholder when the catalog is not wired yet: an orange sphere projectile.
                template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                template.transform.SetParent(_templateHolder.transform, false);
                template.transform.localScale = Vector3.one * 0.5f;
                var renderer = template.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(1f, 0.45f, 0.1f);
                template.GetComponent<Collider>().isTrigger = true;
                var body = template.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }
            template.name = "HellfireBreath_Flame";
            // Bug MIS v2 C03: the Fire Wand fireball's opaque core sphere, grown by FireBreathFlame, drew flat yellow
            // discs. The breath shows only the fireball's flame particles and halo.
            foreach (var mesh in template.GetComponentsInChildren<MeshRenderer>(true))
                if (mesh.name == "Core") mesh.enabled = false;
            if (template.GetComponent<Projectile>() == null) template.AddComponent<Projectile>();
            if (template.GetComponent<FireBreathFlame>() == null) template.AddComponent<FireBreathFlame>();
            _flameTemplate = template;
            return _flameTemplate;
        }
    }
}
