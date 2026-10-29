using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Everything the VS Results screen shows at the end of a run (vampire.survivors.wiki/w/Results, fetched 2026-10-02):
    /// top left the stage name, mode and gold bonus with time survived, gold earned, level reached and enemies defeated;
    /// bottom left each weapon's level, damage, time used and DPS (the most damage and the most DPS in yellow); top right
    /// the character and the Golden Eggs used (Soul Eggs); then every weapon and passive picked up with its level (yellow
    /// when max), the PowerUps active and their level, the pickups and how many, and the Arcanas; the run's unlocks.
    /// </summary>
    public sealed class RunResults
    {
        public enum EndKind { Death, Victory, Quit }

        public struct WeaponRow
        {
            public string Name;
            public Sprite Icon;
            public int Level;
            public bool Maxed;
            public double Damage;
            public float SecondsUsed;
            public double Dps;
        }

        public struct ItemRow
        {
            public string Name;
            public Sprite Icon;
            public int Level;
            public bool Maxed;
            public bool IsWeapon;
        }

        public EndKind End;
        public string StageLine = "";
        /// <summary>Why the run ended when it was not a death in battle: the level's guardian still stood at 30:00 (else empty).</summary>
        public string EndReason = "";
        public string Mode = "Normal";
        public int GoldBonusPercent;
        public float SecondsSurvived;
        public int Gold;
        public int Level = 1;
        public int Kills;
        public readonly List<WeaponRow> Weapons = new List<WeaponRow>();
        /// <summary>Row of the most overall damage, or -1 (shown in yellow).</summary>
        public int TopDamageRow = -1;
        /// <summary>Row of the most DPS, or -1 (shown in yellow).</summary>
        public int TopDpsRow = -1;
        public string Character = "";
        public int SoulEggs;
        public readonly List<ItemRow> Items = new List<ItemRow>();
        public readonly List<KeyValuePair<string, int>> PowerUps = new List<KeyValuePair<string, int>>();
        public readonly List<KeyValuePair<string, int>> Pickups = new List<KeyValuePair<string, int>>();
        public readonly List<string> Arcanas = new List<string>();
        public readonly List<Sprite> ArcanaIcons = new List<Sprite>();
        public readonly List<string> Unlocks = new List<string>();

        /// <summary>The Results of the run now ending, read from the run's live systems and the save.</summary>
        public static RunResults Capture(EndKind end)
        {
            var results = new RunResults { End = end };
            var session = GameSessionManager.Instance;
            if (session != null) results.SecondsSurvived = session.SurvivalTime;

            var progression = LevelProgressionManager.Instance;
            if (progression != null)
            {
                var level = progression.CurrentLevel;
                results.StageLine = $"Level {progression.CurrentStage}: {level.Name}";
                if (end == EndKind.Death && progression.StageFailed) results.EndReason = LevelProgressionManager.GuardianFailed(progression.CurrentStage);
            }
            results.Mode = ModeText();
            results.GoldBonusPercent = Mathf.RoundToInt((CampaignLevelCatalog.CurrentGold - 1f) * 100f);

            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var experience = player != null ? player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>() : null;
            if (experience != null) results.Level = experience.CurrentLevel;

            var stats = RunStatsTracker.Instance;
            var manager = WeaponManager.Instance;
            if (stats != null)
            {
                results.Kills = stats.TotalKills;
                results.Gold = stats.TotalGoldCollected;
                results.AddWeapons(stats.Weapons);
                foreach (var item in stats.Items)
                {
                    var definition = WeaponDefinitionLookup.Find(item.Type);
                    int max = manager != null ? manager.GetMaxWeaponLevel(item.Type) : (definition != null ? definition.MaxLevel : 0);
                    results.Items.Add(new ItemRow
                    {
                        Name = UpgradeText.NameOf(item.Type), Icon = definition != null ? definition.Icon : null,
                        Level = item.Level, Maxed = max > 0 && item.Level >= max, IsWeapon = item.IsWeapon
                    });
                }
                results.Pickups.AddRange(stats.Pickups);
                results.Unlocks.AddRange(stats.Unlocks);
            }

            GameData data = null;
            if (SoulHunter.Core.Services.GameServices.Instance != null &&
                SoulHunter.Core.Services.GameServices.Instance.TryGet(out SaveService save)) data = save.CurrentData;
            if (data != null)
            {
                string name = GameContentCatalog.CurrentName(data.SelectedCharacterName);
                var character = GameContentCatalog.FindCharacterExact(name);
                results.Character = character != null ? character.CharacterName : name;
                // VS Results: the Golden Eggs used this run, i.e. the ones it started with (eggs gained mid-run count next run).
                var eggs = SoulHunter.Gameplay.Player.SoulEggs.AppliedThisRun ?? SoulHunter.Gameplay.Player.SoulEggs.For(data, name);
                results.SoulEggs = eggs != null ? eggs.Eggs : 0;
                results.AddPowerUps(data);
            }

            var arcanas = ArcanaManager.Instance;
            if (arcanas != null && arcanas.ActiveArcanas != null)
                foreach (var arcana in arcanas.ActiveArcanas)
                {
                    if (arcana == null) continue;
                    results.Arcanas.Add(string.IsNullOrEmpty(arcana.CardNumber) ? arcana.CardName : $"{arcana.CardNumber} - {arcana.CardName}");
                    results.ArcanaIcons.Add(arcana.CardIcon);
                }
            return results;
        }

        /// <summary>The weapon table: every weapon held (and any other damage source that hit), with the yellow rows.</summary>
        public void AddWeapons(IReadOnlyList<RunStatsTracker.WeaponRecord> records)
        {
            foreach (var record in records)
            {
                if (record == null) continue;
                int level = record.Weapon != null ? Mathf.Max(record.Level, record.Weapon.CurrentLevel) : record.Level;
                Weapons.Add(new WeaponRow
                {
                    Name = string.IsNullOrEmpty(record.Name) ? "Unknown" : record.Name, Icon = record.Icon, Level = level,
                    Maxed = record.Evolved || (record.MaxLevel > 0 && level >= record.MaxLevel),
                    Damage = record.Damage, SecondsUsed = record.SecondsUsed, Dps = record.Dps
                });
            }
            var damage = new List<double>(Weapons.Count);
            var dps = new List<double>(Weapons.Count);
            foreach (var row in Weapons) { damage.Add(row.Damage); dps.Add(row.Dps); }
            TopDamageRow = RunResultsFormat.Highest(damage);
            TopDpsRow = RunResultsFormat.Highest(dps);
        }

        /// <summary>
        /// "All the powerups that were active during the run, and their level": every PowerUp with a rank, except a maxed
        /// one switched off (wiki PowerUps, On/Off), which was not active.
        /// </summary>
        public void AddPowerUps(GameData data)
        {
            foreach (var info in SoulHunter.Core.Persistence.PowerUps.All)
            {
                int rank = SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, info.Id);
                if (rank > 0) PowerUps.Add(new KeyValuePair<string, int>(info.Name, rank));
            }
        }

        /// <summary>The run's modes, as the character select screen names them; "Normal" with none on.</summary>
        public static string ModeText()
        {
            var modes = new List<string>();
            if (RunModes.Hurry) modes.Add("Hurry");
            if (RunModes.Hyper) modes.Add("Hyper");
            if (RunModes.Inverse) modes.Add("Inverse");
            if (RunModes.Endless) modes.Add(RunModes.EndlessCycle > 0 ? $"Endless (cycle {RunModes.EndlessCycle + 1})" : "Endless");
            if (RunModes.Arcana) modes.Add("Arcana");
            if (RunModes.LimitBreak) modes.Add("Limit Break");
            if (RunModes.RandomEvents) modes.Add(RunModeOptions.Name(RunMode.RandomEvents));
            if (RunModes.RandomLevelUp) modes.Add(RunModeOptions.Name(RunMode.RandomLevelUp));
            return modes.Count == 0 ? "Normal" : string.Join(", ", modes);
        }
    }
}
