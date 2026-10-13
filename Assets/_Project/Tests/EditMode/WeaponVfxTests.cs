using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: Har weapon ka apna projectile/effect ho (orange "Bullet" sphere nahi), usme wahi components hon
    /// jo weapon code maangta hai, gameplay VFX library bhari ho, aur pickups par placeholder cube na dikhe.
    /// </summary>
    public class WeaponVfxTests
    {
        private static readonly string[] Placeholders =
        {
            "Assets/Prefabs/Weapons/Bullet.prefab", "Assets/Prefabs/Weapons/Axe_Model.prefab", "Assets/Prefabs/Weapons/Cross_Model.prefab",
        };

        [Test]
        public void NoWeapon_UsesThePlaceholderBulletAxeOrCross()
        {
            var placeholders = Placeholders.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var weapon in prefab.GetComponentsInChildren<AutoAttackWeapon>(true))
                {
                    var property = new SerializedObject(weapon).GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is GameObject go)
                            Assert.That(placeholders.Contains(go), Is.False, $"{prefab.name}.{property.name} still uses {go.name}.");
                }
            }
        }

        /// <summary>
        /// Owner (2026-10-13): "kala dhuan bahut adhik dikh raha tha". Peachone and Dusk Raven drop up to 11 blasts a second
        /// on one spot, so a weapon blast carries no smoke; a one-off explosion keeps 3 light puffs (it was 8 at 0.6-0.7).
        /// </summary>
        [Test]
        public void WeaponBlasts_HaveNoSmoke_AndTheExplosionOnlyALittle()
        {
            foreach (string name in new[] { "SH10_Fx_PeachoneBlast", "SH10_Fx_DuskRavenBlast" })
            {
                var blast = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Weapons/Projectiles/{name}.prefab");
                Assert.IsNotNull(blast, name);
                var systems = blast.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(systems.Any(p => p.gameObject.name == "Fire"), $"{name} still shows its fire.");
                Assert.That(systems.Any(p => p.gameObject.name == "Smoke"), Is.False, $"{name} carries no smoke.");
                foreach (var renderer in blast.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    Assert.That(renderer.sharedMaterial != null && renderer.sharedMaterial.name == "SH10_Vfx_Smoke", Is.False, $"{name}/{renderer.name} draws smoke.");
            }

            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/VFX_Explosion.prefab");
            Assert.IsNotNull(explosion);
            var smoke = explosion.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(p => p.gameObject.name == "Smoke");
            Assert.IsNotNull(smoke, "A one-off explosion keeps a little smoke.");
            var bursts = new ParticleSystem.Burst[smoke.emission.burstCount];
            smoke.emission.GetBursts(bursts);
            Assert.That(bursts.Sum(b => b.maxCount), Is.LessThanOrEqualTo(3), "Smoke puffs.");
            var colour = smoke.main.startColor;
            Assert.That(Mathf.Max(colour.colorMin.a, colour.colorMax.a), Is.LessThanOrEqualTo(0.3f + 0.001f), "Smoke alpha.");
        }

        [TestCase("SH10_Proj_MagicBolt", "Flying")]
        [TestCase("SH10_Proj_Fireball", "Flying")]
        [TestCase("SH10_Proj_HolyBolt", "Flying")]
        [TestCase("SH10_Proj_GunBullet", "Flying")]
        [TestCase("SH10_Proj_Knife", "Flying")]
        [TestCase("SH10_Proj_ThousandEdge", "Flying")]
        [TestCase("SH10_Proj_ManaShot", "Flying")]
        [TestCase("SH10_Proj_CherryBomb", "Flying")]
        [TestCase("SH10_Proj_Bone", "Physics")]
        [TestCase("SH10_Proj_Rune", "Physics")]
        [TestCase("SH10_Proj_Axe", "Physics")]
        [TestCase("SH10_Proj_Cross", "Boomerang")]
        [TestCase("SH10_Fx_HolyWaterPool", "Zone")]
        [TestCase("SH10_Fx_LightningStrike", "Zone")]
        [TestCase("SH10_Fx_PeachoneBlast", "Zone")]
        [TestCase("SH10_Fx_Pentagram", "Visual")]
        [TestCase("SH10_Fx_FreezeBeam", "Visual")]
        public void WeaponObject_HasTheComponentsItsWeaponExpects(string name, string body)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Weapons/Projectiles/{name}.prefab");
            Assert.IsNotNull(prefab, name);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), "Area scaling multiplies the prefab scale.");
            Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled), $"{name} draws something.");
            var collider = prefab.GetComponent<Collider>();
            var rb = prefab.GetComponent<Rigidbody>();
            bool projectile = prefab.GetComponent<Projectile>() != null;
            switch (body)
            {
                case "Flying":
                    Assert.IsTrue(projectile, $"{name} is moved by Projectile.");
                    Assert.IsTrue(collider.isTrigger);
                    break;
                case "Physics":
                    Assert.IsFalse(projectile, $"{name} is moved by its Rigidbody; Projectile would drag it forward and block ProjectileDamage.");
                    Assert.IsFalse(rb.isKinematic);
                    Assert.IsTrue(collider.isTrigger);
                    break;
                case "Boomerang":
                    Assert.IsFalse(projectile);
                    Assert.IsTrue(collider.isTrigger, "A solid cross would shove the player.");
                    break;
                case "Zone":
                    Assert.IsFalse(projectile, $"{name} must stay where it is placed.");
                    Assert.IsTrue(collider.isTrigger);
                    Assert.That(((SphereCollider)collider).radius, Is.GreaterThanOrEqualTo(0.8f), $"{name} hit area matches what is drawn.");
                    break;
                case "Visual":
                    Assert.IsNull(collider, $"{name} is only an effect.");
                    break;
            }
        }

        [Test]
        public void VfxLibrary_HasEveryEffect_AndNoneLoops()
        {
            var library = Resources.Load<VfxLibrary>(VfxLibrary.ResourcePath);
            Assert.IsNotNull(library);
            foreach (VfxKind kind in System.Enum.GetValues(typeof(VfxKind)))
            {
                var prefab = library.Get(kind);
                Assert.IsNotNull(prefab, $"{kind} missing.");
                if (kind == VfxKind.Slash)
                {
                    // The Whip's slash arc is a sprite quad that fades itself out.
                    Assert.IsNotNull(prefab.GetComponent<VfxQuadFade>(), "Slash fades and hides itself.");
                    Assert.That(prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap").name, Does.Contain("slash_arc"));
                    continue;
                }
                var systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(systems, Is.Not.Empty, $"{kind} has no particles.");
                Assert.That(systems.All(p => !p.main.loop), $"{kind} must end so the pool can reuse it.");
                Assert.That(systems.All(p => p.GetComponent<ParticleSystemRenderer>().sharedMaterial != null), $"{kind} material.");
            }
        }

        [TestCase("Assets/Prefabs/Items/XP_Gem.prefab")]
        [TestCase("Assets/Prefabs/Items/Chest.prefab")]
        [TestCase("Assets/Prefabs/Items/HealthChiken.prefab")]
        [TestCase("Assets/Prefabs/Items/Magnet_Pickup.prefab")]
        [TestCase("Assets/Prefabs/Items/TimeFreeze_Pickup.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Gold_Coin.prefab")]
        public void Pickup_ShowsItsModel_NotAPlaceholderCube(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var visible = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToList();
            Assert.That(visible, Is.Not.Empty, path);
            Assert.That(visible.Select(r => r.GetComponent<MeshFilter>().sharedMesh.name), Does.Not.Contain("Cube"), $"{path} still draws the placeholder cube.");
            var motion = prefab.GetComponent<PickupIdleMotion>();
            Assert.IsNotNull(motion, $"{path} bobs and turns.");
            Assert.IsNotNull(motion.Visual);
            Assert.That(motion.Visual, Is.Not.SameAs(prefab.transform), "Only the visual moves, never the pickup root.");
        }
    }
}
