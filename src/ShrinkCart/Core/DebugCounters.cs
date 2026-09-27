namespace ShrinkCart.Core
{
    /// <summary>
    /// 热路径计数器（自增一个 int，开销可忽略）。开启调试日志时每 10 秒输出一次并清零，
    /// 用来核对 hook 是否按预期早退：分类次数应约等于出现过的不同物体数，扫描次数约为每车每秒 2 次。
    /// </summary>
    internal static class DebugCounters
    {
        private const float ReportIntervalSeconds = 10f;

        internal static int TriggerCalls;
        internal static int TriggerFastPath;
        internal static int ScansProcessed;
        internal static int Classifications;

        private static float _nextReportTime;

        internal static void MaybeReport(float now)
        {
            if (now < _nextReportTime)
            {
                return;
            }

            _nextReportTime = now + ReportIntervalSeconds;
            if (ModLog.DebugEnabled && (TriggerCalls != 0 || ScansProcessed != 0 || Classifications != 0))
            {
                ModLog.Debug(
                    "Counters (last " + ReportIntervalSeconds + "s): triggerCalls=" + TriggerCalls +
                    " triggerFastPath=" + TriggerFastPath +
                    " scansProcessed=" + ScansProcessed +
                    " classified=" + Classifications);
            }

            Reset();
        }

        internal static void Reset()
        {
            TriggerCalls = 0;
            TriggerFastPath = 0;
            ScansProcessed = 0;
            Classifications = 0;
        }
    }
}
