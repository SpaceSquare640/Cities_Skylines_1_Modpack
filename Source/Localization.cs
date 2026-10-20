using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;

namespace AIGaming
{
    internal static class Localization
    {
        private const string DefaultLanguage = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["settings.title"] = "AI Gaming - Early Preview",
                    ["settings.versionShort"] = "Version 0.1.0 - Early preview",
                    ["settings.status"] = "Status: AI control is not available in this version.",
                    ["settings.description"] = "This preview reads basic city information only. It does not change the simulation.",
                    ["settings.website"] = "Website: https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.summaryButton"] = "Write city summary to log",
                },
                ["zh-tw"] = new Dictionary<string, string>
                {
                    ["settings.title"] = "AI Gaming－早期預覽",
                    ["settings.versionShort"] = "版本 0.1.0－早期預覽",
                    ["settings.status"] = "狀態：此版本尚未提供 AI 控制功能。",
                    ["settings.description"] = "此預覽版只讀取基本城市資訊，不會修改模擬狀態。",
                    ["settings.website"] = "網站：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.summaryButton"] = "將城市摘要寫入記錄",
                },
            };

        internal static string Get(string key)
        {
            return Get(key, new object[0]);
        }

        internal static string Get(string key, params object[] arguments)
        {
            string language = ResolveLanguage();
            string value;

            if (!Tables[DefaultLanguage].TryGetValue(key, out value))
            {
                return key;
            }

            Dictionary<string, string> table;
            if (Tables.TryGetValue(language, out table) && table.TryGetValue(key, out string translated))
            {
                value = translated;
            }

            return arguments == null || arguments.Length == 0
                ? value
                : string.Format(value, arguments);
        }

        private static string ResolveLanguage()
        {
            try
            {
                if (!LocaleManager.exists)
                {
                    return DefaultLanguage;
                }

                string language = LocaleManager.instance.language;
                if (string.IsNullOrEmpty(language))
                {
                    return DefaultLanguage;
                }

                language = language.ToLowerInvariant();
                if (language == "zh" || language.StartsWith("zh-", StringComparison.Ordinal))
                {
                    return "zh-tw";
                }

                return language == "en" || language.StartsWith("en-", StringComparison.Ordinal)
                    ? "en"
                    : DefaultLanguage;
            }
            catch
            {
                return DefaultLanguage;
            }
        }
    }
}
