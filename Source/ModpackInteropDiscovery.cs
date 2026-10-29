using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ColossalFramework.Plugins;
using UnityEngine;

namespace AIGaming
{
    internal enum InteropProviderState
    {
        NotInstalled,
        NotEnabled,
        InstalledWithoutInterface,
        ManifestInvalid,
        DiscoveryFailed,
        Ambiguous,
        InterfaceIncomplete,
        Incompatible,
        Compatible
    }

    internal sealed class InteropProviderStatus
    {
        internal string ProviderId { get; set; }
        internal string ProviderVersion { get; set; }
        internal string ContractVersion { get; set; }
        internal string Edition { get; set; }
        internal string CapabilityId { get; set; }
        internal string CapabilityVersion { get; set; }
        internal bool CapabilityEntryPointAvailable { get; set; }
        internal InteropProviderState State { get; set; }
    }

    internal static class ModpackInteropDiscovery
    {
        private const string AiImproveProviderId = "com.spacesquare640.ai-improve";
        private const string AiImproveCapabilityId = "com.spacesquare640.ai-improve.feature-status.read";
        private const string TransportProviderId = "com.spacesquare640.automated-public-transport-planning";
        private const string TransportCapabilityId = "com.spacesquare640.automated-public-transport-planning.plan-preview";
        private const string ManifestMethodName = "GetModpackInteropManifest";
        private const string InvokeMethodName = "InvokeCapability";
        private const int MaximumStringLength = 1024;
        private const int MaximumDictionaryEntries = 128;
        private const int MaximumArrayLength = 1024;
        private const int MaximumManifestCapabilities = 64;
        private const int MaximumNestingDepth = 4;

        private static readonly ProviderDefinition[] Providers =
        {
            new ProviderDefinition(
                AiImproveProviderId,
                AiImproveCapabilityId,
                new[] { "aiimprove", "aiimprovemod" }),
            new ProviderDefinition(
                TransportProviderId,
                TransportCapabilityId,
                new[]
                {
                    "transport",
                    "automatedpublictransportplanning",
                    "automatedpublictransportplanningmod"
                })
        };

        internal static IList<InteropProviderStatus> Discover()
        {
            DiagnosticLog.Detail("Interop discovery started.");

            var presence = new ProviderPresence[Providers.Length];
            var manifests = new List<ProviderManifest>();
            var statuses = new List<InteropProviderStatus>(Providers.Length);

            for (int i = 0; i < presence.Length; i++)
            {
                presence[i] = new ProviderPresence();
            }

            try
            {
                PluginManager manager = PluginManager.instance;
                if (manager == null)
                {
                    return CreateDiscoveryFailedStatuses();
                }

                foreach (PluginManager.PluginInfo plugin in manager.GetPluginsInfo())
                {
                    if (plugin == null)
                    {
                        continue;
                    }

                    object modInstance = plugin.userModInstance;
                    Type modType = modInstance == null ? null : modInstance.GetType();
                    string assemblyName = GetAssemblyName(modType);
                    int matchedProviderIndex = FindProviderByDisplayIdentity(plugin.name, modType, assemblyName);
                    if (matchedProviderIndex >= 0)
                    {
                        if (plugin.isEnabled)
                        {
                            presence[matchedProviderIndex].Enabled = true;
                        }
                        else
                        {
                            presence[matchedProviderIndex].Disabled = true;
                        }
                    }

                    if (!plugin.isEnabled || modInstance == null || modType == null)
                    {
                        continue;
                    }

                    MethodInfo manifestMethod = FindManifestMethod(modType);
                    if (manifestMethod == null)
                    {
                        continue;
                    }

                    MethodInfo invokeMethod = FindInvokeMethod(modType);
                    try
                    {
                        object rawManifest = manifestMethod.Invoke(modInstance, null);
                        IDictionary<string, object> manifest = rawManifest as IDictionary<string, object>;
                        if (manifest == null || !IsValidValue(manifest, 1))
                        {
                            if (matchedProviderIndex >= 0)
                            {
                                presence[matchedProviderIndex].InvalidManifest = true;
                            }

                            continue;
                        }

                        ProviderManifest parsed;
                        if (!TryParseManifest(manifest, invokeMethod != null, out parsed))
                        {
                            if (matchedProviderIndex >= 0)
                            {
                                presence[matchedProviderIndex].InvalidManifest = true;
                            }

                            continue;
                        }

                        manifests.Add(parsed);
                    }
                    catch (Exception)
                    {
                        if (matchedProviderIndex >= 0)
                        {
                            presence[matchedProviderIndex].DiscoveryFailed = true;
                        }

                        DiagnosticLog.Warning("Interop manifest read failed.");
                    }
                }
            }
            catch (Exception)
            {
                DiagnosticLog.Warning("Interop discovery failed.");
                return CreateDiscoveryFailedStatuses();
            }

            for (int i = 0; i < Providers.Length; i++)
            {
                statuses.Add(BuildStatus(Providers[i], presence[i], manifests));
                LogStatus(statuses[statuses.Count - 1]);
            }

            DiagnosticLog.Detail("Interop discovery complete.");
            return statuses;
        }

        private static IList<InteropProviderStatus> CreateDiscoveryFailedStatuses()
        {
            var statuses = new List<InteropProviderStatus>(Providers.Length);
            for (int i = 0; i < Providers.Length; i++)
            {
                var status = new InteropProviderStatus
                {
                    ProviderId = Providers[i].ProviderId,
                    CapabilityId = Providers[i].CapabilityId,
                    State = InteropProviderState.DiscoveryFailed
                };
                statuses.Add(status);
                LogStatus(status);
            }

            DiagnosticLog.Detail("Interop discovery complete.");
            return statuses;
        }

        private static InteropProviderStatus BuildStatus(
            ProviderDefinition provider,
            ProviderPresence presence,
            IList<ProviderManifest> manifests)
        {
            ProviderManifest match = null;
            int matchCount = 0;
            for (int i = 0; i < manifests.Count; i++)
            {
                if (string.Equals(manifests[i].ProviderId, provider.ProviderId, StringComparison.Ordinal))
                {
                    match = manifests[i];
                    matchCount++;
                }
            }

            if (matchCount > 1)
            {
                return new InteropProviderStatus
                {
                    ProviderId = provider.ProviderId,
                    CapabilityId = provider.CapabilityId,
                    State = InteropProviderState.Ambiguous
                };
            }

            if (match != null)
            {
                CapabilityManifest capability = FindCapability(provider, match);
                InteropProviderState state = EvaluateCompatibility(match, capability);
                if (state == InteropProviderState.Compatible && !match.HasInvokeCapability)
                {
                    state = InteropProviderState.InterfaceIncomplete;
                }

                return new InteropProviderStatus
                {
                    ProviderId = match.ProviderId,
                    ProviderVersion = match.ProviderVersion,
                    ContractVersion = match.ContractVersion,
                    Edition = match.Edition,
                    CapabilityId = provider.CapabilityId,
                    CapabilityVersion = capability == null ? null : capability.Version,
                    CapabilityEntryPointAvailable = match.HasInvokeCapability,
                    State = state
                };
            }

            InteropProviderState fallbackState;
            if (presence.Enabled)
            {
                fallbackState = presence.DiscoveryFailed
                    ? InteropProviderState.DiscoveryFailed
                    : presence.InvalidManifest
                        ? InteropProviderState.ManifestInvalid
                        : InteropProviderState.InstalledWithoutInterface;
            }
            else if (presence.Disabled)
            {
                fallbackState = InteropProviderState.NotEnabled;
            }
            else
            {
                fallbackState = InteropProviderState.NotInstalled;
            }

            return new InteropProviderStatus
            {
                ProviderId = provider.ProviderId,
                CapabilityId = provider.CapabilityId,
                State = fallbackState
            };
        }

        private static CapabilityManifest FindCapability(ProviderDefinition provider, ProviderManifest manifest)
        {
            for (int i = 0; i < manifest.Capabilities.Count; i++)
            {
                if (string.Equals(manifest.Capabilities[i].CapabilityId, provider.CapabilityId, StringComparison.Ordinal))
                {
                    return manifest.Capabilities[i];
                }
            }

            return null;
        }

        private static InteropProviderState EvaluateCompatibility(
            ProviderManifest manifest,
            CapabilityManifest capability)
        {
            if (!IsContractVersionCompatible(manifest.ContractVersion) || capability == null ||
                !string.Equals(capability.Access, "ReadOnly", StringComparison.Ordinal) ||
                !IsCapabilityVersionCompatible(capability.Version))
            {
                return InteropProviderState.Incompatible;
            }

            return InteropProviderState.Compatible;
        }

        private static bool TryParseManifest(
            IDictionary<string, object> manifest,
            bool hasInvokeCapability,
            out ProviderManifest parsed)
        {
            parsed = null;
            string providerId;
            string providerVersion;
            string contractVersion;
            string edition;
            object capabilitiesValue;

            if (!TryGetString(manifest, "providerId", out providerId) ||
                !TryGetString(manifest, "providerVersion", out providerVersion) ||
                !TryGetString(manifest, "contractVersion", out contractVersion) ||
                !TryGetString(manifest, "edition", out edition) ||
                !manifest.TryGetValue("capabilities", out capabilitiesValue))
            {
                return false;
            }

            if (providerId.Length == 0 || !IsSafeVersionString(providerVersion) ||
                !IsSafeVersionString(contractVersion) ||
                (edition != "release" && edition != "development"))
            {
                return false;
            }

            object[] capabilityItems = capabilitiesValue as object[];
            if (capabilityItems == null || capabilityItems.Length > MaximumManifestCapabilities)
            {
                return false;
            }

            var capabilities = new List<CapabilityManifest>(capabilityItems.Length);
            for (int i = 0; i < capabilityItems.Length; i++)
            {
                IDictionary<string, object> capabilityDictionary = capabilityItems[i] as IDictionary<string, object>;
                string capabilityId;
                string version;
                string access;
                if (capabilityDictionary == null ||
                    !TryGetString(capabilityDictionary, "capabilityId", out capabilityId) ||
                    !TryGetString(capabilityDictionary, "version", out version) ||
                    !TryGetString(capabilityDictionary, "access", out access) ||
                    capabilityId.Length == 0 || !IsSafeVersionString(version) ||
                    (access != "ReadOnly" && access != "Write"))
                {
                    return false;
                }

                capabilities.Add(new CapabilityManifest(capabilityId, version, access));
            }

            parsed = new ProviderManifest(
                providerId,
                providerVersion,
                contractVersion,
                edition,
                capabilities,
                hasInvokeCapability);
            return true;
        }

        private static bool IsValidValue(object value, int depth)
        {
            if (value == null || value is int || value is long || value is bool || value is string)
            {
                return value == null || !(value is string) || ((string)value).Length <= MaximumStringLength;
            }

            if (value is double)
            {
                double number = (double)value;
                return !double.IsNaN(number) && !double.IsInfinity(number);
            }

            if (depth > MaximumNestingDepth)
            {
                return false;
            }

            IDictionary<string, object> dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                if (dictionary.Count > MaximumDictionaryEntries)
                {
                    return false;
                }

                foreach (KeyValuePair<string, object> entry in dictionary)
                {
                    if (entry.Key == null || entry.Key.Length > MaximumStringLength ||
                        !IsValidValue(entry.Value, depth + 1))
                    {
                        return false;
                    }
                }

                return true;
            }

            object[] array = value as object[];
            if (array != null)
            {
                if (array.Length > MaximumArrayLength)
                {
                    return false;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (!IsValidValue(array[i], depth + 1))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static bool TryGetString(IDictionary<string, object> values, string key, out string result)
        {
            object value;
            if (values.TryGetValue(key, out value) && value is string && ((string)value).Length <= MaximumStringLength)
            {
                result = (string)value;
                return true;
            }

            result = null;
            return false;
        }

        private static MethodInfo FindManifestMethod(Type type)
        {
            MethodInfo method = type.GetMethod(
                ManifestMethodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            return method != null && method.IsPublic && !method.IsStatic &&
                   method.ReturnType == typeof(IDictionary<string, object>)
                ? method
                : null;
        }

        private static MethodInfo FindInvokeMethod(Type type)
        {
            Type dictionaryType = typeof(IDictionary<string, object>);
            Type callbackType = typeof(Action<IDictionary<string, object>>);
            MethodInfo method = type.GetMethod(
                InvokeMethodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { dictionaryType, callbackType },
                null);

            return method != null && method.IsPublic && !method.IsStatic && method.ReturnType == typeof(void)
                ? method
                : null;
        }

        private static bool IsContractVersionCompatible(string version)
        {
            int major;
            int minor;
            if (!TryParseMajorMinor(version, out major, out minor))
            {
                return false;
            }

            return major == 1 && minor >= 0;
        }

        private static bool IsCapabilityVersionCompatible(string version)
        {
            int major;
            if (!TryParseSemanticVersion(version, out major))
            {
                return false;
            }

            return major == 1;
        }

        private static bool TryParseMajorMinor(string version, out int major, out int minor)
        {
            major = 0;
            minor = 0;
            if (string.IsNullOrEmpty(version) || version.Length > 64)
            {
                return false;
            }

            string[] parts = version.Split('.');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major) &&
                   int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor) &&
                   major >= 0 && minor >= 0;
        }

        private static bool TryParseSemanticVersion(string version, out int major)
        {
            major = 0;
            if (!IsSafeVersionString(version))
            {
                return false;
            }

            int suffixIndex = version.IndexOfAny(new[] { '-', '+' });
            string numericVersion = suffixIndex < 0 ? version : version.Substring(0, suffixIndex);
            string[] parts = numericVersion.Split('.');
            int minor;
            int patch;
            return parts.Length == 3 &&
                   int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major) &&
                   int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor) &&
                   int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out patch) &&
                   major >= 0 && minor >= 0 && patch >= 0;
        }

        private static bool IsSafeVersionString(string version)
        {
            if (string.IsNullOrEmpty(version) || version.Length > 64)
            {
                return false;
            }

            for (int i = 0; i < version.Length; i++)
            {
                char character = version[i];
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'A' && character <= 'Z') &&
                    !(character >= 'a' && character <= 'z') &&
                    character != '.' && character != '+' && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindProviderByDisplayIdentity(string pluginName, Type modType, string assemblyName)
        {
            for (int i = 0; i < Providers.Length; i++)
            {
                if (MatchesIdentity(pluginName, Providers[i].IdentityAliases) ||
                    MatchesIdentity(modType == null ? null : modType.Name, Providers[i].IdentityAliases) ||
                    MatchesIdentity(assemblyName, Providers[i].IdentityAliases))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool MatchesIdentity(string value, IList<string> aliases)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string normalized = NormalizeIdentity(value);
            for (int i = 0; i < aliases.Count; i++)
            {
                if (string.Equals(normalized, aliases[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeIdentity(string value)
        {
            var normalized = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsLetterOrDigit(character))
                {
                    normalized.Append(char.ToLowerInvariant(character));
                }
            }

            return normalized.ToString();
        }

        private static string GetAssemblyName(Type modType)
        {
            try
            {
                return modType == null || modType.Assembly == null
                    ? null
                    : modType.Assembly.GetName().Name;
            }
            catch
            {
                return null;
            }
        }

        private static void LogStatus(InteropProviderStatus status)
        {
            if (status.State == InteropProviderState.Compatible ||
                status.State == InteropProviderState.InterfaceIncomplete ||
                status.State == InteropProviderState.Incompatible)
            {
                DiagnosticLog.Detail("Interop provider=" + DiagnosticLogPolicy.GetProviderLabel(status.ProviderId) + " | status=" + status.State.ToString() + ".");
                return;
            }

            DiagnosticLog.Detail("Interop provider=" + DiagnosticLogPolicy.GetProviderLabel(status.ProviderId) + " | unavailable: reason=" + GetReasonCode(status.State) + ".");
        }

        private static string GetReasonCode(InteropProviderState state)
        {
            switch (state)
            {
                case InteropProviderState.NotInstalled:
                    return "not-installed";
                case InteropProviderState.NotEnabled:
                    return "not-enabled";
                case InteropProviderState.InstalledWithoutInterface:
                    return "no-compatible-interface";
                case InteropProviderState.ManifestInvalid:
                    return "manifest-invalid";
                case InteropProviderState.DiscoveryFailed:
                    return "discovery-failed";
                case InteropProviderState.Ambiguous:
                    return "duplicate-provider";
                case InteropProviderState.InterfaceIncomplete:
                    return "capability-entrypoint-unavailable";
                case InteropProviderState.Incompatible:
                    return "version-or-capability-incompatible";
                default:
                    return "unavailable";
            }
        }

        private sealed class ProviderDefinition
        {
            internal ProviderDefinition(string providerId, string capabilityId, string[] identityAliases)
            {
                ProviderId = providerId;
                CapabilityId = capabilityId;
                IdentityAliases = identityAliases;
            }

            internal string ProviderId { get; private set; }
            internal string CapabilityId { get; private set; }
            internal IList<string> IdentityAliases { get; private set; }
        }

        private sealed class ProviderPresence
        {
            internal bool Enabled;
            internal bool Disabled;
            internal bool InvalidManifest;
            internal bool DiscoveryFailed;
        }

        private sealed class ProviderManifest
        {
            internal ProviderManifest(
                string providerId,
                string providerVersion,
                string contractVersion,
                string edition,
                IList<CapabilityManifest> capabilities,
                bool hasInvokeCapability)
            {
                ProviderId = providerId;
                ProviderVersion = providerVersion;
                ContractVersion = contractVersion;
                Edition = edition;
                Capabilities = capabilities;
                HasInvokeCapability = hasInvokeCapability;
            }

            internal string ProviderId { get; private set; }
            internal string ProviderVersion { get; private set; }
            internal string ContractVersion { get; private set; }
            internal string Edition { get; private set; }
            internal IList<CapabilityManifest> Capabilities { get; private set; }
            internal bool HasInvokeCapability { get; private set; }
        }

        private sealed class CapabilityManifest
        {
            internal CapabilityManifest(string capabilityId, string version, string access)
            {
                CapabilityId = capabilityId;
                Version = version;
                Access = access;
            }

            internal string CapabilityId { get; private set; }
            internal string Version { get; private set; }
            internal string Access { get; private set; }
        }
    }
}
