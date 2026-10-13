using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: StageFloorTests verifies VS-style stage ground: every level has its own 20x20 floor
    /// tile with one flat walking collider and themed obstacles that block only the player, the tile centre
    /// (spawn) and the cross of paths stay open, and the old showcase ground and decor are gone from play.
    /// </summary>
    public class StageFloorTests
    {
        private static int ObstacleLayer => LayerMask.NameToLayer("Obstacle");

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
                var instance = Object.Instantiate(floor);
                try
                {
                    var ground = instance.GetComponentsInChildren<MeshRenderer>().First(r => r.name == floor.name);
                    var bounds = ground.bounds;
                    Assert.That(bounds.size.x, Is.EqualTo(20f).Within(.05f), $"{floor.name} is 20 wide.");
                    Assert.That(bounds.size.z, Is.EqualTo(20f).Within(.05f), $"{floor.name} is 20 deep (lies on the ground plane).");
                    Assert.That(bounds.max.y, Is.LessThan(1f), $"{floor.name} ground detail stays low.");
                }
                finally { Object.DestroyImmediate(instance); }
            }
            Assert.That(new System.Collections.Generic.HashSet<GameObject>(floors).Count, Is.EqualTo(10), "Every level has a different floor.");
        }

        [Test]
        public void EnemyBodies_MakeNoPhysicsPairsWithEachOther()
        {
            // 60 FPS pass (2026-10-13): a packed horde of trigger bodies made thousands of enemy-enemy pairs and 170 trigger
            // callbacks a frame that nothing used (the crowd push is computed in EnemyCrowd, not by physics).
            int enemy = LayerMask.NameToLayer("Enemy");
            Assert.That(Physics.GetIgnoreLayerCollision(enemy, enemy), Is.True);
            foreach (string other in new[] { "Player", "Projectile", "Default" })
                Assert.That(Physics.GetIgnoreLayerCollision(enemy, LayerMask.NameToLayer(other)), Is.False, "Enemies still meet " + other);
        }

        [Test]
        public void EveryStage_HasObstacles_ThatBlockOnlyThePlayer_AndKeepSpawnAndPathsOpen()
        {
            // Learning Comment: VS jaisi rukawatein (ped, kabr, khambe) sirf player ko rokein; beech aur raaste khule rahein.
            Assert.That(ObstacleLayer, Is.GreaterThanOrEqualTo(0), "Obstacle layer exists.");
            int player = LayerMask.NameToLayer("Player");
            for (int layer = 0; layer < 32; layer++)
                Assert.That(Physics.GetIgnoreLayerCollision(ObstacleLayer, layer), Is.EqualTo(layer != player),
                    $"Obstacles collide with layer {layer} ({LayerMask.LayerToName(layer)}) only if it is the player.");

            foreach (var floor in GameContentCatalog.Load().StageFloors)
            {
                var colliders = floor.GetComponentsInChildren<Collider>(true);
                Assert.That(colliders.Length, Is.EqualTo(2), $"{floor.name}: ground box + one obstacle collider.");
                var blocker = colliders.OfType<MeshCollider>().Single();
                Assert.That(blocker.name, Is.EqualTo(floor.name + "_Blockers"));
                Assert.That(blocker.isTrigger, Is.False);
                Assert.That(blocker.gameObject.layer, Is.EqualTo(ObstacleLayer), $"{floor.name} blockers layer.");
                Assert.That(blocker.GetComponent<MeshRenderer>().enabled, Is.False, $"{floor.name} blocker boxes are invisible.");
                var visual = floor.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == floor.name + "_Obstacles");
                Assert.That(visual.enabled, Is.True);
                Assert.That(visual.gameObject.layer, Is.EqualTo(ObstacleLayer));

                var mesh = blocker.sharedMesh;
                Assert.That(mesh, Is.Not.Null);
                // Owner (2026-09-30): "ground ka obstacle 70% hata do". Of the 7-16 obstacles each floor used to carry, 30% are
                // kept: 2-5 per tile (build_grounds.py OBSTACLE_KEEP). Every blocker is one 12-triangle box.
                int boxes = mesh.triangles.Length / 3 / 12;
                Assert.That(boxes, Is.InRange(1, 5), $"{floor.name} keeps 30% of its obstacles: {boxes} blocker boxes.");
                foreach (var local in mesh.vertices)
                {
                    var p = blocker.transform.TransformPoint(local) - floor.transform.position;
                    Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(10.01f), $"{floor.name} obstacle stays inside the tile.");
                    Assert.That(Mathf.Abs(p.z), Is.LessThanOrEqualTo(10.01f), $"{floor.name} obstacle stays inside the tile.");
                    Assert.That(new Vector2(p.x, p.z).magnitude, Is.GreaterThan(4f), $"{floor.name} keeps the spawn centre open ({p}).");
                    Assert.That(Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.z)), Is.GreaterThan(1f), $"{floor.name} keeps the paths open ({p}).");
                    Assert.That(p.y, Is.GreaterThan(-.5f).And.LessThan(6f), $"{floor.name} blocker height.");
                }
            }
        }

        [Test]
        public void LegacyShowcaseGround_IsRemoved()
        {
            // Learning Comment: Purani 3x3 showcase zameen aur uske assets ab project mein nahi hone chahiye.
            for (int stage = 1; stage <= 10; stage++)
            {
                string ground = $"SH10_L{stage:00}_Ground";
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Environment/{ground}.prefab"), Is.Null, ground);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{ground}.fbx"), Is.Null, ground);
                Assert.That(AssetDatabase.FindAssets(ground + "_ t:Material", new[] { "Assets/Art/Materials" }), Is.Empty, ground);
                string scene = System.IO.File.ReadAllText($"Assets/Scenes/Levels/SH10_L{stage:00}_Showcase.unity");
                Assert.That(scene, Does.Not.Contain(ground), $"Showcase scene {stage} still places the old ground.");
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Ground.prefab"), Is.Null);
            Assert.That(System.IO.File.ReadAllText("Assets/Scenes/Gameplay/Main_Gameplay.unity"),
                Does.Not.Contain("fe3832335d0f98340a4b2c2441e777f8"), "Main_Gameplay's fallback chunk still points at the old ground.");
        }

        [TestCase("SH10_L01_Enemy", true)]
        [TestCase("SH10_L05_Elite", true)]
        [TestCase("SH10_L10_Boss", true)]
        [TestCase("SH10_L02_Pine", true)]
        [TestCase("SH10_L01_Crypt", true)]
        [TestCase("Showcase Camera", false)]
        [TestCase("Asset pack title", false)]
        public void ShowcaseDecorAndStatues_AreHiddenInPlay(string objectName, bool hidden)
        {
            // Learning Comment: Showcase ki sajawat aur khade putle game mein na dikhen; stage floor ki apni rukawatein hain.
            Assert.That(CampaignSceneConnector.IsHiddenInPlay(objectName), Is.EqualTo(hidden));
        }
    }
}
