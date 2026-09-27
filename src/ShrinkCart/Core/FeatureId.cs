namespace ShrinkCart.Core
{
    /// <summary>
    /// 功能标识。Harmony patch（[PatchFeature]）、非公开游戏成员（[GameMember]）与配置项都归属到功能；
    /// 任一依赖失效只关闭对应功能（见 <see cref="FeatureAvailability"/>）。
    /// </summary>
    internal enum FeatureId
    {
        /// <summary>生命周期（换关等）。不可用时所有主机侧功能都不激活。</summary>
        Core = 0,

        /// <summary>车内物品观测：trigger 进车信号、原版车内扫描、车辆登记与对象分类。</summary>
        CartObservation = 1,

        ItemShrink = 2,
        CartNestingGuard = 3,
        EnemyCrush = 4,
        DeadHeadShrink = 5,
        PlayerScaling = 6,

        /// <summary>代币箱/外观箱的 ScalerCore 控制器支持（所有端运行，不受主机权限控制）。</summary>
        ValuableBoxSupport = 7
    }

    internal static class FeatureIds
    {
        internal const int Count = 8;

        private static readonly FeatureId[] None = new FeatureId[0];
        private static readonly FeatureId[] CoreOnly = { FeatureId.Core };
        private static readonly FeatureId[] CoreAndObservation = { FeatureId.Core, FeatureId.CartObservation };

        /// <summary>功能运行所依赖的其他功能（依赖不可用时本功能也不可用）。</summary>
        internal static FeatureId[] DependenciesOf(FeatureId feature)
        {
            switch (feature)
            {
                case FeatureId.Core:
                case FeatureId.ValuableBoxSupport:
                    return None;
                case FeatureId.CartObservation:
                    return CoreOnly;
                default:
                    return CoreAndObservation;
            }
        }
    }
}
