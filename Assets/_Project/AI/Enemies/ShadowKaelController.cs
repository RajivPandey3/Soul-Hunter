using UnityEngine;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Shadow Kael is the Level 10 Boss that mirrors the player's combat loadout at encounter start:
    /// the player's stats (as a PlayerStats copy) and every weapon the player is using, at its level,
    /// retargeted at the player. Mirrored revivals bring the boss back without counting as a kill.
    /// </summary>
    public class ShadowKaelController : MonoBehaviour
    {
        public float MirroredMight;
        public float MirroredCooldown;
        public float MirroredArea;
        public float MirroredMoveSpeed;
        public int MirroredWeaponCount;
        public int MirroredArmor;
        public int MirroredRevivals;

        private HealthController _healthController;
        private bool _captured;
        // The weapons this encounter mirrored, removed when a pooled body starts a new encounter (Bug MIS AI-01).
        private readonly System.Collections.Generic.List<GameObject> _mirroredWeapons = new System.Collections.Generic.List<GameObject>();

        /// <summary>The boss's copy of the player's stats, read by every mirrored weapon.</summary>
        public PlayerStats MirroredStats { get; private set; }

        private void Start()
        {
            if (!_captured) CaptureFrom(FindFirstObjectByType<PlayerController>());
        }

        /// <summary>
        /// Encounter start: copies the player's stats and the weapons the player is really using (evolved and union
        /// weapons included, replaced base weapons excluded) at their levels. The copy is fixed from then on.
        /// EnemySpawner calls this as soon as the boss spawns; Start covers a boss placed any other way.
        /// </summary>
        public void CaptureFrom(PlayerController playerController)
        {
            if (_captured) return;
            _captured = true;
            ClearMirroredWeapons();

            PlayerStats stats = null;
            WeaponManager weapons = null;
            if (playerController != null)
            {
                stats = playerController.GetComponent<PlayerStats>();
                weapons = playerController.GetComponentInChildren<WeaponManager>();
                if (weapons == null) weapons = WeaponManager.Instance;
            }

            // Stats first: mirrored weapons read them in Awake/OnEnable, like the player's own.
            MirroredStats = GetComponent<PlayerStats>();
            if (MirroredStats == null) MirroredStats = gameObject.AddComponent<PlayerStats>();
            MirroredStats.CopyFrom(stats);
            MirroredMight = MirroredStats.Might;
            MirroredCooldown = MirroredStats.Cooldown;
            MirroredArea = MirroredStats.Area;
            MirroredMoveSpeed = MirroredStats.MoveSpeedMultiplier;
            MirroredArmor = MirroredStats.Armor;
            MirroredRevivals = MirroredStats.Revivals;

            MirroredWeaponCount = 0;
            if (weapons != null)
            {
                // The boss is drawn 3x bigger; its weapons keep the player's size.
                float bossScale = Mathf.Max(0.01f, transform.lossyScale.x);
                foreach (var pair in weapons.GetEquippedWeapons())
                {
                    var weaponInstance = Instantiate(pair.Key, transform);
                    _mirroredWeapons.Add(weaponInstance);
                    weaponInstance.transform.localScale = pair.Key.transform.localScale / bossScale;
                    // Retarget before the weapon's first attack.
                    ConfigureMirroredWeapon(weaponInstance);
                    weaponInstance.SetActive(true);
                    WeaponManager.ApplyWeaponLevel(weaponInstance, pair.Value.CurrentLevel);
                    MirroredWeaponCount++;
                }
                Debug.Log($"[ShadowKael] Mirrored {MirroredWeaponCount} weapons from the player.");
            }

            var enemyController = GetComponent<EnemyController>();
            if (enemyController != null)
                enemyController.ApplyCampaignSpeed(enemyController.CampaignSpeedMultiplier * MirroredMoveSpeed);

            // Armor reaches the boss's HealthController through the stats copy (PlayerStats.SyncArmor).
            // Revivals: a lethal hit is survived without OnDied, so a revival never counts as the boss kill.
            _healthController = GetComponent<HealthController>();
            if (_healthController != null) _healthController.TryPreventDeath = TryRevive;
        }

        /// <summary>
        /// The boss body is pooled (EnemySpawner._bossPools) and comes back in the next Endless cycle. Each spawn is a new
        /// encounter (AGENTS.md Level 10: "At encounter start, the relevant player weapon/stat loadout must be captured"),
        /// so leaving play ends this encounter's copy and the next CaptureFrom takes the player's loadout of that moment
        /// (Bug MIS AI-01: the body kept cycle 1's weapons, stats and spent revivals, and skipped the mirrored speed).
        /// </summary>
        private void OnDisable()
        {
            _captured = false;
        }

        /// <summary>Removes the previous encounter's mirrored weapons; they are switched off at once so they never fire.</summary>
        private void ClearMirroredWeapons()
        {
            for (int i = 0; i < _mirroredWeapons.Count; i++)
            {
                var old = _mirroredWeapons[i];
                if (old == null) continue;
                old.SetActive(false);
                Destroy(old);
            }
            _mirroredWeapons.Clear();
        }

        /// <summary>
        /// Lethal hit: spends one mirrored revival and restores the boss (VS revival: half health), else lets it die.
        /// </summary>
        private bool TryRevive()
        {
            if (MirroredRevivals <= 0 || _healthController == null) return false;
            MirroredRevivals--;
            _healthController.ReviveFromDeath();
            Debug.Log($"[ShadowKael] Revived; {MirroredRevivals} revivals left.");
            return true;
        }

        /// <summary>
        /// Learning Comment:
        /// Invert weapon targeting and collision layers on mirrored weapon instances.
        /// - Instantiated root aur recursive child transforms ko Enemy layer assign karte hain.
        /// - DamageCaster components ko Player layer target karne ke liye reconfigure karte hain.
        /// - ProjectileDamage aur TouchDamage components ke TargetTag ko "Player" par set karte hain.
        /// - GarlicWeapon aur WhipWeapon ke detection masks ko Player layer par set karte hain.
        /// </summary>
        public void ConfigureMirroredWeapon(GameObject weaponInstance)
        {
            if (weaponInstance == null) return;

            // 1. Assign root and all recursive child transforms to the Enemy layer
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                SetLayerRecursively(weaponInstance, enemyLayer);
            }

            int playerLayerMask = LayerMask.GetMask("Player");

            // 2. Reconfigure all DamageCaster components to target Player layer
            var damageCasters = weaponInstance.GetComponentsInChildren<DamageCaster>(true);
            foreach (var caster in damageCasters)
            {
                caster.TargetLayer = playerLayerMask;
            }

            // 3. Reconfigure any ProjectileDamage components to have TargetTag set to "Player"
            var projectileDamages = weaponInstance.GetComponentsInChildren<ProjectileDamage>(true);
            foreach (var proj in projectileDamages)
            {
                proj.TargetTag = "Player";
            }

            // 4. Reconfigure any TouchDamage components to have TargetTag set to "Player"
            var touchDamages = weaponInstance.GetComponentsInChildren<TouchDamage>(true);
            foreach (var touch in touchDamages)
            {
                touch.TargetTag = "Player";
            }

            // 5. Reconfigure GarlicWeapon and WhipWeapon detection masks to target Player layer
            var garlicWeapons = weaponInstance.GetComponentsInChildren<GarlicWeapon>(true);
            foreach (var garlic in garlicWeapons)
            {
                garlic.TargetLayer = playerLayerMask;
            }

            var whipWeapons = weaponInstance.GetComponentsInChildren<WhipWeapon>(true);
            foreach (var whip in whipWeapons)
            {
                whip.TargetLayer = playerLayerMask;
            }
        }

        /// <summary>
        /// Learning Comment:
        /// Recursively sets the GameObject layer for an object and all its children.
        /// Direct hierarchy traversal ensures zero runtime allocation.
        /// </summary>
        public static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
            }
        }
    }
}
