using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using ICities;

namespace AIGaming
{
    internal sealed partial class LocalAiRequestController
    {
        private readonly List<View> _views = new List<View>();
        private bool _refreshingViews;
        private string _responseLimitText;
        private string _responseText = string.Empty;
        private bool _confirmEnabled;
        private bool _cancelEnabled;
        private bool _viewErrorReported;
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
        internal event Action ReviewRequested;

        internal LocalAiRequestController()
        {
            int value = UnityEngine.PlayerPrefs.GetInt(LocalAiResponseLimit.PreferenceKey, LocalAiResponseLimit.DefaultMiB);
            int bytes;
            if (!LocalAiResponseLimit.TryParseMiB(value.ToString(CultureInfo.InvariantCulture), out value, out bytes))
            {
                value = LocalAiResponseLimit.DefaultMiB;
                bytes = 0;
            }
            _responseLimitText = value.ToString(CultureInfo.InvariantCulture);
            _maximumResponseBytes = bytes;
        }

        internal void Attach(UIHelperBase group)
        {
            if (group == null || _disposed) return;
            Attach(group.AddGroup(Localization.Get("ui.ask")), group.AddGroup(Localization.Get("ui.data")), group.AddGroup(Localization.Get("ui.review")));
        }

        internal void Attach(UIHelperBase ask, UIHelperBase data, UIHelperBase review)
        {
            if (_disposed || ask == null || data == null || review == null) return;
            _refreshingViews = true;
            try { _views.Add(new View(this, ask, data, review)); }
            finally { _refreshingViews = false; }
            RefreshLocalizedContent();
        }

        internal void RefreshLocalizedContent()
        {
            if (_disposed || _refreshingViews) return;
            _refreshingViews = true;
            try
            {
                for (int i = _views.Count - 1; i >= 0; i--)
                {
                    if (!_views[i].IsAlive) { _views[i].Dispose(); _views.RemoveAt(i); continue; }
                    try { _views[i].Refresh(); }
                    catch
                    {
                        if (!_viewErrorReported)
                        {
                            _viewErrorReported = true;
                            DiagnosticLog.Warning("UI view refresh failed.");
                        }
                    }
                }
            }
            finally { _refreshingViews = false; }
        }

        internal void OnCityUnload()
        {
            if (_disposed) return;
            _captureGeneration.Invalidate();
            _capturePending = false;
            _requestGeneration.InvalidateResponse();
            if (_activeClient != null) { _activeClient.Cancel(); _activeClient = null; }
            _preparedRequest = null;
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.none";
            _responseLocalizationKey = "localAi.response.none";
            _responseText = string.Empty;
            _confirmEnabled = false;
            _cancelEnabled = false;
            _statusLocalizationKey = "localAi.status.ready";
            _statusArguments = new object[0];
            RefreshLocalizedContent();
        }

        internal void Shutdown()
        {
            OnCityUnload();
            _disposed = true;
            for (int i = 0; i < _views.Count; i++) _views[i].Dispose();
            _views.Clear();
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
            if (_disposed || _activeClient != null || _capturePending) return;
            int responseLimitMiB;
            int responseLimitBytes;
            if (!LocalAiResponseLimit.TryParseMiB(_responseLimitText, out responseLimitMiB, out responseLimitBytes))
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
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.none";
            SetConfirmEnabled(false);
            SetCancelEnabled(false);
            long generation = _captureGeneration.Begin();
            _capturePending = true;
            SetCancelEnabled(true);
            SetLocalizedStatus("localAi.status.capturing");
            RequestReviewView();

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
                    RefreshLocalizedContent();
                    SetConfirmEnabled(true);
                    SetCancelEnabled(true);
                    SetLocalizedStatus("localAi.status.review");
                    RequestReviewView();
                },
                delegate(string errorCode)
                {
                    if (_disposed || !_captureGeneration.IsCurrent(generation)) return;
                    _capturePending = false;
                    SetCancelEnabled(false);
                    SetLocalizedStatus(errorCode == "dlc_detection_unavailable"
                        ? "localAi.error.dlc_detection_unavailable"
                        : "localAi.error.snapshot_unavailable");
                });
        }

        private void ConfirmRequest()
        {
            if (_disposed || _activeClient != null) return;
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
            SetConfirmEnabled(false);
            SetCancelEnabled(true);
            SetLocalizedResponse("localAi.response.pending");
            SetLocalizedStatus("localAi.status.starting");

            var client = new CompanionClient();
            DiagnosticLog.Detail("Local AI request started.");
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            _activeClient = client;
            RefreshLocalizedContent();
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
                SetCancelEnabled(false);
                SetLocalizedStatus("localAi.error.companion_start_failed");
            }
        }

        private void CompleteRequest(long generation, CompanionClient client, LocalAiResult result, long elapsedMs)
        {
            if (_disposed || !_requestGeneration.IsResponseCurrent(generation)) return;
            DiagnosticLog.Detail("Local AI request elapsedMs=" + Math.Max(0, elapsedMs).ToString(CultureInfo.InvariantCulture) + ".");
            if (ReferenceEquals(_activeClient, client)) _activeClient = null;
            SetCancelEnabled(false);

            if (result != null && result.Success)
            {
                SetResponseText(result.Content);
                SetLocalizedStatus("localAi.status.success", result.ModelName);
                DiagnosticLog.Detail("Local AI request completed | status=success.");
                RequestReviewView();
                return;
            }

            string errorCode = result == null ? "companion_request_failed" : result.ErrorCode;
            SetLocalizedResponse("localAi.response.none");
            SetLocalizedStatus("localAi.error." + (string.IsNullOrEmpty(errorCode) ? "generic" : errorCode));
            DiagnosticLog.Warning("Local AI request failed | status=" + GetSafeErrorCategory(errorCode) + ".");
            RequestReviewView();
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
                SetCancelEnabled(false);
                return;
            }

            _captureGeneration.Invalidate();
            _capturePending = false;
            _preparedRequest = null;
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.none";
            RefreshLocalizedContent();
            SetLocalizedStatus("localAi.status.discarded");
            SetConfirmEnabled(false);
            SetCancelEnabled(false);
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

        private void SetConfirmEnabled(bool value)
        {
            _confirmEnabled = value;
            RefreshLocalizedContent();
        }

        private void SetCancelEnabled(bool value)
        {
            _cancelEnabled = value;
            RefreshLocalizedContent();
        }

        private void SetLocalizedStatus(string key, params object[] arguments)
        {
            _statusLocalizationKey = key;
            _statusArguments = arguments == null ? new object[0] : (object[])arguments.Clone();
            RefreshLocalizedContent();
        }

        private void SetLocalizedResponse(string key)
        {
            _responseLocalizationKey = key;
            _responseText = string.Empty;
            RefreshLocalizedContent();
        }

        private void SetResponseText(string text)
        {
            _responseLocalizationKey = null;
            _responseText = text ?? string.Empty;
            RefreshLocalizedContent();
        }

        private void RequestReviewView()
        {
            try { if (ReviewRequested != null) ReviewRequested(); }
            catch { }
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
                _displayedPreviewRequest = null;
                _previewLocalizationKey = "localAi.preview.stale";
                RefreshLocalizedContent();
                SetConfirmEnabled(false);
                SetCancelEnabled(false);
                SetLocalizedStatus("localAi.status.reviewAgain");
            }
            if (_preparedRequest == null) return;
            _preparedRequest = null;
            _displayedPreviewRequest = null;
            _previewLocalizationKey = "localAi.preview.stale";
            RefreshLocalizedContent();
            SetConfirmEnabled(false);
            SetCancelEnabled(false);
            SetLocalizedStatus("localAi.status.reviewAgain");
        }

        private void SelectAllFields()
        {
            if (_disposed || _refreshingViews) return;
            bool changed = !_includeGameDate || !_includePopulation || !_includeFundsCents || !_includeResidentialDemand ||
                !_includeCommercialDemand || !_includeWorkplaceDemand || !_includeUtilities || !_includeZoning || !_includeTrafficTransit || !_includeServiceStatus;
            _includeGameDate = _includePopulation = _includeFundsCents = _includeResidentialDemand = _includeCommercialDemand = true;
            _includeWorkplaceDemand = _includeUtilities = _includeZoning = _includeTrafficTransit = _includeServiceStatus = true;
            if (changed) InvalidatePreview();
            RefreshLocalizedContent();
        }

        private void UpdateFlag(ref bool field, bool value)
        {
            if (_disposed || _refreshingViews || field == value) return;
            field = value;
            InvalidatePreview();
            RefreshLocalizedContent();
        }

        private void OnAllowLaunchChanged(bool value) { UpdateFlag(ref _allowLaunch, value); }
        private void OnModelChanged(string value)
        {
            value = value ?? string.Empty;
            if (_disposed || _refreshingViews || _modelName == value) return;
            _modelName = value;
            InvalidatePreview();
            RefreshLocalizedContent();
        }
        private void OnModelSubmitted(string value) { OnModelChanged(value); }
        private void OnResponseLimitChanged(string value)
        {
            value = value ?? string.Empty;
            if (_disposed || _refreshingViews || _responseLimitText == value) return;
            _responseLimitText = value;
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
            RefreshLocalizedContent();
        }
        private void OnResponseLimitSubmitted(string value) { OnResponseLimitChanged(value); }
        private void OnQuestionChanged(string value)
        {
            value = value ?? string.Empty;
            if (_disposed || _refreshingViews || _question == value) return;
            _question = value;
            InvalidatePreview();
            RefreshLocalizedContent();
        }
        private void OnQuestionSubmitted(string value) { OnQuestionChanged(value); }
        private void OnGameDateChanged(bool value) { UpdateFlag(ref _includeGameDate, value); }
        private void OnPopulationChanged(bool value) { UpdateFlag(ref _includePopulation, value); }
        private void OnFundsChanged(bool value) { UpdateFlag(ref _includeFundsCents, value); }
        private void OnResidentialChanged(bool value) { UpdateFlag(ref _includeResidentialDemand, value); }
        private void OnCommercialChanged(bool value) { UpdateFlag(ref _includeCommercialDemand, value); }
        private void OnWorkplaceChanged(bool value) { UpdateFlag(ref _includeWorkplaceDemand, value); }
        private void OnUtilitiesChanged(bool value) { UpdateFlag(ref _includeUtilities, value); }
        private void OnZoningChanged(bool value) { UpdateFlag(ref _includeZoning, value); }
        private void OnTrafficTransitChanged(bool value) { UpdateFlag(ref _includeTrafficTransit, value); }
        private void OnServiceStatusChanged(bool value) { UpdateFlag(ref _includeServiceStatus, value); }

    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Implementation update signed by OpenAI-Codex_ui_state_arch.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
