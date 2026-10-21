using System.Collections.Generic;
using ICities;
using UnityEngine;

namespace AIGaming
{
    public sealed class AIGamingMod : IUserMod
    {
        public string Name => "AI Gaming";

        public string Description =>
            "Early preview of a mod that will let a local or cloud AI model play Cities: Skylines, " +
            "or assist you while you play. This version reads basic city information and optional Modpack " +
            "interop manifests only; it does not call other mod capabilities or control the game.";

        public void OnEnabled()
        {
            Debug.Log("[AI Gaming] Early preview loaded.");
        }

        public void OnDisabled()
        {
            Debug.Log("[AI Gaming] Mod disabled.");
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null)
            {
                return;
            }

            UIHelperBase group = helper.AddGroup(
                Localization.Get("settings.title") + " - " + Localization.Get("settings.versionShort"));
            group.AddButton(Localization.Get("settings.status"), IgnoreInformationClick);
            group.AddButton(Localization.Get("settings.description"), IgnoreInformationClick);
            group.AddButton(Localization.Get("settings.website"), IgnoreInformationClick);
            AddInteropStatusRows(group);
            group.AddButton(Localization.Get("settings.summaryButton"), CitySummary.QueueWrite);
        }

        private static void AddInteropStatusRows(UIHelperBase group)
        {
            group.AddButton(Localization.Get("settings.interop.title"), IgnoreInformationClick);

            try
            {
                IList<InteropProviderStatus> statuses = ModpackInteropDiscovery.Discover();
                for (int i = 0; i < statuses.Count; i++)
                {
                    group.AddButton(GetInteropStatusText(statuses[i]), IgnoreInformationClick);
                }
            }
            catch
            {
                group.AddButton(Localization.Get("interop.aiImprove.discoveryFailed"), IgnoreInformationClick);
                group.AddButton(Localization.Get("interop.transport.discoveryFailed"), IgnoreInformationClick);
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
    }
}
