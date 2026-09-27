using System;

namespace ShrinkCart.Core
{
    /// <summary>
    /// 主机/单机判定（SemiFunc.IsMasterClientOrSingleplayer），结果缓存 0.25 秒。
    /// 判定抛异常时按"非主机"处理（fail-closed），宁可不做也不在客户端误做主机决策。
    /// </summary>
    internal sealed class Authority
    {
        private const float CheckIntervalSeconds = 0.25f;

        private float _nextCheckTime;

        internal bool IsHost { get; private set; }

        internal bool Poll(float unscaledNow)
        {
            if (unscaledNow < _nextCheckTime)
            {
                return IsHost;
            }

            _nextCheckTime = unscaledNow + CheckIntervalSeconds;
            try
            {
                IsHost = SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch (Exception ex)
            {
                IsHost = false;
                ModLog.WarnOnce("authority", "Host check failed; ShrinkCart stays passive until it succeeds: " + ex.Message);
            }

            return IsHost;
        }
    }
}
