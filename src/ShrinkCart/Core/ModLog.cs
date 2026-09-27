using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace ShrinkCart.Core
{
    /// <summary>
    /// 统一日志入口。
    /// Info：加载摘要、迁移、关键状态变化；Warning：成员缺失、patch 失败、ScalerCore 调用失败；
    /// Error：功能在运行期异常而被停用；Debug：受配置「启用调试日志」控制，以 Info 级别输出。
    /// 同一问题的重复告警用 <see cref="WarnOnce"/> 去重，避免在高频路径刷屏。
    /// </summary>
    internal static class ModLog
    {
        private static readonly HashSet<string> OnceKeys = new HashSet<string>();
        private static ManualLogSource _source;
        private static Func<bool> _debugEnabled;

        internal static void Initialize(ManualLogSource source, Func<bool> debugEnabled)
        {
            _source = source;
            _debugEnabled = debugEnabled;
            OnceKeys.Clear();
        }

        internal static bool DebugEnabled
        {
            get
            {
                try
                {
                    return _debugEnabled != null && _debugEnabled();
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static void Info(string message)
        {
            if (_source != null)
            {
                _source.LogInfo(message);
            }
        }

        internal static void Warn(string message)
        {
            if (_source != null)
            {
                _source.LogWarning(message);
            }
        }

        internal static void Error(string message)
        {
            if (_source != null)
            {
                _source.LogError(message);
            }
        }

        internal static void Debug(string message)
        {
            if (_source != null && DebugEnabled)
            {
                _source.LogInfo("[debug] " + message);
            }
        }

        internal static void WarnOnce(string key, string message)
        {
            if (OnceKeys.Add(key))
            {
                Warn(message);
            }
        }
    }
}
