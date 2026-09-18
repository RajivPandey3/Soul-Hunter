using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment:
    /// AchievementManagerTests verifies the deterministic architecture and unlock evaluation rules
    /// of AchievementManager in EditMode.
    ///
    /// Core Contracts Under Test:
    /// 1. Deterministic Dependency Injection: Configure(), Initialize(), and SetDependencies()
    ///    explicitly inject GameSessionManager, RunStatsTracker, and WeaponManager without relying on runtime scene searches.
    /// 2. Lifecycle Event Management: Subscriptions bind to injected dependencies on active/OnEnable,
    ///    cleanly unbind on OnDisable or re-Configure, and avoid duplicate callbacks.
    /// 3. Unlock Conditions:
    ///    - SurviveTime: Survival seconds evaluated against target threshold.
    ///    - KillCount: Bound RunStatsTracker TotalKills evaluated against target threshold.
    ///    - WeaponLevelUp: Bound WeaponManager weapon level evaluated against target threshold, handling invalid weapon enums safely.
    /// 4. Edge Cases & Idempotency: Unlocked achievements are not re-unlocked, null catalog elements are safely ignored,
    ///    and missing dependencies do not throw exceptions.
    /// 5. Zero-Leak Lifecycle: All instantiated GameObjects and ScriptableObjects are destroyed immediately in TearDown.
    /// </summary>
    public class AchievementManagerTests
    {
        private List<GameObject> _createdGameObjects;
        private List<ScriptableObject> _createdScriptableObjects;

        private AchievementManager _manager;
        private GameSessionManager _session;
        private RunStatsTracker _stats;
        private WeaponManager _weapons;

        [SetUp]
        public void SetUp()
        {
            // Learning Comment:
            // Har test ke liye clean tracking lists banate hain taake TearDown mein sab cleanup ho sake.
            _createdGameObjects = new List<GameObject>();
            _createdScriptableObjects = new List<ScriptableObject>();

            // Learning Comment:
            // Dependencies ko inactive GameObjects par instantiate karte hain taake Awake() ke static singletons
            // global state ko pollute na karein, aur deterministic injection ko isolated verify kiya ja sake.
            var sessionGo = CreateGameObject("TestGameSessionManager");
            sessionGo.SetActive(false);
            _session = sessionGo.AddComponent<GameSessionManager>();

            var statsGo = CreateGameObject("TestRunStatsTracker");
            statsGo.SetActive(false);
            _stats = statsGo.AddComponent<RunStatsTracker>();

            var weaponsGo = CreateGameObject("TestWeaponManager");
            weaponsGo.SetActive(false);
            _weapons = weaponsGo.AddComponent<WeaponManager>();

            var managerGo = CreateGameObject("TestAchievementManager");
            managerGo.SetActive(false);
            _manager = managerGo.AddComponent<AchievementManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // Learning Comment:
            // Memory aur hierarchy leaks rokne ke liye tamam test GameObjects aur ScriptableObjects ko DestroyImmediate se delete karte hain.
            if (_createdGameObjects != null)
            {
                for (int i = _createdGameObjects.Count - 1; i >= 0; i--)
                {
                    if (_createdGameObjects[i] != null)
                    {
                        Object.DestroyImmediate(_createdGameObjects[i]);
                    }
                }
                _createdGameObjects.Clear();
            }

            if (_createdScriptableObjects != null)
            {
                for (int i = _createdScriptableObjects.Count - 1; i >= 0; i--)
                {
                    if (_createdScriptableObjects[i] != null)
                    {
                        Object.DestroyImmediate(_createdScriptableObjects[i]);
                    }
                }
                _createdScriptableObjects.Clear();
            }

            // Learning Comment:
            // Agar kisi test execution ke doran static Instance assign ho gaya ho, tou usay null reset karte hain.
            ResetStaticSingleton<RunStatsTracker>(nameof(RunStatsTracker.Instance));
            ResetStaticSingleton<GameSessionManager>(nameof(GameSessionManager.Instance));
            ResetStaticSingleton<WeaponManager>(nameof(WeaponManager.Instance));
        }

        #region Helper Methods

        private GameObject CreateGameObject(string name = "TestGameObject")
        {
            var go = new GameObject(name);
            _createdGameObjects.Add(go);
            return go;
        }

        private AchievementData CreateAchievement(string name, AchievementData.AchievementType type, float targetValue, string targetWeaponName = null)
        {
            var ach = ScriptableObject.CreateInstance<AchievementData>();
            ach.AchievementName = name;
            ach.Type = type;
            ach.TargetValue = targetValue;
            ach.TargetWeaponName = targetWeaponName;
            ach.UnlockedItemName = $"{name}_Reward";
            _createdScriptableObjects.Add(ach);
            return ach;
        }

        private UpgradeData CreateUpgradeData(UpgradeData.UpgradeType type, int level)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.UpgradeName = $"{type}_Level_{level}";
            upgrade.Type = type;
            upgrade.Level = level;
            _createdScriptableObjects.Add(upgrade);
            return upgrade;
        }

        private void TriggerSurvivalSecond(GameSessionManager session, int second)
        {
            // Learning Comment:
            // GameSessionManager ka private OnSurvivalSecondChanged delegate field reflection ke zariye invoke karte hain
            // taake EditMode mein bina real-time update loop ke timer tick simulate kiya ja sake.
            var field = typeof(GameSessionManager).GetField(
                nameof(GameSessionManager.OnSurvivalSecondChanged),
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                var fields = typeof(GameSessionManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (f.FieldType == typeof(Action<int>))
                    {
                        field = f;
                        break;
                    }
                }
            }

            var handler = field?.GetValue(session) as Action<int>;
            handler?.Invoke(second);
        }

        private static void InvokeEditModeLifecycle(MonoBehaviour behaviour, string methodName)
        {
            // Plain EditMode tests do not provide a Player frame, so invoke lifecycle
            // callbacks explicitly whenever the contract under test depends on them.
            var method = behaviour.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Expected lifecycle method {methodName} was not found.");
            method.Invoke(behaviour, null);
        }

        private void ResetStaticSingleton<T>(string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            prop?.GetSetMethod(true)?.Invoke(null, new object[] { null });
        }

        #endregion

        #region Dependency Injection Tests

        [Test]
        public void Configure_InjectsDependenciesDeterministically_ExposesProperties()
        {
            // Learning Comment:
            // Configure() method scene lookups ke baghair GameSessionManager, RunStatsTracker, aur WeaponManager
            // ko deterministically bind karta hai aur unke public properties ko expose karta hai.
            _manager.Configure(_session, _stats, _weapons);

            Assert.That(_manager.Session, Is.SameAs(_session));
            Assert.That(_manager.Stats, Is.SameAs(_stats));
            Assert.That(_manager.Weapons, Is.SameAs(_weapons));
        }

        [Test]
        public void Initialize_InjectsDependenciesDeterministically_ExposesProperties()
        {
            // Learning Comment:
            // Initialize() Configure() ka direct alias hai aur wohi deterministic injection ensure karta hai.
            _manager.Initialize(_session, _stats, _weapons);

            Assert.That(_manager.Session, Is.SameAs(_session));
            Assert.That(_manager.Stats, Is.SameAs(_stats));
            Assert.That(_manager.Weapons, Is.SameAs(_weapons));
        }

        [Test]
        public void SetDependencies_InjectsDependenciesDeterministically_ExposesProperties()
        {
            // Learning Comment:
            // SetDependencies() bhi Configure() ka contract follow karta hai for setup scripts.
            _manager.SetDependencies(_session, _stats, _weapons);

            Assert.That(_manager.Session, Is.SameAs(_session));
            Assert.That(_manager.Stats, Is.SameAs(_stats));
            Assert.That(_manager.Weapons, Is.SameAs(_weapons));
        }

        #endregion

        #region Event Subscription & Unsubscription Tests

        [Test]
        public void ActiveManager_SubscribesToEvents_AndUnlocksOnKillsChanged()
        {
            // Learning Comment:
            // Active aur enabled state mein AchievementManager injected tracker ke OnKillsChanged event par listen karta hai.
            var ach = CreateAchievement("Kills_1", AchievementData.AchievementType.KillCount, 1);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.False);

            _stats.AddKill();

            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.True);
        }

        [Test]
        public void ActiveManager_SubscribesToEvents_AndUnlocksOnSurvivalSecondChanged()
        {
            // Learning Comment:
            // Active aur enabled state mein session ke OnSurvivalSecondChanged event se timer updates receive hoti hain.
            var ach = CreateAchievement("Survive_30", AchievementData.AchievementType.SurviveTime, 30);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            Assert.That(_manager.IsAchievementUnlocked("Survive_30"), Is.False);

            TriggerSurvivalSecond(_session, 30);

            Assert.That(_manager.IsAchievementUnlocked("Survive_30"), Is.True);
        }

        [Test]
        public void ActiveManager_SubscribesToEvents_AndUnlocksOnWeaponAcquiredOrUpgraded()
        {
            // Learning Comment:
            // Active aur enabled state mein WeaponManager ke upgrade event par achievements evaluate hoti hain.
            var ach = CreateAchievement("Whip_Level2", AchievementData.AchievementType.WeaponLevelUp, 2, "Whip");
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            Assert.That(_manager.IsAchievementUnlocked("Whip_Level2"), Is.False);

            var upgrade = CreateUpgradeData(UpgradeData.UpgradeType.Whip, 2);
            _weapons.ApplyUpgrade(upgrade);

            Assert.That(_manager.IsAchievementUnlocked("Whip_Level2"), Is.True);
        }

        [Test]
        public void DisabledManager_UnsubscribesFromEvents_DoesNotTriggerUnlocks()
        {
            // Learning Comment:
            // OnDisable() par tamam events unsubscribe ho jate hain taake inactive state mein callbacks execute na hon.
            var ach = CreateAchievement("Kills_1", AchievementData.AchievementType.KillCount, 1);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            // Component ko disable karte hain
            _manager.enabled = false;
            InvokeEditModeLifecycle(_manager, "OnDisable");

            _stats.AddKill();

            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.False,
                "Disabled AchievementManager should not respond to events.");
        }

        [Test]
        public void ReenablingManager_ResubscribesToEvents_ReceivesSubsequentCallbacks()
        {
            // Learning Comment:
            // Disable ke baad re-enable karne par OnEnable() dobara subscriptions establish karta hai.
            var ach = CreateAchievement("Kills_2", AchievementData.AchievementType.KillCount, 2);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            _manager.enabled = false;
            InvokeEditModeLifecycle(_manager, "OnDisable");
            _stats.AddKill();
            Assert.That(_manager.IsAchievementUnlocked("Kills_2"), Is.False);

            _manager.enabled = true;
            InvokeEditModeLifecycle(_manager, "OnEnable");
            _stats.AddKill();

            Assert.That(_manager.IsAchievementUnlocked("Kills_2"), Is.True,
                "Re-enabled AchievementManager must receive events and unlock achievement.");
        }

        [Test]
        public void ReConfigure_UnsubscribesOldDependencies_AndSubscribesNewDependencies()
        {
            // Learning Comment:
            // Re-Configure() purane dependencies se events unbind karta hai aur naye dependencies ko bind karta hai.
            var ach = CreateAchievement("Kills_1", AchievementData.AchievementType.KillCount, 1);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            // Nayi dependencies create karte hain
            var newStatsGo = CreateGameObject("NewStatsTracker");
            newStatsGo.SetActive(false);
            var newStats = newStatsGo.AddComponent<RunStatsTracker>();

            var newSessionGo = CreateGameObject("NewSessionManager");
            newSessionGo.SetActive(false);
            var newSession = newSessionGo.AddComponent<GameSessionManager>();

            var newWeaponsGo = CreateGameObject("NewWeaponManager");
            newWeaponsGo.SetActive(false);
            var newWeapons = newWeaponsGo.AddComponent<WeaponManager>();

            // Re-configure with new dependencies
            _manager.Configure(newSession, newStats, newWeapons);

            // Purane stats par kill trigger karne se unlock NAHI hona chahiye
            _stats.AddKill();
            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.False,
                "Events from old dependencies must not trigger callbacks after re-Configure.");

            // Naye stats par kill trigger karne se unlock HONA chahiye
            newStats.AddKill();
            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.True,
                "Events from new dependencies must trigger callbacks after re-Configure.");
        }

        #endregion

        #region SurviveTime Condition Evaluation Tests

        [Test]
        public void SurviveTime_ThresholdUnmet_DoesNotUnlock()
        {
            // Learning Comment:
            // Jab tak survived seconds TargetValue se kam hain, achievement lock rehni chahiye.
            var ach = CreateAchievement("Survive_60s", AchievementData.AchievementType.SurviveTime, 60);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            TriggerSurvivalSecond(_session, 59);

            Assert.That(_manager.IsAchievementUnlocked("Survive_60s"), Is.False);
            Assert.That(_manager.UnlockedAchievements, Does.Not.Contain("Survive_60s"));
        }

        [Test]
        public void SurviveTime_ThresholdMet_UnlocksAchievement()
        {
            // Learning Comment:
            // Jab survived seconds TargetValue ke barabar ya us se zyada hon, achievement unlock honi chahiye.
            var ach = CreateAchievement("Survive_60s", AchievementData.AchievementType.SurviveTime, 60);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            TriggerSurvivalSecond(_session, 60);

            Assert.That(_manager.IsAchievementUnlocked("Survive_60s"), Is.True);
            Assert.That(_manager.UnlockedAchievements, Contains.Item("Survive_60s"));
        }

        #endregion

        #region KillCount Condition Evaluation Tests

        [Test]
        public void KillCount_ThresholdUnmet_DoesNotUnlock()
        {
            // Learning Comment:
            // Injected RunStatsTracker ka TotalKills TargetValue se kam hone par achievement unlock nahi hoti.
            var ach = CreateAchievement("Kills_10", AchievementData.AchievementType.KillCount, 10);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            for (int i = 0; i < 9; i++)
            {
                _stats.AddKill();
            }

            Assert.That(_stats.TotalKills, Is.EqualTo(9));
            Assert.That(_manager.IsAchievementUnlocked("Kills_10"), Is.False);
        }

        [Test]
        public void KillCount_ThresholdMet_UnlocksAchievement()
        {
            // Learning Comment:
            // Injected RunStatsTracker ka TotalKills TargetValue tak pohanchne par achievement unlock hoti hai.
            var ach = CreateAchievement("Kills_10", AchievementData.AchievementType.KillCount, 10);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            for (int i = 0; i < 10; i++)
            {
                _stats.AddKill();
            }

            Assert.That(_stats.TotalKills, Is.EqualTo(10));
            Assert.That(_manager.IsAchievementUnlocked("Kills_10"), Is.True);
            Assert.That(_manager.UnlockedAchievements, Contains.Item("Kills_10"));
        }

        #endregion

        #region WeaponLevelUp Condition Evaluation Tests

        [Test]
        public void WeaponLevelUp_LevelUnmet_DoesNotUnlock()
        {
            // Learning Comment:
            // Injected WeaponManager mein weapon ka level TargetValue se kam hone par unlock nahi hota.
            var ach = CreateAchievement("Whip_Level5", AchievementData.AchievementType.WeaponLevelUp, 5, "Whip");
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            _weapons.GiveWeapon(UpgradeData.UpgradeType.Whip, 4);
            _manager.CheckAchievements();

            Assert.That(_manager.IsAchievementUnlocked("Whip_Level5"), Is.False);
        }

        [Test]
        public void WeaponLevelUp_LevelMet_UnlocksAchievement()
        {
            // Learning Comment:
            // Injected WeaponManager mein weapon ka level TargetValue tak pohanchte hi unlock trigger hota hai.
            var ach = CreateAchievement("Whip_Level5", AchievementData.AchievementType.WeaponLevelUp, 5, "Whip");
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            _weapons.GiveWeapon(UpgradeData.UpgradeType.Whip, 5);
            _manager.CheckAchievements();

            Assert.That(_manager.IsAchievementUnlocked("Whip_Level5"), Is.True);
            Assert.That(_manager.UnlockedAchievements, Contains.Item("Whip_Level5"));
        }

        [Test]
        public void WeaponLevelUp_InvalidWeaponEnumName_HandledSafelyWithoutException()
        {
            // Learning Comment:
            // Agar TargetWeaponName kisi valid UpgradeType enum se match na kare,
            // tou Enum.TryParse safely false return kare aur koi unhandled exception throw na ho.
            var ach = CreateAchievement("Invalid_Weapon_Ach", AchievementData.AchievementType.WeaponLevelUp, 1, "NonExistentSuperWeapon_999");
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            Assert.DoesNotThrow(() => _manager.CheckAchievements());
            Assert.That(_manager.IsAchievementUnlocked("Invalid_Weapon_Ach"), Is.False);
        }

        [Test]
        public void WeaponLevelUp_NullOrEmptyWeaponName_HandledSafelyWithoutException()
        {
            // Learning Comment:
            // Null ya empty TargetWeaponName par bhi system crash nahi hota aur safely locked rehta hai.
            var ach = CreateAchievement("Null_Weapon_Ach", AchievementData.AchievementType.WeaponLevelUp, 1, null);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            Assert.DoesNotThrow(() => _manager.CheckAchievements());
            Assert.That(_manager.IsAchievementUnlocked("Null_Weapon_Ach"), Is.False);
        }

        #endregion

        #region Idempotency & Edge Cases Tests

        [Test]
        public void AlreadyUnlockedAchievement_IsNotReUnlocked_Idempotent()
        {
            // Learning Comment:
            // Pehle se unlock shuda achievement ko dobara unlock nahi kiya jata (idempotency maintain hoti hai).
            var ach = CreateAchievement("Survive_10s", AchievementData.AchievementType.SurviveTime, 10);
            _manager.SetAchievements(new List<AchievementData> { ach });
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            TriggerSurvivalSecond(_session, 10);
            Assert.That(_manager.IsAchievementUnlocked("Survive_10s"), Is.True);
            Assert.That(_manager.UnlockedAchievements.Count, Is.EqualTo(1));

            // Dobara trigger karne par count 1 hi rehna chahiye
            TriggerSurvivalSecond(_session, 20);
            _manager.CheckAchievements();

            Assert.That(_manager.UnlockedAchievements.Count, Is.EqualTo(1));
        }

        [Test]
        public void CatalogWithNullEntries_SafelySkipped_ValidAchievementsStillUnlock()
        {
            // Learning Comment:
            // Agar achievements catalog list mein null elements hon, tou unhe safely skip kiya jata hai
            // aur valid elements bina error ke evaluate aur unlock hote hain.
            var validAch = CreateAchievement("Valid_Survive", AchievementData.AchievementType.SurviveTime, 5);
            var catalog = new List<AchievementData> { null, validAch, null };

            _manager.SetAchievements(catalog);
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);
            InvokeEditModeLifecycle(_manager, "OnEnable");

            Assert.DoesNotThrow(() => TriggerSurvivalSecond(_session, 5));
            Assert.That(_manager.IsAchievementUnlocked("Valid_Survive"), Is.True);
        }

        [Test]
        public void NullCatalog_SafelyHandled_WithoutThrowing()
        {
            // Learning Comment:
            // Agar _allAchievements list hi null ho, tou CheckAchievements() safely return kare.
            _manager.SetAchievements(null);
            _manager.Configure(_session, _stats, _weapons);
            _manager.gameObject.SetActive(true);

            Assert.DoesNotThrow(() => _manager.CheckAchievements());
        }

        [Test]
        public void NullDependencies_SafelyHandled_WithoutThrowing()
        {
            // Learning Comment:
            // Agar dependencies null inject ki jayein, tou lifecycle methods ya checks par NullReferenceException nahi aana chahiye.
            var killAch = CreateAchievement("Kills_1", AchievementData.AchievementType.KillCount, 1);
            var wepAch = CreateAchievement("Whip_1", AchievementData.AchievementType.WeaponLevelUp, 1, "Whip");
            var timeAch = CreateAchievement("Time_1", AchievementData.AchievementType.SurviveTime, 1);

            _manager.SetAchievements(new List<AchievementData> { killAch, wepAch, timeAch });

            Assert.DoesNotThrow(() => _manager.Configure(null, null, null));
            Assert.That(_manager.Session, Is.Null);
            Assert.That(_manager.Stats, Is.Null);
            Assert.That(_manager.Weapons, Is.Null);

            Assert.DoesNotThrow(() => _manager.gameObject.SetActive(true));
            Assert.DoesNotThrow(() => _manager.CheckAchievements());
            Assert.DoesNotThrow(() => _manager.enabled = false);

            Assert.That(_manager.IsAchievementUnlocked("Kills_1"), Is.False);
            Assert.That(_manager.IsAchievementUnlocked("Whip_1"), Is.False);
        }

        #endregion
    }
}
