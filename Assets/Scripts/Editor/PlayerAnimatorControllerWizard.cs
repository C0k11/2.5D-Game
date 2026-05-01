#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 一键生成 Player 默认 AnimatorController（3D Humanoid 用）：
    ///   - 参数：Speed / VelocityY / IsGrounded / Jump / Land / Dash / Attack / AttackStep
    ///   - 状态：
    ///       Locomotion (1D blend tree by Speed: Idle / Walk / Run)
    ///       JumpStart / JumpLoop / Fall / Land
    ///       Dash
    ///       Attack1 / Attack2 / Attack3
    ///   - 转移按上面 PlayerAnimator 期望连好
    ///
    /// State 不绑 clip，等用户拖入 Mixamo 的 .fbx 子动画。
    /// 输出：Assets/Art/Animation/PlayerAnimator.controller
    /// </summary>
    public static class PlayerAnimatorControllerWizard
    {
        private const string OutputDir = "Assets/Art/Animation";
        private const string OutputPath = OutputDir + "/PlayerAnimator.controller";

        private static AnimatorController _ctrl;

        [MenuItem("Tools/Player/Create Default 3D AnimatorController")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
                AssetDatabase.Refresh();
            }

            if (File.Exists(OutputPath))
            {
                if (!EditorUtility.DisplayDialog("覆盖确认", $"{OutputPath} 已存在，覆盖？", "覆盖", "取消"))
                    return;
                AssetDatabase.DeleteAsset(OutputPath);
            }

            _ctrl = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);
            var ctrl = _ctrl;

            // 参数
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("AttackStep", AnimatorControllerParameterType.Int);

            var sm = ctrl.layers[0].stateMachine;
            sm.entryPosition = new Vector3(0, 0);
            sm.exitPosition  = new Vector3(900, 0);
            sm.anyStatePosition = new Vector3(0, 200);

            // ---- Locomotion (1D blend tree by Speed) ----
            var locomotion = sm.AddState("Locomotion", new Vector3(220, 0));
            var blend = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed" };
            blend.AddChild((Motion)null, 0f);    // Idle slot
            blend.AddChild((Motion)null, 2f);    // Walk slot
            blend.AddChild((Motion)null, 6f);    // Run slot
            AssetDatabase.AddObjectToAsset(blend, ctrl);
            locomotion.motion = blend;
            sm.defaultState = locomotion;

            // ---- 跳跃链 ----
            var sJumpStart = sm.AddState("JumpStart", new Vector3(220, 150));
            var sJumpLoop  = sm.AddState("JumpLoop",  new Vector3(420, 150));
            var sFall      = sm.AddState("Fall",      new Vector3(620, 150));
            var sLand      = sm.AddState("Land",      new Vector3(220, 300));
            var sDash      = sm.AddState("Dash",      new Vector3(420, 300));
            var sAtk1      = sm.AddState("Attack1",   new Vector3(220, 450));
            var sAtk2      = sm.AddState("Attack2",   new Vector3(420, 450));
            var sAtk3      = sm.AddState("Attack3",   new Vector3(620, 450));

            // ---- 转移 ----
            // Locomotion ↔ JumpStart by Jump trigger
            T(locomotion, sJumpStart, useTrigger: "Jump");

            // JumpStart → JumpLoop after exit time
            T(sJumpStart, sJumpLoop, hasExitTime: true, exitTime: 0.4f);

            // JumpLoop → Fall when VelocityY < 0
            T(sJumpLoop, sFall, conds: new[] { Cond(AnimatorConditionMode.Less, "VelocityY", 0f) });

            // Fall / JumpLoop → Land by Land trigger
            T(sFall, sLand, useTrigger: "Land");
            T(sJumpLoop, sLand, useTrigger: "Land");

            // Land → Locomotion after exit time
            T(sLand, locomotion, hasExitTime: true, exitTime: 0.7f);

            // Any → Dash by Dash trigger
            T(null, sDash, useTrigger: "Dash");
            T(sDash, locomotion, hasExitTime: true, exitTime: 0.85f);

            // Any → Attack1/2/3 by Attack trigger + AttackStep
            T(null, sAtk1, conds: new[] { Cond(AnimatorConditionMode.Equals, "AttackStep", 0) }, useTrigger: "Attack");
            T(null, sAtk2, conds: new[] { Cond(AnimatorConditionMode.Equals, "AttackStep", 1) }, useTrigger: "Attack");
            T(null, sAtk3, conds: new[] { Cond(AnimatorConditionMode.Equals, "AttackStep", 2) }, useTrigger: "Attack");
            T(sAtk1, locomotion, hasExitTime: true, exitTime: 0.95f);
            T(sAtk2, locomotion, hasExitTime: true, exitTime: 0.95f);
            T(sAtk3, locomotion, hasExitTime: true, exitTime: 0.95f);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = ctrl;
            EditorGUIUtility.PingObject(ctrl);
            Debug.Log($"[PlayerAnimatorControllerWizard] 已生成 {OutputPath}。\n" +
                      "下一步：\n" +
                      "  1. 打开 controller，点 Locomotion 状态，看到 BlendTree\n" +
                      "  2. 把 idle.fbx 拖进 BlendTree 第 0 槽（Speed=0）\n" +
                      "  3. 把 walking.fbx 拖进第 1 槽（Speed=2）\n" +
                      "  4. 把 running.fbx 拖进第 2 槽（Speed=6）\n" +
                      "  5. JumpStart/JumpLoop/Fall 拖 jump.fbx 不同段（或先全部拖 jump.fbx 凑用）\n" +
                      "  6. Dash 状态留空或拖任意闪避动画");

            _ctrl = null;
        }

        private static AnimatorCondition Cond(AnimatorConditionMode mode, string param, float threshold = 0f)
            => new AnimatorCondition { mode = mode, parameter = param, threshold = threshold };

        private static AnimatorStateTransition T(
            AnimatorState from, AnimatorState to,
            AnimatorCondition[] conds = null,
            bool hasExitTime = false, float exitTime = 0f, float duration = 0.1f,
            string useTrigger = null)
        {
            AnimatorStateTransition tr = from == null
                ? _ctrl.layers[0].stateMachine.AddAnyStateTransition(to)
                : from.AddTransition(to);

            tr.hasExitTime = hasExitTime;
            tr.exitTime = exitTime;
            tr.duration = duration;
            tr.canTransitionToSelf = false;

            if (conds != null)
                foreach (var c in conds) tr.AddCondition(c.mode, c.threshold, c.parameter);

            if (!string.IsNullOrEmpty(useTrigger))
                tr.AddCondition(AnimatorConditionMode.If, 0f, useTrigger);

            return tr;
        }
    }
}
#endif
