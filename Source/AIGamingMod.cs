using ICities;
using UnityEngine;

namespace AIGaming
{
    public sealed class AIGamingMod : IUserMod
    {
        public string Name => "AI Gaming";

        public string Description =>
            "Early preview of a mod that will let a local or cloud AI model play Cities: Skylines, " +
            "or assist you while you play. This version only reads basic city information and does not control the game yet.";

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
            group.AddButton(Localization.Get("settings.summaryButton"), CitySummary.QueueWrite);
        }

        private static void IgnoreInformationClick()
        {
            // UIHelperBase exposes buttons but no static label control.
        }
    }
}
