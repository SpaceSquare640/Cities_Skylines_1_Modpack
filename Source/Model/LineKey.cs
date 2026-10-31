// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
namespace AutomatedPublicTransportVehicleManagement.Model
{
    // A process-local observation identity, not a persistent game line generation.
    public struct LineKey
    {
        private readonly int sessionId;
        private readonly ushort lineId;
        private readonly long observationSequence;

        public LineKey(int sessionId, ushort lineId, long observationSequence)
        {
            this.sessionId = sessionId;
            this.lineId = lineId;
            this.observationSequence = observationSequence;
        }

        public int SessionId { get { return sessionId; } }
        public ushort LineId { get { return lineId; } }
        public long ObservationSequence { get { return observationSequence; } }
    }
}
