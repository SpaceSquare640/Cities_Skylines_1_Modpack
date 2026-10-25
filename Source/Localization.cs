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
                    ["settings.versionShort"] = "Version 0.4.0 - Early preview",
                    ["settings.status"] = "Status: local AI suggestions are available after confirmation; game control is unavailable.",
                    ["settings.description"] = "This preview can send selected schema v1 city fields to a local Ollama model through an optional companion after per-request confirmation. It does not connect to cloud models or control the simulation.",
                    ["settings.website"] = "Website: https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "Optional Modpack modules (discovery only):",
                    ["settings.summaryButton"] = "Write city snapshot to log",
                    ["localAi.title"] = "Local AI suggestions (Ollama Local only)",
                    ["localAi.consentLabel"] = "Data and connection notice",
                    ["localAi.consentNotice"] = "AI Gaming connects only to Ollama on this computer at http://localhost:11434. It requires a listed model with local data and rejects cloud-tagged names, but it cannot inspect Ollama's configuration or guarantee Ollama will not route elsewhere. Disable Ollama cloud features for stricter local-only use. Only the checked schema v1 city fields and your question are sent after preview and confirmation; AI Gaming does not fall back to another provider. The loopback session is authenticated but not encrypted; software able to inspect or inject into the game or companion may see or alter it.",
                    ["localAi.allowLaunch"] = "Allow AI Gaming to start its companion after I confirm each request (off by default; resets when the game restarts)",
                    ["localAi.model"] = "Ollama model name (enter a model installed locally)",
                    ["localAi.question"] = "Question for the local model",
                    ["localAi.selectAll"] = "Select all six city fields",
                    ["localAi.include.gameDate"] = "Include game date",
                    ["localAi.include.population"] = "Include HUD population",
                    ["localAi.include.fundsCents"] = "Include funds (cents)",
                    ["localAi.include.residentialDemand"] = "Include residential demand",
                    ["localAi.include.commercialDemand"] = "Include commercial demand",
                    ["localAi.include.workplaceDemand"] = "Include workplace demand",
                    ["localAi.previewButton"] = "Prepare preview (captures on the simulation thread)",
                    ["localAi.preview.none"] = "No request preview prepared.",
                    ["localAi.previewLabel"] = "Exact request preview (scroll to review all content)",
                    ["localAi.preview.stale"] = "Settings changed. Prepare a new preview before sending.",
                    ["localAi.preview.destination"] = "Target: Ollama on this computer at http://localhost:11434. AI Gaming has no direct cloud fallback; Ollama's own cloud settings may affect routing. Disable Ollama cloud features for stricter local-only use.",
                    ["localAi.preview.model"] = "Model: {0}",
                    ["localAi.preview.schema"] = "Schema version 1 fields to send:",
                    ["localAi.preview.question"] = "Player question to send:",
                    ["localAi.preview.confirmInstruction"] = "This is advice only. Press Confirm to start the companion and send this request once.",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.status.ready"] = "No AI request is active.",
                    ["localAi.status.inputRequired"] = "Enter a model name and a question before preparing a request.",
                    ["localAi.status.capturing"] = "Capturing the selected city fields on the simulation thread. The UI remains responsive.",
                    ["localAi.status.review"] = "Review the exact fields, values, model and question above. Nothing has been sent yet.",
                    ["localAi.status.reviewFirst"] = "Prepare and review a request before confirming it.",
                    ["localAi.status.reviewAgain"] = "The request changed. Prepare and review it again.",
                    ["localAi.status.launchDisabled"] = "Companion auto-start is disabled. Enable it, prepare a fresh preview, then confirm.",
                    ["localAi.status.starting"] = "Starting the companion after your confirmation; connecting only to local Ollama.",
                    ["localAi.status.success"] = "Suggestion received from model {0}. It has not changed the game.",
                    ["localAi.status.cancelled"] = "Request cancelled. Data already sent to the local model cannot be recalled.",
                    ["localAi.status.discarded"] = "Prepared request discarded; no data was sent.",
                    ["localAi.confirmButton"] = "Confirm and send this request once",
                    ["localAi.cancelButton"] = "Cancel or discard request",
                    ["localAi.response.none"] = "No AI response.",
                    ["localAi.responseLabel"] = "Local model response",
                    ["localAi.response.pending"] = "Waiting for the local model. You can cancel this request.",
                    ["localAi.error.snapshot_unavailable"] = "The selected city snapshot is unavailable. No request was sent.",
                    ["localAi.error.simulation_unavailable"] = "The simulation is not ready. No request was sent.",
                    ["localAi.error.companion_build_not_pinned"] = "This build has no pinned companion hash. Companion launch is disabled.",
                    ["localAi.error.companion_edition_invalid"] = "The companion edition is invalid. No connection was made.",
                    ["localAi.error.companion_missing"] = "The matching companion file is missing. No request was sent.",
                    ["localAi.error.companion_hash_mismatch"] = "The companion hash does not match this mod build. No request was sent.",
                    ["localAi.error.companion_cache_unavailable"] = "The verified companion could not be staged in the local cache. No request was sent.",
                    ["localAi.error.companion_start_failed"] = "The companion could not be started. No request was sent.",
                    ["localAi.error.companion_handshake_timeout"] = "The companion did not complete its private startup handshake. No request was sent.",
                    ["localAi.error.companion_identity_mismatch"] = "The companion identity or version does not match. No request was sent.",
                    ["localAi.error.companion_exited"] = "The companion exited before the request. No request was sent.",
                    ["localAi.error.companion_unavailable"] = "The local companion connection failed. No retry or fallback was attempted.",
                    ["localAi.error.companion_request_failed"] = "The companion request failed. No retry or fallback was attempted.",
                    ["localAi.error.companion_rejected_request"] = "The companion rejected the request. No retry or fallback was attempted.",
                    ["localAi.error.companion_response_invalid"] = "The companion response failed verification and was discarded.",
                    ["localAi.error.timeout"] = "The local request timed out. No retry or fallback was attempted.",
                    ["localAi.error.cancelled"] = "The local request was cancelled.",
                    ["localAi.error.model_not_found"] = "That model is not installed in Ollama. Choose an installed model.",
                    ["localAi.error.local_model_required"] = "Only an installed local model is accepted. Cloud model tags are not allowed.",
                    ["localAi.error.local_authentication_failed"] = "Ollama Local rejected the request. No credential was logged.",
                    ["localAi.error.ollama_error"] = "Ollama Local returned an error. Details were not written to the game log.",
                    ["localAi.error.ollama_unavailable"] = "Ollama Local is not responding at http://localhost:11434.",
                    ["localAi.error.invalid_response"] = "Ollama Local returned an invalid response, which was discarded.",
                    ["localAi.error.generic"] = "The local request failed. No retry or fallback was attempted.",
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
                    ["settings.versionShort"] = "版本 0.4.0－早期預覽",
                    ["settings.status"] = "狀態：確認後可取得本機 AI 建議；此版本不提供遊戲控制。",
                    ["settings.description"] = "此預覽版可在每次確認後，透過選用 companion 將所選 schema v1 城市欄位送至本機 Ollama。它不連接雲端模型，也不控制模擬。",
                    ["settings.website"] = "網站：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "選用 Modpack 模組（只探索狀態）：",
                    ["settings.summaryButton"] = "將城市快照寫入記錄",
                    ["localAi.title"] = "本機 AI 建議（僅 Ollama Local）",
                    ["localAi.consentLabel"] = "資料與連線提示",
                    ["localAi.consentNotice"] = "AI Gaming 只連線至此電腦的 Ollama：http://localhost:11434。它要求模型清單中有本機資料的項目，並拒絕雲端標籤，但無法檢查 Ollama 設定，也不能保證 Ollama 不會轉送到其他地方。若要更嚴格地只在本機處理，請停用 Ollama 雲端功能。只有你勾選的 schema v1 城市欄位和問題會在預覽並確認後送出；AI Gaming 不會切換到其他供應商。Loopback 工作階段有驗證但未加密；能檢查或注入遊戲／companion 的本機程式可能看到或改動資料。",
                    ["localAi.allowLaunch"] = "允許 AI Gaming 在我逐次確認請求後啟動 companion（預設關閉；遊戲重啟後重設）",
                    ["localAi.model"] = "Ollama 模型名稱（輸入本機已安裝的模型）",
                    ["localAi.question"] = "要詢問本機模型的問題",
                    ["localAi.selectAll"] = "選取全部六項城市資料",
                    ["localAi.include.gameDate"] = "包含遊戲日期",
                    ["localAi.include.population"] = "包含 HUD 人口",
                    ["localAi.include.fundsCents"] = "包含資金（分）",
                    ["localAi.include.residentialDemand"] = "包含住宅需求",
                    ["localAi.include.commercialDemand"] = "包含商業需求",
                    ["localAi.include.workplaceDemand"] = "包含工作場所需求",
                    ["localAi.previewButton"] = "準備預覽（在 simulation thread 擷取）",
                    ["localAi.preview.none"] = "尚未準備請求預覽。",
                    ["localAi.previewLabel"] = "完整請求預覽（請捲動檢查全部內容）",
                    ["localAi.preview.stale"] = "設定已變更。送出前請重新準備預覽。",
                    ["localAi.preview.destination"] = "目標：此電腦上的 Ollama，http://localhost:11434。AI Gaming 不會直接切換雲端供應商；Ollama 自身的雲端設定可能影響資料路由。若要更嚴格地只在本機處理，請停用 Ollama 雲端功能。",
                    ["localAi.preview.model"] = "模型：{0}",
                    ["localAi.preview.schema"] = "將送出的 schema version 1 欄位：",
                    ["localAi.preview.question"] = "將送出的玩家問題：",
                    ["localAi.preview.confirmInstruction"] = "回覆只作建議，不會改變遊戲。按下確認後才會啟動 companion 並送出一次。",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.status.ready"] = "目前沒有 AI 請求。",
                    ["localAi.status.inputRequired"] = "請先輸入模型名稱與問題，再準備請求。",
                    ["localAi.status.capturing"] = "正在 simulation thread 擷取所選城市欄位；設定頁不會等待。",
                    ["localAi.status.review"] = "請核對上方欄位、數值、模型與問題；目前尚未送出。",
                    ["localAi.status.reviewFirst"] = "請先準備並檢視請求，再確認送出。",
                    ["localAi.status.reviewAgain"] = "請求內容已改變，請重新準備並檢視。",
                    ["localAi.status.launchDisabled"] = "companion 自動啟動已關閉。請啟用後重新預覽，再確認送出。",
                    ["localAi.status.starting"] = "已依你的確認啟動 companion；只連接本機 Ollama。",
                    ["localAi.status.success"] = "已收到模型 {0} 的建議；遊戲狀態沒有改變。",
                    ["localAi.status.cancelled"] = "請求已取消。已送到本機模型的資料無法撤回。",
                    ["localAi.status.discarded"] = "已丟棄準備好的請求；沒有送出資料。",
                    ["localAi.confirmButton"] = "確認並送出這次請求",
                    ["localAi.cancelButton"] = "取消或丟棄請求",
                    ["localAi.response.none"] = "目前沒有 AI 回覆。",
                    ["localAi.responseLabel"] = "本機模型回覆",
                    ["localAi.response.pending"] = "正在等待本機模型；你可以取消請求。",
                    ["localAi.error.snapshot_unavailable"] = "無法取得所選城市快照，沒有送出請求。",
                    ["localAi.error.simulation_unavailable"] = "模擬尚未就緒，沒有送出請求。",
                    ["localAi.error.companion_build_not_pinned"] = "此建置沒有固定 companion 雜湊；已停用啟動。",
                    ["localAi.error.companion_edition_invalid"] = "companion 版本類型無效，沒有建立連線。",
                    ["localAi.error.companion_missing"] = "找不到相符的 companion 檔案，沒有送出請求。",
                    ["localAi.error.companion_hash_mismatch"] = "companion 雜湊與模組建置不符，沒有送出請求。",
                    ["localAi.error.companion_cache_unavailable"] = "無法將已驗證的 companion 放入本機快取，沒有送出請求。",
                    ["localAi.error.companion_start_failed"] = "無法啟動 companion，沒有送出請求。",
                    ["localAi.error.companion_handshake_timeout"] = "companion 未完成匿名 pipe 啟動交握，沒有送出請求。",
                    ["localAi.error.companion_identity_mismatch"] = "companion 身分或版本不符，沒有送出請求。",
                    ["localAi.error.companion_exited"] = "companion 在請求前已結束，沒有送出請求。",
                    ["localAi.error.companion_unavailable"] = "本機 companion 連線失敗，沒有重試或切換來源。",
                    ["localAi.error.companion_request_failed"] = "companion 請求失敗，沒有重試或切換來源。",
                    ["localAi.error.companion_rejected_request"] = "companion 拒絕請求，沒有重試或切換來源。",
                    ["localAi.error.companion_response_invalid"] = "companion 回覆驗證失敗，已丟棄。",
                    ["localAi.error.timeout"] = "本機請求逾時，沒有重試或切換來源。",
                    ["localAi.error.cancelled"] = "本機請求已取消。",
                    ["localAi.error.model_not_found"] = "Ollama 未安裝此模型，請選擇已安裝的模型。",
                    ["localAi.error.local_model_required"] = "只接受本機已安裝的模型，不允許雲端模型標籤。",
                    ["localAi.error.local_authentication_failed"] = "Ollama Local 拒絕請求；沒有記錄任何憑證。",
                    ["localAi.error.ollama_error"] = "Ollama Local 回報錯誤；遊戲記錄未寫入錯誤詳情。",
                    ["localAi.error.ollama_unavailable"] = "無法連接 http://localhost:11434 的 Ollama Local。",
                    ["localAi.error.invalid_response"] = "Ollama Local 回覆格式無效，已丟棄。",
                    ["localAi.error.generic"] = "本機請求失敗，沒有重試或切換來源。",
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
// Implementation update signed by OpenAI-Codex.
