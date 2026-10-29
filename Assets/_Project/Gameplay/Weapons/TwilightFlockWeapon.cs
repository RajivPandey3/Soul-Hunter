using System.Collections;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Twilight Flock: the union of Peachone and Dusk Raven (own name, 2026-10-12; not yet owner-approved), built on VS
    /// Vandalier (vampire.survivors.wiki/w/Vandalier, raw, fetched 2026-10-12). Infobox: damage 28, area 2.2, speed 1
    /// (+20%), amount 20, duration 4.0 s, cooldown 1.0 s, interval 0.025 s, knockback 2, "Has two areas that circulate
    /// in both directions", "Acquisition will free a weapon slot". "By default, Vandalier fires 4 sets of 20
    /// projectiles. Each set fires all of its projectiles at about the same spot, set by the position of the visible
    /// zone at the instant that set starts, alternating between each zone."
    /// It has 8 levels of its own: L2 Area +20%; L3 Cooldown -0.3 s; L4 Area +20%; L5 Cooldown -0.3 s; L6 Area +20%;
    /// L7 Cooldown -0.3 s; L8 Area +20% ("Each Cooldown reduction level up actually reduces Cooldown by 0.25 seconds";
    /// max-area 3, max-cooldown 0.25 s). So it arrives at level 1 and is levelled like a weapon
    /// (UpgradeData.UpgradeType.TwilightFlock, WeaponUnionData.HasResultType).
    /// Area is given against the birds' 100%: 2.2 is the level-8 birds' own +120%. Soul Hunter's own numbers, as for
    /// Peachone: the 3-unit orbit, the 50 degrees a second pace (here x1.25, the wiki's Speed 1 against the birds' 80%)
    /// and the blast prefab's size. Damage is Magic, as Peachone's (owner, 2026-09-26: weapons typed by theme).
    /// The wiki's Gemini counterparts belong to the two base birds and stay on their own after the union
    /// (GeminiCounterpart); this weapon gets none.
    /// </summary>
    public class TwilightFlockWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        public override float Knockback => 2f; // VS wiki Vandalier: knockback 2
        protected override bool ShowsAttackPose => false; // an orbiting area weapon: no swing toward enemies

        public GameObject BirdVisual; // one copy flies over each zone
        public GameObject BombPrefab;
        public float CircleRadius = 3f;

        public const float VsBaseDamage = 28f;
        public const float VsBaseArea = 2.2f;
        public const int VsAmount = 20;
        public const float VsCooldown = 1f;
        public const float VsDuration = 4f;
        public const float VsSetInterval = 1f;   // "1 second roughly equal to 1 set by default"
        public const float VsInterval = 0.025f;
        /// <summary>Peachone's 50 degrees a second is VS Speed 80%; the union's Speed is 1 (+20%).</summary>
        public const float OrbitDegreesPerSecond = 50f * (1f / 0.8f);
        public const float BirdHeight = PeachoneWeapon.BirdHeight;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float LevelArea => VsBaseArea + 0.2f * ((Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        public float CooldownForLevel => VsCooldown - 0.25f * ((Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0));
        public int BombCount => VsAmount;
        public float ActiveSeconds => VsDuration * DurationMultiplier;

        /// <summary>Sets fired by the last activation, and how many of them each zone fired (tests).</summary>
        public int LastActivationSets { get; private set; }
        public readonly int[] SetsByZone = new int[2];

        private readonly GameObject[] _birds = new GameObject[2];
        private float _angle;

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = CooldownForLevel;
            if (BirdVisual != null)
                for (int i = 0; i < _birds.Length; i++)
                    _birds[i] = Instantiate(BirdVisual, transform.position, Quaternion.identity, transform);
        }

        /// <summary>Where zone 0 (clockwise) or zone 1 (counterclockwise) is now, on the ground around the holder.</summary>
        public Vector3 ZonePosition(int zone)
        {
            // Seen from the game camera a falling angle is clockwise. The two zones start on opposite sides.
            float degrees = zone == 0 ? -_angle : 180f + _angle;
            float rad = degrees * Mathf.Deg2Rad;
            return transform.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * CircleRadius;
        }

        protected override void Update()
        {
            base.Update();
            // VS: "Speed increases how fast the circle moves."
            _angle += Time.deltaTime * OrbitDegreesPerSecond * SpeedMultiplier;
            for (int i = 0; i < _birds.Length; i++)
            {
                if (_birds[i] == null) continue;
                Vector3 at = ZonePosition(i);
                Vector3 before = _birds[i].transform.position - Vector3.up * BirdHeight;
                _birds[i].transform.position = at + Vector3.up * BirdHeight;
                Vector3 heading = at - before;
                heading.y = 0f;
                if (heading.sqrMagnitude > 1e-6f) _birds[i].transform.rotation = Quaternion.LookRotation(heading);
            }
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            AttackCooldown = CooldownForLevel;
        }

        protected override void Attack()
        {
            if (BombPrefab == null) return;
            SetNextAttackIn(ActiveSeconds + CooldownForLevel * CooldownMultiplier);
            // As Peachone (CMB-09): an upgrade fires at once, so the running activation and its set stop and the new one
            // starts alone (bug hunt 2026-10-29: Twilight Flock still ran two).
            if (_activation != null) StopCoroutine(_activation);
            if (_bombingRun != null) StopCoroutine(_bombingRun);
            _bombingRun = null;
            _activation = StartCoroutine(Activation());
        }

        private Coroutine _activation, _bombingRun;

        private IEnumerator Activation()
        {
            LastActivationSets = 0;
            SetsByZone[0] = SetsByZone[1] = 0;
            float setInterval = VsSetInterval * CooldownMultiplier;
            float elapsed = 0f;
            while (elapsed < ActiveSeconds - 0.001f)
            {
                int zone = LastActivationSets % 2; // "alternating between each zone"
                LastActivationSets++;
                SetsByZone[zone]++;
                _bombingRun = StartCoroutine(BombingRun(ZonePosition(zone)));
                yield return new WaitForSeconds(setInterval);
                elapsed += setInterval;
            }
        }

        private IEnumerator BombingRun(Vector3 spot)
        {
            for (int i = 0; i < BombCount + ExtraAmount; i++)
            {
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, spot, Quaternion.identity)
                    : Instantiate(BombPrefab, spot, Quaternion.identity);
                if (bomb != null)
                {
                    // VS: "Area increases the Area-of-Effect of the projectiles, but not the size of the bombarding circle".
                    bomb.transform.localScale = BombPrefab.transform.localScale * AreaMultiplier * LevelArea;
                    var damageDealer = bomb.GetComponent<ProjectileDamage>();
                    if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                    damageDealer.DamageAmount = ScaledDamage(VsBaseDamage);
                    damageDealer.DamageType = DamageType.Magic;
                    damageDealer.SourceWeaponName = "Twilight Flock";
                    AimAtHostiles(bomb);

                    var lifetime = bomb.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(1.5f);
                    else Destroy(bomb, 1.5f);
                }
                yield return new WaitForSeconds(VsInterval * CooldownMultiplier);
            }
        }
    }
}
