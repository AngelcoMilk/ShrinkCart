namespace ShrinkCart.Domain
{
    /// <summary>
    /// 物品/头颅维护 Tick 的间隔（对应配置「性能」分组）：
    /// 动态扫描开启时按当前跟踪数量分档，再限制在 [最小, 最大] 之间；关闭时固定用最小间隔。
    /// </summary>
    internal static class ScanCadence
    {
        internal const int FewTrackedThreshold = 6;
        internal const int SomeTrackedThreshold = 15;
        internal const int ManyTrackedThreshold = 30;
        internal const float SomeTrackedIntervalSeconds = 0.35f;
        internal const float ManyTrackedIntervalSeconds = 0.75f;

        internal static float Interval(bool dynamicEnabled, int trackedCount, float minimumSeconds, float maximumSeconds)
        {
            if (maximumSeconds < minimumSeconds)
            {
                maximumSeconds = minimumSeconds;
            }

            if (!dynamicEnabled)
            {
                return minimumSeconds;
            }

            float interval;
            if (trackedCount <= 0)
            {
                interval = maximumSeconds;
            }
            else if (trackedCount <= FewTrackedThreshold)
            {
                interval = minimumSeconds;
            }
            else if (trackedCount <= SomeTrackedThreshold)
            {
                interval = SomeTrackedIntervalSeconds;
            }
            else if (trackedCount <= ManyTrackedThreshold)
            {
                interval = ManyTrackedIntervalSeconds;
            }
            else
            {
                interval = maximumSeconds;
            }

            return MathUtil.Clamp(interval, minimumSeconds, maximumSeconds);
        }
    }
}
