using System;
using BepInEx.Configuration;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 只用 BepInEx 公开 API 读取 cfg 中尚未被类型化绑定的原始值。
    /// 插件 Awake 时文件里的值都在 ConfigFile 的孤儿条目中，<c>TryGetEntry</c> 只查已绑定条目，读不到它们。
    /// 这里用 <c>Bind&lt;string&gt;</c>（会把孤儿值移入已绑定条目）读出原始文本，再 <c>Remove</c>（只删已绑定条目），
    /// 组合起来就是"读出并删除"，不需要反射访问 BepInEx 的私有 OrphanedEntries。
    /// 调用期间应保持 <c>ConfigFile.SaveOnConfigSet = false</c>，由调用方最后统一保存。
    /// </summary>
    internal sealed class RawConfigAccess
    {
        private const string MissingSentinel = "\u0001ShrinkCart.Missing\u0001";

        private static readonly ConfigDescription HiddenDescription =
            new ConfigDescription(string.Empty, null, "HideFromREPOConfig");

        private readonly ConfigFile _config;

        internal RawConfigAccess(ConfigFile config)
        {
            _config = config;
        }

        /// <summary>读出并删除 <paramref name="section"/>/<paramref name="key"/> 的原始文本；不存在时返回 false。</summary>
        internal bool TryTake(string section, string key, out string raw)
        {
            raw = null;
            ConfigDefinition definition;
            try
            {
                definition = new ConfigDefinition(section, key);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (_config.ContainsKey(definition))
            {
                raw = _config[definition].GetSerializedValue();
                _config.Remove(definition);
                return true;
            }

            ConfigEntry<string> probe = _config.Bind(definition, MissingSentinel, HiddenDescription);
            string value = probe.Value;
            _config.Remove(definition);
            if (value == MissingSentinel)
            {
                return false;
            }

            raw = value;
            return true;
        }

        /// <summary>删除一个配置项（不存在时什么也不做）。</summary>
        internal bool Delete(string section, string key)
        {
            return TryTake(section, key, out _);
        }
    }
}
