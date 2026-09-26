using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS pressure plates in the real game: each stage lays four Rune Plates on open ground; stepping on one starts a
    /// stage formation with the stage's enemies, then it waits out its cooldown before it can fire again.
    /// </summary>
    public class PressurePlatePlayModeTests : GameplayPlayModeTestBase
    {
        private StageItemSpawner _items;
        private Vector3 _start;

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false; // only plate events bring enemies
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            _items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => _items != null && _items.Plates.Count == 4, "the stage's four Rune Plates");
            _start = player.transform.position;
        }

        private static IEnumerator StepOn(PressurePlate plate)
        {
            var player = PlayerController.Instance;
            Vector3 to = plate.transform.position;
            to.y = player.transform.position.y;
            player.Rigidbody.position = to;
            player.transform.position = to;
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        }

        private static int Fliers() => EnemyController.ActiveEnemies.Count(e => e != null && e.isActiveAndEnabled && e.IsFlying);

        private static IEnumerator StepOff(PressurePlate plate)
        {
            var player = PlayerController.Instance;
            Vector3 away = plate.transform.position + Vector3.right * 5f;
            away.y = player.transform.position.y;
            player.Rigidbody.position = away;
            player.transform.position = away;
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator StageStart_LaysFourPlates_OnOpenGround()
        {
            yield return Boot();
            foreach (var plate in _items.Plates)
            {
                Vector3 offset = plate.transform.position - _start;
                offset.y = 0f;
                Assert.That(StageItems.PlateOffsets().Min(o => Vector3.Distance(offset, o)), Is.LessThan(10f), "At its planned spot.");
                Assert.That(GroundCheck.IsOpen(plate.transform.position, 1.0f), Is.True, "On open ground.");
                Assert.That(plate.IsReady, Is.True);
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SteppingOnAPlate_StartsAFormation_ThenItRecharges()
        {
            yield return Boot();
            var plate = _items.Plates[0];
            plate.CooldownSeconds = 2f; // VS waits 20 s; shortened so the test sees it recharge
            Assert.That(Fliers(), Is.EqualTo(0));

            yield return StepOn(plate);
            Assert.That(plate.TimesTriggered, Is.EqualTo(1), "Stepping on it fires it. " + Errors());
            int first = Fliers();
            Assert.That(first, Is.GreaterThanOrEqualTo(PressurePlate.EventCount / 2), "A formation of the stage's enemies is on its way.");
            Assert.That(plate.IsReady, Is.False, "It recharges after firing.");

            yield return StepOff(plate);
            yield return StepOn(plate);
            Assert.That(plate.TimesTriggered, Is.EqualTo(1), "Stepping on it again while it recharges does nothing.");

            yield return StepOff(plate);
            float until = Time.time + 2.2f;
            while (Time.time < until) yield return null;
            Assert.That(plate.IsReady, Is.True, "Ready again after its cooldown.");
            yield return StepOn(plate);
            Assert.That(plate.TimesTriggered, Is.EqualTo(2), "It fires again once recharged.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator NextStage_ReplacesThePlates()
        {
            yield return Boot();
            var old = _items.Plates.ToList();
            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            yield return null;
            yield return null;
            Assert.That(old.All(p => p == null), Is.True, "Stage 1's plates are removed.");
            Assert.That(_items.Plates.Count, Is.EqualTo(4), "Stage 2 lays its own.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
