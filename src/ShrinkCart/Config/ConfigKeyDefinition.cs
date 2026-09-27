using System;
using BepInEx.Configuration;
using ShrinkCart.Core;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 一个配置项的完整定义。Section/Key 随配置语言切换（REPOConfig 只显示键名，没有显示名机制），
    /// <see cref="Id"/> 是语言无关的稳定标识，用于迁移、测试与文档。
    /// </summary>
    internal sealed class ConfigKeyDefinition
    {
        internal ConfigKeyDefinition(
            string id,
            FeatureId feature,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            Type valueType,
            object defaultValue,
            float? minimum,
            float? maximum,
            string chineseDescription,
            string englishDescription,
            string since)
        {
            Id = id;
            Feature = feature;
            ChineseSection = chineseSection;
            EnglishSection = englishSection;
            ChineseKey = chineseKey;
            EnglishKey = englishKey;
            ValueType = valueType;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            ChineseDescription = chineseDescription;
            EnglishDescription = englishDescription;
            Since = since;
        }

        internal string Id { get; }
        internal FeatureId Feature { get; }
        internal string ChineseSection { get; }
        internal string EnglishSection { get; }
        internal string ChineseKey { get; }
        internal string EnglishKey { get; }
        internal Type ValueType { get; }
        internal object DefaultValue { get; }
        internal float? Minimum { get; }
        internal float? Maximum { get; }
        internal string ChineseDescription { get; }
        internal string EnglishDescription { get; }

        /// <summary>引入该配置项的版本（仅用于文档）。</summary>
        internal string Since { get; }

        internal string Section(bool english)
        {
            return english ? EnglishSection : ChineseSection;
        }

        internal string Key(bool english)
        {
            return english ? EnglishKey : ChineseKey;
        }

        internal ConfigDescription CreateDescription(bool english)
        {
            string text = english ? EnglishDescription : ChineseDescription;
            if (ValueType == typeof(float) && Minimum.HasValue && Maximum.HasValue)
            {
                return new ConfigDescription(text, new AcceptableValueRange<float>(Minimum.Value, Maximum.Value));
            }

            return new ConfigDescription(text);
        }
    }
}
