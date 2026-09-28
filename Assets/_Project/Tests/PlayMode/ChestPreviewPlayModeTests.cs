using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The chest-opening screen's 3D chest: filmed from its front (the side with its lock plate), and OPEN swings the lid
    /// (with its skull) up and back, away from the camera.
    /// </summary>
    public class ChestPreviewPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Chest_IsFilmedFromTheFront_AndTheLidSwingsUpAndBack()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var pool = PickupPoolManager.Instance;
            var stage = ChestPreviewStage.Create(pool.ChestPrefab, pool.GoldCoinPrefab, arcana: false);
            try
            {
                yield return WaitUntilRealtime(() => stage.Landed, "the chest to land");
                var renderers = stage.GetComponentsInChildren<Renderer>();
                var lockPlate = renderers.FirstOrDefault(r => r.name.StartsWith("LockPlate"));
                var skull = renderers.FirstOrDefault(r => r.name.StartsWith("Cranium"));
                var cam = stage.GetComponentInChildren<Camera>();
                Assert.That(lockPlate, Is.Not.Null, "The chest model has its lock plate. Parts: " + string.Join(", ", renderers.Select(r => r.name)));
                Assert.That(skull, Is.Not.Null, "The chest model has its skull.");
                var body = renderers.Aggregate(renderers[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; });

                float lockSide = Mathf.Sign(lockPlate.bounds.center.z - body.center.z);
                Assert.That(Mathf.Sign(cam.transform.position.z - body.center.z), Is.EqualTo(lockSide), "The camera is on the lock's side (the front).");

                var hinge = stage.transform.Find("Visual(Clone)/LidHinge") ?? stage.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "LidHinge");
                Assert.That(hinge, Is.Not.Null, "The lid hangs on a hinge.");
                Assert.That(skull.transform.IsChildOf(hinge), Is.True, "The skull is part of the lid.");
                Vector3 skullClosed = skull.transform.position;
                stage.Open(0);
                yield return WaitUntilRealtime(() => stage.LidOpen, "the lid to open");
                yield return null;
                Vector3 skullOpen = skull.transform.position;
                Debug.Log($"[ChestPreviewTest] lockSide {lockSide}, skull closed {skullClosed}, open {skullOpen}, body {body}");
                Assert.That(stage.LidOpen, Is.True);
                Assert.That((skullOpen.z - skullClosed.z) * lockSide, Is.LessThan(-body.extents.z * 0.5f), "The lid swings back, away from the camera.");
                Assert.That(skullOpen.y, Is.GreaterThan(hinge.position.y), "The open lid stands above its hinge (it swung up and over, not down).");
                Assert.That((skullOpen - hinge.position).magnitude, Is.EqualTo((skullClosed - hinge.position).magnitude).Within(0.01f),
                    "The lid turns about the hinge.");
            }
            finally { Object.Destroy(stage.gameObject); }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
