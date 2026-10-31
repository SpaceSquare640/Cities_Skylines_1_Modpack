// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using AutomatedPublicTransportVehicleManagement.Model;

namespace AutomatedPublicTransportVehicleManagement.State
{
    public static class ObservationGate
    {
        // This only validates a published snapshot's local context. It grants no write authority.
        public static bool IsCurrent(LineKey key, int currentSessionId, long currentSequence)
        {
            return key.SessionId > 0 && key.LineId != 0 && key.ObservationSequence > 0 &&
                key.SessionId == currentSessionId && key.ObservationSequence == currentSequence;
        }
    }
}
