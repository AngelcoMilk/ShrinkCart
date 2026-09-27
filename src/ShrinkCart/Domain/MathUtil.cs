using System;

namespace ShrinkCart.Domain
{
    /// <summary>不依赖 UnityEngine 的数值工具，语义与 Mathf 对应方法一致，便于脱离游戏做单元测试。</summary>
    internal static class MathUtil
    {
        internal static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        /// <summary>保留两位小数；舍入方式与 Mathf.Round 相同（Math.Round 的银行家舍入）。</summary>
        internal static float RoundTo2Decimals(float value)
        {
            return (float)Math.Round(value * 100f) / 100f;
        }

        /// <summary>与 Mathf.Approximately 相同的相对误差比较。</summary>
        internal static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), float.Epsilon * 8f);
        }
    }
}
