#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Makes the heroes' weapon attacks visible while they run (owner request, 2026-09-25). The shared hero
    /// controller (SH10_Kael.controller) played Attack as a full-body state, so it froze the legs and was only
    /// reachable from the manual attack input. This moves Attack to an "UpperBodyAttack" override layer masked
    /// to the Spine (arms, head, held weapon), so the legs keep the Idle/Run cycle from the base layer.
    /// Menu: Soul Hunter/Content/Set Up Hero Attack Layer, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.PlayerAnimatorSetup.SetUp -quit
    /// </summary>
    public static class PlayerAnimatorSetup
    {
        public const string ControllerPath = "Assets/_Project/Animation/Controllers/SH10_Kael.controller";
        public const string MaskPath = "Assets/_Project/Animation/Controllers/SH10_UpperBody.mask";
        public const string LayerName = "UpperBodyAttack";
        public const string AttackTrigger = "Attack";
        public const float AttackSpeed = 1.6f;

        /// <summary>Bone paths below the Animator (hero prefabs and Kael's model share this rig).</summary>
        public static readonly string[] UpperBodyBones =
        {
            "Rig/Root/Spine", "Rig/Root/Spine/Head",
            "Rig/Root/Spine/UpperArm.L", "Rig/Root/Spine/UpperArm.L/Forearm.L",
            "Rig/Root/Spine/UpperArm.R", "Rig/Root/Spine/UpperArm.R/Forearm.R",
        };

        public static readonly string[] LowerBodyBones =
        {
            "Rig", "Rig/Root",
            "Rig/Root/Thigh.L", "Rig/Root/Thigh.L/Shin.L",
            "Rig/Root/Thigh.R", "Rig/Root/Thigh.R/Shin.R",
        };

        [MenuItem("Soul Hunter/Content/Set Up Hero Attack Layer")]
        public static void SetUp()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new System.IO.FileNotFoundException("Hero controller missing", ControllerPath);

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }
            var paths = UpperBodyBones.Concat(LowerBodyBones).ToArray();
            mask.transformCount = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                mask.SetTransformPath(i, paths[i]);
                mask.SetTransformActive(i, i < UpperBodyBones.Length);
            }
            EditorUtility.SetDirty(mask);

            // Take the attack clip from the base layer, then remove the full-body Attack state there.
            var baseMachine = controller.layers[0].stateMachine;
            var baseAttack = baseMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Attack");
            Motion attackClip = baseAttack != null ? baseAttack.motion : null;
            var existing = controller.layers.FirstOrDefault(l => l.name == LayerName);
            if (attackClip == null && existing != null)
                attackClip = existing.stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Attack")?.motion;
            if (attackClip == null) throw new System.InvalidOperationException("No Attack clip found in " + ControllerPath);
            foreach (var t in baseMachine.anyStateTransitions.Where(t => t.conditions.Any(c => c.parameter == AttackTrigger)).ToArray())
                baseMachine.RemoveAnyStateTransition(t);
            if (baseAttack != null) baseMachine.RemoveState(baseAttack);

            if (existing != null)
            {
                int index = System.Array.FindIndex(controller.layers, l => l.name == LayerName);
                controller.RemoveLayer(index);
            }
            var machine = new AnimatorStateMachine { name = LayerName, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var empty = machine.AddState("Empty");
            empty.writeDefaultValues = true;
            machine.defaultState = empty;
            var attack = machine.AddState("Attack");
            attack.motion = attackClip;
            attack.speed = AttackSpeed; // a quick swing (~0.6 s), so a new shot rarely waits on the last pose
            var enter = machine.AddAnyStateTransition(attack);
            enter.AddCondition(AnimatorConditionMode.If, 0f, AttackTrigger);
            enter.duration = 0.05f;
            enter.hasExitTime = false;
            enter.canTransitionToSelf = false;
            var leave = attack.AddTransition(empty);
            leave.hasExitTime = true;
            leave.exitTime = 0.9f;
            leave.duration = 0.15f;

            controller.AddLayer(new AnimatorControllerLayer
            {
                name = LayerName,
                defaultWeight = 1f,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = machine,
            });
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerAnimatorSetup] Attack moved to the {LayerName} layer ({attackClip.name}, {((AnimationClip)attackClip).length:F2}s).");
        }
    }
}
#endif
