namespace ShrinkCart.Domain
{
    /// <summary>可缩小对象的分类，决定使用哪一组配置倍率。</summary>
    internal enum ShrinkCategory
    {
        Tiny,
        Small,
        Medium,
        Big,
        Wide,
        Tall,
        VeryTall,
        EnemyOrbSmall,
        EnemyOrbMedium,
        EnemyOrbBig,
        EnemyOrbBerserker,
        Surplus,
        ValuableBox,

        /// <summary>商店用品，以及无法识别体型的贵重物。</summary>
        Fallback
    }
}
