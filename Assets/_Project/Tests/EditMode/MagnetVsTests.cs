using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS Magnet (vampire.survivors.wiki/w/Magnet, fetched 2026-10-09): "Magnet = 30 * Character bonus * PowerUp
    /// multiplier", every modifier multiplies, the Golden Egg adds to the base, and there is no upper limit.
    /// </summary>
    public class MagnetVsTests
    {
        private GameObject _player;
        private CharacterData _character;

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
            if (_character != null) Object.DestroyImmediate(_character);
        }

        private PlayerStats CreatePlayer()
        {
            _player = new GameObject("MagnetPlayer");
            _player.AddComponent<HealthController>().Initialize(100);
            _player.AddComponent<PlayerExperience>();
            return _player.AddComponent<PlayerStats>();
        }

        private CharacterData Character(float magnet)
        {
            _character = ScriptableObject.CreateInstance<CharacterData>();
            _character.StartingMagnet = magnet;
            return _character;
        }

        private static GameData PowerUpRank(int rank)
        {
            var data = new GameData();
            PowerUps.SetRank(data, PowerUpId.Magnet, rank);
            return data;
        }

        [Test]
        public void Magnet_IsBaseTimesCharacterTimesPowerUpTimesItems()
        {
            var stats = CreatePlayer();
            stats.ApplyPowerUps(PowerUpRank(2));
            stats.ApplyCharacter(Character(2f));
            stats.MultiplyMagnet(WeaponManager.AttractorbStep(1));
            Assert.That(stats.Magnet, Is.EqualTo(2f * 1.5625f * 1.5f).Within(1e-4), "VS multiplies every Magnet modifier.");
            Assert.That(stats.MagnetRadius, Is.EqualTo(PlayerStats.BaseMagnetRadius * stats.Magnet).Within(1e-4));
        }

        [Test]
        public void TheOrder_DoesNotMatter_CharacterBeforeOrAfterPowerUps()
        {
            var a = CreatePlayer();
            a.ApplyCharacter(Character(2f));
            a.ApplyPowerUps(PowerUpRank(2));
            float first = a.Magnet;
            TearDown();
            var b = CreatePlayer();
            b.ApplyPowerUps(PowerUpRank(2));
            b.ApplyCharacter(Character(2f));
            Assert.That(b.Magnet, Is.EqualTo(first).Within(1e-4));
        }

        [Test]
        public void SoulEggMagnet_AddsToTheBase_BeforeTheMultipliers()
        {
            var stats = CreatePlayer();
            stats.ApplyPowerUps(PowerUpRank(2));
            stats.AddMagnet(0.01f);
            stats.MultiplyMagnet(1.5f);
            Assert.That(stats.Magnet, Is.EqualTo(1.01f * 1.5625f * 1.5f).Within(1e-4), "VS: the Golden Egg's +0.3 goes on the base 30.");
        }

        [Test]
        public void Magnet_HasNoCap()
        {
            var stats = CreatePlayer();
            for (int i = 0; i < 10; i++) stats.MultiplyMagnet(2f);
            Assert.That(stats.Magnet, Is.EqualTo(1024f).Within(1e-2), "wiki: \"There is no upper limit to how much Magnet the player may have.\"");
        }

        [Test]
        public void TheSelectScreenSheet_MatchesTheRun()
        {
            var data = PowerUpRank(1);
            var stats = CreatePlayer();
            stats.ApplyPowerUps(data);
            stats.ApplyCharacter(Character(1.3f));
            var sheet = CharacterStatSheet.Build(_character, data);
            Assert.That(sheet.Magnet, Is.EqualTo(stats.Magnet).Within(1e-4));
        }

        [Test]
        public void ShadowKaelsCopy_KeepsTheMagnet()
        {
            var stats = CreatePlayer();
            stats.ApplyCharacter(Character(1.5f));
            stats.MultiplyMagnet(1.5f);
            var mirror = new GameObject("Mirror").AddComponent<PlayerStats>();
            try
            {
                mirror.CopyFrom(stats);
                Assert.That(mirror.Magnet, Is.EqualTo(stats.Magnet).Within(1e-4));
            }
            finally { Object.DestroyImmediate(mirror.gameObject); }
        }
    }
}
