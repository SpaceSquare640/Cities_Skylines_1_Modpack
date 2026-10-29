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
        private readonly UITextField _consentDisplay;
        private readonly UITextField _modelField;
        private readonly UITextField _responseLimitField;
        private readonly UITextField _questionField;
        private readonly UITextField _previewDisplay;
        private readonly UIButton _statusDisplay;
        private readonly UITextField _responseDisplay;
        private readonly UIButton _confirmButton;
        private readonly UIButton _cancelButton;
        private readonly UICheckBox[] _schemaCheckboxes = new UICheckBox[10];
        private string _modelName = string.Empty;
        private string _question = string.Empty;
        private int _maximumResponseBytes;
        private bool _allowLaunch;
        private bool _includeGameDate = true;
        private bool _includePopulation = true;
        private bool _includeFundsCents = true;
        private bool _includeResidentialDemand = true;
        private bool _includeCommercialDemand = true;
        private bool _includeWorkplaceDemand = true;
        private bool _includeUtilities = true;
        private bool _includeZoning = true;
        private bool _includeTrafficTransit = true;
        private bool _includeServiceStatus = true;
        private LocalAiRequest _preparedRequest;
        private LocalAiRequest _displayedPreviewRequest;
        private readonly RequestCaptureGeneration _captureGeneration = new RequestCaptureGeneration();
        private bool _capturePending;
        private string _previewLocalizationKey = "localAi.preview.none";
        private string _responseLocalizationKey = "localAi.response.none";
        private string _statusLocalizationKey = "localAi.status.ready";
        private object[] _statusArguments = new object[0];
        private CompanionClient _activeClient;
        private readonly RequestCaptureGeneration _requestGeneration = new RequestCaptureGeneration();
        private bool _disposed;

        internal LocalAiRequestController(UIHelperBase group)
        {
            _group = group;
            _consentDisplay = CreateReadOnlyTextField(group, Localization.Get("localAi.consentLabel"), Localization.Get("localAi.consentNotice"));
            UICheckBox allowLaunch = group.AddCheckbox(Localization.Get("localAi.allowLaunch"), false, OnAllowLaunchChanged) as UICheckBox;
            SettingsLayout.FitCheckbox(group, allowLaunch);
            _modelField = group.AddTextfield(Localization.Get("localAi.model"), string.Empty, OnModelChanged, OnModelSubmitted) as UITextField;
            int responseLimitMiB = UnityEngine.PlayerPrefs.GetInt(LocalAiResponseLimit.PreferenceKey, LocalAiResponseLimit.DefaultMiB);
            int responseLimitBytes;
            if (!LocalAiResponseLimit.TryParseMiB(responseLimitMiB.ToString(CultureInfo.InvariantCulture), out responseLimitMiB, out responseLimitBytes))
            {
                responseLimitMiB = LocalAiResponseLimit.DefaultMiB;
                responseLimitBytes = 0;
            }
            _maximumResponseBytes = responseLimitBytes;
            _responseLimitField = group.AddTextfield(
                Localization.Get("localAi.responseLimit"),
                responseLimitMiB.ToString(CultureInfo.InvariantCulture),
                OnResponseLimitChanged,
                OnResponseLimitSubmitted) as UITextField;
            SettingsLayout.FitTextField(group, _responseLimitField);
            _questionField = group.AddTextfield(Localization.Get("localAi.question"), string.Empty, OnQuestionChanged, OnQuestionSubmitted) as UITextField;
            UIButton selectAll = group.AddButton(Localization.Get("localAi.selectAll"), SelectAllFields) as UIButton;
            SettingsLayout.FitButton(group, selectAll);
            _schemaCheckboxes[0] = group.AddCheckbox(Localization.Get("localAi.include.gameDate"), true, OnGameDateChanged) as UICheckBox;
            _schemaCheckboxes[1] = group.AddCheckbox(Localization.Get("localAi.include.population"), true, OnPopulationChanged) as UICheckBox;
            _schemaCheckboxes[2] = group.AddCheckbox(Localization.Get("localAi.include.fundsCents"), true, OnFundsChanged) as UICheckBox;
            _schemaCheckboxes[3] = group.AddCheckbox(Localization.Get("localAi.include.residentialDemand"), true, OnResidentialChanged) as UICheckBox;
            _schemaCheckboxes[4] = group.AddCheckbox(Localization.Get("localAi.include.commercialDemand"), true, OnCommercialChanged) as UICheckBox;
            _schemaCheckboxes[5] = group.AddCheckbox(Localization.Get("localAi.include.workplaceDemand"), true, OnWorkplaceChanged) as UICheckBox;
            _schemaCheckboxes[6] = group.AddCheckbox(Localization.Get("localAi.include.utilities"), true, OnUtilitiesChanged) as UICheckBox;
            _schemaCheckboxes[7] = group.AddCheckbox(Localization.Get("localAi.include.zoning"), true, OnZoningChanged) as UICheckBox;
            _schemaCheckboxes[8] = group.AddCheckbox(Localization.Get("localAi.include.trafficTransit"), true, OnTrafficTransitChanged) as UICheckBox;
            _schemaCheckboxes[9] = group.AddCheckbox(Localization.Get("localAi.include.serviceStatus"), true, OnServiceStatusChanged) as UICheckBox;
            for (int i = 0; i < _schemaCheckboxes.Length; i++) SettingsLayout.FitCheckbox(group, _schemaCheckboxes[i]);

            _previewDisplay = CreateReadOnlyTextField(group, Localization.Get("localAi.previewLabel"), Localization.Get("localAi.preview.none"));
            _statusDisplay = group.AddButton(Localization.Get("localAi.status.ready"), IgnoreClick) as UIButton;
            SettingsLayout.FitButton(group, _statusDisplay);
            _responseDisplay = CreateReadOnlyTextField(group, Localization.Get("localAi.responseLabel"), Localization.Get("localAi.response.none"));
            group.AddButton(Localization.Get("localAi.previewButton"), PrepareRequest);
            _confirmButton = group.AddButton(Localization.Get("localAi.confirmButton"), ConfirmRequest) as UIButton;
            _cancelButton = group.AddButton(Localization.Get("localAi.cancelButton"), CancelRequest) as UIButton;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
            RefreshLocalizedContent();
        }

        internal void RefreshLocalizedContent()
        {
            SettingsLayout.SetTextFieldLabel(_group, _consentDisplay, Localization.Get("localAi.consentLabel"));
            SettingsLayout.SetTextFieldLabel(_group, _modelField, Localization.Get("localAi.model"));
            SettingsLayout.SetTextFieldLabel(_group, _responseLimitField, Localization.Get("localAi.responseLimit"));
            SettingsLayout.SetTextFieldLabel(_group, _questionField, Localization.Get("localAi.question"));
            SettingsLayout.SetTextFieldLabel(_group, _previewDisplay, Localization.Get("localAi.previewLabel"));
            SettingsLayout.SetTextFieldLabel(_group, _responseDisplay, Localization.Get("localAi.responseLabel"));
            SetFieldText(_consentDisplay, Localization.Get("localAi.consentNotice"));
            if (_displayedPreviewRequest != null)
            {
                SetFieldText(_previewDisplay, FormatPreview(_displayedPreviewRequest));
            }
            else if (!string.IsNullOrEmpty(_previewLocalizationKey))
            {
                SetFieldText(_previewDisplay, Localization.Get(_previewLocalizationKey));
            }

            if (!string.IsNullOrEmpty(_responseLocalizationKey))
            {
                SetFieldText(_responseDisplay, Localization.Get(_responseLocalizationKey));
            }
            if (!string.IsNullOrEmpty(_statusLocalizationKey))
            {
                SetStatus(Localization.Get(_statusLocalizationKey, _statusArguments));
            }
        }

        internal void Shutdown()
        {
            _disposed = true;
            _captureGeneration.Invalidate();
            _capturePending = false;
            _requestGeneration.InvalidateResponse();
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
            int responseLimitMiB;
            int responseLimitBytes;
            if (_responseLimitField == null || !LocalAiResponseLimit.TryParseMiB(_responseLimitField.text, out responseLimitMiB, out responseLimitBytes))
            {
                InvalidatePreview();
                SetLocalizedStatus("localAi.error.response_limit_invalid");
                return;
            }
            _maximumResponseBytes = responseLimitBytes;
            UnityEngine.PlayerPrefs.SetInt(LocalAiResponseLimit.PreferenceKey, responseLimitMiB);
            UnityEngine.PlayerPrefs.Save();

            if (string.IsNullOrEmpty(_modelName) || _modelName.Trim().Length == 0 ||
                string.IsNullOrEmpty(_question) || _question.Trim().Length == 0)
            {
                SetLocalizedStatus("localAi.status.inputRequired");
                return;
            }

            _preparedRequest = null;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
            SetLocalizedStatus("localAi.status.capturing");
            long generation = _captureGeneration.Begin();
            _capturePending = true;

            var options = new CityCaptureOptions
            {
                IncludeUtilities = _includeUtilities,
                IncludeZoning = _includeZoning,
                IncludeTrafficTransit = _includeTrafficTransit,
                IncludeServiceStatus = _includeServiceStatus
            };
            CitySummary.QueueCapture(options,
                delegate(CitySnapshot snapshot)
                {
                    if (_disposed || !_captureGeneration.IsCurrent(generation)) return;
                    _capturePending = false;
                    LocalAiRequest request = CreateRequest(snapshot);
                    _preparedRequest = request;
                    _displayedPreviewRequest = request;
                    _previewLocalizationKey = null;
                    SetFieldText(_previewDisplay, FormatPreview(request));
                    SetEnabled(_confirmButton, true);
                    SetEnabled(_cancelButton, true);
                    SetLocalizedStatus("localAi.status.review");
                },
                delegate(string errorCode)
                {
                    if (_disposed || !_captureGeneration.IsCurrent(generation)) return;
                    _capturePending = false;
                    SetLocalizedStatus(errorCode == "dlc_detection_unavailable"
                        ? "localAi.error.dlc_detection_unavailable"
                        : "localAi.error.snapshot_unavailable");
                });
        }

        private void ConfirmRequest()
        {
            if (_disposed) return;
            if (_preparedRequest == null)
            {
                SetLocalizedStatus("localAi.status.reviewFirst");
                return;
            }
            if (!_allowLaunch)
            {
                SetLocalizedStatus("localAi.status.launchDisabled");
                return;
            }

            LocalAiRequest request = _preparedRequest;
            _preparedRequest = null;
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, true);
            SetLocalizedResponse("localAi.response.pending");
            SetLocalizedStatus("localAi.status.starting");

            var client = new CompanionClient();
            DiagnosticLog.Detail("Local AI request started.");
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            _activeClient = client;
            long generation = _requestGeneration.BeginResponse();
            bool queued = ThreadPool.QueueUserWorkItem(delegate
            {
                LocalAiResult result = client.Send(request);
                long elapsedMs = (long)((System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                DispatchToUi(delegate { CompleteRequest(generation, client, result, elapsedMs); });
            });
            if (!queued)
            {
                _activeClient = null;
                client.Cancel();
                SetEnabled(_cancelButton, false);
                SetLocalizedStatus("localAi.error.companion_start_failed");
            }
        }

        private void CompleteRequest(long generation, CompanionClient client, LocalAiResult result, long elapsedMs)
        {
            if (_disposed || !_requestGeneration.IsResponseCurrent(generation)) return;
            DiagnosticLog.Detail("Local AI request elapsedMs=" + Math.Max(0, elapsedMs).ToString(CultureInfo.InvariantCulture) + ".");
            if (ReferenceEquals(_activeClient, client)) _activeClient = null;
            SetEnabled(_cancelButton, false);

            if (result != null && result.Success)
            {
                SetResponseText(result.Content);
                SetLocalizedStatus("localAi.status.success", result.ModelName);
                DiagnosticLog.Detail("Local AI request completed | status=success.");
                return;
            }

            string errorCode = result == null ? "companion_request_failed" : result.ErrorCode;
            SetLocalizedResponse("localAi.response.none");
            SetLocalizedStatus("localAi.error." + (string.IsNullOrEmpty(errorCode) ? "generic" : errorCode));
            DiagnosticLog.Warning("Local AI request failed | status=" + GetSafeErrorCategory(errorCode) + ".");
        }

        private void CancelRequest()
        {
            if (_disposed) return;
            if (_activeClient != null)
            {
                CompanionClient client = _activeClient;
                _activeClient = null;
                _requestGeneration.InvalidateResponse();
                client.Cancel();
                SetLocalizedResponse("localAi.response.none");
                SetLocalizedStatus("localAi.status.cancelled");
                SetEnabled(_cancelButton, false);
                return;
            }

            _captureGeneration.Invalidate();
            _capturePending = false;
            _preparedRequest = null;
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.none";
            SetFieldText(_previewDisplay, Localization.Get(_previewLocalizationKey));
            SetLocalizedStatus("localAi.status.discarded");
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
                MaximumResponseBytes = _maximumResponseBytes,
                IncludeGameDate = _includeGameDate,
                IncludePopulation = _includePopulation,
                IncludeFundsCents = _includeFundsCents,
                IncludeResidentialDemand = _includeResidentialDemand,
                IncludeCommercialDemand = _includeCommercialDemand,
                IncludeWorkplaceDemand = _includeWorkplaceDemand,
                IncludeUtilities = _includeUtilities,
                IncludeZoning = _includeZoning,
                IncludeTrafficTransit = _includeTrafficTransit,
                IncludeServiceStatus = _includeServiceStatus
            };
        }

        private static string FormatPreview(LocalAiRequest request)
        {
            var text = new StringBuilder();
            text.AppendLine(Localization.Get("localAi.preview.destination"));
            text.AppendLine(Localization.Get("localAi.preview.model", request.ModelName));
            text.AppendLine(Localization.Get("localAi.preview.schema"));
            text.AppendLine(request.MaximumResponseBytes == 0
                ? Localization.Get("localAi.preview.responseLimit.unlimited")
                : Localization.Get("localAi.preview.responseLimit.configured",
                    (request.MaximumResponseBytes / LocalAiResponseLimit.BytesPerMiB).ToString(CultureInfo.InvariantCulture)));
            AppendField(text, request.IncludeGameDate, Localization.Get("localAi.field.gameDate"), request.Snapshot.GameDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendField(text, request.IncludePopulation, Localization.Get("localAi.field.population"), request.Snapshot.Population.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeFundsCents, Localization.Get("localAi.field.fundsCents"), request.Snapshot.FundsCents.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeResidentialDemand, Localization.Get("localAi.field.residentialDemand"), request.Snapshot.ResidentialDemand.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeCommercialDemand, Localization.Get("localAi.field.commercialDemand"), request.Snapshot.CommercialDemand.ToString(CultureInfo.InvariantCulture));
            AppendField(text, request.IncludeWorkplaceDemand, Localization.Get("localAi.field.workplaceDemand"), request.Snapshot.WorkplaceDemand.ToString(CultureInfo.InvariantCulture));
            if (request.IncludeUtilities && request.Snapshot.Utilities != null)
            {
                UtilitiesSnapshot data = request.Snapshot.Utilities;
                text.AppendLine(Localization.Get("localAi.preview.utilities",
                    data.ElectricityCapacity.ToString(CultureInfo.InvariantCulture),
                    data.ElectricityConsumption.ToString(CultureInfo.InvariantCulture),
                    data.WaterCapacity.ToString(CultureInfo.InvariantCulture),
                    data.WaterConsumption.ToString(CultureInfo.InvariantCulture),
                    data.SewageCapacity.ToString(CultureInfo.InvariantCulture),
                    data.SewageAccumulation.ToString(CultureInfo.InvariantCulture),
                    data.GarbageAmount.ToString(CultureInfo.InvariantCulture),
                    data.GarbageCapacity.ToString(CultureInfo.InvariantCulture),
                    data.GarbageAccumulation.ToString(CultureInfo.InvariantCulture),
                    data.IncinerationCapacity.ToString(CultureInfo.InvariantCulture)));
            }
            if (request.IncludeZoning && request.Snapshot.Zoning != null)
            {
                ZoningSnapshot data = request.Snapshot.Zoning;
                text.AppendLine(Localization.Get("localAi.preview.zoning",
                    data.ResidentialBuildingCount.ToString(CultureInfo.InvariantCulture), data.ResidentialBuildingArea.ToString(CultureInfo.InvariantCulture),
                    data.CommercialBuildingCount.ToString(CultureInfo.InvariantCulture), data.CommercialBuildingArea.ToString(CultureInfo.InvariantCulture),
                    data.IndustrialBuildingCount.ToString(CultureInfo.InvariantCulture), data.IndustrialBuildingArea.ToString(CultureInfo.InvariantCulture),
                    data.OfficeBuildingCount.ToString(CultureInfo.InvariantCulture), data.OfficeBuildingArea.ToString(CultureInfo.InvariantCulture)));
            }
            if (request.IncludeTrafficTransit && request.Snapshot.TrafficTransit != null)
            {
                TrafficTransitSnapshot data = request.Snapshot.TrafficTransit;
                text.AppendLine(Localization.Get("localAi.preview.trafficTransit",
                    data.TrafficFlow.ToString(CultureInfo.InvariantCulture),
                    data.AveragePassengers.ToString(CultureInfo.InvariantCulture)));
            }
            if (request.IncludeServiceStatus && request.Snapshot.ServiceStatus != null)
            {
                ServiceStatusSnapshot data = request.Snapshot.ServiceStatus;
                text.AppendLine(Localization.Get("localAi.preview.serviceStatus",
                    data.PoliceCriminalAmount.ToString(CultureInfo.InvariantCulture),
                    data.PoliceCriminalCapacity.ToString(CultureInfo.InvariantCulture),
                    data.PoliceExtraCriminals.ToString(CultureInfo.InvariantCulture),
                    data.FireCoverageSegmentCount == 0
                        ? Localization.Get("localAi.preview.unavailable")
                        : data.FireCoverageMeanByte.ToString("0.##", CultureInfo.InvariantCulture),
                    data.FireCoverageSegmentCount.ToString(CultureInfo.InvariantCulture),
                    data.HealthSickCount.ToString(CultureInfo.InvariantCulture),
                    data.HealthCapacity.ToString(CultureInfo.InvariantCulture),
                    data.HealthCareProductivity.ToString(CultureInfo.InvariantCulture)));
            }
            text.Append(Localization.Get("localAi.field.installedAvailableDlcs"));
            text.Append('=');
            string[] availableDlcs = request.Snapshot.InstalledAvailableDlcIdentifiers ?? new string[0];
            text.AppendLine(availableDlcs.Length == 0
                ? Localization.Get("localAi.dlc.noneDetected")
                : string.Join(", ", availableDlcs));
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

        private void SetLocalizedStatus(string key, params object[] arguments)
        {
            _statusLocalizationKey = key;
            _statusArguments = arguments == null ? new object[0] : (object[])arguments.Clone();
            SetStatus(Localization.Get(key, _statusArguments));
        }

        private void SetLocalizedResponse(string key)
        {
            _responseLocalizationKey = key;
            SetFieldText(_responseDisplay, Localization.Get(key));
        }

        private void SetResponseText(string text)
        {
            _responseLocalizationKey = null;
            SetFieldText(_responseDisplay, text);
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
            if (_disposed) return;
            if (_capturePending)
            {
                _captureGeneration.Invalidate();
                _capturePending = false;
                _previewLocalizationKey = "localAi.preview.stale";
                SetFieldText(_previewDisplay, Localization.Get(_previewLocalizationKey));
                SetEnabled(_confirmButton, false);
                SetEnabled(_cancelButton, false);
                SetLocalizedStatus("localAi.status.reviewAgain");
            }
            if (_preparedRequest == null) return;
            _preparedRequest = null;
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.stale";
            SetFieldText(_previewDisplay, Localization.Get(_previewLocalizationKey));
            SetEnabled(_confirmButton, false);
            SetEnabled(_cancelButton, false);
            SetLocalizedStatus("localAi.status.reviewAgain");
        }

        private void SelectAllFields()
        {
            _includeGameDate = true;
            _includePopulation = true;
            _includeFundsCents = true;
            _includeResidentialDemand = true;
            _includeCommercialDemand = true;
            _includeWorkplaceDemand = true;
            _includeUtilities = true;
            _includeZoning = true;
            _includeTrafficTransit = true;
            _includeServiceStatus = true;
            for (int i = 0; i < _schemaCheckboxes.Length; i++)
            {
                if (_schemaCheckboxes[i] != null) _schemaCheckboxes[i].isChecked = true;
            }
            InvalidatePreview();
        }

        private void OnAllowLaunchChanged(bool value) { _allowLaunch = value; InvalidatePreview(); }
        private void OnModelChanged(string value) { _modelName = value ?? string.Empty; InvalidatePreview(); }
        private void OnModelSubmitted(string value) { OnModelChanged(value); }
        private void OnResponseLimitChanged(string value)
        {
            if (_disposed) return;
            InvalidatePreview();
            int responseLimitMiB;
            int responseLimitBytes;
            if (!LocalAiResponseLimit.TryParseMiB(value, out responseLimitMiB, out responseLimitBytes))
            {
                SetLocalizedStatus("localAi.error.response_limit_invalid");
                return;
            }

            _maximumResponseBytes = responseLimitBytes;
            UnityEngine.PlayerPrefs.SetInt(LocalAiResponseLimit.PreferenceKey, responseLimitMiB);
            UnityEngine.PlayerPrefs.Save();
        }
        private void OnResponseLimitSubmitted(string value) { OnResponseLimitChanged(value); }
        private void OnQuestionChanged(string value) { _question = value ?? string.Empty; InvalidatePreview(); }
        private void OnQuestionSubmitted(string value) { OnQuestionChanged(value); }
        private void OnGameDateChanged(bool value) { _includeGameDate = value; InvalidatePreview(); }
        private void OnPopulationChanged(bool value) { _includePopulation = value; InvalidatePreview(); }
        private void OnFundsChanged(bool value) { _includeFundsCents = value; InvalidatePreview(); }
        private void OnResidentialChanged(bool value) { _includeResidentialDemand = value; InvalidatePreview(); }
        private void OnCommercialChanged(bool value) { _includeCommercialDemand = value; InvalidatePreview(); }
        private void OnWorkplaceChanged(bool value) { _includeWorkplaceDemand = value; InvalidatePreview(); }
        private void OnUtilitiesChanged(bool value) { _includeUtilities = value; InvalidatePreview(); }
        private void OnZoningChanged(bool value) { _includeZoning = value; InvalidatePreview(); }
        private void OnTrafficTransitChanged(bool value) { _includeTrafficTransit = value; InvalidatePreview(); }
        private void OnServiceStatusChanged(bool value) { _includeServiceStatus = value; InvalidatePreview(); }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
