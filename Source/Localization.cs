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
                    ["settings.versionShort"] = "Version 0.3.0 - Early preview",
                    ["settings.status"] = "Status: AI control is not available in this version.",
                    ["settings.description"] = "This preview captures a small read-only city snapshot and discovers optional provider manifests. It does not call provider capabilities or change the simulation.",
                    ["settings.website"] = "Website: https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "Optional Modpack modules (discovery only):",
                    ["settings.summaryButton"] = "Write city snapshot to log",
                    ["interop.unknown"] = "unknown",
                    ["interop.aiImprove.notInstalled"] = "AI Improve: not installed.",
                    ["interop.aiImprove.notEnabled"] = "AI Improve: installed but not enabled.",
                    ["interop.aiImprove.noInterface"] = "AI Improve: installed; no compatible interop interface; version unknown.",
                    ["interop.aiImprove.manifestInvalid"] = "AI Improve: invalid interop manifest; version unknown.",
                    ["interop.aiImprove.discoveryFailed"] = "AI Improve: discovery failed; integration unavailable.",
                    ["interop.aiImprove.ambiguous"] = "AI Improve: multiple providers detected; integration disabled.",
                    ["interop.aiImprove.interfaceIncomplete"] = "AI Improve: manifest compatible (provider {0}, contract {1}, edition {2}, capability {3}), but the capability entry point is unavailable; not called.",
                    ["interop.aiImprove.incompatible"] = "AI Improve: incompatible (provider {0}, contract {1}, edition {2}, capability {3}); not called.",
                    ["interop.aiImprove.compatible"] = "AI Improve: compatible (provider {0}, contract {1}, edition {2}, capability {3}); not called.",
                    ["interop.transport.notInstalled"] = "Transport: not installed.",
                    ["interop.transport.notEnabled"] = "Transport: installed but not enabled.",
                    ["interop.transport.noInterface"] = "Transport: installed; no compatible interop interface; version unknown.",
                    ["interop.transport.manifestInvalid"] = "Transport: invalid interop manifest; version unknown.",
                    ["interop.transport.discoveryFailed"] = "Transport: discovery failed; integration unavailable.",
                    ["interop.transport.ambiguous"] = "Transport: multiple providers detected; integration disabled.",
                    ["interop.transport.interfaceIncomplete"] = "Transport: manifest compatible (provider {0}, contract {1}, edition {2}, capability {3}), but the capability entry point is unavailable; not called.",
                    ["interop.transport.incompatible"] = "Transport: incompatible (provider {0}, contract {1}, edition {2}, capability {3}); not called.",
                    ["interop.transport.compatible"] = "Transport: compatible (provider {0}, contract {1}, edition {2}, capability {3}); not called.",
                },
                ["zh-tw"] = new Dictionary<string, string>
                {
                    ["settings.title"] = "AI Gaming－早期預覽",
                    ["settings.versionShort"] = "版本 0.3.0－早期預覽",
                    ["settings.status"] = "狀態：此版本尚未提供 AI 控制功能。",
                    ["settings.description"] = "此預覽版只擷取精簡的唯讀城市快照，並探索選用模組的互通 manifest；不會呼叫能力或修改模擬狀態。",
                    ["settings.website"] = "網站：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "選用 Modpack 模組（只探索狀態）：",
                    ["settings.summaryButton"] = "將城市快照寫入記錄",
                    ["interop.unknown"] = "未知",
                    ["interop.aiImprove.notInstalled"] = "AI Improve：未安裝。",
                    ["interop.aiImprove.notEnabled"] = "AI Improve：已安裝但未啟用。",
                    ["interop.aiImprove.noInterface"] = "AI Improve：已安裝，但沒有相容互通介面；版本未知。",
                    ["interop.aiImprove.manifestInvalid"] = "AI Improve：互通 manifest 無效；版本未知。",
                    ["interop.aiImprove.discoveryFailed"] = "AI Improve：探索失敗；互通功能不可用。",
                    ["interop.aiImprove.ambiguous"] = "AI Improve：偵測到多個提供方；已停用整合。",
                    ["interop.aiImprove.interfaceIncomplete"] = "AI Improve：manifest 版本相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}），但能力入口不可用；未呼叫。",
                    ["interop.aiImprove.incompatible"] = "AI Improve：不相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}）；未呼叫。",
                    ["interop.aiImprove.compatible"] = "AI Improve：相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}）；未呼叫。",
                    ["interop.transport.notInstalled"] = "Transport：未安裝。",
                    ["interop.transport.notEnabled"] = "Transport：已安裝但未啟用。",
                    ["interop.transport.noInterface"] = "Transport：已安裝，但沒有相容互通介面；版本未知。",
                    ["interop.transport.manifestInvalid"] = "Transport：互通 manifest 無效；版本未知。",
                    ["interop.transport.discoveryFailed"] = "Transport：探索失敗；互通功能不可用。",
                    ["interop.transport.ambiguous"] = "Transport：偵測到多個提供方；已停用整合。",
                    ["interop.transport.interfaceIncomplete"] = "Transport：manifest 版本相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}），但能力入口不可用；未呼叫。",
                    ["interop.transport.incompatible"] = "Transport：不相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}）；未呼叫。",
                    ["interop.transport.compatible"] = "Transport：相容（提供方 {0}、契約 {1}、版本類型 {2}、能力 {3}）；未呼叫。",
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
