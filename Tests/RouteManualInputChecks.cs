using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutomatedPublicTransportPlanning.Planning;

// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
// Contributor: OpenAI-Codex_manual_input_tests
internal static class RouteManualInputChecks
{
    public static void Run()
    {
        ReadOnlyCollection<RouteManualToken> tokens = Parse("1,3,2", 3);
        Require(tokens.Count == 3 && tokens[0].OriginalStopIndex == 1 &&
            tokens[1].OriginalStopIndex == 3 && tokens[2].OriginalStopIndex == 2,
            "manual order must retain each selected one-based index");
        Require(tokens[0].SegmentId == 0, "an original-stop token cannot also select a road");

        tokens = Parse(" f123 ,b456,3 ", 3);
        Require(tokens[0].OriginalStopIndex == 0 && tokens[0].SegmentId == 123 && tokens[0].Forward,
            "forward road tokens must retain the selected segment and direction");
        Require(tokens[1].OriginalStopIndex == 0 && tokens[1].SegmentId == 456 && !tokens[1].Forward,
            "backward road tokens must retain the selected segment and direction");
        Require(tokens[2].OriginalStopIndex == 3, "mixed road and original-stop tokens are supported");
        tokens = Parse("f1,b1,3", 3);
        Require(tokens[0].Forward && !tokens[1].Forward && tokens[0].SegmentId == tokens[1].SegmentId,
            "opposite directions on one captured segment remain distinct input tokens");
        Require(Parse("f65535,2,3", 3)[0].SegmentId == ushort.MaxValue, "maximum segment ID is valid input");

        for (int count = 3; count <= 6; count++)
        {
            List<string> indices = new List<string>();
            for (int i = 1; i <= count; i++) { indices.Add(i.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            Require(Parse(string.Join(",", indices.ToArray()), count).Count == count,
                "every supported original stop count must parse");
        }
        Require(Parse("1,2,3".PadRight(128), 3).Count == 3, "the 128-character limit is inclusive");

        string[] malformed = {
            null, string.Empty, "   ", ",2,3", "1,,3", "1,2,", "1,2", "1,2,3,4",
            "0,2,3", "-1,2,3", "+1,2,3", "4,2,3", "999999999999999999999,2,3",
            "f0,2,3", "b0,2,3", "f65536,2,3", "f-1,2,3", "f+1,2,3", "fNaN,2,3",
            "b999999999999999999999,2,3", "f,2,3", "1,1,3", "1,01,3", "f1,f001,3",
            "b1,b001,3", "1,2,3".PadRight(129)
        };
        foreach (string input in malformed) { Reject(input, 3); }
        Reject("1,2", 2);
        Reject("1,2,3,4,5,6,7", 7);
        Reject("1,2,3", 0);
        Reject("1,2,3", -1);
        Console.WriteLine("PASS manual input: order, directional roads, bounds, exact count, malformed input and normalized duplicates");
    }

    private static ReadOnlyCollection<RouteManualToken> Parse(string input, int stopCount)
    {
        ReadOnlyCollection<RouteManualToken> tokens;
        string reason;
        Require(RouteManualInput.TryParse(input, stopCount, out tokens, out reason),
            "valid input must parse: " + input + "; " + reason);
        Require(tokens != null && string.IsNullOrEmpty(reason), "valid parsing must produce tokens without an error");
        return tokens;
    }

    private static void Reject(string input, int stopCount)
    {
        ReadOnlyCollection<RouteManualToken> tokens = new List<RouteManualToken> {
            new RouteManualToken(1, 0, false) }.AsReadOnly();
        string reason = "previous error";
        Require(!RouteManualInput.TryParse(input, stopCount, out tokens, out reason),
            "invalid input must be rejected: " + input + " (count=" + stopCount + ")");
        Require(tokens == null && !string.IsNullOrEmpty(reason),
            "rejection must clear old tokens and explain why");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
