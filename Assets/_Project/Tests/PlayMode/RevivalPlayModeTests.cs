using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS Revival (vampire.survivors.wiki/w/Revival, fetched 2026-10-09): "Each point of Revival a player has allows them to
    /// revive once upon death with 50% of their Max Health"; on death "they will instead be greeted with a 'Revive' button";
    /// "the player resumes control of their character exactly where they left off"; "a large amount of invulnerability
    /// frames"; "The health gained upon reviving counts as healing" (Soul Eater). v1.6.0: "Fix for revives triggering twice."
    /// </summary>
    public class RevivalPlayModeTests : GameplayPlayModeTestBase
    {
        private PlayerController _player;
        private PlayerStats _stats;
        private HealthController _health;

        private IEnumerator Boot(int revivals)
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            _player = PlayerController.Instance;
            _stats = _player.Stats;
            _health = _player.GetComponent<HealthController>();
            // No kills, so no level-up screen pauses the test; only the given Revivals count, whatever the save gives.
            foreach (var weapon in _player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            while (_stats.UseRevival()) { }
            _stats.AddRevival(revivals);
            _health.ShieldedUntil = -1f;
            _health.IsInvincible = false;
            yield return null;
        }

        private void LethalHit()
        {
            _health.TakeDamage(new DamagePacket(_health.MaxHealth * 10, _player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }

        [UnityTest]
        public IEnumerator ALethalHit_WithARevival_PausesOnTheReviveButton_ThenRevivesWhereItFell()
        {
            yield return Boot(1);
            int deaths = 0;
            _health.OnDied += () => deaths++;
            Vector3 fellAt = _player.transform.position;
            bool pauseWasOn = PauseAction() != null && PauseAction().enabled;

            LethalHit();
            yield return null;
            yield return null; // let the theme size the button

            var screen = ReviveScreen.Current;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The Revive screen shows. " + Errors());
            Assert.That(deaths, Is.EqualTo(0), "A held death fires no OnDied.");
            Assert.That(GameSessionManager.Instance.IsRunFinished, Is.False, "No Game Over while a Revival is left.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "The game is paused under the Revive button.");
            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(_stats.Revivals, Is.EqualTo(1), "Nothing is spent before Revive is pressed.");
            StringAssert.Contains("1 Revival", screen.RevivalsText.text);
            if (pauseWasOn) Assert.That(PauseAction().enabled, Is.False, "The pause key is off while the screen is open.");
            Assert.That(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == screen.ReviveButton.gameObject, Is.True,
                "Keyboard and controller focus is on Revive.");

            // Themed Primary button at the theme's standard size, on screen, on its own Expand overlay canvas.
            var element = screen.ReviveButton.GetComponent<ThemedElement>();
            Assert.That(element, Is.Not.Null);
            Assert.That(element.ButtonKind, Is.EqualTo(UITheme.ButtonKind.Primary));
            Assert.That(element.Size, Is.EqualTo(ThemedElement.SizeRole.Standard));
            var rect = (RectTransform)screen.ReviveButton.transform;
            Assert.That(rect.rect.width, Is.EqualTo(UITheme.Layout.ButtonWidth).Within(1f));
            Assert.That(rect.rect.height, Is.EqualTo(UITheme.Layout.ButtonHeight).Within(1f));
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
                Assert.That(corner.x >= 0f && corner.y >= 0f && corner.x <= Screen.width && corner.y <= Screen.height, Is.True, $"Revive at {corner} is off a {Screen.width}x{Screen.height} window.");
            var scaler = screen.Canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(screen.Canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));

            // The screen waits: real time passes, the player stays down and nothing is spent.
            float until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.That(screen.IsOpen && _health.CurrentHealth == 0 && _stats.Revivals == 1, Is.True);

            Click(screen.ReviveButton, "Revive");
            int expected = HealthController.RevivalHealth(_health.MaxHealth);
            Assert.That(_health.CurrentHealth, Is.EqualTo(expected), "Revived with 50% of Max Health.");
            Assert.That(_stats.Revivals, Is.EqualTo(0), "Exactly one Revival spent.");
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(deaths, Is.EqualTo(0));
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f), "The game resumes. " + GameTime.PauserNames());
            Assert.That(Vector3.Distance(_player.transform.position, fellAt), Is.LessThan(0.05f), "It resumes exactly where it fell.");
            if (pauseWasOn) Assert.That(PauseAction().enabled, Is.True, "The pause key works again.");
            Assert.That(_player.gameObject.activeSelf, Is.True);
            Assert.That(GameSessionManager.Instance.IsRunFinished, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheRevivalImmunity_RunsOnGameTime_ThenHitsLandAgain()
        {
            yield return Boot(1);
            LethalHit();
            yield return null;
            Click(ReviveScreen.Current.ReviveButton, "Revive");
            float revivedAt = Time.time;
            int health = _health.CurrentHealth;
            Assert.That(_health.IsRevivalInvulnerable, Is.True);

            // A pause (a level-up or chest screen) does not use it up.
            var pauser = new object();
            GameTime.Pause(pauser);
            float until = Time.realtimeSinceStartup + 2.2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            GameTime.Resume(pauser);
            Assert.That(_health.IsRevivalInvulnerable, Is.True, "2.2 s of paused real time leave the revival's immunity running.");

            // A dash ending inside the window does not end it (the dash owns IsInvincible only).
            _health.IsInvincible = true;
            _health.IsInvincible = false;
            _health.TakeDamage(new DamagePacket(5, _player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(_health.CurrentHealth, Is.EqualTo(health), "No damage during the revival's immunity.");

            yield return WaitUntilRealtime(() => !_health.IsRevivalInvulnerable, "the revival's immunity to end");
            Assert.That(Time.time - revivedAt, Is.GreaterThanOrEqualTo(HealthController.RevivalInvulnerabilitySeconds + HealthController.VsInvulnerabilityTime - 0.05f),
                "1.5 s plus the VS 240 ms, in game time.");
            int beforeHit = _health.CurrentHealth; // Recovery may have healed meanwhile
            _health.TakeDamage(new DamagePacket(5, _player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(_health.CurrentHealth, Is.LessThan(beforeHit), "Hits land again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator OneDeath_SpendsExactlyOneRevival_EvenWithTwoHitsAndTwoPresses()
        {
            yield return Boot(2);
            LethalHit();
            LethalHit(); // the same frame: VS v1.6.0 "Fix for revives triggering twice"
            yield return null;
            var screen = ReviveScreen.Current;
            Assert.That(screen.IsOpen, Is.True);
            Assert.That(screen.TimesShown, Is.EqualTo(1), "One death opens the screen once.");
            StringAssert.Contains("2 Revivals", screen.RevivalsText.text);

            Click(screen.ReviveButton, "Revive");
            screen.PressRevive();
            Assert.That(_player.ConfirmRevive(), Is.False, "Nothing is held any more.");
            Assert.That(_stats.Revivals, Is.EqualTo(1), "One death, one Revival.");
            Assert.That(_health.CurrentHealth, Is.EqualTo(HealthController.RevivalHealth(_health.MaxHealth)));
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WithNoRevivalLeft_ThePlayerDies_AndNoReviveScreenShows()
        {
            yield return Boot(0);
            LethalHit();
            yield return null;
            Assert.That(ReviveScreen.Current == null || !ReviveScreen.Current.IsOpen, Is.True);
            Assert.That(GameSessionManager.Instance.IsRunFinished, Is.True, "Game Over (the Results screen). " + Errors());
        }

        [UnityTest]
        public IEnumerator TheRevivesHeal_RaisesAHeldSoulEatersDamage()
        {
            yield return Boot(1);
            // A Soul Eater held by the player (GarlicWeapon with life steal, as the evolution prefab is).
            var go = new GameObject("TestSoulEater");
            go.SetActive(false);
            go.transform.SetParent(_player.transform, false);
            var soulEater = go.AddComponent<GarlicWeapon>();
            typeof(GarlicWeapon).GetField("_lifeStealPerHit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(soulEater, 0.2f);
            go.SetActive(true);
            yield return null;

            LethalHit();
            yield return null;
            int before = soulEater.HealthHealed;
            Click(ReviveScreen.Current.ReviveButton, "Revive");
            int restored = HealthController.RevivalHealth(_health.MaxHealth);
            Assert.That(soulEater.HealthHealed - before, Is.EqualTo(restored), "The revive's health counts as healing.");
            Assert.That(soulEater.HealDamageBonus, Is.EqualTo(GarlicWeapon.HealDamageBonusFor(soulEater.HealthHealed)));
            Assert.That(soulEater.HealDamageBonus, Is.GreaterThanOrEqualTo(restored / GarlicWeapon.HealthPerDamageBonus), "+1 damage per 60 healed.");
            Object.Destroy(go);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
