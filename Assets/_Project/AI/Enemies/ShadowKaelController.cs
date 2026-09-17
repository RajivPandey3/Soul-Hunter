using UnityEngine;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Shadow Kael is the Level 10 Boss that mirrors the player's combat loadout at spawn.
    /// This controller reads the player's stats and weapons at initialization.
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

        private void Start()
        {
            var playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                var player = playerController.gameObject;
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    MirroredMight = stats.Might;
                    MirroredCooldown = stats.Cooldown;
                    MirroredArea = stats.Area;
                    MirroredMoveSpeed = stats.MoveSpeedMultiplier;
                    MirroredArmor = stats.Armor;
                    MirroredRevivals = stats.Revivals;
                }

                var weapons = player.GetComponentInChildren<WeaponManager>();
                if (weapons != null)
                {
                    MirroredWeaponCount = weapons.GetActiveWeaponsCount();
                    Debug.Log($"[ShadowKael] Mirrored {MirroredWeaponCount} active weapons from player.");
                    
                    var activeWeaponTypes = weapons.GetActiveWeaponTypes();
                    foreach (var type in activeWeaponTypes)
                    {
                        var prefab = weapons.GetWeaponObject(type);
                        if (prefab != null)
                        {
                            var weaponInstance = Instantiate(prefab, this.transform);
                            weaponInstance.SetActive(true);
                            
                            // Optionally set the weapon's level based on mirrored stats or player's level
                            var autoAttackWeapon = weaponInstance.GetComponent<AutoAttackWeapon>();
                            if (autoAttackWeapon != null)
                            {
                                int playerLevel = weapons.GetWeaponLevel(type);
                                while (autoAttackWeapon.CurrentLevel < playerLevel)
                                {
                                    int previousLevel = autoAttackWeapon.CurrentLevel;
                                    autoAttackWeapon.LevelUp();
                                    if (autoAttackWeapon.CurrentLevel <= previousLevel)
                                        break;
                                }
                                
                                autoAttackWeapon.DamageAmount *= MirroredMight;
                                autoAttackWeapon.AttackCooldown *= MirroredCooldown;
                            }
                            
                            weaponInstance.transform.localScale *= MirroredArea;
                            
                            // Reconfigure mirrored weapon layers and targeting to attack Player
                            ConfigureMirroredWeapon(weaponInstance);
                        }
                    }
                }
                
                var enemyController = GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.ApplyCampaignSpeed(MirroredMoveSpeed);
                }
            }

            // Learning Comment:
            // Attached HealthController ko setup karte hain:
            // 1. DamageModifier ke zariye MirroredArmor ka flat damage reduction lagate hain (minimum 1 damage).
            // 2. OnDied event subscribe karte hain taake lethal damage par revival consume ho sake.
            _healthController = GetComponent<HealthController>();
            if (_healthController != null)
            {
                _healthController.DamageModifier = ApplyArmorDamageReduction;
                _healthController.OnDied -= HandleDeath;
                _healthController.OnDied += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            // Learning Comment:
            // Memory leak se bachne ke liye attached HealthController ke OnDied event se unsubscribe karte hain.
            if (_healthController != null)
            {
                _healthController.OnDied -= HandleDeath;
            }
        }

        /// <summary>
        /// Learning Comment:
        /// Applies flat armor damage reduction from MirroredArmor to incoming damage,
        /// ensuring a minimum damage of 1 is always dealt.
        /// </summary>
        public int ApplyArmorDamageReduction(int incomingDamage)
        {
            return Mathf.Max(1, incomingDamage - MirroredArmor);
        }

        /// <summary>
        /// Learning Comment:
        /// Handles death event of Shadow Kael. If MirroredRevivals > 0,
        /// decrements MirroredRevivals and calls HealthController.ReviveFromDeath()
        /// to restore boss health and keep the boss alive.
        /// </summary>
        private void HandleDeath()
        {
            if (MirroredRevivals > 0)
            {
                MirroredRevivals--;
                if (_healthController == null)
                {
                    _healthController = GetComponent<HealthController>();
                }

                if (_healthController != null)
                {
                    _healthController.ReviveFromDeath();
                }
            }
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
