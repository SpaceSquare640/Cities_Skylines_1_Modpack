using System;
using System.Collections.Generic;
using ICities;
using UnityEngine;

namespace AIGaming
{
    public sealed class AIGamingMod : IUserMod
    {
        private LocalAiRequestController _localAiRequestController;
        private UIHelperBase _settingsHelper;
        private readonly List<Action> _localizedRefreshActions = new List<Action>();

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
        }

        public void OnDisabled()
        {
            if (_localAiRequestController != null)
            {
                _localAiRequestController.Shutdown();
                _localAiRequestController = null;
            }
            DiagnosticLog.Stop();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null)
            {
                return;
            }

            _settingsHelper = helper;
            _localizedRefreshActions.Clear();
            helper.AddDropdown(
                Localization.Get("settings.language"),
                Localization.GetLanguageNames(),
                Localization.SelectedLanguageIndex,
                OnLanguageSelectionChanged);

            UIHelperBase group = helper.AddGroup(
                Localization.Get("settings.title") + " - " + Localization.Get("settings.versionShort"));
            AddInformationRow(group, "settings.status");
            AddInformationRow(group, "settings.description");
            AddInformationRow(group, "settings.website");
            AddInteropStatusRows(group);
            group.AddButton(Localization.Get("settings.summaryButton"), CitySummary.QueueWrite);

            if (_localAiRequestController != null)
            {
                _localAiRequestController.Shutdown();
            }
            UIHelperBase localAiGroup = group.AddGroup(Localization.Get("localAi.title"));
            _localAiRequestController = new LocalAiRequestController(localAiGroup);
        }

        private void OnLanguageSelectionChanged(int index)
        {
            string previousLanguage = Localization.CurrentLanguage;
            if (!Localization.SetLanguage(index)) return;

            SettingsLayout.RelocalizeExistingControls(_settingsHelper, previousLanguage);
            for (int i = 0; i < _localizedRefreshActions.Count; i++)
            {
                _localizedRefreshActions[i]();
            }
            if (_localAiRequestController != null)
            {
                _localAiRequestController.RefreshLocalizedContent();
            }
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

        private static void IgnoreInformationClick()
        {
            // UIHelperBase exposes buttons but no static label control.
        }

        private void AddInformationRow(UIHelperBase group, string key)
        {
            AddInformationRow(group, delegate { return Localization.Get(key); });
        }

        private void AddInformationRow(UIHelperBase group, Func<string> getText)
        {
            ColossalFramework.UI.UIButton button = group.AddButton(getText(), IgnoreInformationClick) as ColossalFramework.UI.UIButton;
            SettingsLayout.FitButton(group, button);
            _localizedRefreshActions.Add(delegate
            {
                if (button == null) return;
                button.text = getText();
                SettingsLayout.FitButton(group, button);
            });
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
