using System;
using AutomatedPublicTransportPlanning.Planning;

// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteEditRequestGateChecks
{
    public static void Run()
    {
        RouteEditRequestGate gate = new RouteEditRequestGate();
        RouteEditRequestTicket first;
        Require(gate.TryBegin(out first) && gate.IsBusy && gate.IsOwner(first), "first request owns the gate");
        RouteEditRequestTicket rejected;
        Require(!gate.TryBegin(out rejected) && rejected == null, "busy request cannot replace the owner");
        int published = 0;
        Require(gate.TryPublish(first, delegate { published++; }), "current result can publish");
        Require(published == 1, "publication runs once");
        gate.Reset(delegate { published = 0; });
        Require(!gate.IsBusy && !gate.IsOwner(first) && published == 0, "reset invalidates and clears the old request");
        RouteEditRequestTicket next;
        Require(gate.TryBegin(out next), "new city can begin another request");
        Require(!gate.TryPublish(first, delegate { published = 99; }), "old preview cannot publish in the new city");
        Require(!gate.Complete(first) && gate.IsBusy && gate.IsOwner(next), "old finally cannot unlock the new owner");
        Require(published == 0, "old callback did not execute");
        Require(!gate.IsOwner(null) && !gate.Complete(null), "null is never an owner");
        Require(gate.Complete(next) && !gate.IsBusy, "the actual owner can complete");
        Require(!gate.TryPublish(next, delegate { published++; }), "completed callbacks cannot publish later");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
