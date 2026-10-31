using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;

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
            tokens = null;
            reason = string.Empty;
            if (!RouteCreationRules.IsStopCountAllowed(stopCount))
            { reason = "Manual editing supports three to six existing stops."; return false; }
            if (string.IsNullOrEmpty(input) || input.Length > 128)
            { reason = "Enter a manual stop list of at most 128 characters."; return false; }
            string[] parts = input.Split(',');
            if (parts.Length != stopCount)
            { reason = "Provide exactly one token for each existing stop."; return false; }
            List<RouteManualToken> parsed = new List<RouteManualToken>(stopCount);
            HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
            foreach (string part in parts)
            {
                string token = part.Trim();
                bool road = token.Length > 0 && (token[0] == 'f' || token[0] == 'b');
                int value;
                if (!int.TryParse(road ? token.Substring(1) : token, NumberStyles.None, CultureInfo.InvariantCulture, out value) || value < 1 || value > (road ? ushort.MaxValue : stopCount))
                { reason = "Use one-based stop indices or f<segment>/b<segment> with segment IDs 1–65535."; return false; }
                string key = (road ? token.Substring(0, 1) : "original:") + value.ToString(CultureInfo.InvariantCulture);
                if (!selected.Add(key))
                { reason = "Each original stop or directional road token may appear only once."; return false; }
                parsed.Add(road ? new RouteManualToken(0, (ushort)value, token[0] == 'f') : new RouteManualToken(value, 0, false));
            }
            tokens = parsed.AsReadOnly();
            return true;
        }
    }
}
