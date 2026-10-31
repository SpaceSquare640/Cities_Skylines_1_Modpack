using System;
using System.Collections.Generic;
using AutomatedPublicTransportPlanning.Planning;
using UnityEngine;

// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteEditPlanChecks
{
    public static void Run()
    {
        ExistingRouteStop[] originalStops = ExistingStops();
        ExistingRouteSnapshot original = new ExistingRouteSnapshot(17, 23, "Bus", originalStops);
        RouteStopSite[] targets = Targets();
        RouteEditPlan plan;
        string reason;
        Require(RouteEditPlan.TryCreate(original, RouteEditMode.Manual, targets, out plan, out reason), reason);
        Require(plan.Original.LineId == 17 && plan.Mode == RouteEditMode.Manual, "manual edit retains line identity and mode");
        originalStops[0] = new ExistingRouteStop(99, new Vector3(99, 0, 0));
        targets[0] = new RouteStopSite(99, 1, true, 0, new Vector3(99, 0, 0), false);
        Require(plan.Original.Stops[0].NodeId == 1 && plan.Stops[0].SegmentId == 10,
            "snapshots and plans must isolate caller arrays");
        Require(((IList<ExistingRouteStop>)plan.Original.Stops).IsReadOnly &&
                ((IList<RouteStopSite>)plan.Stops).IsReadOnly, "stop lists must be read-only");

        Require(RouteEditPlan.TryCreate(original, RouteEditMode.Automatic, Targets(), out plan, out reason), reason);
        Require(plan.Original.LineId == 17 && plan.Mode == RouteEditMode.Automatic,
            "automatic edit retains line identity and mode");
        RouteStopSite[] fourTargets = { Targets()[0], Targets()[1], Targets()[2], Site(40, 60) };
        Reject(original, RouteEditMode.Manual, fourTargets, "manual editing cannot add a stop");
        Reject(original, RouteEditMode.Automatic, fourTargets, "automatic editing cannot add a stop");

        Require(original.Matches(17, 23, "Bus", ExistingStops()), "unchanged line must match");
        Require(!original.Matches(18, 23, "Bus", ExistingStops()), "changed line id must be stale");
        Require(!original.Matches(17, 24, "Bus", ExistingStops()), "changed build index must be stale");
        Require(!original.Matches(17, 23, "Other Bus", ExistingStops()), "changed prefab must be stale");
        ExistingRouteStop[] live = ExistingStops();
        live[0] = new ExistingRouteStop(9, live[0].Position);
        Require(!original.Matches(17, 23, "Bus", live), "changed stop node must be stale");
        live = ExistingStops();
        live[0] = new ExistingRouteStop(1, new Vector3(0.000001f, 0, 0));
        Require(!original.Matches(17, 23, "Bus", live), "even a small position change must be stale");
        Require(!original.Matches(17, 23, "Bus", null), "missing live stops must be stale");

        Reject(null, RouteEditMode.Manual, Targets(), "missing original");
        Reject(new ExistingRouteSnapshot(0, 23, "Bus", ExistingStops()), RouteEditMode.Manual, Targets(), "zero line id");
        Reject(new ExistingRouteSnapshot(17, 23, "", ExistingStops()), RouteEditMode.Manual, Targets(), "missing prefab");
        Reject(original, (RouteEditMode)99, Targets(), "unknown mode");
        Reject(original, RouteEditMode.Manual, null, "missing target stops");
        live = ExistingStops();
        live[0] = new ExistingRouteStop(0, Vector3.zero);
        Reject(new ExistingRouteSnapshot(17, 23, "Bus", live), RouteEditMode.Manual, Targets(), "zero original stop node");
        live = ExistingStops();
        live[1] = new ExistingRouteStop(1, live[1].Position);
        Reject(new ExistingRouteSnapshot(17, 23, "Bus", live), RouteEditMode.Manual, Targets(), "duplicate original stop node");
        live = ExistingStops();
        live[0] = new ExistingRouteStop(1, new Vector3(float.NaN, 0, 0));
        Reject(new ExistingRouteSnapshot(17, 23, "Bus", live), RouteEditMode.Manual, Targets(), "nonfinite original position");

        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            targets = Targets();
            targets[0] = Site(10, value);
            Reject(original, RouteEditMode.Manual, targets, "nonfinite target position");
        }
        targets = Targets();
        targets[1] = Site(20, 2.5f);
        Reject(original, RouteEditMode.Manual, targets, "loop-closing distance boundary");
        targets = Targets();
        targets[1] = Site(10, 20);
        Reject(original, RouteEditMode.Manual, targets, "duplicate target lane");
        RouteStopSite[] two = { Site(10, 0), Site(20, 20) };
        Reject(new ExistingRouteSnapshot(17, 23, "Bus", new[] { ExistingStops()[0], ExistingStops()[1] }),
            RouteEditMode.Manual, two, "fewer than three stops");
        ExistingRouteStop[] sevenOriginal = new ExistingRouteStop[7];
        RouteStopSite[] sevenTargets = new RouteStopSite[7];
        for (int i = 0; i < 7; i++)
        {
            sevenOriginal[i] = new ExistingRouteStop((ushort)(i + 1), new Vector3(i * 20, 0, 0));
            sevenTargets[i] = Site((ushort)(10 + i), i * 20);
        }
        Reject(new ExistingRouteSnapshot(17, 23, "Bus", sevenOriginal), RouteEditMode.Automatic,
            sevenTargets, "more than six stops");
    }

    private static ExistingRouteStop[] ExistingStops()
    {
        return new[] { new ExistingRouteStop(1, Vector3.zero),
            new ExistingRouteStop(2, new Vector3(20, 0, 0)),
            new ExistingRouteStop(3, new Vector3(40, 0, 0)) };
    }

    private static RouteStopSite Site(ushort segment, float x)
    {
        return new RouteStopSite(segment, 1, true, 0, new Vector3(x, 0, 0), false);
    }

    private static RouteStopSite[] Targets()
    {
        return new[] { Site(10, 0), Site(20, 20), Site(30, 40) };
    }

    private static void Reject(ExistingRouteSnapshot original, RouteEditMode mode,
                               IList<RouteStopSite> targets, string description)
    {
        RouteEditPlan plan;
        string reason;
        Require(!RouteEditPlan.TryCreate(original, mode, targets, out plan, out reason), description);
        Require(plan == null && !string.IsNullOrEmpty(reason), "rejection must clear the plan and explain why: " + description);
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) { throw new InvalidOperationException(description); }
    }
}
