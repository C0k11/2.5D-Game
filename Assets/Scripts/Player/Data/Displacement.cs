using UnityEngine;

namespace Game.Player.Data
{
    /// <summary>
    /// 强制位移数据（冲刺、击退、特殊跳跃等）。
    /// 移植自参考项目 DevelopAUnityActionGameIn5Min，去除 Odin Inspector 依赖。
    ///
    /// speedCurve：归一化时间(0..1) -> 速度因子。实际速度 = maxSpeed * curve(t/length) * direction。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDisplacement", menuName = "Game/Player/Displacement")]
    public class Displacement : ScriptableObject
    {
        public float maxSpeed = 24f;
        public float length = 0.2f;
        public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Tooltip("强制位移期间是否无视重力（将 Y 速度清零）。")]
        public bool zeroGravity = true;
    }
}
