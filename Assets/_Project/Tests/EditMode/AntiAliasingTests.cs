using NUnit.Framework;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Anti-aliasing option (owner, 2026-10-30, release audit question 8; an SH extra, VS Options list none). FXAA is the
    /// default, including on a save written before the option existed; the three modes are named for the Options row.
    /// </summary>
    public class AntiAliasingTests
    {
        [Test]
        public void FxaaIsTheDefault_AlsoForAnOlderSave()
        {
            Assert.That(new GameData().AntiAliasing, Is.EqualTo(1), "A new save starts on FXAA.");
            var old = JsonUtility.FromJson<GameData>("{\"Currency\": 10}");
            Assert.That(old.AntiAliasing, Is.EqualTo(1), "A save without the field loads FXAA.");
            Assert.That(DisplaySettingsApplier.AntiAliasingNames, Is.EqualTo(new[] { "Off", "FXAA", "SMAA" }));
        }
    }
}
