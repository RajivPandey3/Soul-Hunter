using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment:
    /// 100% Data-Driven Architecture (Strict Rule):
    /// Enemy ki speed, health, damage jaisi chizein script (MonoBehaviour) mein hardcode nahi honi chahiye.
    /// Ye ek ScriptableObject hoga jo 'ScriptableObjects' folder mein save hoga.
    /// Isse designer bina code chhue naye dushman (zombie, bat, boss) bana sakta hai.
    /// </summary>
    /// <summary>
    /// VS enemy movement skills (vampire.survivors.wiki, Enemies): most enemies home on the player; "Floaty" ones move
    /// in a wavy pattern; "Fixed Direction" ones pick a line when they appear and keep it, crossing the screen.
    /// </summary>
    public enum EnemyMovement
    {
        Homing = 0,
        Floaty = 1,
        FixedDirection = 2,
        /// <summary>
        /// VS statboxes that list both skills ("[[Floaty]], [[Fixed Direction]]": Sneaky Head, Big Sneaky Head; wiki
        /// Sneaky_Head, fetched 2026-10-09): the enemy keeps its Fixed Direction line and weaves along it.
        /// </summary>
        FloatyFixedDirection = 3,
    }

    [CreateAssetMenu(fileName = "New Enemy Data", menuName = "Soul Hunter/Data/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string EnemyName;
        /// <summary>
        /// A colour the model is drawn in (alpha 0 = its own colours). Level 1's VS enemy types share the Level 1 models
        /// until they have their own (owner, 2026-10-03), so each type is told apart by its tint and size.
        /// </summary>
        public Color VisualTint = new Color(0f, 0f, 0f, 0f);
        public int MaxHealth = 10;
        public float MoveSpeed = 3f;
        public int DamageToPlayer = 5;

        [Tooltip("VS knockback taken: how far a hit pushes this enemy back (bats and skeletons 1, heavy 0.3, ghosts 0)")]
        [Min(0f)] public float Knockback = 1f;
        [Tooltip("VS: the most knockback this enemy can take after Garlic raised it")]
        [Min(0f)] public float MaxKnockback = 3f;

        [Tooltip("VS movement skill: Homing (default), Floaty (wavy), FixedDirection (straight line across the screen)")]
        public EnemyMovement Movement = EnemyMovement.Homing;
        [Tooltip("Floaty: sideways weave at its widest, as a fraction of the forward direction. Provisional")]
        [Range(0f, 1.5f)] public float FloatyWeave = 0.7f;
        [Tooltip("Floaty: seconds for one full weave left and right. Provisional")]
        [Min(0.2f)] public float FloatyPeriodSeconds = 1.8f;
        public float DropChanceGem = 95f;
        public float DropChanceChicken = 5f;
        [Tooltip("% chance (before Luck) to drop a gold coin on death")]
        public float DropChanceGold = 2f;
        [Tooltip("Gold in each dropped coin, before Greed")]
        public int GoldValue = 1;

        [Tooltip("XP in the Soul Gem this enemy drops (VS: tougher enemies drop bigger gems). Provisional: 10 enemy, 50 elite, 250 boss")]
        [Min(1)] public int ExperienceValue = 10;

        [Tooltip("Agar sach hai, toh ye dushman (Boss) marne par Chest (Khazana) giraega")]
        public bool DropsChest = false;

        [Tooltip("VS boss rule: when above 0, max health is this x the player's level (MaxHealth is then ignored)")]
        [Min(0)] public int HealthPerPlayerLevel = 0;
        [Tooltip("VS HP x Level with a fraction (wiki Skullone (scaling): health 6.5): added to HealthPerPlayerLevel, and " +
                 "the product is rounded. Only used while HealthPerPlayerLevel is above 0")]
        [Range(0f, 0.99f)] public float HealthPerPlayerLevelFraction = 0f;

        [Tooltip("VS lives (wiki Scarleton: \"Has 3 lives, meaning that it can resurrect twice\"). 1 = an ordinary enemy")]
        [Min(1)] public int Lives = 1;

        // ---- VS resistances (vampire.survivors.wiki/w/Enemies, "Resistances"). Defaults: none, as before. ----
        [Tooltip("VS res-freeze: a weapon freeze (Clock Lancet) fails when this is higher than the weapon's freeze chance. " +
                 "Orologion ignores it. Garlic lowers it by 0.1 per hit unless the enemy resists debuffs. VS bosses: 1.1")]
        [Min(0f)] public float FreezeResistance = 0f;
        [Tooltip("VS res-kill: immune to instant kills (Pentagram, Soul Toll / Rosary)")]
        public bool InstantKillResistance = false;
        [Tooltip("VS res-debuff: immune to Garlic's knockback and freeze-resistance reductions")]
        public bool DebuffResistance = false;
        [Tooltip("VS res-defang (wiki statboxes, e.g. Trinacria: res-defang 1): the Defang PowerUp never makes it harmless")]
        public bool DefangResistance = false;

        [Tooltip("VS enemy/boss skills (EnemyAbilities runs them). Empty = an ordinary enemy")]
        public System.Collections.Generic.List<EnemyAbilitySpec> Abilities = new System.Collections.Generic.List<EnemyAbilitySpec>();

        /// <summary>Max health before Curse: per-level health for level-scaled enemies (bosses), else MaxHealth.</summary>
        public int HealthForPlayerLevel(int playerLevel) =>
            HealthPerPlayerLevel > 0
                ? Mathf.Max(1, Mathf.RoundToInt((HealthPerPlayerLevel + Mathf.Clamp01(HealthPerPlayerLevelFraction)) * Mathf.Max(1, playerLevel)))
                : MaxHealth;
    }
}
