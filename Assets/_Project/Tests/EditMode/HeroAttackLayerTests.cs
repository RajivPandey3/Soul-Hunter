using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: Hero ka attack pose sirf upper body par chale (pair daudte rahein), aur weapon fire hone par
    /// trigger ho sake. Pehle Attack full-body state tha jo sirf manual attack button se chalta tha.
    /// </summary>
    public class HeroAttackLayerTests
    {
        // Same values as SoulHunter.Editor.Content.PlayerAnimatorSetup (editor code is not visible to this assembly).
        private const string ControllerPath = "Assets/_Project/Animation/Controllers/SH10_Kael.controller";
        private const string LayerName = "UpperBodyAttack";
        private const string AttackTrigger = "Attack";
        private static readonly string[] UpperBodyBones =
        {
            "Rig/Root/Spine", "Rig/Root/Spine/Head", "Rig/Root/Spine/UpperArm.L", "Rig/Root/Spine/UpperArm.L/Forearm.L",
            "Rig/Root/Spine/UpperArm.R", "Rig/Root/Spine/UpperArm.R/Forearm.R",
        };
        private static readonly string[] LowerBodyBones =
        {
            "Rig", "Rig/Root", "Rig/Root/Thigh.L", "Rig/Root/Thigh.L/Shin.L", "Rig/Root/Thigh.R", "Rig/Root/Thigh.R/Shin.R",
        };

        [Test]
        public void HeroController_PlaysAttackOnAnUpperBodyLayer_AndTheLegsKeepRunning()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller);
            var baseLayer = controller.layers[0];
            Assert.That(baseLayer.stateMachine.states.Select(s => s.state.name), Does.Not.Contain("Attack"), "Base layer must not freeze the legs.");
            Assert.That(baseLayer.stateMachine.states.Select(s => s.state.name), Does.Contain("Run"));

            var layer = controller.layers.Single(l => l.name == LayerName);
            Assert.That(layer.defaultWeight, Is.EqualTo(1f));
            Assert.That(layer.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
            var attack = layer.stateMachine.states.Select(s => s.state).Single(s => s.name == "Attack");
            Assert.IsNotNull(attack.motion, "Attack state has no clip.");
            Assert.That(layer.stateMachine.defaultState.name, Is.EqualTo("Empty"));
            Assert.That(layer.stateMachine.anyStateTransitions.Any(t => t.destinationState == attack &&
                t.conditions.Any(c => c.parameter == AttackTrigger)), "Attack trigger starts the pose.");
            Assert.That(attack.transitions.Any(t => t.hasExitTime && t.destinationState.name == "Empty"), "The pose ends by itself.");

            var mask = layer.avatarMask;
            Assert.IsNotNull(mask);
            for (int i = 0; i < mask.transformCount; i++)
            {
                string path = mask.GetTransformPath(i);
                bool upper = path.Contains("/Spine");
                Assert.That(mask.GetTransformActive(i), Is.EqualTo(upper), $"{path} in the upper-body mask.");
            }
            Assert.That(Enumerable.Range(0, mask.transformCount).Count(i => mask.GetTransformActive(i)), Is.EqualTo(UpperBodyBones.Length));
        }

        [Test]
        public void UpperBodyMask_MatchesEveryHeroRig()
        {
            // Learning Comment: Mask ke paths har hero prefab ke Animator ke neeche sach mein hone chahiye.
            foreach (string guid in AssetDatabase.FindAssets("SH10_Hero_ t:Prefab", new[] { "Assets/Prefabs/Player" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var animator = prefab.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(animator, prefab.name);
                foreach (string path in UpperBodyBones.Concat(LowerBodyBones))
                    Assert.IsNotNull(animator.transform.Find(path), $"{prefab.name} has no {path}.");
            }
        }
    }
}
