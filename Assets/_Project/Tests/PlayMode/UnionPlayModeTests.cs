using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner decision (2026-09-30): the six weapon unions Soul Hunter invented (not VS unions; two made Holy Wand and one
    /// could never fire) are removed. The union system stays for VS's own unions (vampire.survivors.wiki/w/Evolution#Union).
    /// </summary>
    public class UnionPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheGame_HasNoInventedUnions()
        {
            yield return BootIntoGameplay();
            var unions = GetPrivateField<List<WeaponUnionData>>(WeaponManager.Instance, "_availableUnions") ?? new List<WeaponUnionData>();
            var live = unions.Where(u => u != null).Select(u => u.UnionName).ToList();
            // 2026-10-12: the only unions are VS's own, built by NewVsItemsBuilder and listed in GameContent.Unions
            // (Twilight Flock = VS Vandalier, Revenant Wheel = VS Phieraggi).
            var vsUnions = (SoulHunter.Gameplay.Data.GameContentCatalog.Load().Unions ?? new WeaponUnionData[0])
                .Where(u => u != null).Select(u => u.UnionName).ToList();
            Assert.That(live, Is.EquivalentTo(vsUnions), "Only the VS unions are available: " + string.Join(", ", live));
            Assert.That(live, Is.EquivalentTo(new[] { "Union_TwilightFlock", "Union_RevenantWheel" }));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
