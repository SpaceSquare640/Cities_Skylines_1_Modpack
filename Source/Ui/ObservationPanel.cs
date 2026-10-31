// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using ICities;
using ColossalFramework.UI;
using AutomatedPublicTransportVehicleManagement.Lifecycle;

namespace AutomatedPublicTransportVehicleManagement.Ui
{
    internal static class ObservationPanel
    {
        private static UITextField status;
        internal static void Build(UIHelperBase helper)
        {
            Release();
            UIHelperBase group = helper.AddGroup("Read-only bus observations");
            group.AddButton("Refresh bus census", CitySession.Request);
            status = group.AddTextfield("Historical observation (no control)", CitySession.Display, null) as UITextField;
            if (status != null) { status.multiline = true; status.readOnly = true; status.height = 320f; }
        }
        internal static void Update()
        {
            UITextField field = status;
            if (field == null) return;
            string text = CitySession.Display;
            if (field.text != text) field.text = text;
        }
        internal static void Release() { status = null; }
    }
}
