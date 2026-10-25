namespace AIImprove
{
    // Signed: OpenAI-Codex (2026-10-25).
    // Compiled only into the Development build. This exposes no mutable tracker state and performs
    // a non-allocating lookup for the sanitation lifecycle probe.
    internal static partial class SanitationIdleSeekTracker
    {
        internal static bool TryGetAssignedBuilding(
            TransferManager.TransferReason material, ushort vehicleId, out ushort buildingId)
        {
            return AssignmentsFor(material).TryGetValue(vehicleId, out buildingId);
        }
    }
}
