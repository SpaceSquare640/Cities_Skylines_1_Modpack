using System;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal interface IRouteEditSurface
    {
        bool IsCurrent { get; }
        bool HasExpectedIdentity { get; }
        int StopCount { get; }
        bool Complete { get; }
        Vector3 ReadPosition(int index);
        bool MoveStop(int index, Vector3 position, bool fixedPlatform);
        void UpdateLine();
    }

    internal enum RouteEditExecutionOutcome
    {
        Refused,
        Applied,
        PositionsRestored,
        RecoveryIncomplete,
        Invalidated
    }

    internal sealed class RouteEditExecutionResult
    {
        public readonly RouteEditExecutionOutcome Outcome;
        public readonly string Detail;

        public RouteEditExecutionResult(RouteEditExecutionOutcome outcome, string detail)
        {
            Outcome = outcome;
            Detail = detail ?? string.Empty;
        }
    }

    internal static class RouteEditExecutor
    {
        public static RouteEditExecutionResult Execute(RouteEditPlan plan, IRouteEditSurface surface)
        {
            // Deliberate red-phase seam: implementation follows observed cloud test failure.
            throw new NotImplementedException("Route editing execution and observed recovery are not implemented yet.");
        }
    }
}
