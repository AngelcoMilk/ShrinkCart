using System.Collections.Generic;
using BepInEx.Configuration;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 按当前配置语言绑定 <see cref="ConfigSchema"/> 中的配置项：
    /// 读取值时当前语言的键优先、另一种语言的键兜底（切换语言不丢值），另一种语言的键随后被删除。
    /// 绑定时传入的默认值始终是声明的默认值（cfg 中 "Default value" 注释因此是真实默认值）。
    /// </summary>
    internal sealed class LocalizedConfigBinder
    {
        private readonly ConfigFile _config;
        private readonly RawConfigAccess _raw;
        private readonly bool _english;
        private readonly Dictionary<string, ConfigEntryBase> _entries = new Dictionary<string, ConfigEntryBase>();
        private readonly HashSet<string> _storedIds = new HashSet<string>();

        internal LocalizedConfigBinder(ConfigFile config, RawConfigAccess raw, bool english)
        {
            _config = config;
            _raw = raw;
            _english = english;
        }

        internal IEnumerable<ConfigEntryBase> Entries => _entries.Values;

        internal ConfigEntry<T> Bind<T>(ConfigKeyDefinition definition)
        {
            string activeSection = definition.Section(_english);
            string activeKey = definition.Key(_english);
            string otherSection = definition.Section(!_english);
            string otherKey = definition.Key(!_english);
            bool sameKey = activeSection == otherSection && activeKey == otherKey;

            bool hasActive = _raw.TryTake(activeSection, activeKey, out string activeRaw);
            string otherRaw = null;
            bool hasOther = !sameKey && _raw.TryTake(otherSection, otherKey, out otherRaw);

            ConfigEntry<T> entry = _config.Bind(
                activeSection,
                activeKey,
                (T)definition.DefaultValue,
                definition.CreateDescription(_english));

            string stored = hasActive ? activeRaw : hasOther ? otherRaw : null;
            if (stored != null)
            {
                entry.SetSerializedValue(stored);
                _storedIds.Add(definition.Id);
            }

            _entries[definition.Id] = entry;
            return entry;
        }

        /// <summary>该配置项在 cfg 中是否已有存储值（任一语言的键）。用于"新键不存在时才继承旧键"。</summary>
        internal bool HadStoredValue(ConfigKeyDefinition definition)
        {
            return _storedIds.Contains(definition.Id);
        }

        internal ConfigEntryBase Entry(ConfigKeyDefinition definition)
        {
            return _entries.TryGetValue(definition.Id, out ConfigEntryBase entry) ? entry : null;
        }
    }
}
