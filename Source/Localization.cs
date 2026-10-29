using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIGaming
{
    internal static class Localization
    {
        private const string DefaultLanguage = "en";
        private const string LanguagePreferenceKey = "AIGaming.Language";
        private static readonly string[] LanguageCodes = { "en", "zh-tw", "zh-cn", "ja", "ko" };
        private static readonly string[] LanguageNames = { "English", "繁體中文", "简体中文", "日本語", "한국어" };
        private static bool _languageLoaded;
        private static string _selectedLanguage;

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["settings.language"] = "Language",
                    ["settings.title"] = "AI Gaming - Early Preview",
                    ["settings.versionShort"] = "Version 0.4.0 - Early preview",
                    ["settings.status"] = "Status: local AI suggestions are available after confirmation; game control is unavailable.",
                    ["settings.description"] = "After each confirmation, this preview can send selected city snapshot schema v3 fields inside request schema v4—including utility totals, built zoning counts and area, traffic and public-transport statistics, and service indicators—plus game-reported installed-and-available DLC identifiers to local Ollama through an optional companion. The local AI response body limit defaults to unlimited and can be set in the settings. It does not connect to cloud models or control the simulation.",
                    ["settings.website"] = "Website: https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "Optional Modpack modules (discovery only):",
                    ["settings.summaryButton"] = "Write city snapshot to log",
                    ["localAi.title"] = "Local AI suggestions (Ollama Local only)",
                    ["localAi.consentLabel"] = "Data and connection notice",
                    ["localAi.consentNotice"] = "AI Gaming connects only to Ollama on this computer at http://localhost:11434. It requires a listed model with local data and rejects cloud-tagged names, but it cannot inspect Ollama's configuration or guarantee Ollama will not route elsewhere. Disable Ollama cloud features for stricter local-only use. After preview and confirmation, it sends only the checked schema v3 city fields, the DLC identifiers the game API reports as installed and available (always included), and your question. Utility values are raw game units; zoning covers built buildings, not vacant zoned cells; traffic and passenger statistics have unspecified scales/windows; police and healthcare are workload/capacity, not geographic coverage. Fire coverage is an average byte over created network segments. AI Gaming does not fall back to another provider. The loopback session is authenticated but not encrypted; software able to inspect or inject into the game or companion may see or alter it.",
                    ["localAi.allowLaunch"] = "Allow AI Gaming to start its companion after I confirm each request (off by default; resets when the game restarts)",
                    ["localAi.model"] = "Ollama model name (enter a model installed locally)",
                    ["localAi.responseLimit"] = "Ollama response body limit (MiB; 0 = unlimited; 1-2047)",
                    ["localAi.question"] = "Question for the local model",
                    ["localAi.selectAll"] = "Select all city data",
                    ["localAi.include.gameDate"] = "Include game date",
                    ["localAi.include.population"] = "Include HUD population",
                    ["localAi.include.fundsCents"] = "Include funds (cents)",
                    ["localAi.include.residentialDemand"] = "Include residential demand",
                    ["localAi.include.commercialDemand"] = "Include commercial demand",
                    ["localAi.include.workplaceDemand"] = "Include workplace demand",
                    ["localAi.include.utilities"] = "Include electricity, water, sewage and garbage data",
                    ["localAi.include.zoning"] = "Include built buildings and area by use (not vacant zoned cells)",
                    ["localAi.include.trafficTransit"] = "Include traffic-flow and public-transport statistics",
                    ["localAi.include.serviceStatus"] = "Include fire segment coverage and police/health workload",
                    ["localAi.previewButton"] = "Prepare preview (captures on the simulation thread)",
                    ["localAi.preview.none"] = "No request preview prepared.",
                    ["localAi.previewLabel"] = "Exact request preview (scroll to review all content)",
                    ["localAi.preview.stale"] = "Settings changed. Prepare a new preview before sending.",
                    ["localAi.preview.destination"] = "Target: Ollama on this computer at http://localhost:11434. AI Gaming has no direct cloud fallback; Ollama's own cloud settings may affect routing. Disable Ollama cloud features for stricter local-only use.",
                    ["localAi.preview.model"] = "Model: {0}",
                    ["localAi.preview.schema"] = "Request schema v4 (city snapshot schema v3) fields to send (the game's installed-and-available DLC identifiers are always included):",
                    ["localAi.preview.responseLimit.unlimited"] = "Ollama /api/chat HTTP response body limit: unlimited (no application-level size cap; includes the JSON envelope).",
                    ["localAi.preview.responseLimit.configured"] = "Ollama /api/chat HTTP response body limit: discard the full response above {0} MiB (includes the JSON envelope).",
                    ["localAi.preview.utilities"] = "Utilities (raw game values; physical units unverified): power capacity/consumption {0}/{1}; water {2}/{3}; sewage capacity/accumulation {4}/{5}; garbage amount/capacity/accumulation {6}/{7}/{8}; incineration capacity {9}.",
                    ["localAi.preview.zoning"] = "Built buildings by use (count/area in game units; vacant zoned cells excluded): residential {0}/{1}; commercial {2}/{3}; industrial {4}/{5}; office {6}/{7}.",
                    ["localAi.preview.trafficTransit"] = "Traffic/transit: game-reported traffic-flow statistic {0} (raw scale/window unknown; not a congestion percentage); average public-transport passengers {1} (period unspecified).",
                    ["localAi.preview.serviceStatus"] = "Service indicators: police criminal amount/capacity/extra {0}/{1}/{2}; fire mean coverage byte {3}/255 across {4} created network segments; health sick count/capacity/productivity {5}/{6}/{7}. Police and health are citywide workload/capacity, not geographic coverage; zero fire segments means no coverage sample.",
                    ["localAi.preview.unavailable"] = "unavailable",
                    ["localAi.preview.question"] = "Player question to send:",
                    ["localAi.preview.confirmInstruction"] = "This is advice only. Press Confirm to start the companion and send this request once.",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.field.installedAvailableDlcs"] = "installedAvailableDlcs (game API identifiers)",
                    ["localAi.dlc.noneDetected"] = "(none reported by the game API)",
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
                    ["localAi.error.dlc_detection_unavailable"] = "The game could not verify installed and available DLC. No preview was prepared and no request was sent. Try again when the platform DLC state is ready.",
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
                    ["localAi.error.response_limit_invalid"] = "Enter a whole number from 0 to 2047 MiB. Use 0 for no application-level response size limit.",
                    ["localAi.error.response_limit_exceeded"] = "The complete Ollama /api/chat HTTP response body exceeded the configured limit and was discarded.",
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
                    ["settings.language"] = "語言",
                    ["settings.title"] = "AI Gaming－早期預覽",
                    ["settings.versionShort"] = "版本 0.4.0－早期預覽",
                    ["settings.status"] = "狀態：確認後可取得本機 AI 建議；此版本不提供遊戲控制。",
                    ["settings.description"] = "每次確認後，此預覽版可透過選用 companion，將所選城市快照 schema v3 欄位（request schema v4，包含公用事業、已建分區統計、交通與公共運輸統計、服務指標）及遊戲回報已安裝且可用的 DLC 識別名稱送至本機 Ollama。本機 AI 回應本文上限預設為無上限，使用者可在設定中自行設定。它不連接雲端模型，也不控制模擬。",
                    ["settings.website"] = "網站：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "選用 Modpack 模組（只探索狀態）：",
                    ["settings.summaryButton"] = "將城市快照寫入記錄",
                    ["localAi.title"] = "本機 AI 建議（僅 Ollama Local）",
                    ["localAi.consentLabel"] = "資料與連線提示",
                    ["localAi.consentNotice"] = "AI Gaming 只連線至此電腦的 Ollama：http://localhost:11434。它要求模型清單中有本機資料的項目，並拒絕雲端標籤，但無法檢查 Ollama 設定，也不能保證 Ollama 不會轉送到其他地方。若要更嚴格地只在本機處理，請停用 Ollama 雲端功能。預覽並確認後，只會送出你勾選的 schema v3 城市欄位、遊戲 API 回報已安裝且可用的 DLC 識別名稱（自動納入），以及你的問題。公用事業是遊戲原始值；分區只表示已建建築，不含空置分區格；交通與乘客統計的尺度／期間未確認；警察與醫療是負荷／容量，不是地理覆蓋；消防為已建路段覆蓋 byte 平均。AI Gaming 不會切換供應商。Loopback 工作階段有驗證但未加密；能檢查或注入遊戲／companion 的本機程式可能看到或改動資料。",
                    ["localAi.allowLaunch"] = "允許 AI Gaming 在我逐次確認請求後啟動 companion（預設關閉；遊戲重啟後重設）",
                    ["localAi.model"] = "Ollama 模型名稱（輸入本機已安裝的模型）",
                    ["localAi.responseLimit"] = "Ollama 回應本文上限（MiB；0 = 無上限；1-2047）",
                    ["localAi.question"] = "要詢問本機模型的問題",
                    ["localAi.selectAll"] = "選取全部城市資料",
                    ["localAi.include.gameDate"] = "包含遊戲日期",
                    ["localAi.include.population"] = "包含 HUD 人口",
                    ["localAi.include.fundsCents"] = "包含資金（分）",
                    ["localAi.include.residentialDemand"] = "包含住宅需求",
                    ["localAi.include.commercialDemand"] = "包含商業需求",
                    ["localAi.include.workplaceDemand"] = "包含工作場所需求",
                    ["localAi.include.utilities"] = "包含電力、供水、污水及垃圾處理資料",
                    ["localAi.include.zoning"] = "包含各用途已建建築數量及面積（不含空置分區格）",
                    ["localAi.include.trafficTransit"] = "包含交通流量及公共運輸統計",
                    ["localAi.include.serviceStatus"] = "包含消防路段覆蓋與警察／醫療負荷",
                    ["localAi.previewButton"] = "準備預覽（在 simulation thread 擷取）",
                    ["localAi.preview.none"] = "尚未準備請求預覽。",
                    ["localAi.previewLabel"] = "完整請求預覽（請捲動檢查全部內容）",
                    ["localAi.preview.stale"] = "設定已變更。送出前請重新準備預覽。",
                    ["localAi.preview.destination"] = "目標：此電腦上的 Ollama，http://localhost:11434。AI Gaming 不會直接切換雲端供應商；Ollama 自身的雲端設定可能影響資料路由。若要更嚴格地只在本機處理，請停用 Ollama 雲端功能。",
                    ["localAi.preview.model"] = "模型：{0}",
                    ["localAi.preview.schema"] = "將送出的 request schema version 4 欄位（城市快照 schema version 3；遊戲回報已安裝且可用的 DLC 識別名稱會自動納入）：",
                    ["localAi.preview.responseLimit.unlimited"] = "Ollama /api/chat HTTP 回應本文上限：無上限（應用程式不設大小限制；包含 JSON 外層資料）。",
                    ["localAi.preview.responseLimit.configured"] = "Ollama /api/chat HTTP 回應本文超過 {0} MiB 時會整份丟棄（包含 JSON 外層資料）。",
                    ["localAi.preview.utilities"] = "公用事業（遊戲原始值；物理單位未確認）：電力容量／消耗 {0}/{1}；供水 {2}/{3}；污水容量／累積 {4}/{5}；垃圾量／容量／累積 {6}/{7}/{8}；焚化容量 {9}。",
                    ["localAi.preview.zoning"] = "各用途已建建築（數量／遊戲面積單位；不含空置分區格）：住宅 {0}/{1}；商業 {2}/{3}；工業 {4}/{5}；辦公 {6}/{7}。",
                    ["localAi.preview.trafficTransit"] = "交通／公共運輸：遊戲回報交通流量統計 {0}（原始尺度／時間窗未確認；不是壅塞百分比）；公共運輸平均乘客數 {1}（統計期間未確認）。",
                    ["localAi.preview.serviceStatus"] = "服務指標：警察犯罪量／容量／超額 {0}/{1}/{2}；消防已建路段覆蓋 byte 平均 {3}/255，樣本路段 {4}；醫療病患數／容量／生產力 {5}/{6}/{7}。警察與醫療是全市負荷／容量，不是地理覆蓋；消防路段為 0 表示沒有樣本。",
                    ["localAi.preview.unavailable"] = "無法取得",
                    ["localAi.preview.question"] = "將送出的玩家問題：",
                    ["localAi.preview.confirmInstruction"] = "回覆只作建議，不會改變遊戲。按下確認後才會啟動 companion 並送出一次。",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.field.installedAvailableDlcs"] = "installedAvailableDlcs（遊戲 API 識別名稱）",
                    ["localAi.dlc.noneDetected"] = "（遊戲 API 未回報任何項目）",
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
                    ["localAi.error.dlc_detection_unavailable"] = "遊戲目前無法確認已安裝且可用的 DLC。沒有準備預覽或送出請求；請在平台 DLC 狀態就緒後重試。",
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
                    ["localAi.error.response_limit_invalid"] = "請輸入 0 至 2047 的整數 MiB；輸入 0 表示應用程式不設回應大小上限。",
                    ["localAi.error.response_limit_exceeded"] = "Ollama /api/chat 的完整 HTTP 回應本文超過設定上限，已整份丟棄。",
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
                ["zh-cn"] = new Dictionary<string, string>
                {
                    ["settings.language"] = "语言",
                    ["settings.title"] = "AI Gaming - 早期预览",
                    ["settings.versionShort"] = "版本 0.4.0 - 早期预览",
                    ["settings.status"] = "状态：确认后可获取本地 AI 建议；此版本不提供游戏控制。",
                    ["settings.description"] = "每次确认后，此预览版可通过可选 companion 将所选城市快照 schema v3 字段（request schema v4，包含公用事业、已建分区统计、交通与公共交通统计、服务指标）及游戏报告已安装且可用的 DLC 标识发送到本地 Ollama。本地 AI 响应正文上限默认为无限制，用户可在设置中自行指定。它不连接云端模型，也不控制模拟。",
                    ["settings.website"] = "网站：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "可选 Modpack 模组（仅检测状态）：",
                    ["settings.summaryButton"] = "将城市快照写入日志",
                    ["localAi.title"] = "本地 AI 建议（仅 Ollama Local）",
                    ["localAi.consentLabel"] = "数据与连接提示",
                    ["localAi.consentNotice"] = "AI Gaming 只连接到此电脑上的 Ollama：http://localhost:11434。它要求模型列表中有本地模型条目，并拒绝带有云端标签的名称，但无法检查 Ollama 的设置，也不能保证 Ollama 不会将请求转发到其他位置。若要更严格地仅在本地处理，请停用 Ollama 云端功能。预览并确认后，只会发送你勾选的 schema v3 城市字段、游戏 API 报告已安装且可用的 DLC 标识（自动包含）以及你的问题。公用事业为游戏原始值；分区仅统计已建建筑，不含空置分区格；交通与乘客统计的尺度／周期未确认；警察与医疗是负荷／容量，不是地理覆盖；消防为路段覆盖 byte 平均。AI Gaming 不会切换提供方。Loopback 会话经过身份验证但未加密；能够检查或注入游戏或 companion 的软件可能看到或篡改数据。",
                    ["localAi.allowLaunch"] = "允许 AI Gaming 在我逐次确认每个请求后启动 companion（默认关闭；重启游戏后重置）",
                    ["localAi.model"] = "Ollama 模型名称（输入本地已安装的模型）",
                    ["localAi.responseLimit"] = "Ollama 响应正文上限（MiB；0 = 无限制；1-2047）",
                    ["localAi.question"] = "要询问本地模型的问题",
                    ["localAi.selectAll"] = "选择全部城市数据",
                    ["localAi.include.gameDate"] = "包含游戏日期",
                    ["localAi.include.population"] = "包含 HUD 人口",
                    ["localAi.include.fundsCents"] = "包含资金（分）",
                    ["localAi.include.residentialDemand"] = "包含住宅需求",
                    ["localAi.include.commercialDemand"] = "包含商业需求",
                    ["localAi.include.workplaceDemand"] = "包含工作场所需求",
                    ["localAi.include.utilities"] = "包含电力、供水、污水和垃圾处理数据",
                    ["localAi.include.zoning"] = "包含各用途已建建筑数量和面积（不含空置分区格）",
                    ["localAi.include.trafficTransit"] = "包含交通流量和公共交通统计",
                    ["localAi.include.serviceStatus"] = "包含消防路段覆盖及警察／医疗负荷",
                    ["localAi.previewButton"] = "准备预览（在 simulation thread 中采集）",
                    ["localAi.preview.none"] = "尚未准备请求预览。",
                    ["localAi.previewLabel"] = "完整请求预览（请滚动检查全部内容）",
                    ["localAi.preview.stale"] = "设置已更改。发送前请重新准备预览。",
                    ["localAi.preview.destination"] = "目标：此电脑上的 Ollama，http://localhost:11434。AI Gaming 不会直接切换到云端提供方；Ollama 自身的云端设置可能影响数据路由。若要更严格地仅在本地处理，请停用 Ollama 云端功能。",
                    ["localAi.preview.model"] = "模型：{0}",
                    ["localAi.preview.schema"] = "将发送的 request schema version 4 字段（城市快照 schema version 3；游戏报告已安装且可用的 DLC 标识始终包含）：",
                    ["localAi.preview.responseLimit.unlimited"] = "Ollama /api/chat HTTP 响应正文上限：无限制（应用程序不设大小上限；包含 JSON 外层数据）。",
                    ["localAi.preview.responseLimit.configured"] = "Ollama /api/chat HTTP 响应正文超过 {0} MiB 时将整份丢弃（包含 JSON 外层数据）。",
                    ["localAi.preview.utilities"] = "公用事业（游戏原始值；物理单位未确认）：电力容量／消耗 {0}/{1}；供水 {2}/{3}；污水容量／累积 {4}/{5}；垃圾量／容量／累积 {6}/{7}/{8}；焚烧容量 {9}。",
                    ["localAi.preview.zoning"] = "各用途已建建筑（数量／游戏面积单位；不含空置分区格）：住宅 {0}/{1}；商业 {2}/{3}；工业 {4}/{5}；办公 {6}/{7}。",
                    ["localAi.preview.trafficTransit"] = "交通／公共交通：游戏报告的交通流量统计 {0}（原始尺度／时间窗未确认；不是拥堵百分比）；公共交通平均乘客数 {1}（统计周期未确认）。",
                    ["localAi.preview.serviceStatus"] = "服务指标：警察犯罪量／容量／超额 {0}/{1}/{2}；消防已建路段覆盖 byte 平均 {3}/255，样本路段 {4}；医疗病患数／容量／生产力 {5}/{6}/{7}。警察与医疗是全市负荷／容量，不是地理覆盖；消防路段为 0 表示没有样本。",
                    ["localAi.preview.unavailable"] = "无法取得",
                    ["localAi.preview.question"] = "将发送的玩家问题：",
                    ["localAi.preview.confirmInstruction"] = "回复仅供建议，不会更改游戏。按下确认后才会启动 companion 并发送一次此请求。",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.field.installedAvailableDlcs"] = "installedAvailableDlcs（游戏 API 标识）",
                    ["localAi.dlc.noneDetected"] = "（游戏 API 未报告任何项目）",
                    ["localAi.status.ready"] = "当前没有 AI 请求。",
                    ["localAi.status.inputRequired"] = "请先输入模型名称和问题，再准备请求。",
                    ["localAi.status.capturing"] = "正在 simulation thread 中采集所选城市字段；设置页面仍可响应。",
                    ["localAi.status.review"] = "请核对上方字段、数值、模型和问题；目前尚未发送。",
                    ["localAi.status.reviewFirst"] = "请先准备并检查请求，然后再确认发送。",
                    ["localAi.status.reviewAgain"] = "请求内容已更改，请重新准备并检查。",
                    ["localAi.status.launchDisabled"] = "companion 自动启动已关闭。请启用后重新预览，再确认发送。",
                    ["localAi.status.starting"] = "已根据你的确认启动 companion；仅连接本地 Ollama。",
                    ["localAi.status.success"] = "已收到模型 {0} 的建议；游戏状态未更改。",
                    ["localAi.status.cancelled"] = "请求已取消。已发送到本地模型的数据无法撤回。",
                    ["localAi.status.discarded"] = "已丢弃准备好的请求；没有发送数据。",
                    ["localAi.confirmButton"] = "确认并发送此请求一次",
                    ["localAi.cancelButton"] = "取消或丢弃请求",
                    ["localAi.response.none"] = "当前没有 AI 回复。",
                    ["localAi.responseLabel"] = "本地模型回复",
                    ["localAi.response.pending"] = "正在等待本地模型；你可以取消此请求。",
                    ["localAi.error.snapshot_unavailable"] = "无法获取所选城市快照；没有发送请求。",
                    ["localAi.error.dlc_detection_unavailable"] = "游戏目前无法确认已安装且可用的 DLC。未准备预览，也未发送请求；请在平台 DLC 状态就绪后重试。",
                    ["localAi.error.simulation_unavailable"] = "模拟尚未就绪；没有发送请求。",
                    ["localAi.error.companion_build_not_pinned"] = "此构建未固定 companion 哈希；已禁用启动。",
                    ["localAi.error.companion_edition_invalid"] = "companion 版本类型无效；未建立连接。",
                    ["localAi.error.companion_missing"] = "找不到匹配的 companion 文件；没有发送请求。",
                    ["localAi.error.companion_hash_mismatch"] = "companion 哈希与此模组构建不匹配；没有发送请求。",
                    ["localAi.error.companion_cache_unavailable"] = "无法将已验证的 companion 暂存到本地缓存；没有发送请求。",
                    ["localAi.error.companion_start_failed"] = "无法启动 companion；没有发送请求。",
                    ["localAi.error.companion_handshake_timeout"] = "companion 未完成私有启动握手；没有发送请求。",
                    ["localAi.error.companion_identity_mismatch"] = "companion 身份或版本不匹配；没有发送请求。",
                    ["localAi.error.companion_exited"] = "companion 在请求前已退出；没有发送请求。",
                    ["localAi.error.companion_unavailable"] = "本地 companion 连接失败；未重试或切换来源。",
                    ["localAi.error.companion_request_failed"] = "companion 请求失败；未重试或切换来源。",
                    ["localAi.error.companion_rejected_request"] = "companion 拒绝了请求；未重试或切换来源。",
                    ["localAi.error.companion_response_invalid"] = "companion 回复未通过验证，已丢弃。",
                    ["localAi.error.timeout"] = "本地请求超时；未重试或切换来源。",
                    ["localAi.error.cancelled"] = "本地请求已取消。",
                    ["localAi.error.model_not_found"] = "Ollama 未安装此模型。请选择已安装的模型。",
                    ["localAi.error.local_model_required"] = "只接受本地已安装的模型；不允许云端模型标签。",
                    ["localAi.error.local_authentication_failed"] = "Ollama Local 拒绝了请求；没有记录凭据。",
                    ["localAi.error.ollama_error"] = "Ollama Local 返回错误；详情未写入游戏日志。",
                    ["localAi.error.ollama_unavailable"] = "Ollama Local 未在 http://localhost:11434 响应。",
                    ["localAi.error.invalid_response"] = "Ollama Local 返回无效回复，已丢弃。",
                    ["localAi.error.response_limit_invalid"] = "请输入 0 至 2047 的整数 MiB；输入 0 表示应用程序不设响应大小上限。",
                    ["localAi.error.response_limit_exceeded"] = "Ollama /api/chat 的完整 HTTP 响应正文超过设定上限，已整份丢弃。",
                    ["localAi.error.generic"] = "本地请求失败；未重试或切换来源。",
                    ["interop.unknown"] = "未知",
                    ["interop.aiImprove.notInstalled"] = "AI Improve：未安装。",
                    ["interop.aiImprove.notEnabled"] = "AI Improve：已安装但未启用。",
                    ["interop.aiImprove.noInterface"] = "AI Improve：已安装，但没有兼容的互通接口；版本未知。",
                    ["interop.aiImprove.manifestInvalid"] = "AI Improve：互通清单无效；版本未知。",
                    ["interop.aiImprove.discoveryFailed"] = "AI Improve：检测失败；集成不可用。",
                    ["interop.aiImprove.ambiguous"] = "AI Improve：检测到多个提供方；已禁用集成。",
                    ["interop.aiImprove.interfaceIncomplete"] = "AI Improve：清单版本兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}），但能力入口不可用；未调用。",
                    ["interop.aiImprove.incompatible"] = "AI Improve：不兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}）；未调用。",
                    ["interop.aiImprove.compatible"] = "AI Improve：兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}）；未调用。",
                    ["interop.transport.notInstalled"] = "Transport：未安装。",
                    ["interop.transport.notEnabled"] = "Transport：已安装但未启用。",
                    ["interop.transport.noInterface"] = "Transport：已安装，但没有兼容的互通接口；版本未知。",
                    ["interop.transport.manifestInvalid"] = "Transport：互通清单无效；版本未知。",
                    ["interop.transport.discoveryFailed"] = "Transport：检测失败；集成不可用。",
                    ["interop.transport.ambiguous"] = "Transport：检测到多个提供方；已禁用集成。",
                    ["interop.transport.interfaceIncomplete"] = "Transport：清单版本兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}），但能力入口不可用；未调用。",
                    ["interop.transport.incompatible"] = "Transport：不兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}）；未调用。",
                    ["interop.transport.compatible"] = "Transport：兼容（提供方 {0}、契约 {1}、版本类型 {2}、能力 {3}）；未调用。",
                },
                ["ja"] = new Dictionary<string, string>
                {
                    ["settings.language"] = "言語",
                    ["settings.title"] = "AI Gaming - 早期プレビュー",
                    ["settings.versionShort"] = "バージョン 0.4.0 - 早期プレビュー",
                    ["settings.status"] = "状態：確認後にローカル AI の提案を利用できます。このバージョンではゲームを操作できません。",
                    ["settings.description"] = "各リクエストの確認後、request schema v4 内の city snapshot schema v3 フィールド（公共サービス、建設済み用途別統計、交通・公共交通統計、サービス指標）と、ゲームがインストール済みかつ利用可能と判定した DLC 識別子を、オプションの companion 経由でローカル Ollama へ送信できます。ローカル AI 応答本文の上限はデフォルトで無制限で、設定から変更できます。クラウドモデルには接続せず、シミュレーションも操作しません。",
                    ["settings.website"] = "ウェブサイト：https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "オプションの Modpack モジュール（検出状態のみ）：",
                    ["settings.summaryButton"] = "都市スナップショットをログに記録",
                    ["localAi.title"] = "ローカル AI の提案（Ollama Local のみ）",
                    ["localAi.consentLabel"] = "データと接続に関する通知",
                    ["localAi.consentNotice"] = "AI Gaming はこのコンピューター上の Ollama（http://localhost:11434）のみに接続します。モデル一覧にローカルデータを持つ項目が必要で、クラウドタグ付きの名前は拒否しますが、Ollama の設定を確認することも、Ollama が他の場所へ転送しないことを保証することもできません。より厳格にローカルのみで使うには、Ollama のクラウド機能を無効にしてください。確認後、チェックした schema v3 の都市データ、DLC 識別子（自動で含まれます）、および質問のみを送信します。公共サービスはゲーム値、用途別統計は建設済みの建物のみです。交通と乗客統計の尺度／期間は未確認です。警察と医療は負荷／容量であり、地理的なカバー率ではありません。消防はネットワーク区間の coverage byte 平均です。AI Gaming は別のプロバイダーへ切り替えません。Loopback セッションは認証されますが暗号化されません。ゲームまたは companion を検査・注入できるソフトウェアは、データを見たり変更したりする可能性があります。",
                    ["localAi.allowLaunch"] = "各リクエストを確認した後に AI Gaming が companion を起動することを許可（既定ではオフ；ゲーム再起動後にリセット）",
                    ["localAi.model"] = "Ollama モデル名（ローカルにインストール済みのモデルを入力）",
                    ["localAi.responseLimit"] = "Ollama 応答本文の上限（MiB、0 = 無制限、1-2047）",
                    ["localAi.question"] = "ローカルモデルへの質問",
                    ["localAi.selectAll"] = "都市データをすべて選択",
                    ["localAi.include.gameDate"] = "ゲーム内の日付を含める",
                    ["localAi.include.population"] = "HUD の人口を含める",
                    ["localAi.include.fundsCents"] = "資金（セント）を含める",
                    ["localAi.include.residentialDemand"] = "住宅需要を含める",
                    ["localAi.include.commercialDemand"] = "商業需要を含める",
                    ["localAi.include.workplaceDemand"] = "職場需要を含める",
                    ["localAi.include.utilities"] = "電力・水道・下水・ごみ処理データを含める",
                    ["localAi.include.zoning"] = "用途別の建設済み建物数と面積を含める（未使用の区画は含まない）",
                    ["localAi.include.trafficTransit"] = "交通流量と公共交通の統計を含める",
                    ["localAi.include.serviceStatus"] = "消防の区間カバー率と警察・医療の負荷を含める",
                    ["localAi.previewButton"] = "プレビューを準備（simulation thread で取得）",
                    ["localAi.preview.none"] = "リクエストのプレビューはまだありません。",
                    ["localAi.previewLabel"] = "送信内容のプレビュー（スクロールして全文を確認）",
                    ["localAi.preview.stale"] = "設定が変更されました。送信前に新しいプレビューを準備してください。",
                    ["localAi.preview.destination"] = "送信先：このコンピューター上の Ollama（http://localhost:11434）。AI Gaming が直接クラウドへ切り替えることはありません。Ollama 自身のクラウド設定がデータの転送先に影響する場合があります。より厳格にローカルのみで使うには、Ollama のクラウド機能を無効にしてください。",
                    ["localAi.preview.model"] = "モデル：{0}",
                    ["localAi.preview.schema"] = "送信する request schema version 4 の項目（city snapshot schema version 3。ゲーム API がインストール済みかつ利用可能と判定した DLC 識別子は常に含まれます）：",
                    ["localAi.preview.responseLimit.unlimited"] = "Ollama /api/chat HTTP 応答本文の上限：無制限（アプリケーションによるサイズ制限なし、JSON を含む）。",
                    ["localAi.preview.responseLimit.configured"] = "Ollama /api/chat HTTP 応答本文が {0} MiB を超えると、応答全体を破棄します（JSON を含む）。",
                    ["localAi.preview.utilities"] = "公共サービス（ゲームの生値。物理単位は未確認）：電力の容量／消費 {0}/{1}、水道 {2}/{3}、下水の容量／蓄積 {4}/{5}、ごみの量／容量／蓄積 {6}/{7}/{8}、焼却容量 {9}。",
                    ["localAi.preview.zoning"] = "用途別の建設済み建物（数／ゲーム内面積単位。未使用区画は除外）：住宅 {0}/{1}、商業 {2}/{3}、工業 {4}/{5}、オフィス {6}/{7}。",
                    ["localAi.preview.trafficTransit"] = "交通／公共交通：ゲーム報告の交通流量統計 {0}（尺度／期間は未確認。渋滞率ではありません）；公共交通の平均乗客数 {1}（期間未確認）。",
                    ["localAi.preview.serviceStatus"] = "サービス指標：警察の犯罪数／容量／超過 {0}/{1}/{2}；消防の建設済みネットワーク区間の coverage byte 平均 {3}/255（区間数 {4}）；医療の病人／容量／生産性 {5}/{6}/{7}。警察と医療は市全体の負荷／容量で、地理的なカバー率ではありません。消防区間が 0 の場合は coverage を取得できません。",
                    ["localAi.preview.unavailable"] = "取得できません",
                    ["localAi.preview.question"] = "送信するプレイヤーの質問：",
                    ["localAi.preview.confirmInstruction"] = "回答は助言のみで、ゲームは変更されません。確認を押すと companion が起動し、このリクエストを一度だけ送信します。",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.field.installedAvailableDlcs"] = "installedAvailableDlcs（ゲーム API 識別子）",
                    ["localAi.dlc.noneDetected"] = "（ゲーム API から報告なし）",
                    ["localAi.status.ready"] = "AI リクエストは実行中ではありません。",
                    ["localAi.status.inputRequired"] = "リクエストを準備する前に、モデル名と質問を入力してください。",
                    ["localAi.status.capturing"] = "simulation thread で選択した都市データを取得中です。設定画面は応答を続けます。",
                    ["localAi.status.review"] = "上記の項目、値、モデル、質問を確認してください。まだ送信されていません。",
                    ["localAi.status.reviewFirst"] = "送信を確認する前に、リクエストを準備して内容を確認してください。",
                    ["localAi.status.reviewAgain"] = "リクエストが変更されました。もう一度準備して内容を確認してください。",
                    ["localAi.status.launchDisabled"] = "companion の自動起動は無効です。有効にして新しいプレビューを準備し、確認してください。",
                    ["localAi.status.starting"] = "確認に従って companion を起動しています。ローカル Ollama のみに接続します。",
                    ["localAi.status.success"] = "モデル {0} から提案を受け取りました。ゲームは変更されていません。",
                    ["localAi.status.cancelled"] = "リクエストをキャンセルしました。ローカルモデルへ送信済みのデータは取り消せません。",
                    ["localAi.status.discarded"] = "準備したリクエストを破棄しました。データは送信されていません。",
                    ["localAi.confirmButton"] = "確認してこのリクエストを一度送信",
                    ["localAi.cancelButton"] = "リクエストをキャンセルまたは破棄",
                    ["localAi.response.none"] = "AI の回答はありません。",
                    ["localAi.responseLabel"] = "ローカルモデルの回答",
                    ["localAi.response.pending"] = "ローカルモデルの応答を待っています。このリクエストはキャンセルできます。",
                    ["localAi.error.snapshot_unavailable"] = "選択した都市スナップショットを取得できません。リクエストは送信されていません。",
                    ["localAi.error.dlc_detection_unavailable"] = "ゲームがインストール済みかつ利用可能な DLC を確認できません。プレビューは準備されず、リクエストも送信されません。プラットフォームの DLC 状態が準備できてから再試行してください。",
                    ["localAi.error.simulation_unavailable"] = "シミュレーションの準備ができていません。リクエストは送信されていません。",
                    ["localAi.error.companion_build_not_pinned"] = "このビルドには固定された companion ハッシュがありません。起動は無効です。",
                    ["localAi.error.companion_edition_invalid"] = "companion のエディションが無効です。接続は行われませんでした。",
                    ["localAi.error.companion_missing"] = "一致する companion ファイルがありません。リクエストは送信されていません。",
                    ["localAi.error.companion_hash_mismatch"] = "companion のハッシュがこの Mod のビルドと一致しません。リクエストは送信されていません。",
                    ["localAi.error.companion_cache_unavailable"] = "検証済み companion をローカルキャッシュに配置できません。リクエストは送信されていません。",
                    ["localAi.error.companion_start_failed"] = "companion を起動できませんでした。リクエストは送信されていません。",
                    ["localAi.error.companion_handshake_timeout"] = "companion がプライベートな起動ハンドシェイクを完了しませんでした。リクエストは送信されていません。",
                    ["localAi.error.companion_identity_mismatch"] = "companion の ID またはバージョンが一致しません。リクエストは送信されていません。",
                    ["localAi.error.companion_exited"] = "リクエスト前に companion が終了しました。リクエストは送信されていません。",
                    ["localAi.error.companion_unavailable"] = "ローカル companion への接続に失敗しました。再試行や切り替えは行われませんでした。",
                    ["localAi.error.companion_request_failed"] = "companion のリクエストに失敗しました。再試行や切り替えは行われませんでした。",
                    ["localAi.error.companion_rejected_request"] = "companion がリクエストを拒否しました。再試行や切り替えは行われませんでした。",
                    ["localAi.error.companion_response_invalid"] = "companion の応答を検証できなかったため破棄しました。",
                    ["localAi.error.timeout"] = "ローカルリクエストがタイムアウトしました。再試行や切り替えは行われませんでした。",
                    ["localAi.error.cancelled"] = "ローカルリクエストをキャンセルしました。",
                    ["localAi.error.model_not_found"] = "そのモデルは Ollama にインストールされていません。インストール済みのモデルを選択してください。",
                    ["localAi.error.local_model_required"] = "インストール済みのローカルモデルのみ使用できます。クラウドモデルのタグは許可されません。",
                    ["localAi.error.local_authentication_failed"] = "Ollama Local がリクエストを拒否しました。認証情報はログに記録されていません。",
                    ["localAi.error.ollama_error"] = "Ollama Local がエラーを返しました。詳細はゲームログに書き込まれていません。",
                    ["localAi.error.ollama_unavailable"] = "Ollama Local が http://localhost:11434 で応答していません。",
                    ["localAi.error.invalid_response"] = "Ollama Local が無効な応答を返したため破棄しました。",
                    ["localAi.error.response_limit_invalid"] = "0～2047 MiB の整数を入力してください。0 はアプリケーションによるサイズ制限なしです。",
                    ["localAi.error.response_limit_exceeded"] = "Ollama /api/chat の HTTP 応答本文全体が設定した上限を超えたため破棄しました。",
                    ["localAi.error.generic"] = "ローカルリクエストに失敗しました。再試行や切り替えは行われませんでした。",
                    ["interop.unknown"] = "不明",
                    ["interop.aiImprove.notInstalled"] = "AI Improve：未インストール。",
                    ["interop.aiImprove.notEnabled"] = "AI Improve：インストール済みですが無効です。",
                    ["interop.aiImprove.noInterface"] = "AI Improve：インストール済みですが互換性のある相互運用インターフェイスがありません。バージョン不明。",
                    ["interop.aiImprove.manifestInvalid"] = "AI Improve：相互運用マニフェストが無効です。バージョン不明。",
                    ["interop.aiImprove.discoveryFailed"] = "AI Improve：検出に失敗しました。連携を利用できません。",
                    ["interop.aiImprove.ambiguous"] = "AI Improve：複数のプロバイダーが検出されたため、連携を無効にしました。",
                    ["interop.aiImprove.interfaceIncomplete"] = "AI Improve：マニフェストの互換性あり（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）ですが、機能のエントリーポイントがありません。呼び出していません。",
                    ["interop.aiImprove.incompatible"] = "AI Improve：互換性なし（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）。呼び出していません。",
                    ["interop.aiImprove.compatible"] = "AI Improve：互換性あり（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）。呼び出していません。",
                    ["interop.transport.notInstalled"] = "Transport：未インストール。",
                    ["interop.transport.notEnabled"] = "Transport：インストール済みですが無効です。",
                    ["interop.transport.noInterface"] = "Transport：インストール済みですが互換性のある相互運用インターフェイスがありません。バージョン不明。",
                    ["interop.transport.manifestInvalid"] = "Transport：相互運用マニフェストが無効です。バージョン不明。",
                    ["interop.transport.discoveryFailed"] = "Transport：検出に失敗しました。連携を利用できません。",
                    ["interop.transport.ambiguous"] = "Transport：複数のプロバイダーが検出されたため、連携を無効にしました。",
                    ["interop.transport.interfaceIncomplete"] = "Transport：マニフェストの互換性あり（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）ですが、機能のエントリーポイントがありません。呼び出していません。",
                    ["interop.transport.incompatible"] = "Transport：互換性なし（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）。呼び出していません。",
                    ["interop.transport.compatible"] = "Transport：互換性あり（プロバイダー {0}、契約 {1}、エディション {2}、機能 {3}）。呼び出していません。",
                },
                ["ko"] = new Dictionary<string, string>
                {
                    ["settings.language"] = "언어",
                    ["settings.title"] = "AI Gaming - 얼리 프리뷰",
                    ["settings.versionShort"] = "버전 0.4.0 - 얼리 프리뷰",
                    ["settings.status"] = "상태: 확인 후 로컬 AI 제안을 받을 수 있습니다. 이 버전은 게임을 제어하지 않습니다.",
                    ["settings.description"] = "요청마다 확인한 뒤 request schema v4 안의 city snapshot schema v3 필드(공공시설, 건축된 용도별 통계, 교통·대중교통 통계, 서비스 지표)와 게임이 설치 및 사용 가능하다고 보고한 DLC 식별자를 선택적 companion을 통해 로컬 Ollama로 보낼 수 있습니다. 로컬 AI 응답 본문 제한은 기본적으로 무제한이며 설정에서 지정할 수 있습니다. 클라우드 모델에 연결하지 않으며 시뮬레이션을 제어하지 않습니다.",
                    ["settings.website"] = "웹사이트: https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/",
                    ["settings.interop.title"] = "선택적 Modpack 모듈(검색된 상태만 표시):",
                    ["settings.summaryButton"] = "도시 스냅샷을 로그에 기록",
                    ["localAi.title"] = "로컬 AI 제안(Ollama Local 전용)",
                    ["localAi.consentLabel"] = "데이터 및 연결 안내",
                    ["localAi.consentNotice"] = "AI Gaming은 이 컴퓨터의 Ollama(http://localhost:11434)에만 연결합니다. 모델 목록에 로컬 데이터가 있는 항목이 필요하며 클라우드 태그가 붙은 이름은 거부하지만, Ollama 설정을 검사하거나 Ollama가 다른 곳으로 전달하지 않는다고 보장할 수는 없습니다. 더 엄격하게 로컬에서만 사용하려면 Ollama 클라우드 기능을 끄세요. 확인 후 체크한 schema v3 도시 데이터, 자동 포함되는 DLC 식별자, 질문만 전송됩니다. 공공시설은 게임 원시값이고 용도별 통계는 건축된 건물만 포함합니다. 교통·승객 통계의 척도／기간은 확인되지 않았습니다. 경찰·의료는 부하／용량으로 지리적 커버리지가 아니며, 소방은 구간 coverage byte 평균입니다. AI Gaming은 다른 제공자로 전환하지 않습니다. Loopback 세션은 인증되지만 암호화되지는 않습니다. 게임 또는 companion을 검사하거나 주입할 수 있는 소프트웨어는 데이터를 보거나 변경할 수 있습니다.",
                    ["localAi.allowLaunch"] = "요청을 확인한 후 AI Gaming이 companion을 시작하도록 허용(기본값: 꺼짐; 게임을 다시 시작하면 초기화)",
                    ["localAi.model"] = "Ollama 모델 이름(로컬에 설치된 모델 입력)",
                    ["localAi.responseLimit"] = "Ollama 응답 본문 제한(MiB; 0 = 제한 없음; 1-2047)",
                    ["localAi.question"] = "로컬 모델에 질문하기",
                    ["localAi.selectAll"] = "도시 데이터 모두 선택",
                    ["localAi.include.gameDate"] = "게임 날짜 포함",
                    ["localAi.include.population"] = "HUD 인구 포함",
                    ["localAi.include.fundsCents"] = "자금(센트) 포함",
                    ["localAi.include.residentialDemand"] = "주거 수요 포함",
                    ["localAi.include.commercialDemand"] = "상업 수요 포함",
                    ["localAi.include.workplaceDemand"] = "직장 수요 포함",
                    ["localAi.include.utilities"] = "전력·물·하수·쓰레기 처리 데이터 포함",
                    ["localAi.include.zoning"] = "용도별 건축된 건물 수와 면적 포함(빈 구획 제외)",
                    ["localAi.include.trafficTransit"] = "교통 흐름 및 대중교통 통계 포함",
                    ["localAi.include.serviceStatus"] = "소방 구간 커버리지와 경찰·의료 부하 포함",
                    ["localAi.previewButton"] = "미리 보기 준비(simulation thread에서 캡처)",
                    ["localAi.preview.none"] = "아직 요청 미리 보기가 준비되지 않았습니다.",
                    ["localAi.previewLabel"] = "전체 요청 미리 보기(스크롤하여 모든 내용 확인)",
                    ["localAi.preview.stale"] = "설정이 변경되었습니다. 보내기 전에 새 미리 보기를 준비하세요.",
                    ["localAi.preview.destination"] = "대상: 이 컴퓨터의 Ollama(http://localhost:11434). AI Gaming은 클라우드 제공자로 직접 전환하지 않습니다. Ollama 자체의 클라우드 설정이 데이터 경로에 영향을 줄 수 있습니다. 더 엄격하게 로컬에서만 사용하려면 Ollama 클라우드 기능을 끄세요.",
                    ["localAi.preview.model"] = "모델: {0}",
                    ["localAi.preview.schema"] = "보낼 request schema version 4 필드(city snapshot schema version 3; 게임 API가 설치 및 사용 가능하다고 보고한 DLC 식별자는 항상 포함됨):",
                    ["localAi.preview.responseLimit.unlimited"] = "Ollama /api/chat HTTP 응답 본문 제한: 제한 없음(애플리케이션 크기 제한 없음, JSON 포함).",
                    ["localAi.preview.responseLimit.configured"] = "Ollama /api/chat HTTP 응답 본문이 {0} MiB를 넘으면 전체 응답을 버립니다(JSON 포함).",
                    ["localAi.preview.utilities"] = "공공시설(게임 원시값; 물리 단위 미확인): 전력 용량/소비 {0}/{1}; 물 {2}/{3}; 하수 용량/누적 {4}/{5}; 쓰레기 양/용량/누적 {6}/{7}/{8}; 소각 용량 {9}.",
                    ["localAi.preview.zoning"] = "용도별 건축된 건물(수/게임 면적 단위; 빈 구획 제외): 주거 {0}/{1}; 상업 {2}/{3}; 산업 {4}/{5}; 사무 {6}/{7}.",
                    ["localAi.preview.trafficTransit"] = "교통/대중교통: 게임이 보고한 교통 흐름 통계 {0}(척도/기간 미확인; 혼잡률 아님); 대중교통 평균 승객 {1}(기간 미확인).",
                    ["localAi.preview.serviceStatus"] = "서비스 지표: 경찰 범죄 수/용량/초과 {0}/{1}/{2}; 소방의 건축된 네트워크 구간 coverage byte 평균 {3}/255(구간 수 {4}); 의료 환자 수/용량/생산성 {5}/{6}/{7}. 경찰·의료는 도시 부하/용량이며 지리적 커버리지가 아닙니다. 소방 구간 수가 0이면 coverage를 사용할 수 없습니다.",
                    ["localAi.preview.unavailable"] = "사용할 수 없음",
                    ["localAi.preview.question"] = "보낼 플레이어 질문:",
                    ["localAi.preview.confirmInstruction"] = "답변은 조언일 뿐이며 게임을 변경하지 않습니다. 확인을 누르면 companion이 시작되고 이 요청을 한 번 보냅니다.",
                    ["localAi.field.gameDate"] = "gameDate",
                    ["localAi.field.population"] = "population",
                    ["localAi.field.fundsCents"] = "fundsCents",
                    ["localAi.field.residentialDemand"] = "residentialDemand",
                    ["localAi.field.commercialDemand"] = "commercialDemand",
                    ["localAi.field.workplaceDemand"] = "workplaceDemand",
                    ["localAi.field.installedAvailableDlcs"] = "installedAvailableDlcs(게임 API 식별자)",
                    ["localAi.dlc.noneDetected"] = "(게임 API가 보고한 항목 없음)",
                    ["localAi.status.ready"] = "현재 진행 중인 AI 요청이 없습니다.",
                    ["localAi.status.inputRequired"] = "요청을 준비하기 전에 모델 이름과 질문을 입력하세요.",
                    ["localAi.status.capturing"] = "simulation thread에서 선택한 도시 데이터를 캡처하고 있습니다. 설정 화면은 계속 반응합니다.",
                    ["localAi.status.review"] = "위의 필드, 값, 모델, 질문을 확인하세요. 아직 전송되지 않았습니다.",
                    ["localAi.status.reviewFirst"] = "확인하여 보내기 전에 요청을 준비하고 검토하세요.",
                    ["localAi.status.reviewAgain"] = "요청이 변경되었습니다. 다시 준비하고 검토하세요.",
                    ["localAi.status.launchDisabled"] = "companion 자동 시작이 꺼져 있습니다. 켠 다음 새 미리 보기를 준비하고 확인하세요.",
                    ["localAi.status.starting"] = "확인에 따라 companion을 시작하는 중입니다. 로컬 Ollama에만 연결합니다.",
                    ["localAi.status.success"] = "모델 {0}의 제안을 받았습니다. 게임은 변경되지 않았습니다.",
                    ["localAi.status.cancelled"] = "요청이 취소되었습니다. 로컬 모델로 이미 보낸 데이터는 취소할 수 없습니다.",
                    ["localAi.status.discarded"] = "준비된 요청을 버렸습니다. 데이터는 전송되지 않았습니다.",
                    ["localAi.confirmButton"] = "확인하고 이 요청 한 번 보내기",
                    ["localAi.cancelButton"] = "요청 취소 또는 버리기",
                    ["localAi.response.none"] = "AI 응답이 없습니다.",
                    ["localAi.responseLabel"] = "로컬 모델 응답",
                    ["localAi.response.pending"] = "로컬 모델을 기다리는 중입니다. 이 요청을 취소할 수 있습니다.",
                    ["localAi.error.snapshot_unavailable"] = "선택한 도시 스냅샷을 사용할 수 없습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.dlc_detection_unavailable"] = "게임에서 설치 및 사용 가능한 DLC를 확인할 수 없습니다. 미리 보기를 준비하지 않았고 요청도 보내지 않았습니다. 플랫폼 DLC 상태가 준비된 후 다시 시도하세요.",
                    ["localAi.error.simulation_unavailable"] = "시뮬레이션이 준비되지 않았습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_build_not_pinned"] = "이 빌드에 고정된 companion 해시가 없습니다. companion 실행이 꺼져 있습니다.",
                    ["localAi.error.companion_edition_invalid"] = "companion 에디션이 올바르지 않습니다. 연결하지 않았습니다.",
                    ["localAi.error.companion_missing"] = "일치하는 companion 파일이 없습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_hash_mismatch"] = "companion 해시가 이 모드 빌드와 일치하지 않습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_cache_unavailable"] = "검증된 companion을 로컬 캐시에 준비하지 못했습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_start_failed"] = "companion을 시작하지 못했습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_handshake_timeout"] = "companion이 비공개 시작 핸드셰이크를 완료하지 못했습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_identity_mismatch"] = "companion ID 또는 버전이 일치하지 않습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_exited"] = "요청 전에 companion이 종료되었습니다. 요청을 보내지 않았습니다.",
                    ["localAi.error.companion_unavailable"] = "로컬 companion 연결에 실패했습니다. 재시도하거나 다른 곳으로 전환하지 않았습니다.",
                    ["localAi.error.companion_request_failed"] = "companion 요청에 실패했습니다. 재시도하거나 다른 곳으로 전환하지 않았습니다.",
                    ["localAi.error.companion_rejected_request"] = "companion이 요청을 거부했습니다. 재시도하거나 다른 곳으로 전환하지 않았습니다.",
                    ["localAi.error.companion_response_invalid"] = "companion 응답 확인에 실패하여 버렸습니다.",
                    ["localAi.error.timeout"] = "로컬 요청 시간이 초과되었습니다. 재시도하거나 다른 곳으로 전환하지 않았습니다.",
                    ["localAi.error.cancelled"] = "로컬 요청이 취소되었습니다.",
                    ["localAi.error.model_not_found"] = "해당 모델이 Ollama에 설치되어 있지 않습니다. 설치된 모델을 선택하세요.",
                    ["localAi.error.local_model_required"] = "로컬에 설치된 모델만 허용됩니다. 클라우드 모델 태그는 사용할 수 없습니다.",
                    ["localAi.error.local_authentication_failed"] = "Ollama Local이 요청을 거부했습니다. 자격 증명은 로그에 기록되지 않았습니다.",
                    ["localAi.error.ollama_error"] = "Ollama Local에서 오류가 발생했습니다. 세부 정보는 게임 로그에 기록되지 않았습니다.",
                    ["localAi.error.ollama_unavailable"] = "Ollama Local이 http://localhost:11434에서 응답하지 않습니다.",
                    ["localAi.error.invalid_response"] = "Ollama Local에서 잘못된 응답을 반환하여 버렸습니다.",
                    ["localAi.error.response_limit_invalid"] = "0~2047 MiB의 정수를 입력하세요. 0은 애플리케이션 크기 제한 없음입니다.",
                    ["localAi.error.response_limit_exceeded"] = "Ollama /api/chat HTTP 응답 본문 전체가 설정한 제한을 초과하여 버렸습니다.",
                    ["localAi.error.generic"] = "로컬 요청에 실패했습니다. 재시도하거나 다른 곳으로 전환하지 않았습니다.",
                    ["interop.unknown"] = "알 수 없음",
                    ["interop.aiImprove.notInstalled"] = "AI Improve: 설치되지 않음.",
                    ["interop.aiImprove.notEnabled"] = "AI Improve: 설치되었지만 활성화되지 않음.",
                    ["interop.aiImprove.noInterface"] = "AI Improve: 설치되었지만 호환되는 상호 운용 인터페이스가 없음; 버전 알 수 없음.",
                    ["interop.aiImprove.manifestInvalid"] = "AI Improve: 상호 운용 매니페스트가 올바르지 않음; 버전 알 수 없음.",
                    ["interop.aiImprove.discoveryFailed"] = "AI Improve: 검색 실패; 통합을 사용할 수 없음.",
                    ["interop.aiImprove.ambiguous"] = "AI Improve: 여러 제공자가 감지되어 통합이 꺼짐.",
                    ["interop.aiImprove.interfaceIncomplete"] = "AI Improve: 매니페스트 호환 가능(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3})이지만 기능 진입점이 없어 호출하지 않음.",
                    ["interop.aiImprove.incompatible"] = "AI Improve: 호환되지 않음(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3}); 호출하지 않음.",
                    ["interop.aiImprove.compatible"] = "AI Improve: 호환됨(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3}); 호출하지 않음.",
                    ["interop.transport.notInstalled"] = "Transport: 설치되지 않음.",
                    ["interop.transport.notEnabled"] = "Transport: 설치되었지만 활성화되지 않음.",
                    ["interop.transport.noInterface"] = "Transport: 설치되었지만 호환되는 상호 운용 인터페이스가 없음; 버전 알 수 없음.",
                    ["interop.transport.manifestInvalid"] = "Transport: 상호 운용 매니페스트가 올바르지 않음; 버전 알 수 없음.",
                    ["interop.transport.discoveryFailed"] = "Transport: 검색 실패; 통합을 사용할 수 없음.",
                    ["interop.transport.ambiguous"] = "Transport: 여러 제공자가 감지되어 통합이 꺼짐.",
                    ["interop.transport.interfaceIncomplete"] = "Transport: 매니페스트 호환 가능(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3})이지만 기능 진입점이 없어 호출하지 않음.",
                    ["interop.transport.incompatible"] = "Transport: 호환되지 않음(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3}); 호출하지 않음.",
                    ["interop.transport.compatible"] = "Transport: 호환됨(제공자 {0}, 계약 {1}, 에디션 {2}, 기능 {3}); 호출하지 않음.",
                },
            };

        internal static string Get(string key)
        {
            return Get(key, new object[0]);
        }

        internal static string Get(string key, params object[] arguments)
        {
            return GetForLanguage(key, CurrentLanguage, arguments);
        }

        internal static string GetForLanguage(string key, string language)
        {
            return GetForLanguage(key, language, new object[0]);
        }

        private static string GetForLanguage(string key, string language, object[] arguments)
        {
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

        internal static string CurrentLanguage
        {
            get
            {
                EnsureLanguageLoaded();
                return _selectedLanguage;
            }
        }

        internal static int SelectedLanguageIndex
        {
            get
            {
                string language = CurrentLanguage;
                for (int i = 0; i < LanguageCodes.Length; i++)
                {
                    if (LanguageCodes[i] == language) return i;
                }
                return 0;
            }
        }

        internal static string[] GetLanguageNames()
        {
            return (string[])LanguageNames.Clone();
        }

        internal static bool SetLanguage(int index)
        {
            if (index < 0 || index >= LanguageCodes.Length) return false;

            string language = LanguageCodes[index];
            if (language == CurrentLanguage) return false;

            _selectedLanguage = language;
            _languageLoaded = true;
            try
            {
                PlayerPrefs.SetString(LanguagePreferenceKey, language);
                PlayerPrefs.Save();
            }
            catch
            {
                Debug.LogWarning("[AI Gaming] Could not persist the selected UI language.");
            }
            return true;
        }

        internal static Dictionary<string, string> GetTextReplacements(string previousLanguage, string currentLanguage)
        {
            var replacements = new Dictionary<string, string>();
            var ambiguousTexts = new HashSet<string>();

            foreach (string key in Tables[DefaultLanguage].Keys)
            {
                string previousText = GetForLanguage(key, previousLanguage);
                string currentText = GetForLanguage(key, currentLanguage);
                if (previousText == currentText || ambiguousTexts.Contains(previousText)) continue;

                string existingText;
                if (replacements.TryGetValue(previousText, out existingText) && existingText != currentText)
                {
                    replacements.Remove(previousText);
                    ambiguousTexts.Add(previousText);
                }
                else
                {
                    replacements[previousText] = currentText;
                }
            }

            string previousTitle = GetForLanguage("settings.title", previousLanguage) + " - " +
                GetForLanguage("settings.versionShort", previousLanguage);
            string currentTitle = GetForLanguage("settings.title", currentLanguage) + " - " +
                GetForLanguage("settings.versionShort", currentLanguage);
            if (previousTitle != currentTitle && !ambiguousTexts.Contains(previousTitle))
            {
                replacements[previousTitle] = currentTitle;
            }

            return replacements;
        }

        private static void EnsureLanguageLoaded()
        {
            if (_languageLoaded) return;

            _languageLoaded = true;
            _selectedLanguage = DefaultLanguage;
            try
            {
                string savedLanguage = PlayerPrefs.GetString(LanguagePreferenceKey, DefaultLanguage);
                if (Tables.ContainsKey(savedLanguage)) _selectedLanguage = savedLanguage;
            }
            catch
            {
                _selectedLanguage = DefaultLanguage;
            }
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
