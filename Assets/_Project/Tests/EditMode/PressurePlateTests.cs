using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>VS pressure plates: the Rune Plate prefab and where each stage lays its plates.</summary>
    public class PressurePlateTests
    {
        [Test]
        public void RunePlate_IsAFlatTrigger_ThatWeaponsIgnore()
        {
            var prefab = GameContentCatalog.Load().RunePlate;
            Assert.That(prefab, Is.Not.Null, "GameContentCatalog.RunePlate is set (Soul Hunter/Content/Import Stage Props).");
            Assert.That(prefab.GetComponent<PressurePlate>(), Is.Not.Null);
            Assert.That(prefab.CompareTag("Enemy"), Is.False, "Weapons must not hit it.");
            Assert.That(prefab.layer, Is.Not.EqualTo(LayerMask.NameToLayer("Enemy")));
            var trigger = prefab.GetComponent<Collider>();
            Assert.That(trigger != null && trigger.isTrigger, Is.True, "A trigger: stepping on it never blocks the player.");
            Assert.That(PressurePlate.DefaultCooldownSeconds, Is.GreaterThanOrEqualTo(20f), "VS: at least 20 s between triggers.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Any(r => SoulHunter.Gameplay.VFX.PropMergedLook.Draws(r, "Rune")), Is.True, "Glowing runes show it is ready (merged or not).");
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Assert.That(bounds.size.y, Is.LessThan(0.3f), "Flat on the ground.");
                Assert.That(bounds.size.x, Is.InRange(2.0f, 3.0f), "About 2.5 units across.");
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.05f), "Lies on its pivot, under the hero's feet.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void EveryStage_LaysFourPlates_StraightOutFromTheStart()
        {
            var offsets = StageItems.PlateOffsets();
            Assert.That(offsets.Count, Is.EqualTo(4));
            foreach (var o in offsets)
            {
                Assert.That(o.magnitude, Is.EqualTo(StageItems.PlateDistance).Within(0.01f));
                Assert.That(Mathf.Min(Mathf.Abs(o.x), Mathf.Abs(o.z)), Is.LessThan(0.01f), "On a straight N/E/S/W line, between the item diagonals.");
            }
            Assert.That(offsets.Select(o => (Mathf.Round(o.x), Mathf.Round(o.z))).Distinct().Count(), Is.EqualTo(4));
        }
    }
}
