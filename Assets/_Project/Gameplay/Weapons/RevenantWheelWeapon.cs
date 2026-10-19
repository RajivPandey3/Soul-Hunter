using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Revenant Wheel: the union of the Gun and the Azure Repeater, which also needs Tirajisu (own name, 2026-10-12;
    /// not yet owner-approved), built on VS Phieraggi (vampire.survivors.wiki/w/Phieraggi, raw, fetched 2026-10-12).
    /// Infobox: damage 15, speed 100%, amount 4, pierce 7, cooldown 1.4 s, interval 0.1 s, delay 0.2 s, knockback 0.05;
    /// "Ignores Duration"; "Acquisition will free a weapon slot"; max-damage and max-amount "+1 per Revival, max +10".
    /// "Phieraggi shoot four blue lasers that rotate clockwise around the character. It retracts when not firing."
    /// "The uptime is not affected by Duration. Area increases the width of the lasers. Amount affects how many lasers
    /// are fired at once." "Each Revival gives Phieraggi +1 Amount and +1 Base Damage, caps at +10 each." "Phieraggi has
    /// a 0.2 second Hitbox Delay." "The lasers does not start at the character and is a little bit away, forming a
    /// circle of a blind spot." "Using up Revivals ... will decrease the number of laser fired and its damage", so the
    /// Revivals are read live.
    ///
    /// Numbers the page does not give (balance method, each with its reason):
    /// - Uptime 0.8 s: "The threshold for permanent uptime starts at -42% Cooldown"; 1.4 s x 0.58 = 0.81 s, the time a
    ///   laser must last for the next volley to begin as it ends.
    /// - Turn rate 90 degrees a second at Speed 100% (times Speed; "Negative speed makes it spin counterclockwise"):
    ///   with four lasers a quarter turn a second sweeps the whole ring once a second, about once per volley.
    /// - Reach: the lasers start 1.2 units out (just past the hero's body) and are 9 units long, about the Gun bullet's
    ///   reach on screen; 0.5 units wide at Area 100%.
    /// - Pierce: see RevenantWheelLaser.
    /// The lasers deal Normal damage like both guns (owner, 2026-09-26: guns are Normal). The page has no level table
    /// (only Limit Break), so it does not level. The look is the Clock Lancet's beam, tinted (not owner-approved art).
    /// </summary>
    public class RevenantWheelWeapon : AutoAttackWeapon
    {
        public override bool UsesPierce => true;
        public override float Knockback => 0.05f; // VS wiki Phieraggi: knockback 0.05

        /// <summary>The laser's look: a beam drawn along +Z for 20 units from its root (the freeze beam's shape).</summary>
        public GameObject LaserVisualPrefab;

        public const float VsBaseDamage = 15f;
        public const int VsAmount = 4;
        public const int VsPierce = 7;
        public const float VsCooldown = 1.4f;
        public const float VsInterval = 0.1f;
        public const float VsHitboxDelay = 0.2f;
        public const int MaxRevivalBonus = 10;
        public const float UptimeSeconds = 0.8f;
        public const float TurnDegreesPerSecond = 90f;
        public const float InnerRadius = 1.2f;
        public const float LaserLength = 9f;
        public const float LaserWidth = 0.5f;
        private const float VisualLength = 20f;

        // The two guns it replaces have 8 levels; the union has none of its own.
        public override int MaxLevel => 8;
        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        /// <summary>The holder's unused Revivals that count: "+1 per Revival, max +10".</summary>
        public static int RevivalBonus(int revivals) => Mathf.Clamp(revivals, 0, MaxRevivalBonus);
        public int CurrentRevivalBonus => RevivalBonus(OwnerStats != null ? OwnerStats.Revivals : 0);
        public float BaseDamage => VsBaseDamage + CurrentRevivalBonus;
        public int LaserCount => VsAmount + CurrentRevivalBonus + ExtraAmount;
        public int Pierce => VsPierce + ExtraPierce;
        public override float ArcanaZoneBaseDamage => BaseDamage;

        private readonly List<RevenantWheelLaser> _lasers = new List<RevenantWheelLaser>();
        private float _wheelAngle;

        /// <summary>Lasers out now (tests).</summary>
        public int ActiveLasers
        {
            get
            {
                int n = 0;
                foreach (var laser in _lasers) if (laser != null && laser.gameObject.activeSelf) n++;
                return n;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Update()
        {
            base.Update();
            // Clockwise seen from the game camera is a rising yaw.
            _wheelAngle = Mathf.Repeat(_wheelAngle + TurnDegreesPerSecond * SpeedMultiplier * Time.deltaTime, 360f);
            foreach (var laser in _lasers)
            {
                if (laser == null || !laser.gameObject.activeSelf) continue;
                if (Time.time >= laser.EndsAt) { laser.gameObject.SetActive(false); continue; } // "It retracts when not firing."
                laser.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, _wheelAngle + laser.OffsetDegrees, 0f));
            }
        }

        private void OnDisable()
        {
            foreach (var laser in _lasers) if (laser != null) laser.gameObject.SetActive(false);
        }

        protected override void Attack()
        {
            int count = Mathf.Max(1, LaserCount);
            // VS: the lasers of a volley come 0.1 s apart, spread evenly round the wheel.
            FireInSuccession(count, VsInterval, _laserShot ??= LaserShot);
        }

        private SuccessionShot _laserShot;

        private void LaserShot(int i, int count, Vector3 arg)
        {
            FireLaser(i * 360f / count);
        }

        private void FireLaser(float offsetDegrees)
        {
            var laser = FreeLaser();
            laser.OffsetDegrees = offsetDegrees;
            laser.EndsAt = Time.time + UptimeSeconds; // "The uptime is not affected by Duration."
            laser.Pierce = Pierce;
            var go = laser.gameObject;
            go.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, _wheelAngle + offsetDegrees, 0f));
            go.transform.localScale = new Vector3(AreaMultiplier, 1f, 1f); // "Area increases the width of the lasers."
            go.SetActive(true);

            var damage = go.GetComponent<ProjectileDamage>();
            damage.DamageAmount = ScaledDamage(BaseDamage);
            damage.DamageType = DamageType.Normal;
            damage.SourceWeaponName = "Revenant Wheel";
            laser.Arm(damage, go.GetComponent<HitboxDelay>(), VsHitboxDelay);
            AimAtHostiles(go);
        }

        /// <summary>A switched-off laser to reuse, or a new one.</summary>
        private RevenantWheelLaser FreeLaser()
        {
            foreach (var existing in _lasers)
                if (existing != null && !existing.gameObject.activeSelf) return existing;

            var go = new GameObject("RevenantWheelLaser");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.8f, InnerRadius + LaserLength * 0.5f);
            box.size = new Vector3(LaserWidth, 2f, LaserLength);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            go.AddComponent<ProjectileDamage>();
            go.AddComponent<HitboxDelay>();
            var laser = go.AddComponent<RevenantWheelLaser>();
            if (LaserVisualPrefab != null)
            {
                var visual = Instantiate(LaserVisualPrefab, go.transform);
                visual.name = "Visual";
                visual.transform.localPosition = new Vector3(0f, 0f, InnerRadius);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = new Vector3(1f, 1f, LaserLength / VisualLength);
                visual.SetActive(true);
                // A pooled-lifetime timer on the visual would switch it off for good; the laser decides when it shows.
                var pooled = visual.GetComponent<PooledLifetime>();
                if (pooled != null) Destroy(pooled);
            }
            _lasers.Add(laser);
            return laser;
        }
    }
}
