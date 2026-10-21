---
title: AI Improve 相容性
description: "AI Improve 與其他熱門 Cities: Skylines 1 模組一起使用時的行為。"
---

AI Improve 盡量不和其他模組互相干擾。當另一個模組已經把某項工作做好時，AI Improve 會偵測到並主動讓位。

| 模組 | AI Improve 的處理方式 |
| --- | --- |
| **Harmony** | 必須使用。AI Improve 透過它修改遊戲行為。 |
| **UnifiedUI** | 選用。提供[遊戲內面板](/Cities_Skylines_1_Modpack/zh-tw/wiki/ai-improve/in-game/)。 |
| **Improved Public Transport**、**Express Bus Services** | 安裝其中任何一個時，「縮短空站停留」與「拉開同線車輛間距」會停用，因為這些模組已經負責決定車輛何時發車。 |
| **Transfer Manager CE** | 它的消防派遣啟用時，AI Improve 讓它決定車輛要去哪一場火，只保留每棟建築的出勤上限。 |
| **Traffic Manager: President Edition（TM:PE）** | 會偵測。TM:PE 會取代遊戲的尋路。AI Improve 針對 TM:PE 尋路的緊急車輛成本調整目前無法載入，啟用 TM:PE 時不會生效，修正正在測試中。選用的緊急車輛中途改道不受影響。 |
| **Real Time** | 會偵測。兩者會碰到同一個消防派遣步驟，但 Real Time 在那裡只做自己的記錄，不會互相干擾。 |
| **SingleTrainTrackAI**、**Reversible Tram AI** | 會偵測。AI Improve 不會去做它們負責的工作。 |

AI Improve 不會改變任何載具的載客量。

## Release 與 Development 版同時使用

不要同時啟用 AI Improve 的兩個版本。它們是同一個模組，會互相衝突。

## 回報相容性問題

如果和其他模組一起使用時出問題，請開一個 [Issue](https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack/issues/new/choose)，附上遊戲版本、完整模組清單、相關的 AI Improve 功能、重現步驟，以及輸出記錄的相關段落（見[常見問題](/Cities_Skylines_1_Modpack/zh-tw/wiki/ai-improve/faq/)）。
