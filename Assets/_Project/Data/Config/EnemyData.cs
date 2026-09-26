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
    }

    [CreateAssetMenu(fileName = "New Enemy Data", menuName = "Soul Hunter/Data/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string EnemyName;
        public int MaxHealth = 10;
        public float MoveSpeed = 3f;
        public int DamageToPlayer = 5;

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

        /// <summary>Max health before Curse: per-level health for level-scaled enemies (bosses), else MaxHealth.</summary>
        public int HealthForPlayerLevel(int playerLevel) =>
            HealthPerPlayerLevel > 0 ? HealthPerPlayerLevel * Mathf.Max(1, playerLevel) : MaxHealth;
    }
}
