// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using System;
using AutomatedPublicTransportVehicleManagement.Model;
using AutomatedPublicTransportVehicleManagement.State;

internal static class ObservationChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static int Main()
    {
        try
        {
            LineKey key = new LineKey(1, 7, 10);
            Check(ObservationGate.IsCurrent(key, 1, 10), "CurrentKey_ReturnsTrue");
            Check(!ObservationGate.IsCurrent(key, 2, 10), "SameLineDifferentCity_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(key, 1, 11), "OlderSequence_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(key, 1, 9), "FutureSequence_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(new LineKey(), 0, 0), "DefaultKey_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(new LineKey(1, 0, 10), 1, 10), "ZeroLine_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(new LineKey(0, 7, 10), 0, 10), "InactiveSession_ReturnsFalse");
            Check(!ObservationGate.IsCurrent(new LineKey(1, 7, 0), 1, 0), "UnpublishedSequence_ReturnsFalse");
            LineObservation unknown = new LineObservation(key, null, null, ObservationQuality.Unavailable);
            Check(!unknown.TargetVehicles.HasValue && !unknown.ActualVehicles.HasValue,
                "UnknownCount_RemainsNull");
            LineObservation zero = new LineObservation(key, 0, 0, ObservationQuality.Valid);
            Check(zero.TargetVehicles == 0 && zero.ActualVehicles == 0,
                "ZeroCount_RemainsZero");
            Check(unknown.Quality == ObservationQuality.Unavailable,
                "UnavailableQuality_Preserved");
            LineObservation partial = new LineObservation(key, null, 4, ObservationQuality.Incomplete);
            Check(!partial.TargetVehicles.HasValue && partial.ActualVehicles == 4,
                "PartialObservation_DoesNotInventTarget");
            Check(partial.Key.SessionId == 1 && partial.Key.LineId == 7 &&
                partial.Key.ObservationSequence == 10, "SnapshotIdentity_Preserved");
            Console.WriteLine("All " + checks + " observation checks passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL " + exception.Message);
            return 1;
        }
    }
}
