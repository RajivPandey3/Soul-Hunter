using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS AI-09 (owner, 2026-10-21: option A). An enemy that stands where it was placed (pile shooter, mine,
    /// still enemy) stays there when the player runs far away, while an ordinary straggler is still brought back.
    /// </summary>
    public class StillHazardRelocationPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AStillEnemyLeftBehind_StaysWhereItWas_AnOrdinaryOneIsBroughtBack()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && !e.IsFlying && !spawner.IsBoss(e)) >= 2,
                "two ordinary enemies");
            var pair = EnemyController.ActiveEnemies.Where(e => e != null && !e.IsFlying && !spawner.IsBoss(e)).Take(2).ToArray();
            var still = pair[0];
            var ordinary = pair[1];
            StillEnemy.Begin(still.gameObject);

            Vector3 player = PlayerController.Instance.transform.position;
            Vector3 stillSpot = player + new Vector3(100f, 0f, 0f);
            still.transform.position = stillSpot;
            ordinary.transform.position = player + new Vector3(-100f, 0f, 0f);

            yield return WaitUntilRealtime(() => ordinary == null || !ordinary.isActiveAndEnabled
                    || Vector3.Distance(ordinary.transform.position, PlayerController.Instance.transform.position) < 40f,
                "the ordinary straggler to be brought back");
            yield return new WaitForSecondsRealtime(1.2f); // more than two relocation checks

            Assert.That(still != null && still.isActiveAndEnabled, Is.True, "The still enemy is still alive.");
            Vector3 offset = still.transform.position - stillSpot;
            offset.y = 0f;
            Assert.That(offset.magnitude, Is.LessThan(0.5f), "A still enemy left behind is not moved ahead of the player.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
