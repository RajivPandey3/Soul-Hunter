using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.Combat
{
    public class PeachoneWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        public override float Knockback => 2f; // VS wiki Peachone: knockback 2
        protected override bool ShowsAttackPose => false; // an orbit/aura/area weapon: no swing toward enemies

        public GameObject BirdVisual; // Bird circling the player
        public GameObject BombPrefab; // The projectile dropped
        public float CircleRadius = 3f;
        
        private float _angle = 0f;

        /// <summary>
        /// Gemini (I) counterpart setup (wiki: Peachone's counterpart "attacks slightly further outward than Peachone"):
        /// a wider orbit, starting on the far side of the hero so the two birds do not overlap. The wiki gives no
        /// number: +1.5 units (half the 3-unit orbit again) is Soul Hunter's choice.
        /// </summary>
        public const float GeminiExtraRadius = 1.5f;
        public void SetOrbit(float radius, float phaseDegrees)
        {
            CircleRadius = radius;
            _angle = phaseDegrees;
        }

        // VS wiki (Peachone): 10 damage, 1.0 s cooldown, 4 bombs. Levels: +1 bomb every level; L2 Area +40%; L3 +10;
        // L4 CD -0.3; L5 Area +40%; L6 +10; L7 CD -0.3; L8 Area +40%. Area bonuses scale our own blast size.
        public const float VsBaseDamage = 10f;
        public const float VsCooldown = 1f;
        public const int VsAmount = 4;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 3 ? 10f : 0f) + (Level >= 6 ? 10f : 0f);
        public int BombCount => VsAmount + (Level - 1);
        public float LevelArea => 1f + 0.4f * ((Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        // wiki footnote: "Each Cooldown reduction level up actually reduces Cooldown by 0.25 seconds".
        public float CooldownForLevel => VsCooldown - (Level >= 4 ? 0.25f : 0f) - (Level >= 7 ? 0.25f : 0f);

        /// <summary>
        /// VS (wiki Peachone): an activation bombards for the Duration (4 s), "1 second roughly equal to 1 set"; Amount
        /// adds projectiles per set; Cooldown bonuses shorten the activation gap, the time between sets and between
        /// projectiles. The next activation comes after the Duration plus the Cooldown.
        /// </summary>
        public const float VsDuration = 4f;
        public const float VsSetInterval = 1f;
        public float ActiveSeconds => VsDuration * DurationMultiplier;
        /// <summary>Sets fired by the last activation (tests).</summary>
        public int LastActivationSets { get; private set; }

        public const float BirdHeight = 1.6f;

        /// <summary>
        /// How fast, and which way, the zone circles the hero. VS wiki (Peachone, raw, re-read 2026-10-12): infobox "Area
        /// orbits clockwise around the player" and Effects "The zone revolves clockwise"; Ebony_Wings: "counterclockwise".
        /// (One tip on the Peachone page says the reverse; the two infoboxes and both Effects texts agree, so they are
        /// followed.) Seen from the game camera a falling angle is clockwise, so Peachone is negative; it circled
        /// counterclockwise before 2026-10-12. 50 degrees a second is Soul Hunter's own pace; both birds have Speed 80%.
        /// </summary>
        protected virtual float OrbitDegreesPerSecond => -50f;
        /// <summary>
        /// Degrees a second the zone circles now. VS wiki Peachone (2026-10-19): "Speed increases how fast the circle
        /// moves"; its Limit Break table has "Speed +5%" (Ebony_Wings the same). So the Speed stat, Hyper and the Limit
        /// Break cards turn the orbit faster, as Twilight Flock's; it ignored Speed before (CMB-04).
        /// </summary>
        public float OrbitRate => OrbitDegreesPerSecond * SpeedMultiplier;
        /// <summary>The weapon's name on its damage (Results screen fallback).</summary>
        protected virtual string ResultsName => "Peachone";

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = CooldownForLevel;
            // BirdVisual was never assigned, so Attack() returned at once and Peachone never dropped a bomb.
            // The prefab now names a bird prefab; the weapon flies its own instance of it.
            if (BirdVisual != null && !BirdVisual.scene.IsValid())
                BirdVisual = Instantiate(BirdVisual, transform.position, Quaternion.identity, transform);
        }

        protected override void Update()
        {
            base.Update();
            if (BirdVisual != null)
            {
                // Circle around the player on the ground plane (it used to circle vertically, in X-Y).
                _angle += Time.deltaTime * OrbitRate;
                float rad = _angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * CircleRadius;
                BirdVisual.transform.position = transform.position + offset + Vector3.up * BirdHeight;
                // The bird faces the way it flies (the tangent, turned round for a clockwise orbit).
                BirdVisual.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * Mathf.Sign(OrbitDegreesPerSecond));
            }
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            AttackCooldown = CooldownForLevel;
        }

        protected override void Attack()
        {
            if (BombPrefab == null || BirdVisual == null) return;
            SetNextAttackIn(ActiveSeconds + CooldownForLevel * CooldownMultiplier);
            StartCoroutine(Activation());
        }

        private IEnumerator Activation()
        {
            LastActivationSets = 0;
            float setInterval = VsSetInterval * CooldownMultiplier;
            float elapsed = 0f;
            while (elapsed < ActiveSeconds - 0.001f)
            {
                LastActivationSets++;
                StartCoroutine(BombingRun());
                yield return new WaitForSeconds(setInterval);
                elapsed += setInterval;
            }
        }

        /// <summary>VS interval (wiki Peachone): 0.025 s between bombs (it was 0.1 s).</summary>
        public const float VsInterval = 0.025f;

        private IEnumerator BombingRun()
        {
            for (int i = 0; i < BombCount + ExtraAmount; i++)
            {
                Vector3 dropPos = BirdVisual.transform.position - Vector3.up * BirdHeight; // blast on the ground under the bird
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, dropPos, Quaternion.identity)
                    : Instantiate(BombPrefab, dropPos, Quaternion.identity);
                if (bomb == null) continue;
                bomb.transform.localScale = BombPrefab.transform.localScale * AreaMultiplier * LevelArea;
                
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(BaseDamage);
                damageDealer.DamageType = DamageType.Magic; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
                damageDealer.SourceWeaponName = ResultsName;
                AimAtHostiles(bomb);
                
                var lifetime = bomb.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(1.5f);
                else Destroy(bomb, 1.5f);
                yield return new WaitForSeconds(VsInterval * CooldownMultiplier);
            }
        }
    }
}
