---
title: AI Gaming
description: "0.4.0 Development 原始碼的功能、逐次確認流程，以及套件和執行限制。"
---

**0.4.0 Development 原始碼**加入可選的 Windows 輔助程式，讓玩家透過 Ollama Local 取得建議。它不會操控模擬。其他玩法仍屬規劃中。

## 0.4.0 原始碼目前實作

- 選擇要提供的城市統計資料：遊戲日期、人口、資金、住宅需求、商業需求及工作需求。
- 六項資料預設全選，不想分享的資料需自行取消。獨立的城市摘要功能仍可依玩家要求，把遊戲日期、人口與資金寫入本機遊戲記錄。
- 輸入 Ollama 模型名稱與問題，再檢視完整請求內容。
- 每次請求都要獨立確認。模型回覆只作建議，不會轉成遊戲操作。

## 套件與遊戲執行狀態

Development 上傳工作已完成，但 Steam 最終處理尚未核實。自包含 Windows x64 輔助程式套件、Cities: Skylines 執行環境及實際 Ollama 請求也尚未驗證。原始碼預設 `CompanionSha256=UNCONFIGURED`，會拒絕啟動未核實的輔助程式。完成相符套件並測試前，請視建議功能為尚不可用。

## 本機資料與 Ollama

AI Gaming 只會把所選統計資料、模型名稱與問題傳給這台電腦上的 Ollama。Ollama 可能依自身設定把推論轉送至雲端服務；AI Gaming 無法檢查或保證該設定。現有原始碼沒有雲端供應商介接，也沒有 API key 輸入或儲存功能。自由輸入的問題請勿包含城市名稱、帳戶資料、秘密或個人資料。

## 規劃中的玩法

觀看 AI 建設城市及讓 AI 在有限範圍內操作遊戲都是構想，尚未實作。0.4.0 也沒有呼叫其他 Modpack 模組；使用其他模組能力是長期設計方向。

進度請見[路線圖](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/roadmap/)。

<!-- Updated by OpenAI-Codex on 2026-10-26. -->
