using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using ICities;

namespace AIGaming
{
    internal sealed partial class LocalAiRequestController
    {
        private sealed class View
        {
            private readonly LocalAiRequestController _owner;
            private readonly UIHelperBase _ask;
            private readonly UIHelperBase _data;
            private readonly UIHelperBase _review;
            private readonly UIComponent _root;
            private readonly UIComponent _dataRoot;
            private readonly UIComponent _reviewRoot;
            private bool _relayout;
            private readonly UITextField _model;
            private readonly UITextField _limit;
            private readonly UITextField _question;
            private readonly UICheckBox _allow;
            private readonly UICheckBox[] _checks = new UICheckBox[10];
            private readonly UIButton _selectAll;
            private readonly UIButton _prepare;
            private readonly UIButton _confirm;
            private readonly UIButton _cancel;
            private readonly UILabel _status;
            private readonly UILabel _askStatus;
            private readonly TextPages _notice;
            private readonly TextPages _preview;
            private readonly TextPages _response;
            private LocalAiRequest _previewBoundRequest;
            private bool _disposedView;

            private static readonly string[] CheckKeys =
            {
                "localAi.include.gameDate", "localAi.include.population", "localAi.include.fundsCents",
                "localAi.include.residentialDemand", "localAi.include.commercialDemand", "localAi.include.workplaceDemand",
                "localAi.include.utilities", "localAi.include.zoning", "localAi.include.trafficTransit", "localAi.include.serviceStatus"
            };

            internal View(LocalAiRequestController owner, UIHelperBase ask, UIHelperBase data, UIHelperBase review)
            {
                _owner = owner;
                _ask = ask;
                _data = data;
                _review = review;
                UIHelper helper = ask as UIHelper;
                _root = helper == null ? null : helper.self as UIComponent;
                helper = data as UIHelper;
                _dataRoot = helper == null ? null : helper.self as UIComponent;
                helper = review as UIHelper;
                _reviewRoot = helper == null ? null : helper.self as UIComponent;
                _notice = new TextPages(ask, "localAi.consentLabel");
                _allow = ask.AddCheckbox(Localization.Get("localAi.allowLaunch"), owner._allowLaunch, owner.OnAllowLaunchChanged) as UICheckBox;
                SettingsLayout.FitCheckbox(ask, _allow);
                _model = ask.AddTextfield(Localization.Get("localAi.model"), owner._modelName, owner.OnModelChanged, owner.OnModelSubmitted) as UITextField;
                SettingsLayout.FitTextField(ask, _model);
                _limit = ask.AddTextfield(Localization.Get("localAi.responseLimit"), owner._responseLimitText, owner.OnResponseLimitChanged, owner.OnResponseLimitSubmitted) as UITextField;
                SettingsLayout.FitTextField(ask, _limit);
                _question = ask.AddTextfield(Localization.Get("localAi.question"), owner._question, owner.OnQuestionChanged, owner.OnQuestionSubmitted) as UITextField;
                SettingsLayout.FitTextField(ask, _question);
                _prepare = ask.AddButton(Localization.Get("localAi.previewButton"), owner.PrepareRequest) as UIButton;
                SettingsLayout.FitButton(ask, _prepare);
                _askStatus = CreateLabel(ask);

                _selectAll = data.AddButton(Localization.Get("localAi.selectAll"), owner.SelectAllFields) as UIButton;
                SettingsLayout.FitButton(data, _selectAll);
                _checks[0] = data.AddCheckbox(Localization.Get(CheckKeys[0]), owner._includeGameDate, owner.OnGameDateChanged) as UICheckBox;
                _checks[1] = data.AddCheckbox(Localization.Get(CheckKeys[1]), owner._includePopulation, owner.OnPopulationChanged) as UICheckBox;
                _checks[2] = data.AddCheckbox(Localization.Get(CheckKeys[2]), owner._includeFundsCents, owner.OnFundsChanged) as UICheckBox;
                _checks[3] = data.AddCheckbox(Localization.Get(CheckKeys[3]), owner._includeResidentialDemand, owner.OnResidentialChanged) as UICheckBox;
                _checks[4] = data.AddCheckbox(Localization.Get(CheckKeys[4]), owner._includeCommercialDemand, owner.OnCommercialChanged) as UICheckBox;
                _checks[5] = data.AddCheckbox(Localization.Get(CheckKeys[5]), owner._includeWorkplaceDemand, owner.OnWorkplaceChanged) as UICheckBox;
                _checks[6] = data.AddCheckbox(Localization.Get(CheckKeys[6]), owner._includeUtilities, owner.OnUtilitiesChanged) as UICheckBox;
                _checks[7] = data.AddCheckbox(Localization.Get(CheckKeys[7]), owner._includeZoning, owner.OnZoningChanged) as UICheckBox;
                _checks[8] = data.AddCheckbox(Localization.Get(CheckKeys[8]), owner._includeTrafficTransit, owner.OnTrafficTransitChanged) as UICheckBox;
                _checks[9] = data.AddCheckbox(Localization.Get(CheckKeys[9]), owner._includeServiceStatus, owner.OnServiceStatusChanged) as UICheckBox;
                for (int i = 0; i < _checks.Length; i++) SettingsLayout.FitCheckbox(data, _checks[i]);

                _preview = new TextPages(review, "localAi.previewLabel");
                _status = CreateLabel(review);
                _confirm = review.AddButton(Localization.Get("localAi.confirmButton"), ConfirmFromView) as UIButton;
                _cancel = review.AddButton(Localization.Get("localAi.cancelButton"), owner.CancelRequest) as UIButton;
                _preview.AvailabilityChanged = OnPreviewAvailabilityChanged;
                SettingsLayout.FitButton(review, _confirm);
                SettingsLayout.FitButton(review, _cancel);
                _response = new TextPages(review, "localAi.responseLabel");
                if (_root != null) _root.eventSizeChanged += OnSizeChanged;
                if (_dataRoot != null) _dataRoot.eventSizeChanged += OnSizeChanged;
                if (_reviewRoot != null) _reviewRoot.eventSizeChanged += OnSizeChanged;
                Relayout();
            }

            internal bool IsAlive { get { return !_disposedView && _root != null && _dataRoot != null && _reviewRoot != null; } }

            internal void Dispose()
            {
                _disposedView = true;
                _previewBoundRequest = null;
                if (_confirm != null) _confirm.isEnabled = false;
                if (_root != null) _root.eventSizeChanged -= OnSizeChanged;
                if (_dataRoot != null) _dataRoot.eventSizeChanged -= OnSizeChanged;
                if (_reviewRoot != null) _reviewRoot.eventSizeChanged -= OnSizeChanged;
                _notice.Dispose();
                _preview.Dispose();
                _response.Dispose();
            }

            internal void Refresh()
            {
                _previewBoundRequest = null;
                SetButton(_confirm, "localAi.confirmButton", false);
                SettingsLayout.SetTextFieldLabel(_ask, _model, Localization.Get("localAi.model"));
                SettingsLayout.SetTextFieldLabel(_ask, _limit, Localization.Get("localAi.responseLimit"));
                SettingsLayout.SetTextFieldLabel(_ask, _question, Localization.Get("localAi.question"));
                SetText(_model, _owner._modelName);
                SetText(_limit, _owner._responseLimitText);
                SetText(_question, _owner._question);
                SetCheck(_allow, _owner._allowLaunch, "localAi.allowLaunch");
                bool[] values = { _owner._includeGameDate, _owner._includePopulation, _owner._includeFundsCents,
                    _owner._includeResidentialDemand, _owner._includeCommercialDemand, _owner._includeWorkplaceDemand,
                    _owner._includeUtilities, _owner._includeZoning, _owner._includeTrafficTransit, _owner._includeServiceStatus };
                for (int i = 0; i < _checks.Length; i++) SetCheck(_checks[i], values[i], CheckKeys[i]);
                SetButton(_selectAll, "localAi.selectAll", true);
                SetButton(_prepare, "localAi.previewButton", !_owner._capturePending && _owner._activeClient == null);
                SetButton(_cancel, "localAi.cancelButton", _owner._cancelEnabled);
                if (_status != null) _status.text = Localization.Get(_owner._statusLocalizationKey, _owner._statusArguments);
                if (_askStatus != null) _askStatus.text = Localization.Get(_owner._statusLocalizationKey, _owner._statusArguments);
                _notice.Refresh(Localization.Get("localAi.consentNotice"));
                _preview.Refresh(_owner._displayedPreviewRequest != null
                    ? FormatPreview(_owner._displayedPreviewRequest)
                    : Localization.Get(_owner._previewLocalizationKey));
                if (_preview.IsUsable && _owner._preparedRequest != null &&
                    ReferenceEquals(_owner._preparedRequest, _owner._displayedPreviewRequest))
                    _previewBoundRequest = _owner._preparedRequest;
                _response.Refresh(string.IsNullOrEmpty(_owner._responseLocalizationKey)
                    ? _owner._responseText : Localization.Get(_owner._responseLocalizationKey));
                Relayout();
                OnPreviewAvailabilityChanged(_preview.IsUsable);
            }

            private void OnSizeChanged(UIComponent component, UnityEngine.Vector2 size) { Relayout(); }

            private void OnPreviewAvailabilityChanged(bool ready)
            {
                SetButton(_confirm, "localAi.confirmButton", RequestPreviewSafety.CanConfirm(
                    _owner._confirmEnabled && IsAlive, ready, _previewBoundRequest, _owner._preparedRequest));
            }

            private void ConfirmFromView()
            {
                if (!RequestPreviewSafety.CanConfirm(_owner._confirmEnabled && IsAlive,
                    _preview.IsUsable, _previewBoundRequest, _owner._preparedRequest)) return;
                _owner.ConfirmRequest();
            }

            private void Relayout()
            {
                if (_relayout) return;
                _relayout = true;
                try
                {
                    FitWidth(_root, _model);
                    FitWidth(_root, _limit);
                    FitWidth(_root, _question);
                    FitCheckboxWidth(_root, _allow);
                    for (int i = 0; i < _checks.Length; i++) FitCheckboxWidth(_dataRoot, _checks[i]);
                    SettingsLayout.FitButton(_ask, _prepare);
                    SettingsLayout.FitButton(_data, _selectAll);
                    SettingsLayout.FitButton(_review, _confirm);
                    SettingsLayout.FitButton(_review, _cancel);
                    if (_status != null && _reviewRoot != null && _reviewRoot.width > 40f) _status.width = _reviewRoot.width - 24f;
                    if (_askStatus != null && _root != null && _root.width > 40f) _askStatus.width = _root.width - 24f;
                    _notice.Relayout(_root);
                    _preview.Relayout(_reviewRoot);
                    _response.Relayout(_reviewRoot);
                }
                finally { _relayout = false; }
            }

            internal static void FitWidth(UIComponent root, UITextField field)
            {
                if (root == null || field == null) return;
                float available = root.width - (field.absolutePosition.x - root.absolutePosition.x) - 24f;
                if (available > 40f && Math.Abs(field.width - available) > 0.5f) field.width = available;
            }

            private static void FitCheckboxWidth(UIComponent root, UICheckBox check)
            {
                if (root == null || check == null || check.label == null || root.width <= 60f) return;
                float available = root.width - 48f;
                if (Math.Abs(check.label.width - available) > 0.5f) check.label.width = available;
            }

            private static void SetText(UITextField field, string value)
            {
                if (field != null && field.text != (value ?? string.Empty)) field.text = value ?? string.Empty;
            }

            private static void SetCheck(UICheckBox check, bool value, string key)
            {
                if (check == null) return;
                if (check.label != null) check.label.text = Localization.Get(key);
                if (check.isChecked != value) check.isChecked = value;
            }

            private static void SetButton(UIButton button, string key, bool enabled)
            {
                if (button == null) return;
                button.text = Localization.Get(key);
                button.isEnabled = enabled;
            }

            private static UILabel CreateLabel(UIHelperBase group)
            {
                UIHelper helper = group as UIHelper;
                UIComponent root = helper == null ? null : helper.self as UIComponent;
                if (root == null) return null;
                UILabel label = root.AddUIComponent<UILabel>();
                label.autoSize = false;
                label.wordWrap = true;
                label.autoHeight = true;
                label.width = Math.Max(100f, root.width - 24f);
                return label;
            }
        }

        private sealed class TextPages
        {
            private readonly UIHelperBase _group;
            private readonly string _labelKey;
            private readonly UITextField _field;
            private readonly UIButton _previous;
            private readonly UIButton _next;
            private readonly Stack<int> _history = new Stack<int>();
            private string _text = string.Empty;
            private int _offset;
            private int _end;
            private bool _rendered;

            internal bool IsUsable { get { return _field != null && _previous != null && _next != null && _rendered; } }
            internal Action<bool> AvailabilityChanged;

            internal TextPages(UIHelperBase group, string labelKey)
            {
                _group = group;
                _labelKey = labelKey;
                _field = group.AddTextfield(Localization.Get(labelKey), string.Empty, Ignore, Ignore) as UITextField;
                if (_field != null)
                {
                    _field.readOnly = true;
                    _field.multiline = true;
                    _field.processMarkup = false;
                    _field.height = 220f;
                    SettingsLayout.FitTextField(group, _field);
                    _field.eventMouseWheel += OnMouseWheel;
                }
                _previous = group.AddButton(Localization.Get("ui.previous"), Previous) as UIButton;
                _next = group.AddButton(Localization.Get("ui.next"), Next) as UIButton;
            }

            internal void Dispose()
            {
                if (_field != null) _field.eventMouseWheel -= OnMouseWheel;
                AvailabilityChanged = null;
            }

            private void OnMouseWheel(UIComponent component, UIMouseEventParameter parameter)
            {
                if (parameter.wheelDelta > 0) Previous();
                else if (parameter.wheelDelta < 0) Next();
                parameter.Use();
            }

            internal void Refresh(string value)
            {
                value = value ?? string.Empty;
                if (!string.Equals(_text, value, StringComparison.Ordinal))
                {
                    _text = value;
                    _offset = 0;
                    _history.Clear();
                }
                SettingsLayout.SetTextFieldLabel(_group, _field, Localization.Get(_labelKey));
                Render();
            }

            internal void Relayout(UIComponent root)
            {
                View.FitWidth(root, _field);
                SettingsLayout.FitButton(_group, _previous);
                SettingsLayout.FitButton(_group, _next);
                Render();
            }

            private void Render()
            {
                if (AvailabilityChanged != null) AvailabilityChanged(false);
                _rendered = false;
                try
                {
                    int columns = _field == null ? 20 : Math.Max(4, Math.Min(20, (int)((_field.width - 30f) / 18f)));
                    int length = WindowLength(_text, _offset, columns);
                    var buffer = new ScrollableTextBuffer(_text);
                    string window = buffer.ReadWindow(_offset, Math.Max(1, length), out _end);
                    if (_field != null) _field.text = window;
                    if (_previous != null) { _previous.text = Localization.Get("ui.previous"); _previous.isEnabled = _history.Count > 0; }
                    if (_next != null) { _next.text = Localization.Get("ui.next"); _next.isEnabled = _end < _text.Length; }
                    _rendered = _field != null && string.Equals(_field.text, window, StringComparison.Ordinal);
                }
                finally
                {
                    if (AvailabilityChanged != null) AvailabilityChanged(IsUsable);
                }
            }

            private void Previous()
            {
                if (_history.Count == 0) return;
                _offset = _history.Pop();
                Render();
            }

            private void Next()
            {
                if (_end >= _text.Length || _end <= _offset) return;
                _history.Push(_offset);
                _offset = _end;
                Render();
            }

            private static int WindowLength(string text, int start, int maxColumns)
            {
                int rows = 1;
                int columns = 0;
                int i = start;
                while (i < text.Length && i - start < 120)
                {
                    char c = text[i];
                    int units = c > 255 ? 2 : 1;
                    if (c == '\n') { rows++; columns = 0; }
                    else if (c != '\r')
                    {
                        columns += units;
                        if (columns >= maxColumns) { rows++; columns = 0; }
                    }
                    i++;
                    if (rows > 6) break;
                }
                return i - start;
            }

            private static void Ignore(string value) { }
        }
    }
}
// Implementation update signed by OpenAI-Codex_ui_state_arch.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
