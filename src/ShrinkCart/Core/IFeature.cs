namespace ShrinkCart.Core
{
    internal enum DeactivateReason
    {
        /// <summary>配置关闭（含总开关）：恢复本功能施加的全部缩放。</summary>
        Disabled,

        /// <summary>失去主机权限：只丢弃状态，不做恢复（ScalerCore 的缩放派发本身也只在主机执行）。</summary>
        AuthorityLost,

        /// <summary>依赖失效或运行期异常：尽力恢复后停用。</summary>
        Unavailable,

        /// <summary>插件卸载：恢复全部。</summary>
        Shutdown
    }

    /// <summary>
    /// 主机侧功能的统一契约。由 <see cref="ModLifecycle"/> 根据 desired 状态
    /// （主机 &amp;&amp; 总开关 &amp;&amp; 功能可用 &amp;&amp; <see cref="IsConfiguredEnabled"/>）激活或停用，并在激活期间每帧调用 <see cref="Tick"/>。
    /// 所有方法都只在 Unity 主线程调用；抛出的异常会让功能被标记为不可用并停用。
    /// </summary>
    internal interface IFeature
    {
        FeatureId Id { get; }

        /// <summary>配置是否启用本功能（总开关由 ModLifecycle 统一处理）。</summary>
        bool IsConfiguredEnabled { get; }

        void Activate(float now);

        void Deactivate(DeactivateReason reason, float now);

        /// <summary>切换关卡前调用：恢复本关施加的缩放并清空本关状态，功能保持激活。</summary>
        void ResetForLevel(float now);

        void Tick(float now);
    }
}
