using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using ICities;

namespace AIGaming
{
    internal sealed class LocalAiRequestController
    {
        private readonly UIHelperBase _group;
        private readonly UITextField _previewDisplay;
        private readonly UIButton _statusDisplay;
        private readonly UITextField _responseDisplay;
        private readonly UIButton _confirmButton;
        private readonly UIButton _cancelButton;
        private readonly UICheckBox[] _schemaCheckboxes = new UICheckBox[6];
        private string _modelName = string.Empty;
        private string _question = string.Empty;
        private bool _allowLaunch;
        private bool _includeGameDate = true;
        private bool _includePopulation = true;
        private bool _includeFundsCents = true;
        private bool _includeResidentialDemand = true;
        private bool _includeCommercialDemand = true;
        private bool _includeWorkplaceDemand = true;
        private LocalAiRequest _preparedRequest;
        private CompanionClient _activeClient;
        private int _generation;
        private bool _disposed;

        internal LocalAiRequestController(UIHelperBase group)
        {
            _group = group;
            CreateReadOnlyTextField(group, Localization.Get("localAi.consentLabel"), Localization.Get("localAi.consentNotice"));
            UICheckBox allowLaunch = group.AddCheckbox(Localization.Get("localAi.allowLaunch"), false, OnAllowLaunchChanged) as UICheckBox;
            SettingsLayout.FitCheckbox(group, allowLaunch);
            group.AddTextfield(Localization.Get("localAi.model"), string.Empty, OnModelChanged, OnModelSubmitted);
            group.AddTextfield(Localization.Get("localAi.question"), string.Empty, OnQuestionChanged, OnQuestionSubmitted);
            group.AddButton(Localization.Get("localAi.selectAll"), SelectAllFields);
            _schemaCheckboxes[0] = group.AddCheckbox(Localization.Get("localAi.include.gameDate"), true, OnGameDateChanged) as UICheckBox;
            _schemaCheckboxes[1] = group.AddCheckbox(Localization.Get("localAi.include.population"), true, OnPopulationChanged) as UICheckBox;
            _schemaCheckboxes[2] = group.AddCheckbox(Localization.Get("localAi.include.fundsCents"), true, OnFundsChanged) as UICheckBox;
            _schemaCheckboxes[3] = group.AddCheckbox(Localization.Get("localAi.include.residentialDemand"), true, OnResidentialChanged) as UICheckBox;
            _schemaCheckboxes[4] = group.AddCheckbox(Localization.Get("localAi.include.commercialDemand"), true, OnCommercialChanged) as UICheckBox;
            _schemaCheckboxes[5] = group.AddCheckbox(Localization.Get("localAi.include.workplaceDemand"), true, OnWorkplaceChanged) as UICheckBox;

            _previewDisplay = CreateReadOnlyTextField(group, Localization.Get("localAi.previewLabel"), Localization.Get("localAi.preview.none"));
            _statusDisplay = group.AddButton(Localization.Get("localAi.status.ready"), IgnoreClick) as UIButton;
            SettingsLayout.FitButton(group, _statusDisplay);
            _responseDisplay = CreateReadOnlyTextField(group, Localization.Get("localAi.responseLabel"), Localization.Get("localAi.response.none"));
            group.AddButton(Localization.Get("localAi.previewButton"), PrepareRequest);
            _confirmButton = group.AddButton(Localization.Get("localAi.confirmButton"), ConfirmRequest) as UIButton;
            _cancelButton = group.AddButton(Localization.Get("localAi.cancelButton"), CancelRequest) as UIButton;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
        }

        internal void Shutdown()
        {
            _disposed = true;
            _generation++;
            _preparedRequest = null;
            if (_activeClient != null)
            {
                _activeClient.Cancel();
                _activeClient = null;
            }
        }

        private void PrepareRequest()
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(_modelName) || _modelName.Trim().Length == 0 ||
                string.IsNullOrEmpty(_question) || _question.Trim().Length == 0)
            {
                SetStatus(Localization.Get("localAi.status.inputRequired"));
                return;
            }

            _preparedRequest = null;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
            SetStatus(Localization.Get("localAi.status.capturing"));
            int generation = ++_generation;

            CitySummary.QueueCapture(
                delegate(CitySnapshot snapshot)
                {
                    if (_disposed || generation != _generation) return;
                    LocalAiRequest request = CreateRequest(snapshot);
                    _preparedRequest = request;
                    SetFieldText(_previewDisplay, FormatPreview(request));
                    SetEnabled(_confirmButton, true);
                    SetEnabled(_cancelButton, true);
                    SetStatus(Localization.Get("localAi.status.review"));
                },
                delegate(string errorCode)
                {
                    if (_disposed || generation != _generation) return;
                    SetStatus(Localization.Get("localAi.error.snapshot_unavailable"));
                });
        }

        private void ConfirmRequest()
        {
            if (_disposed) return;
            if (_preparedRequest == null)
            {
                SetStatus(Localization.Get("localAi.status.reviewFirst"));
                return;
            }
            if (!_allowLaunch)
            {
                SetStatus(Localization.Get("localAi.status.launchDisabled"));
                return;
            }

            LocalAiRequest request = _preparedRequest;
            _preparedRequest = null;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, true);
            SetFieldText(_responseDisplay, Localization.Get("localAi.response.pending"));
            SetStatus(Localization.Get("localAi.status.starting"));

            var client = new CompanionClient();
            _activeClient = client;
            int generation = ++_generation;
            bool queued = ThreadPool.QueueUserWorkItem(delegate
            {
                LocalAiResult result = client.Send(request);
                DispatchToUi(delegate { CompleteRequest(generation, client, result); });
            });
            if (!queued)
            {
                _activeClient = null;
                client.Cancel();
                SetEnabled(_cancelButton, false);
                SetStatus(Localization.Get("localAi.error.companion_start_failed"));
            }
        }

        private void CompleteRequest(int generation, CompanionClient client, LocalAiResult result)
        {
            if (_disposed || generation != _generation) return;
            if (ReferenceEquals(_activeClient, client)) _activeClient = null;
            SetEnabled(_cancelButton, false);

            if (result != null && result.Success)
            {
                SetFieldText(_responseDisplay, result.Content);
                SetStatus(Localization.Get("localAi.status.success", result.ModelName));
                UnityEngine.Debug.Log("[AI Gaming] Local AI request completed | status=success.");
                return;
            }

            string errorCode = result == null ? "companion_request_failed" : result.ErrorCode;
            SetFieldText(_responseDisplay, Localization.Get("localAi.response.none"));
            SetStatus(GetErrorText(errorCode));
            UnityEngine.Debug.LogWarning("[AI Gaming] Local AI request failed | status=" + GetSafeErrorCategory(errorCode) + ".");
        }

        private void CancelRequest()
        {
            if (_disposed) return;
            if (_activeClient != null)
            {
                CompanionClient client = _activeClient;
                _activeClient = null;
                _generation++;
                client.Cancel();
                SetFieldText(_responseDisplay, Localization.Get("localAi.response.none"));
                SetStatus(Localization.Get("localAi.status.cancelled"));
                SetEnabled(_cancelButton, false);
                return;
            }

            _generation++;
            _preparedRequest = null;
            SetFieldText(_previewDisplay, Localization.Get("localAi.preview.none"));
            SetStatus(Localization.Get("localAi.status.discarded"));
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
        }

        private LocalAiRequest CreateRequest(CitySnapshot snapshot)
        {
            return new LocalAiRequest
            {
                ModelName = _modelName,
                Question = _question,
                Snapshot = snapshot,
                IncludeGameDate = _includeGameDate,
                IncludePopulation = _includePopulation,
                IncludeFundsCents = _includeFundsCents,
                IncludeResidentialDemand = _includeResidentialDemand,
                IncludeCommercialDemand = _includeCommercialDemand,
                IncludeWorkplaceDemand = _includeWorkplaceDemand
            };
        }

        private static string FormatPreview(LocalAiRequest request)
        {
            var text = new StringBuilder();
            text.AppendLine(Localization.Get("localAi.preview.destination"));
            text.AppendLine(Localization.Get("localAi.preview.model", request.ModelName));
            text.AppendLine(Localization.Get("localAi.preview.schema"));
            AppendField(text, request.IncludeGameDate, Localization.Get("localAi.field.gameDate"), request.Snapshot.GameDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendField(text, request.IncludePopulation, Localization.Get("localAi.field.population"), request.Snapshot.Population.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeFundsCents, Localization.Get("localAi.field.fundsCents"), request.Snapshot.FundsCents.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeResidentialDemand, Localization.Get("localAi.field.residentialDemand"), request.Snapshot.ResidentialDemand.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeCommercialDemand, Localization.Get("localAi.field.commercialDemand"), request.Snapshot.CommercialDemand.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeWorkplaceDemand, Localization.Get("localAi.field.workplaceDemand"), request.Snapshot.WorkplaceDemand.ToString(CultureInfo.InvariantCulture));
            text.AppendLine(Localization.Get("localAi.preview.question"));
            text.AppendLine(request.Question);
            text.Append(Localization.Get("localAi.preview.confirmInstruction"));
            return text.ToString();
        }

        private static void AppendField(StringBuilder text, bool included, string name, string value)
        {
            if (included) text.AppendLine(name + "=" + value);
        }

        private static UITextField CreateReadOnlyTextField(UIHelperBase group, string label, string text)
        {
            UITextField field = group.AddTextfield(label, text, IgnoreText, IgnoreText) as UITextField;
            if (field != null)
            {
                field.readOnly = true;
                field.multiline = true;
                field.processMarkup = false;
                field.height = EstimateTextHeight(text);
                SettingsLayout.FitTextField(group, field);
            }
            return field;
        }

        private static UIButton CreateDisplayButton(UIHelperBase group, string text)
        {
            return group.AddButton(text, IgnoreClick) as UIButton;
        }

        private static void IgnoreClick()
        {
        }

        private static void IgnoreText(string value)
        {
        }

        private void SetFieldText(UITextField field, string text)
        {
            if (field == null) return;
            try
            {
                field.text = text ?? string.Empty;
                field.height = EstimateTextHeight(field.text);
                SettingsLayout.FitTextField(_group, field);
            }
            catch
            {
            }
        }

        private static float EstimateTextHeight(string text)
        {
            if (string.IsNullOrEmpty(text)) return 72f;
            int lines = 0;
            int column = 0;
            const int columnsPerLine = 36;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lines += Math.Max(1, (column + columnsPerLine - 1) / columnsPerLine);
                    column = 0;
                }
                else
                {
                    if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        column += 2;
                        i++;
                    }
                    else
                    {
                        column += IsWideCharacter(text[i]) ? 2 : 1;
                    }
                    if (column >= columnsPerLine)
                    {
                        lines += column / columnsPerLine;
                        column %= columnsPerLine;
                    }
                }
            }
            lines += Math.Max(1, (column + columnsPerLine - 1) / columnsPerLine);
            return Math.Max(120f, lines * 22f + 20f);
        }

        private static bool IsWideCharacter(char value)
        {
            return (value >= '\u1100' && value <= '\u11FF') ||
                (value >= '\u2E80' && value <= '\uA4CF') ||
                (value >= '\uAC00' && value <= '\uD7A3') ||
                (value >= '\uF900' && value <= '\uFAFF') ||
                (value >= '\uFE30' && value <= '\uFE6F') ||
                (value >= '\uFF00' && value <= '\uFFEF');
        }

        private static void SetButtonText(UIButton button, string text)
        {
            if (button == null) return;
            try { button.text = text ?? string.Empty; }
            catch { }
        }

        private static void SetEnabled(UIButton button, bool enabled)
        {
            if (button == null) return;
            try { button.isEnabled = enabled; }
            catch { }
        }

        private void SetStatus(string text)
        {
            SetButtonText(_statusDisplay, text);
            SettingsLayout.FitButton(_group, _statusDisplay);
        }

        private string GetErrorText(string code)
        {
            if (string.IsNullOrEmpty(code)) return Localization.Get("localAi.error.generic");
            return Localization.Get("localAi.error." + code);
        }

        private static string GetSafeErrorCategory(string code)
        {
            if (string.IsNullOrEmpty(code)) return "request_failed";
            for (int i = 0; i < code.Length; i++)
            {
                char value = code[i];
                if (!((value >= 'a' && value <= 'z') || (value >= '0' && value <= '9') || value == '_'))
                {
                    return "request_failed";
                }
            }
            return code;
        }

        private static void DispatchToUi(Action action)
        {
            try
            {
                Dispatcher dispatcher = ThreadHelper.dispatcher;
                if (dispatcher != null) dispatcher.Dispatch(action);
            }
            catch
            {
            }
        }

        private void InvalidatePreview()
        {
            if (_disposed || _preparedRequest == null) return;
            _preparedRequest = null;
            _generation++;
            SetFieldText(_previewDisplay, Localization.Get("localAi.preview.stale"));
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
            SetStatus(Localization.Get("localAi.status.reviewAgain"));
        }

        private void SelectAllFields()
        {
            _includeGameDate = true;
            _includePopulation = true;
            _includeFundsCents = true;
            _includeResidentialDemand = true;
            _includeCommercialDemand = true;
            _includeWorkplaceDemand = true;
            for (int i = 0; i < _schemaCheckboxes.Length; i++)
            {
                if (_schemaCheckboxes[i] != null) _schemaCheckboxes[i].isChecked = true;
            }
            InvalidatePreview();
        }

        private void OnAllowLaunchChanged(bool value) { _allowLaunch = value; InvalidatePreview(); }
        private void OnModelChanged(string value) { _modelName = value ?? string.Empty; InvalidatePreview(); }
        private void OnModelSubmitted(string value) { OnModelChanged(value); }
        private void OnQuestionChanged(string value) { _question = value ?? string.Empty; InvalidatePreview(); }
        private void OnQuestionSubmitted(string value) { OnQuestionChanged(value); }
        private void OnGameDateChanged(bool value) { _includeGameDate = value; InvalidatePreview(); }
        private void OnPopulationChanged(bool value) { _includePopulation = value; InvalidatePreview(); }
        private void OnFundsChanged(bool value) { _includeFundsCents = value; InvalidatePreview(); }
        private void OnResidentialChanged(bool value) { _includeResidentialDemand = value; InvalidatePreview(); }
        private void OnCommercialChanged(bool value) { _includeCommercialDemand = value; InvalidatePreview(); }
        private void OnWorkplaceChanged(bool value) { _includeWorkplaceDemand = value; InvalidatePreview(); }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
