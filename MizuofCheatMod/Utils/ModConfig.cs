using MelonLoader;
using System.Collections.Generic;

namespace MizuofCheatMod.Utils
{
    internal static class ModConfig
    {
        private static MelonPreferences_Category _category;
        private static Dictionary<string, MelonPreferences_Entry> _entries = new Dictionary<string, MelonPreferences_Entry>();

        internal static void Load()
        {
            _category = MelonPreferences.CreateCategory("MizuofCheatMod", "MizuofCheatMod 配置");
            Register("hook_scan_assemblies", "扫描程序集", true);
            Register("hook_scan_types", "扫描类型", true);
            Register("hook_scan_methods", "扫描方法", false);
            Register("hook_scan_singletons", "扫描单例", true);
            _category.SaveToFile(false);
        }

        private static void Register(string key, string displayName, object defaultValue)
        {
            var entry = _category.CreateEntry(key, defaultValue);
            _entries[key] = entry;
        }

        internal static T Get<T>(string key)
        {
            if (_entries.TryGetValue(key, out var entry))
                return (T)entry.BoxedValue;
            return default;
        }

        internal static void Set<T>(string key, T value)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.BoxedValue = value;
                _category.SaveToFile(false);
            }
        }

        internal static bool IsEnabled(string key)
        {
            return Get<bool>(key);
        }

        internal static Dictionary<string, object> GetAll()
        {
            var result = new Dictionary<string, object>();
            foreach (var kv in _entries)
                result[kv.Key] = kv.Value.BoxedValue;
            return result;
        }
    }
}
