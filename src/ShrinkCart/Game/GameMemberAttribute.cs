using System;
using ShrinkCart.Core;

namespace ShrinkCart.Game
{
    /// <summary>
    /// 声明 <see cref="GameAccess"/> 方法访问的非公开游戏成员（在原版 Assembly-CSharp 中不是 public）。
    /// <list type="bullet">
    /// <item>启动时 <see cref="GameAccessSelfCheck"/> 按声明核对成员仍存在且类型一致，缺失则关闭 <see cref="Features"/> 列出的功能。</item>
    /// <item>构建期 BuildTools 核对：方法 IL 实际引用的非公开成员与声明完全一致，且非公开成员只在 GameAccess 中访问。</item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class GameMemberAttribute : Attribute
    {
        internal GameMemberAttribute(Type owner, string name, Type memberType, params FeatureId[] features)
        {
            Owner = owner;
            Name = name;
            MemberType = memberType;
            Features = features;
        }

        internal Type Owner { get; }

        internal string Name { get; }

        /// <summary>字段类型（用于检测游戏更新后字段类型变化）。</summary>
        internal Type MemberType { get; }

        internal FeatureId[] Features { get; }
    }
}
