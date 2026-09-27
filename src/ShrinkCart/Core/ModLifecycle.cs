using System;
using System.Collections.Generic;

namespace ShrinkCart.Core
{
    /// <summary>
    /// 唯一的生命周期编排者：主机权限变化、开关切换、换关、卸载都经这里分发到各功能。
    /// 每帧为每个功能计算 desired = 主机 &amp;&amp; 总开关 &amp;&amp; 功能可用 &amp;&amp; 功能已启用，
    /// 状态变化时调用 Activate/Deactivate，已激活的功能调用 Tick。
    /// 功能抛出的异常只会让该功能被标记为不可用并停用，不会传播到游戏。
    /// </summary>
    internal sealed class ModLifecycle
    {
        private readonly Authority _authority;
        private readonly FeatureAvailability _availability;
        private readonly Func<bool> _modEnabled;
        private readonly Action _onAuthorityGained;
        private readonly Action<float> _onLevelReset;
        private readonly List<IFeature> _features = new List<IFeature>();
        private readonly bool[] _active = new bool[FeatureIds.Count];
        private readonly HashSet<string> _reportedFailures = new HashSet<string>();
        private bool _wasHost;

        internal ModLifecycle(
            Authority authority,
            FeatureAvailability availability,
            Func<bool> modEnabled,
            Action onAuthorityGained,
            Action<float> onLevelReset)
        {
            _authority = authority;
            _availability = availability;
            _modEnabled = modEnabled;
            _onAuthorityGained = onAuthorityGained;
            _onLevelReset = onLevelReset;
        }

        internal bool IsHost => _wasHost;

        internal bool IsActive(FeatureId feature)
        {
            return _active[(int)feature];
        }

        internal bool IsAvailable(FeatureId feature)
        {
            return _availability.IsAvailable(feature);
        }

        internal void Register(IFeature feature)
        {
            _features.Add(feature);
        }

        internal void Update(float now, float unscaledNow)
        {
            bool host = _authority.Poll(unscaledNow);
            if (host != _wasHost)
            {
                _wasHost = host;
                if (host)
                {
                    try
                    {
                        _onAuthorityGained();
                    }
                    catch (Exception ex)
                    {
                        ReportFailure(FeatureId.CartObservation, "authority gained", ex);
                    }
                }
            }

            bool modEnabled = SafeModEnabled();
            for (int i = 0; i < _features.Count; i++)
            {
                IFeature feature = _features[i];
                int index = (int)feature.Id;
                bool available = _availability.IsAvailable(feature.Id);
                bool desired = host && modEnabled && available && SafeConfiguredEnabled(feature);

                if (desired && !_active[index])
                {
                    Activate(feature, now);
                }
                else if (!desired && _active[index])
                {
                    DeactivateReason reason = !host
                        ? DeactivateReason.AuthorityLost
                        : !available ? DeactivateReason.Unavailable : DeactivateReason.Disabled;
                    Deactivate(feature, reason, now);
                }

                if (!_active[index])
                {
                    continue;
                }

                try
                {
                    feature.Tick(now);
                }
                catch (Exception ex)
                {
                    Fail(feature, "tick", ex, now);
                }
            }
        }

        /// <summary>RunManager.ChangeLevel 之前调用：各功能恢复并清空本关状态，然后清理共享缓存。</summary>
        internal void OnLevelChanging(float now)
        {
            for (int i = 0; i < _features.Count; i++)
            {
                IFeature feature = _features[i];
                if (!_active[(int)feature.Id])
                {
                    continue;
                }

                try
                {
                    feature.ResetForLevel(now);
                }
                catch (Exception ex)
                {
                    Fail(feature, "level reset", ex, now);
                }
            }

            try
            {
                _onLevelReset(now);
            }
            catch (Exception ex)
            {
                ReportFailure(FeatureId.CartObservation, "level reset", ex);
            }
        }

        internal void Shutdown(float now)
        {
            for (int i = 0; i < _features.Count; i++)
            {
                IFeature feature = _features[i];
                if (_active[(int)feature.Id])
                {
                    Deactivate(feature, DeactivateReason.Shutdown, now);
                }
            }
        }

        /// <summary>
        /// Patch 或共享服务捕获到异常时调用：把功能标记为不可用，下一帧由 <see cref="Update"/> 停用依赖它的功能。
        /// 同一功能同一上下文只记录一次错误日志。
        /// </summary>
        internal void ReportFailure(FeatureId feature, string context, Exception ex)
        {
            string reason = "runtime error in " + context + ": " + ex.GetBaseException().Message;
            _availability.MarkUnavailable(feature, reason);
            if (_reportedFailures.Add(feature + "|" + context))
            {
                ModLog.Error("ShrinkCart feature " + feature + " disabled after " + reason + Environment.NewLine + ex);
            }
        }

        private void Activate(IFeature feature, float now)
        {
            try
            {
                feature.Activate(now);
                _active[(int)feature.Id] = true;
                ModLog.Debug("Feature activated: " + feature.Id);
            }
            catch (Exception ex)
            {
                _active[(int)feature.Id] = false;
                ReportFailure(feature.Id, "activate", ex);
            }
        }

        private void Deactivate(IFeature feature, DeactivateReason reason, float now)
        {
            _active[(int)feature.Id] = false;
            try
            {
                feature.Deactivate(reason, now);
                ModLog.Debug("Feature deactivated: " + feature.Id + " reason=" + reason);
            }
            catch (Exception ex)
            {
                ReportFailure(feature.Id, "deactivate", ex);
            }
        }

        private void Fail(IFeature feature, string context, Exception ex, float now)
        {
            ReportFailure(feature.Id, context, ex);
            if (_active[(int)feature.Id])
            {
                Deactivate(feature, DeactivateReason.Unavailable, now);
            }
        }

        private bool SafeModEnabled()
        {
            try
            {
                return _modEnabled();
            }
            catch
            {
                return false;
            }
        }

        private static bool SafeConfiguredEnabled(IFeature feature)
        {
            try
            {
                return feature.IsConfiguredEnabled;
            }
            catch
            {
                return false;
            }
        }
    }
}
