using System;
using System.Collections.Generic;
using ICities;
using UnityEngine;

namespace AIGaming
{
    public sealed class AIGamingMod : IUserMod
    {
        private LocalAiRequestController _localAiRequestController;
        private readonly List<Action> _localizedRefreshActions = new List<Action>();
        private readonly List<SectionNavigation> _navigations = new List<SectionNavigation>();

        public string Name => "AI Gaming";

        public string Description =>
            CompanionBuildInfo.Edition == "development"
            ? "Development Version. This version has DevTool. Be careful; the development version can let your save be broken."
            :
            "Early preview of a mod that will let a local or cloud AI model play Cities: Skylines, " +
            "or assist you while you play. This version can send selected read-only city fields to a local Ollama " +
            "model through an optional companion after per-request confirmation. It does not connect to cloud models " +
            "or control the game.";

        public void OnEnabled()
        {
            DiagnosticLog.Start();
            InGameUiHost.OpenRequested = OpenInGame;
            InGameUiHost.CityUnloaded = OnCityUnloaded;
        }

        public void OnDisabled()
        {
            if (_localAiRequestController != null)
            {
                _localAiRequestController.Shutdown();
                _localAiRequestController = null;
            }
            InGameUiHost.OpenRequested = null;
            InGameUiHost.CityUnloaded = null;
            InGameUiHost.Destroy();
            for (int i = 0; i < _navigations.Count; i++) _navigations[i].Dispose();
            _navigations.Clear();
            DiagnosticLog.Stop();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null)
            {
                return;
            }

            _localizedRefreshActions.Clear();
            ColossalFramework.UI.UIDropDown language = helper.AddDropdown(
                Localization.Get("settings.language"),
                Localization.GetLanguageNames(),
                Localization.SelectedLanguageIndex,
                OnLanguageSelectionChanged) as ColossalFramework.UI.UIDropDown;
            _localizedRefreshActions.Add(delegate { SettingsLayout.SetControlLabel(language, Localization.Get("settings.language")); });

            UIHelperBase group = helper.AddGroup("AI Gaming");
            AddInformationRow(group, "settings.status");
            AttachRequestView(group);
            AddInformationRow(group, "settings.description");
            AddInformationRow(group, "settings.website");
            AddInteropStatusRows(group);
            ColossalFramework.UI.UIButton snapshot = group.AddButton(Localization.Get("settings.summaryButton"), CitySummary.QueueWrite) as ColossalFramework.UI.UIButton;
            _localizedRefreshActions.Add(delegate { if (snapshot != null) snapshot.text = Localization.Get("settings.summaryButton"); });

        }

        private void OnLanguageSelectionChanged(int index)
        {
            if (!Localization.SetLanguage(index)) return;
            InGameUiHost.RefreshLanguage();

            for (int i = 0; i < _localizedRefreshActions.Count; i++)
            {
                _localizedRefreshActions[i]();
            }
            if (_localAiRequestController != null)
            {
                _localAiRequestController.RefreshLocalizedContent();
            }
            for (int i = _navigations.Count - 1; i >= 0; i--)
            {
                if (!_navigations[i].IsAlive) { _navigations[i].Dispose(); _navigations.RemoveAt(i); }
                else _navigations[i].RefreshLanguage();
            }
        }

        private LocalAiRequestController GetController()
        {
            if (_localAiRequestController == null)
            {
                _localAiRequestController = new LocalAiRequestController();
                _localAiRequestController.ReviewRequested += ShowReview;
            }
            return _localAiRequestController;
        }

        private void AttachRequestView(UIHelperBase group)
        {
            PruneNavigations();
            var navigation = new SectionNavigation(group);
            _navigations.Add(navigation);
            GetController().Attach(navigation.Ask, navigation.Data, navigation.Review);
        }

        private void OpenInGame() { InGameUiHost.Open(AttachRequestView); }

        private void OnCityUnloaded()
        {
            if (_localAiRequestController != null) _localAiRequestController.OnCityUnload();
        }

        private void ShowReview()
        {
            for (int i = _navigations.Count - 1; i >= 0; i--)
            {
                if (!_navigations[i].IsAlive) { _navigations[i].Dispose(); _navigations.RemoveAt(i); }
                else _navigations[i].ShowReview();
            }
        }

        private void PruneNavigations()
        {
            for (int i = _navigations.Count - 1; i >= 0; i--)
                if (!_navigations[i].IsAlive) { _navigations[i].Dispose(); _navigations.RemoveAt(i); }
        }

        private void AddInteropStatusRows(UIHelperBase group)
        {
            AddInformationRow(group, "settings.interop.title");

            try
            {
                IList<InteropProviderStatus> statuses = ModpackInteropDiscovery.Discover();
                for (int i = 0; i < statuses.Count; i++)
                {
                    InteropProviderStatus status = statuses[i];
                    AddInformationRow(group, delegate { return GetInteropStatusText(status); });
                }
            }
            catch
            {
                AddInformationRow(group, "interop.aiImprove.discoveryFailed");
                AddInformationRow(group, "interop.transport.discoveryFailed");
            }
        }

        private static string GetInteropStatusText(InteropProviderStatus status)
        {
            bool isAiImprove = status.ProviderId == "com.spacesquare640.ai-improve";
            string prefix = isAiImprove ? "interop.aiImprove." : "interop.transport.";

            switch (status.State)
            {
                case InteropProviderState.NotInstalled:
                    return Localization.Get(prefix + "notInstalled");
                case InteropProviderState.NotEnabled:
                    return Localization.Get(prefix + "notEnabled");
                case InteropProviderState.InstalledWithoutInterface:
                    return Localization.Get(prefix + "noInterface");
                case InteropProviderState.ManifestInvalid:
                    return Localization.Get(prefix + "manifestInvalid");
                case InteropProviderState.DiscoveryFailed:
                    return Localization.Get(prefix + "discoveryFailed");
                case InteropProviderState.Ambiguous:
                    return Localization.Get(prefix + "ambiguous");
                case InteropProviderState.InterfaceIncomplete:
                    return Localization.Get(
                        prefix + "interfaceIncomplete",
                        GetDisplayValue(status.ProviderVersion),
                        GetDisplayValue(status.ContractVersion),
                        GetDisplayValue(status.Edition),
                        GetDisplayValue(status.CapabilityVersion));
                case InteropProviderState.Incompatible:
                    return Localization.Get(
                        prefix + "incompatible",
                        GetDisplayValue(status.ProviderVersion),
                        GetDisplayValue(status.ContractVersion),
                        GetDisplayValue(status.Edition),
                        GetDisplayValue(status.CapabilityVersion));
                case InteropProviderState.Compatible:
                    return Localization.Get(
                        prefix + "compatible",
                        GetDisplayValue(status.ProviderVersion),
                        GetDisplayValue(status.ContractVersion),
                        GetDisplayValue(status.Edition),
                        GetDisplayValue(status.CapabilityVersion));
                default:
                    return Localization.Get(prefix + "discoveryFailed");
            }
        }

        private static string GetDisplayValue(string value)
        {
            return string.IsNullOrEmpty(value) ? Localization.Get("interop.unknown") : value;
        }

        private void AddInformationRow(UIHelperBase group, string key)
        {
            AddInformationRow(group, delegate { return Localization.Get(key); });
        }

        private void AddInformationRow(UIHelperBase group, Func<string> getText)
        {
            ColossalFramework.UI.UILabel label = SettingsLayout.AddInfoLabel(group, getText());
            _localizedRefreshActions.Add(delegate
            {
                SettingsLayout.SetInfoLabel(group, label, getText());
            });
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
