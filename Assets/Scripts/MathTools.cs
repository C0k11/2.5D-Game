using UnityEngine;

// 移植自参考项目 DevelopAUnityActionGameIn5Min 的 MathTools，仅保留移动相关工具
public static class MathTools
{
    /// <summary>
    /// 将数值 from 朝向 to 移动至多 step 距离
    /// </summary>
    public static float MoveTo(this float from, float to, float step)
    {
        if (Mathf.Abs(from - to) <= step)
            return to;
        return to > from ? from + step : from - step;
    }

    /// <summary>
    /// 将向量 from 朝向 to 移动至多 step 距离
    /// </summary>
    public static Vector2 MoveTo(this Vector2 from, Vector2 to, float step)
    {
        if ((from - to).magnitude <= step)
            return to;
        return from + (to - from).normalized * step;
    }
}
