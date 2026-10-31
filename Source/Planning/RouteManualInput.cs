using System;
using System.Collections.ObjectModel;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    // Contributor: OpenAI-Codex_manual_input_tests
    internal struct RouteManualToken
    {
        public readonly int OriginalStopIndex;
        public readonly ushort SegmentId;
        public readonly bool Forward;

        public RouteManualToken(int originalStopIndex, ushort segmentId, bool forward)
        {
            OriginalStopIndex = originalStopIndex;
            SegmentId = segmentId;
            Forward = forward;
        }
    }

    internal static class RouteManualInput
    {
        public static bool TryParse(string input, int stopCount,
                                    out ReadOnlyCollection<RouteManualToken> tokens, out string reason)
        {
            // Deliberate test-first seam; the cloud checks must fail before implementation.
            throw new NotImplementedException("Manual route input validation is not implemented yet.");
        }
    }
}
