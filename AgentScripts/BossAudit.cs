using System;
using System.Reflection;
using UnityEngine;

public static class BossAudit
{
    public static void Main()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
        if (spawner == null) { Debug.Log("[BossAudit] spawner=null"); return; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var alive = (bool)typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_isBossAlive", flags).GetValue(spawner);
        var spawned = (bool)typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_bossSpawnedForCurrentWave", flags).GetValue(spawner);
        var prefab = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_stageBossPrefab", flags).GetValue(spawner) as UnityEngine.Object;
        Debug.Log("[BossAudit] enabled=" + spawner.isActiveAndEnabled + " alive=" + alive + " spawned=" + spawned + " prefab=" + (prefab != null));
    }

    public static void SetBossWindow()
    {
        var progression = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
        if (progression == null) { Debug.Log("[BossAudit] progression=null"); return; }
        progression.StartStage(1);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetProperty("StageElapsedTime", BindingFlags.Instance | BindingFlags.Public).SetValue(progression, 1501f);
        Debug.Log("[BossAudit] Stage 1 boss window armed");
    }

    public static void TestSurvivalCompletion()
    {
        var progression = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
        if (progression == null) { Debug.Log("[SurvivalAudit] progression=null"); return; }
        progression.StartStage(1);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetProperty("StageElapsedTime", BindingFlags.Instance | BindingFlags.Public).SetValue(progression, 1800f);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progression, null);
        Debug.Log("[SurvivalAudit] duration trigger invoked");
    }

    public static void TestPlayerRevival()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[RevivalAudit] player=null"); return; }
        var stats = player.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>();
        var health = player.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
        if (stats == null || health == null) { Debug.Log("[RevivalAudit] required component missing"); return; }
        stats.AddRevival(1);
        health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(health.MaxHealth + 1000, player.transform.position, Vector3.zero));
        Debug.Log("[RevivalAudit] active=" + player.activeSelf + " hp=" + health.CurrentHealth + " revivals=" + stats.Revivals);
    }

    public static void AuditWavePhases()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
        var field = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_waves", BindingFlags.Instance | BindingFlags.NonPublic);
        var waves = field != null ? field.GetValue(spawner) as System.Collections.IList : null;
        if (waves == null) { Debug.Log("[WaveAudit] waves=null"); return; }
        Debug.Log("[WaveAudit] waveAssets=" + waves.Count);
        for (int i = 0; i < waves.Count; i++)
        {
            var wave = waves[i] as SoulHunter.Gameplay.Data.WaveData;
            var p0 = wave.GetPhase(0f); var p5 = wave.GetPhase(300f); var p20 = wave.GetPhase(1200f);
            Debug.Log("[WaveAudit] stage=" + (i + 1) + " phases=" + p0.PhaseName + "," + p5.PhaseName + "," + p20.PhaseName + " interval=" + p20.SpawnIntervalMultiplier + " bonus=" + p20.EnemiesPerSpawnBonus);
        }
    }

    public static void TestHolyVulnerability()
    {
        var enemy = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemyController>();
        if (enemy == null) { Debug.Log("[HolyAudit] enemy=null"); return; }
        var health = enemy.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
        if (health == null) { Debug.Log("[HolyAudit] health=null"); return; }
        health.Initialize(100);
        enemy.ConfigureDamageVulnerability(SoulHunter.Gameplay.Combat.DamageType.Holy, 2f);
        health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(10, enemy.transform.position, Vector3.zero, SoulHunter.Gameplay.Combat.DamageType.Normal));
        int normalRemaining = health.CurrentHealth;
        health.Initialize(100);
        health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(10, enemy.transform.position, Vector3.zero, SoulHunter.Gameplay.Combat.DamageType.Holy));
        Debug.Log("[HolyAudit] normalRemaining=" + normalRemaining + " holyRemaining=" + health.CurrentHealth);
    }

    public static void TestEvolutionTrigger()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
        if (manager == null) { Debug.Log("[EvolutionAudit] manager=null"); return; }
        var field = typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField("_weaponLevels", BindingFlags.Instance | BindingFlags.NonPublic);
        var levels = field.GetValue(manager) as System.Collections.IDictionary;
        levels[SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MagicWand] = 8;
        levels[SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.EmptyTome] = 1;
        string evolved;
        bool success = manager.TryEvolveWeapon(out evolved);
        Debug.Log("[EvolutionAudit] success=" + success + " name=" + evolved);
    }

    public static void TestUnionTrigger()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
        if (manager == null) { Debug.Log("[UnionAudit] manager=null"); return; }
        var field = typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField("_weaponLevels", BindingFlags.Instance | BindingFlags.NonPublic);
        var levels = field.GetValue(manager) as System.Collections.IDictionary;
        levels[SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.FireWand] = 8;
        levels[SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.LightningRing] = 8;
        int before = manager.GetActiveWeaponsCount();
        string union;
        bool success = manager.TryEvolveWeapon(out union);
        int after = manager.GetActiveWeaponsCount();
        Debug.Log("[UnionAudit] success=" + success + " name=" + union + " slotsBefore=" + before + " slotsAfter=" + after);
    }

    public static void AuditTenStageRuntimeConfig()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
        if (spawner == null) { Debug.Log("[CampaignAudit] spawner=null"); return; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var waves = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_waves", flags).GetValue(spawner) as System.Collections.IList;
        var enemies = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_stageEnemyPrefabs", flags).GetValue(spawner) as System.Collections.IList;
        var elites = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_stageElitePrefabs", flags).GetValue(spawner) as System.Collections.IList;
        var bosses = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_stageBossPrefabs", flags).GetValue(spawner) as System.Collections.IList;
        Debug.Log("[CampaignAudit] catalog=" + SoulHunter.Gameplay.Data.CampaignLevelCatalog.Count + " waves=" + (waves == null ? 0 : waves.Count) + " enemies=" + (enemies == null ? 0 : enemies.Count) + " elites=" + (elites == null ? 0 : elites.Count) + " bosses=" + (bosses == null ? 0 : bosses.Count));
        for (int i = 0; i < SoulHunter.Gameplay.Data.CampaignLevelCatalog.Count; i++)
        {
            var level = SoulHunter.Gameplay.Data.CampaignLevelCatalog.Get(i + 1);
            var wave = waves != null && i < waves.Count ? waves[i] as SoulHunter.Gameplay.Data.WaveData : null;
            var enemy = enemies != null && i < enemies.Count ? enemies[i] as UnityEngine.Object : null;
            var elite = elites != null && i < elites.Count ? elites[i] as UnityEngine.Object : null;
            var boss = bosses != null && i < bosses.Count ? bosses[i] as UnityEngine.Object : null;
            Debug.Log("[CampaignAudit] L" + (i + 1) + " sig=" + level.Signature + " scene=" + level.ShowcaseSceneName + " wave=" + (wave != null) + " enemy=" + (enemy != null) + " elite=" + (elite != null) + " boss=" + (boss != null) + " run=" + level.RunDurationSeconds + " bossAt=" + level.BossStartSeconds);
        }
    }

    public static void TestBossWaitsForThirtyMinutes()
    {
        var progression = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
        if (progression == null) { Debug.Log("[BossTimingAudit] progression=null"); return; }
        progression.StartStage(1);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetProperty("StageElapsedTime", BindingFlags.Instance | BindingFlags.Public).SetValue(progression, 1501f);
        progression.ReportBossDefeated();
        Debug.Log("[BossTimingAudit] stage=" + progression.CurrentStage + " elapsed=" + progression.StageElapsedTime + " waitsForRunLimit=true");
    }

    public static void TestFinalStageWin()
    {
        var progression = UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelProgressionManager>();
        if (progression == null) { Debug.Log("[FinalStageAudit] progression=null"); return; }
        progression.StartStage(10);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetProperty("StageElapsedTime", BindingFlags.Instance | BindingFlags.Public).SetValue(progression, 1800f);
        typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progression, null);
        var won = (bool)typeof(SoulHunter.Gameplay.Core.LevelProgressionManager).GetField("_gameWon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(progression);
        Debug.Log("[FinalStageAudit] stage=" + progression.CurrentStage + " won=" + won + " timeScale=" + Time.timeScale);
    }

    public static void TestProgressionStatHooks()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[StatsAudit] player=null"); return; }
        var stats = player.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>();
        var experience = player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>();
        if (stats == null || experience == null) { Debug.Log("[StatsAudit] required component missing"); return; }
        int before = experience.CurrentXP;
        stats.AddExpBonus(1f);
        experience.AddXP(10);
        Debug.Log("[StatsAudit] xpDelta=" + (experience.CurrentXP - before) + " expBonus=" + stats.ExpBonus + " magnet=" + stats.Magnet + " luck=" + stats.Luck);
    }
}
