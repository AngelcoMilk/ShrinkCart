using System;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Features;
using ShrinkCart.Game;
using ShrinkCart.Scaling;

namespace ShrinkCart.Bootstrap
{
    /// <summary>
    /// 组合根：创建并持有全部服务与功能实例。Harmony patch 只通过 <see cref="Current"/> 访问这里暴露的入口；
    /// 初始化失败时 Current 为 null，所有 patch 直接放行原版逻辑。
    /// </summary>
    internal sealed class ShrinkCartRuntime
    {
        private const float CartPruneIntervalSeconds = 1.0f;

        private float _nextCartPrune;

        internal ShrinkCartRuntime(FeatureAvailability availability)
        {
            Availability = availability;
            Authority = new Authority();
            Carts = new CartRegistry();
            Classifier = new ObjectClassifier();
            Ownership = new ScaleOwnership();
            Scaling = new ScalerCoreGateway(Ownership);

            Items = new ItemShrinkFeature(Carts, Classifier, Scaling);
            Nesting = new CartNestingGuardFeature(Classifier);
            Enemies = new EnemyCrushFeature();
            DeadHeads = new DeadHeadShrinkFeature(Carts, Scaling);
            Players = new PlayerScalingFeature(Carts, Scaling);

            Lifecycle = new ModLifecycle(Authority, availability, ModConfig.ModEnabledValue, OnAuthorityGained, OnLevelReset);
            Lifecycle.Register(Items);
            Lifecycle.Register(Nesting);
            Lifecycle.Register(Enemies);
            Lifecycle.Register(DeadHeads);
            Lifecycle.Register(Players);

            Router = new CartObservationRouter(Lifecycle, Carts, Classifier, Items, Nesting, Enemies, DeadHeads);
        }

        internal static ShrinkCartRuntime Current { get; set; }

        internal FeatureAvailability Availability { get; }
        internal Authority Authority { get; }
        internal ModLifecycle Lifecycle { get; }
        internal CartRegistry Carts { get; }
        internal ObjectClassifier Classifier { get; }
        internal ScaleOwnership Ownership { get; }
        internal ScalerCoreGateway Scaling { get; }
        internal CartObservationRouter Router { get; }
        internal ItemShrinkFeature Items { get; }
        internal CartNestingGuardFeature Nesting { get; }
        internal EnemyCrushFeature Enemies { get; }
        internal DeadHeadShrinkFeature DeadHeads { get; }
        internal PlayerScalingFeature Players { get; }

        internal void Update(float now, float unscaledNow)
        {
            Lifecycle.Update(now, unscaledNow);
            if (now >= _nextCartPrune)
            {
                _nextCartPrune = now + CartPruneIntervalSeconds;
                try
                {
                    Carts.Prune(now);
                }
                catch (Exception ex)
                {
                    Lifecycle.ReportFailure(FeatureId.CartObservation, "cart prune", ex);
                }
            }

            DebugCounters.MaybeReport(now);
        }

        internal void OnLevelChanging(float now)
        {
            Lifecycle.OnLevelChanging(now);
        }

        internal void Shutdown(float now)
        {
            Lifecycle.Shutdown(now);
        }

        /// <summary>Patch 捕获到异常时调用：只停用对应功能，异常不向游戏传播。</summary>
        internal void ReportFailure(FeatureId feature, string context, Exception ex)
        {
            Lifecycle.ReportFailure(feature, context, ex);
        }

        private void OnAuthorityGained()
        {
            Carts.RegisterExisting();
        }

        private void OnLevelReset(float now)
        {
            Carts.Clear();
            Classifier.Clear();
            Ownership.Clear();
            DebugCounters.Reset();
        }
    }
}
