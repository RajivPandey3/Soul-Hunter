using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// Level 7's guardian plays VS Avatar of Gaea (vampire.survivors.wiki/w/Avatar_of_Gaea, raw 2026-10-28), Lake Foscari's
    /// 25:00 boss: health 8, movespeed 140, damage 5, knockback 0, max 1, xp 30, res-freeze 2, res-debuff 1.
    /// "It starts off with very little health and once it is defeated, it becomes invulnerable. After a while, it starts
    /// 'healing', then based on [(enemies killed / 1,000 x 0.2 seconds), at most 10 seconds + 2 more], multiple events
    /// happens: The enemy minimum is increased from 1 to 150 ... 5 seconds after the initial countdown, Avatar of Gaea starts
    /// to spawn its own Ghostly Apparitions every 0.1 seconds 101 times (for 10 seconds). It finally stops being
    /// invulnerable." "It increases its max health by the number of enemies killed in this run"; "it also grows bigger by 1%
    /// additively" per 1,000 kills. The owner's Level 7 rule stays on top: it is a ghost (Holy 2x, Normal passes through).
    /// Balance method (the wiki gives the events, not their exact order): the minimum is 1 from its arrival; at the
    /// countdown's end it is 150, the apparitions start 5 s later, and it is vulnerable once the last one has come. The
    /// Soul Egg part ("1 max health per Max Health bonus and 0.01 power per 1% Might bonus") is not built: Soul Hunter's
    /// Soul Eggs give a Max Health multiplier, not points.
    /// </summary>
    public class AvatarOfGaea : MonoBehaviour
    {
        public const int StartHealth = 8;
        public const float SecondsPerThousandKills = 0.2f, MaxCountdown = 10f, ExtraSeconds = 2f;
        public const int MinimumBefore = 1;
        public const float ApparitionDelay = 5f, ApparitionEvery = 0.1f;
        public const int Apparitions = 101;
        public const float GrowthPerThousand = 0.01f;

        public enum Phase { Waiting, Healing, Summoning, Vulnerable }

        private HealthController _health;
        private EnemySpawner _spawner;
        private float _clock;
        private int _summoned;
        private float _nextSummon;
        private Vector3 _baseScale;

        public Phase Current { get; private set; }
        public int ApparitionsSummoned => _summoned;
        public float Countdown { get; private set; }

        /// <summary>The countdown for <paramref name="kills"/> enemies killed this run.</summary>
        public static float CountdownFor(int kills) => Mathf.Min(Mathf.Max(0, kills) / 1000f * SecondsPerThousandKills, MaxCountdown) + ExtraSeconds;

        /// <summary>Its health once it has healed: its own 8 plus every enemy killed this run.</summary>
        public static int HealedHealthFor(int kills) => StartHealth + Mathf.Max(0, kills);

        public static AvatarOfGaea Begin(GameObject boss, EnemySpawner spawner)
        {
            if (boss == null) return null;
            var avatar = boss.GetComponent<AvatarOfGaea>();
            if (avatar == null) avatar = boss.AddComponent<AvatarOfGaea>();
            avatar._spawner = spawner;
            avatar._health = boss.GetComponent<HealthController>();
            avatar._baseScale = boss.transform.localScale;
            avatar.Current = Phase.Waiting;
            avatar._summoned = 0;
            if (avatar._health != null)
            {
                avatar._health.Initialize(StartHealth);
                avatar._health.IsInvincible = false;
                avatar._health.TryPreventDeath = avatar.PreventFirstDeath;
            }
            if (spawner != null) spawner.MinimumOverride = MinimumBefore;
            return avatar;
        }

        /// <summary>
        /// Lake Foscari: "Before the Avatar of Gaea spawns, all the enemies on the screen are cleared to increase the enemy
        /// kill count." Every ordinary enemy on the screen is erased as a kill. Balance method (the page is silent on drops):
        /// they leave no gems, as Pentagram's erased enemies do; a chest carrier's chest passes on (EnemyDrop.PendingChests).
        /// Bosses and enemies that resist an instant kill stay. Returns how many were cleared.
        /// </summary>
        public static int ClearScreen(Camera camera)
        {
            int cleared = 0;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsStageBoss || enemy.ResistsInstantKill) continue;
                if (camera != null)
                {
                    Vector3 v = camera.WorldToViewportPoint(enemy.transform.position);
                    if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) continue;
                }
                var drop = enemy.GetComponent<EnemyDrop>();
                if (drop != null) drop.EraseDropsThisLife();
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                health.EraseByInstantKill();
                cleared++;
            }
            return cleared;
        }

        private static int Kills => SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null ? SoulHunter.Gameplay.Core.RunStatsTracker.Instance.TotalKills : 0;

        // "once it is defeated, it becomes invulnerable": the first lethal hit starts the healing instead.
        private bool PreventFirstDeath()
        {
            if (Current != Phase.Waiting || _health == null) return false;
            _health.Initialize(StartHealth);
            _health.IsInvincible = true;
            Current = Phase.Healing;
            Countdown = CountdownFor(Kills);
            _clock = Countdown;
            return true;
        }

        private void Update()
        {
            float dt = Time.deltaTime; // game time
            if (dt <= 0f || _health == null) return;
            switch (Current)
            {
                case Phase.Healing:
                    _clock -= dt;
                    if (_clock > 0f) return;
                    if (_spawner != null) _spawner.MinimumOverride = -1; // "increased from 1 to 150" (the minute's own)
                    Current = Phase.Summoning;
                    _nextSummon = ApparitionDelay;
                    break;
                case Phase.Summoning:
                    _nextSummon -= dt;
                    while (_nextSummon <= 0f && _summoned < Apparitions)
                    {
                        Summon();
                        _nextSummon += ApparitionEvery;
                    }
                    if (_summoned < Apparitions) return;
                    int kills = Kills;
                    _health.Initialize(HealedHealthFor(kills));
                    _health.IsInvincible = false;
                    transform.localScale = _baseScale * (1f + GrowthPerThousand * (kills / 1000));
                    Current = Phase.Vulnerable;
                    break;
            }
        }

        private void Summon()
        {
            _summoned++;
            if (_spawner == null) return;
            var prefab = _spawner.FindStageType(_summoned % 2 == 0 ? "WailingApparition" : "GrievingApparition");
            if (prefab == null) return;
            Vector2 r = Random.insideUnitCircle.normalized * 2.5f;
            _spawner.SpawnHazardEnemy(prefab, transform.position + new Vector3(r.x, 0f, r.y));
        }

        private void OnDisable()
        {
            if (_spawner != null) _spawner.MinimumOverride = -1;
            if (_health != null && _health.TryPreventDeath == (System.Func<bool>)PreventFirstDeath) _health.TryPreventDeath = null;
            if (_health != null) _health.IsInvincible = false;
            if (_baseScale != Vector3.zero) transform.localScale = _baseScale;
            Current = Phase.Waiting;
        }
    }
}
