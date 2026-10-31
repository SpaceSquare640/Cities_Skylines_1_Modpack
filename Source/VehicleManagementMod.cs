// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using ICities;
using AutomatedPublicTransportVehicleManagement.Ui;

namespace AutomatedPublicTransportVehicleManagement
{
    public sealed class VehicleManagementMod : IUserMod
    {
        public string Name { get { return "Automated Public Transport Vehicle Management"; } }
        public string Description { get { return "Read-only city-bus vehicle census. No automatic allocation or dispatch is implemented."; } }
        public void OnSettingsUI(UIHelperBase helper) { ObservationPanel.Build(helper); }
    }
}
