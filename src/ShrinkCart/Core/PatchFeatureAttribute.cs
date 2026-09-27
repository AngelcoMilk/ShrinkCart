using System;

namespace ShrinkCart.Core
{
    /// <summary>
    /// 标注 Harmony patch 类所属的功能、目标方法的调用频率与执行端。
    /// PatchInstaller 据此在安装失败时只停用对应功能；BuildTools 据此生成 docs/INTERFACES.md 的 Hook 表，
    /// 并要求每个 patch 类都带有本特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class PatchFeatureAttribute : Attribute
    {
        internal PatchFeatureAttribute(FeatureId feature)
        {
            Feature = feature;
        }

        internal FeatureId Feature { get; }

        /// <summary>目标方法被游戏调用的频率（写入文档）。</summary>
        public string Frequency { get; set; }

        /// <summary>目标方法在哪些端执行，以及 ShrinkCart 在哪些端处理（写入文档）。</summary>
        public string Side { get; set; }
    }
}
