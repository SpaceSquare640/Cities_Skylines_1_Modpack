// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
namespace AutomatedPublicTransportVehicleManagement.Model
{
    public enum ObservationQuality
    {
        Valid,
        Unavailable,
        InvalidPrefab,
        Incomplete
    }

    // Null counts are unknown; zero is a measured value. No native objects are retained.
    public sealed class LineObservation
    {
        private readonly LineKey key;
        private readonly int? targetVehicles;
        private readonly int? actualVehicles;
        private readonly ObservationQuality quality;

        public LineObservation(LineKey key, int? targetVehicles, int? actualVehicles,
            ObservationQuality quality)
        {
            this.key = key;
            this.targetVehicles = targetVehicles;
            this.actualVehicles = actualVehicles;
            this.quality = quality;
        }

        public LineKey Key { get { return key; } }
        public int? TargetVehicles { get { return targetVehicles; } }
        public int? ActualVehicles { get { return actualVehicles; } }
        public ObservationQuality Quality { get { return quality; } }
    }
}
