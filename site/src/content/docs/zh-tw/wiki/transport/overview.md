---
title: Automated Public Transport Planning
description: "Survey diagnostics, Development route proposals and the limits of confirmed route-creation attempts."
---

> **Development build update, 31 October 2026:** The GitHub Development prototype can attempt to create a selected route after arming and confirmation; it can change a save and is not read-only. Successful route creation and save/reload have not been established in subscribed-game acceptance. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

Automated Public Transport Planning's Development prototype estimates city-bus routes and can attempt to create a selected proposal after arming and confirmation. The prototype may change saves and does not guarantee runnable routes. Other transport types remain a future direction.

## Survey diagnostics

- **City survey.** The mod reads roads, stops and the transit network in the background. This survey is read-only; the separate Development creation prototype can change a save.
- **模組選項中的診斷工具。**每個按鈕都會把結果寫入遊戲記錄：

| 按鈕 | 回報內容 |
| --- | --- |
| 將測試路線寫入記錄檔 | 確認模組已載入並正常回應。 |
| 列出公車 prefab 詳細資料 | 你的遊戲中可用的公車車型。 |
| 量測規劃成本（會短暫停頓） | 在你的城市上進行規劃需要多少時間。 |
| 擷取世界快照 | 規劃器會用到的道路與站點。 |
| 建立路線圖 | 各站點之間可能的連接網絡。 |

- **語言選擇。**11 種語言，可在模組選項中切換，不需重新啟動。

## 目前還不能做什麼

The older 0.2.0 Release preview does not create, change or suggest bus lines. The Development prototype focuses on city buses; additional transport types remain future directions. See the [roadmap](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/roadmap/).

## Development 版

Development 版是公開測試版，使用前請先備份存檔。

- **規劃預覽（測試中）。**按「Generate a read-only planning preview」，會依城市需求估算市內公車路線，並列出評分。不會建立或修改任何路線。這些估算尚未經遊戲本身的尋路驗證。
- **Route-creation prototype.** A selected proposal can be submitted for creation after the feature is armed and each attempt is confirmed. Proposals do not guarantee connected or runnable routes. Successful creation and save/reload remain unverified in subscribed-game acceptance; use a backup and a separate test city.

## 協助測試

在不同規模的城市上執行診斷工具，並在[回報結果或問題](https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack/issues/new/choose)時附上記錄內容。這能幫助規劃器在推出前調整得更好。

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
