#if DEVTOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using ColossalFramework.UI;
using ICities;
using AutomatedPublicTransportPlanning.Planning;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal static class RouteEditingUi
    {
        private static readonly List<KeyValuePair<UIComponent, int>> s_labels =
            new List<KeyValuePair<UIComponent, int>>();
        private static readonly Dictionary<string, string[]> s_text = CreateText();
        private static UITextField s_line, s_order, s_proposal, s_preview;
        private static UIButton s_apply, s_manual, s_automatic;
        private static UICheckBox s_arm;
        private static bool s_armed, s_confirming, s_waitingPreview, s_canUsePlan;
        private static RouteEditPlan s_seenPlan;
        private static int s_uiVersion;

        internal static void Build(UIHelperBase helper)
        {
            Reset();
            UIHelperBase group = helper.AddGroup(Text(0));
            s_line = group.AddTextfield(Text(1), string.Empty, delegate(string value) { InvalidateInput(); }) as UITextField;
            Caption(s_line, 1);
            if (s_line != null && s_line.parent != null && s_line.parent.parent != null)
            {
                Track(s_line.parent.parent.Find<UILabel>("Label"), 0);
            }
            s_order = group.AddTextfield(Text(2), "1,2,3", delegate(string value) { InvalidateInput(); }) as UITextField;
            Caption(s_order, 2);
            s_proposal = group.AddTextfield(Text(3), "1", delegate(string value) { InvalidateInput(); }) as UITextField;
            Caption(s_proposal, 3);
            s_manual = group.AddButton(Text(4), delegate { Log.Guard("prepare manual route edit", delegate { Prepare(false); }); }) as UIButton;
            Track(s_manual, 4);
            s_automatic = group.AddButton(Text(5), delegate { Log.Guard("prepare automatic route edit", delegate { Prepare(true); }); }) as UIButton;
            Track(s_automatic, 5);
            s_preview = group.AddTextfield(Text(6), string.Empty, null) as UITextField;
            Caption(s_preview, 6);
            if (s_preview != null) { s_preview.readOnly = true; s_preview.multiline = true; s_preview.height = 260f; }
            s_arm = group.AddCheckbox(Text(7), false, delegate(bool value)
            {
                Log.Guard("route edit arming", delegate
                {
                    s_armed = value;
                    if (!value) { s_confirming = false; s_uiVersion++; }
                    Refresh();
                });
            }) as UICheckBox;
            Track(s_arm, 7);
            s_apply = group.AddButton(Text(8), delegate { Log.Guard("confirm existing route edit", BeginConfirmation); }) as UIButton;
            Track(s_apply, 8);
            s_seenPlan = RouteEditingController.CurrentPlan;
            Refresh();
        }

        // Called only from the main-thread settings/update bridge.
        internal static void Refresh()
        {
            if (s_preview == null) { return; }
            RouteEditPlan plan = RouteEditingController.CurrentPlan;
            if (!object.ReferenceEquals(plan, s_seenPlan))
            {
                s_seenPlan = plan;
                s_canUsePlan = s_waitingPreview && plan != null;
                // Do not consume the request when the simulation thread clears the old preview.
                if (plan != null) { s_waitingPreview = false; }
                Disarm();
            }
            for (int i = 0; i < s_labels.Count; i++)
            {
                UIComponent component = s_labels[i].Key;
                if (component == null) { continue; }
                UITextComponent text = component as UITextComponent;
                UICheckBox checkbox = component as UICheckBox;
                if (text != null) { text.text = Text(s_labels[i].Value); }
                else if (checkbox != null) { checkbox.text = Text(s_labels[i].Value); }
            }
            string display = RouteEditingController.PreviewText + "\n\n" + RouteEditingController.StatusText;
            if (s_preview.text != display) { s_preview.text = display; }
            bool busy = RouteEditingController.IsBusy || s_confirming;
            if (s_manual != null) { s_manual.isEnabled = !busy; }
            if (s_automatic != null) { s_automatic.isEnabled = !busy; }
            if (s_apply != null) { s_apply.isEnabled = !busy && s_armed && s_canUsePlan && plan != null; }
        }

        internal static void Reset()
        {
            s_uiVersion++;
            s_labels.Clear();
            s_line = s_order = s_proposal = s_preview = null;
            s_apply = s_manual = s_automatic = null;
            s_arm = null;
            s_armed = s_confirming = s_waitingPreview = s_canUsePlan = false;
            s_seenPlan = null;
        }

        private static void Prepare(bool automatic)
        {
            if (RouteEditingController.IsBusy || s_confirming || s_line == null) { return; }
            ushort line;
            int proposal = 0;
            if (!ushort.TryParse(s_line.text, NumberStyles.None, CultureInfo.InvariantCulture, out line) || line == 0 ||
                (automatic && (s_proposal == null || !int.TryParse(s_proposal.text, NumberStyles.None,
                    CultureInfo.InvariantCulture, out proposal) || proposal < 1)))
            {
                ConfirmPanel.ShowModal(Text(9), Text(12), delegate(UIComponent component, int result) { });
                return;
            }
            Disarm();
            s_canUsePlan = false;
            s_seenPlan = RouteEditingController.CurrentPlan;
            s_waitingPreview = true;
            RouteEditingController.RequestPreview(line, s_order == null ? string.Empty : s_order.text,
                automatic ? proposal - 1 : -1, automatic);
            Refresh();
        }

        private static void BeginConfirmation()
        {
            RouteEditPlan plan = RouteEditingController.CurrentPlan;
            if (!s_armed || !s_canUsePlan || s_confirming || RouteEditingController.IsBusy || plan == null ||
                !object.ReferenceEquals(s_seenPlan, plan)) { return; }
            s_confirming = true;
            int version = s_uiVersion;
            string frozenPreview = RouteEditingController.PreviewText;
            ConfirmPanel.ShowModal(Text(9), Text(10) + "\n\n" + frozenPreview,
                delegate(UIComponent first, int firstResult)
                {
                    Log.Guard("first route edit confirmation", delegate
                    {
                        if (firstResult != 1 || !CanConfirm(plan, version)) { Disarm(); return; }
                        ConfirmPanel.ShowModal(Text(9), Text(11) + "\n\n" + frozenPreview,
                            delegate(UIComponent second, int secondResult)
                            {
                                Log.Guard("final route edit confirmation", delegate
                                {
                                    bool apply = secondResult == 1 && CanConfirm(plan, version);
                                    Disarm();
                                    s_canUsePlan = false;
                                    if (apply) { RouteEditingController.RequestApply(plan); }
                                });
                            });
                    });
                });
            Refresh();
        }

        private static bool CanConfirm(RouteEditPlan plan, int version)
        {
            return version == s_uiVersion && s_armed && s_confirming && s_canUsePlan &&
                !RouteEditingController.IsBusy && object.ReferenceEquals(RouteEditingController.CurrentPlan, plan);
        }

        private static void InvalidateInput()
        {
            s_uiVersion++;
            s_canUsePlan = s_waitingPreview = false;
            Disarm();
        }

        private static void Disarm()
        {
            s_armed = s_confirming = false;
            if (s_arm != null) { s_arm.isChecked = false; }
        }

        private static void Caption(UIComponent component, int key)
        {
            if (component != null && component.parent != null) { Track(component.parent.Find<UILabel>("Label"), key); }
        }

        private static void Track(UIComponent component, int key)
        {
            if (component != null) { s_labels.Add(new KeyValuePair<UIComponent, int>(component, key)); }
        }

        private static string Text(int key)
        {
            string[] strings;
            if (!s_text.TryGetValue(Loc.CurrentLanguage, out strings)) { strings = s_text["en"]; }
            return strings[key];
        }

        private static Dictionary<string, string[]> CreateText()
        {
            Dictionary<string, string[]> text = new Dictionary<string, string[]>();
            text["en"] = new string[] {
                "Existing bus route editing (Development)", "Existing line ID", "Manual stop order: 1,3,2 or f123,b456,3",
                "Planning proposal number (1-based)", "Prepare manual preview", "Prepare automatic preview", "Read-only preview and status",
                "Arm editing; two confirmations are required", "Apply preview to existing line", "Confirm existing route edit",
                "Back up your save and use a disposable copy. This edits the existing line while preserving its ID and stop count. Recovery is best-effort, not atomic. Review the exact preview before continuing.",
                "Final confirmation: apply exactly this preview to the existing line? Stop positions and route paths can change; recovery can fail. Cancel if you have not backed up your save.",
                "Enter a positive existing line ID and, for automatic mode, a positive proposal number." };
            text["zh_TW"] = new string[] {
                "既有公車路線編輯（開發版）", "既有路線 ID", "手動站序：1,3,2 或 f123,b456,3", "規劃提案編號（由 1 起）",
                "準備手動預覽", "準備自動預覽", "唯讀預覽與狀態", "啟用編輯；須確認兩次", "將預覽套用至既有路線", "確認既有路線編輯",
                "請先備份存檔並使用副本。此操作修改既有路線，保留路線 ID 與站數。回復僅盡力而為，並非原子操作。請核對完整預覽。",
                "最後確認：將此預覽套用至既有路線？站位與行車路徑可能改變，回復亦可能失敗。尚未備份請取消。",
                "請輸入大於零的既有路線 ID；自動模式亦須輸入大於零的提案編號。" };
            text["zh_CN"] = new string[] {
                "现有公交线路编辑（开发版）", "现有线路 ID", "手动站序：1,3,2 或 f123,b456,3", "规划方案编号（从 1 开始）",
                "准备手动预览", "准备自动预览", "只读预览与状态", "启用编辑；须确认两次", "将预览应用至现有线路", "确认现有线路编辑",
                "请先备份存档并使用副本。此操作修改现有线路，保留线路 ID 与站数。恢复仅尽力而为，并非原子操作。请核对完整预览。",
                "最后确认：将此预览应用至现有线路？站位与行车路径可能改变，恢复也可能失败。尚未备份请取消。",
                "请输入大于零的现有线路 ID；自动模式还须输入大于零的方案编号。" };
            text["ja"] = new string[] {
                "既存バス路線の編集（開発版）", "既存路線 ID", "手動の停留所順序：1,3,2 または f123,b456,3", "計画候補番号（1 から）",
                "手動プレビューを準備", "自動プレビューを準備", "読み取り専用のプレビューと状態", "編集を有効化（確認が 2 回必要）", "既存路線に適用", "既存路線の編集を確認",
                "セーブをバックアップし、コピーを使用してください。路線 ID と停留所数を保持して編集します。復旧は最善努力であり、原子的ではありません。プレビューを確認してください。",
                "最終確認：このプレビューを適用しますか？停留所の位置と経路が変わり、復旧に失敗する場合があります。未バックアップならキャンセルしてください。",
                "正の既存路線 ID を入力してください。自動モードでは正の候補番号も必要です。" };
            text["ko"] = new string[] {
                "기존 버스 노선 편집（개발판）", "기존 노선 ID", "수동 정류장 순서: 1,3,2 또는 f123,b456,3", "계획 제안 번호（1부터）",
                "수동 미리 보기 준비", "자동 미리 보기 준비", "읽기 전용 미리 보기 및 상태", "편집 활성화（두 번 확인 필요）", "기존 노선에 적용", "기존 노선 편집 확인",
                "저장을 백업하고 복사본을 사용하세요. 노선 ID와 정류장 수를 유지하여 편집합니다. 복구는 최선의 노력이며 원자적 작업이 아닙니다. 미리 보기를 확인하세요.",
                "최종 확인: 이 미리 보기를 적용할까요? 정류장 위치와 경로가 바뀌며 복구가 실패할 수 있습니다. 백업하지 않았다면 취소하세요.",
                "양수인 기존 노선 ID를 입력하세요. 자동 모드에서는 양수인 제안 번호도 필요합니다." };
            return text;
        }
    }
}
#endif
