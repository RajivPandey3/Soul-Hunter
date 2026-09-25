using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: StageFloorTests verifies VS-style stage ground: every level has its own
    /// 20x20 floor tile with one flat collider, and showcase grounds are recognised so they can be hidden.
    /// </summary>
    public class StageFloorTests
    {
        [Test]
        public void EveryStage_HasItsOwnFloorTile_WithAFlatTwentyByTwentyCollider()
        {
            // Learning Comment: Har level ki apni zameen ho, aur InfiniteMap ke 20 unit chunks mein bina gap fit ho.
            var floors = GameContentCatalog.Load().StageFloors;
            Assert.That(floors, Is.Not.Null.And.Length.EqualTo(10));
            for (int i = 0; i < floors.Length; i++)
            {
                var floor = floors[i];
                Assert.That(floor, Is.Not.Null, $"Level {i + 1} floor.");
                Assert.That(floor.name, Is.EqualTo($"SH10_L{i + 1:00}_Floor"));
                var box = floor.GetComponent<BoxCollider>();
                Assert.That(box, Is.Not.Null, $"{floor.name} collider.");
                Assert.That(box.size.x, Is.EqualTo(20f).Within(.001f));
                Assert.That(box.size.z, Is.EqualTo(20f).Within(.001f));
                Assert.That(box.center.y + box.size.y / 2, Is.EqualTo(0f).Within(.001f), "Collider top is the ground plane.");
                Assert.That(floor.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1), "Decor must not block movement.");
                var instance = Object.Instantiate(floor);
                try
                {
                    var renderer = instance.GetComponentInChildren<MeshRenderer>();
                    Assert.That(renderer, Is.Not.Null, $"{floor.name} renderer.");
                    var bounds = renderer.bounds;
                    Assert.That(bounds.size.x, Is.EqualTo(20f).Within(.05f), $"{floor.name} is 20 wide.");
                    Assert.That(bounds.size.z, Is.EqualTo(20f).Within(.05f), $"{floor.name} is 20 deep (lies on the ground plane).");
                    Assert.That(bounds.max.y, Is.LessThan(1f), $"{floor.name} decor stays low.");
                }
                finally { Object.DestroyImmediate(instance); }
            }
            Assert.That(new System.Collections.Generic.HashSet<GameObject>(floors).Count, Is.EqualTo(10), "Every level has a different floor.");
        }

        [TestCase("SH10_L02_Ground", true)]
        [TestCase("SH10_L02_Ground (3)", true)]
        [TestCase("SH10_L02_Pine", false)]
        [TestCase("SH10_L02_Floor", false)]
        public void ShowcaseGround_IsRecognisedByName(string objectName, bool expected)
        {
            Assert.That(CampaignSceneConnector.IsShowcaseGround(objectName), Is.EqualTo(expected));
        }
    }
}
