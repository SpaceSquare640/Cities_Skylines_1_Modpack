#if DEVTOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using ColossalFramework;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    // Move-only Development prototype. Never releases/recreates a line or claims atomic rollback.
    internal static class RouteEditingController
    {
        private static readonly object Sync = new object();
        private static RouteEditPlan s_plan;
        private static string s_status = "No existing-route edit prepared.";
        private static string s_preview = string.Empty;
        private static int s_epoch;
        private static int s_busy;
        private static ushort s_checkLine;
        private static int s_checkStops;
        private static int s_checkFrames;
        public static RouteEditPlan CurrentPlan { get { lock (Sync) { return s_plan; } } }
        public static string StatusText { get { lock (Sync) { return s_status; } } }
        public static string PreviewText { get { lock (Sync) { return s_preview; } } }
        public static bool IsBusy { get { return Interlocked.CompareExchange(ref s_busy, 0, 0) != 0; } }

        public static void ResetForLevelChange()
        {
            Interlocked.Increment(ref s_epoch);
            Interlocked.Exchange(ref s_busy, 0);
            s_checkLine = 0;
            lock (Sync) { s_plan = null; s_preview = string.Empty; s_status = "City changed; prepare a new edit."; }
        }

        public static void RequestPreview(ushort lineId, string manualOrder, int proposalIndex, bool automatic)
        {
            Queue(delegate
            {
                lock (Sync) { s_plan = null; s_preview = string.Empty; }
                ExistingRouteSnapshot original = Capture(lineId);
                PlanningPreviewResult planning = PlanningCapture.LatestPreview;
                List<RouteStopSite> targets = new List<RouteStopSite>();
                if (automatic)
                {
                    RequirePlanning(planning, original);
                    if (proposalIndex < 0 || proposalIndex >= planning.Proposals.Count) { throw new InvalidOperationException("Select a valid one-based proposal number."); }
                    targets.AddRange(planning.Proposals[proposalIndex].StopSites);
                }
                else
                {
                    string[] tokens = (manualOrder ?? string.Empty).Split(',');
                    foreach (string input in tokens)
                    {
                        string token = input.Trim();
                        int index;
                        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out index))
                        {
                            if (index < 1 || index > original.Stops.Count) { throw new InvalidOperationException("Original stop index is outside the captured route."); }
                            targets.Add(ExistingSite(original.Stops[index - 1]));
                        }
                        else
                        {
                            RequirePlanning(planning, original);
                            if (token.Length < 2 || (token[0] != 'f' && token[0] != 'b') ||
                                !int.TryParse(token.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out index)) { throw new InvalidOperationException("Use stop indices or f<segment>/b<segment>, separated by commas."); }
                            bool found = false;
                            foreach (SegmentSnapshotData segment in planning.Snapshot.Segments)
                            {
                                if (segment.Id != index) { continue; }
                                bool forward = token[0] == 'f';
                                if (forward ? !segment.ForwardOpen : !segment.BackwardOpen) { throw new InvalidOperationException("The requested captured road direction is unavailable."); }
                                targets.Add(new RouteStopSite(segment.Id, segment.BuildIndex, forward,
                                    forward ? segment.ForwardLaneIndex : segment.BackwardLaneIndex,
                                    forward ? segment.ForwardStopPosition : segment.BackwardStopPosition,
                                    forward ? segment.ForwardFixedPlatform : segment.BackwardFixedPlatform));
                                found = true;
                                break;
                            }
                            if (!found) { throw new InvalidOperationException("Road segment is not in the current planning capture."); }
                        }
                    }
                }
                RouteEditPlan plan;
                string reason;
                if (!RouteEditPlan.TryCreate(original, automatic ? RouteEditMode.Automatic : RouteEditMode.Manual, targets, out plan, out reason)) { throw new InvalidOperationException(reason); }
                ValidateTargets(plan);
                StringBuilder text = new StringBuilder("Line ").Append(lineId).Append(" | ").Append(plan.Mode).Append(" | ").Append(targets.Count).Append(" stops (unchanged)\n");
                for (int i = 0; i < targets.Count; i++)
                {
                    text.Append(i + 1).Append(": node ").Append(original.Stops[i].NodeId).Append(' ').Append(original.Stops[i].Position)
                        .Append(" -> segment ").Append(targets[i].SegmentId).Append(" lane ").Append(targets[i].LaneIndex).Append(' ').Append(targets[i].Position).Append('\n');
                }
                lock (Sync) { s_plan = plan; s_preview = text.ToString(); s_status = "Preview only. Back up the save; two confirmations are required to apply."; }
            }, "Prepare existing-route edit");
        }

        public static void RequestApply(RouteEditPlan plan)
        {
            Queue(delegate
            {
                int applyingEpoch = Interlocked.CompareExchange(ref s_epoch, 0, 0);
                if (plan == null || !object.ReferenceEquals(plan, CurrentPlan)) { throw new InvalidOperationException("The displayed edit is no longer current."); }
                ExistingRouteSnapshot current = Capture(plan.Original.LineId);
                if (!plan.Original.Matches(current.LineId, current.BuildIndex, current.PrefabName, current.Stops)) { throw new InvalidOperationException("The city or original route changed; prepare a new edit."); }
                ValidateTargets(plan);
                lock (Sync) { s_plan = null; }
                ushort line = plan.Original.LineId;
                TransportManager tm = Singleton<TransportManager>.instance;
                try
                {
                    for (int i = 0; i < plan.Stops.Count; i++)
                    {
                        if (applyingEpoch != Interlocked.CompareExchange(ref s_epoch, 0, 0)) { throw new InvalidOperationException("City changed during route editing."); }
                        Vector3 position = Singleton<NetManager>.instance.m_nodes.m_buffer[tm.m_lines.m_buffer[line].GetStop(i)].m_position;
                        if ((position - plan.Stops[i].Position).sqrMagnitude < 0.0001f) { continue; }
                        if (!tm.m_lines.m_buffer[line].MoveStop(line, i, plan.Stops[i].Position, plan.Stops[i].FixedPlatform) ||
                            tm.m_lines.m_buffer[line].CountStops(line) != plan.Stops.Count) { throw new InvalidOperationException("MoveStop refused or changed the stop count at index " + i + "."); }
                    }
                    if (!tm.m_lines.m_buffer[line].Complete) { throw new InvalidOperationException("The edited route is not complete."); }
                    for (int i = 0; i < plan.Stops.Count; i++)
                    {
                        ushort node = tm.m_lines.m_buffer[line].GetStop(i);
                        if (node == 0 || (Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_position - plan.Stops[i].Position).sqrMagnitude > 0.25f) { throw new InvalidOperationException("The game did not retain a requested stop position."); }
                    }
                    tm.UpdateLine(line);
                    s_checkLine = line; s_checkStops = plan.Stops.Count; s_checkFrames = 0;
                    SetStatus("Edit applied to existing line " + line + "; checking adjacent paths. This is not full-route acceptance.");
                }
                catch (Exception error)
                {
                    if (applyingEpoch != Interlocked.CompareExchange(ref s_epoch, 0, 0)) { Log.Exception("City changed; refusing recovery against another city", error); return; }
                    bool restored = true;
                    // Best effort only: this cannot undo passenger/vehicle/path side effects.
                    for (int i = 0; i < plan.Original.Stops.Count; i++)
                    {
                        try
                        {
                            if (tm.m_lines.m_buffer[line].CountStops(line) != plan.Original.Stops.Count ||
                                !tm.m_lines.m_buffer[line].MoveStop(line, i, plan.Original.Stops[i].Position, false)) { restored = false; }
                        }
                        catch { restored = false; }
                    }
                    try { tm.UpdateLine(line); } catch { restored = false; }
                    SetStatus("Edit failed: " + error.Message + (restored ? " Original positions were requested again; inspect the city and use your backup if needed." : " Recovery was incomplete. Stop and restore your save backup."));
                    Log.Exception("Existing-route edit failed; best-effort recovery=" + restored, error);
                }
            }, "Apply existing-route edit");
        }

        private static void Queue(Action action, string context)
        {
            if (RouteCreationController.IsBusy || Interlocked.CompareExchange(ref s_busy, 1, 0) != 0) { SetStatus("Another route operation is pending."); return; }
            int epoch = Interlocked.CompareExchange(ref s_epoch, 0, 0);
            if (!Singleton<SimulationManager>.exists) { Interlocked.Exchange(ref s_busy, 0); SetStatus("No city is loaded."); return; }
            try
            {
                Singleton<SimulationManager>.instance.AddAction(delegate
                {
                    if (epoch != Interlocked.CompareExchange(ref s_epoch, 0, 0)) { return; }
                    try { action(); }
                    catch (Exception error) { lock (Sync) { s_plan = null; } SetStatus(context + " refused: " + error.Message); Log.Exception(context, error); }
                    finally { if (s_checkLine == 0) { Interlocked.Exchange(ref s_busy, 0); } }
                });
            }
            catch (Exception error) { Interlocked.Exchange(ref s_busy, 0); SetStatus(context + " could not be queued."); Log.Exception(context, error); }
        }

        private static ExistingRouteSnapshot Capture(ushort line)
        {
            if (!Singleton<TransportManager>.exists || !Singleton<NetManager>.exists || !Singleton<SimulationManager>.exists) { throw new InvalidOperationException("City managers are unavailable."); }
            TransportManager tm = Singleton<TransportManager>.instance;
            if (line == 0 || line >= tm.m_lines.m_buffer.Length || (tm.m_lines.m_buffer[line].m_flags & TransportLine.Flags.Created) == 0 || !tm.m_lines.m_buffer[line].Complete) { throw new InvalidOperationException("Select an existing complete city bus line ID."); }
            TransportInfo info = tm.m_lines.m_buffer[line].Info;
            if (info == null || info.m_transportType != TransportInfo.TransportType.Bus || info.m_class == null || info.m_class.m_level != ItemClass.Level.Level1) { throw new InvalidOperationException("Only ordinary Level1 city bus lines are supported."); }
            int count = tm.m_lines.m_buffer[line].CountStops(line);
            if (!RouteCreationRules.IsStopCountAllowed(count)) { throw new InvalidOperationException("This move-only prototype supports three to six existing stops."); }
            List<ExistingRouteStop> stops = new List<ExistingRouteStop>();
            for (int i = 0; i < count; i++)
            {
                ushort node = tm.m_lines.m_buffer[line].GetStop(i);
                if (node == 0) { throw new InvalidOperationException("The line has a missing stop."); }
                stops.Add(new ExistingRouteStop(node, Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_position));
            }
            // Fixed platforms need a separate recovery design; this prototype only edits street stops.
            foreach (ExistingRouteStop stop in stops)
            {
                RouteStopSite site = ExistingSite(stop);
                Vector3 position; bool fixedPlatform;
                if (!StopPlacement.TryGetStopPosition(site.SegmentId, stop.Position, info, out position, out fixedPlatform) || fixedPlatform)
                { throw new InvalidOperationException("This prototype only edits ordinary street stops, not fixed platforms."); }
            }
            return new ExistingRouteSnapshot(line, Singleton<SimulationManager>.instance.m_currentBuildIndex, info.name, stops);
        }

        private static RouteStopSite ExistingSite(ExistingRouteStop stop)
        {
            NetManager net = Singleton<NetManager>.instance;
            uint lane = net.m_nodes.m_buffer[stop.NodeId].m_lane;
            if (lane == 0 || lane >= net.m_lanes.m_buffer.Length) { throw new InvalidOperationException("An existing stop has no connected lane."); }
            ushort segment = net.m_lanes.m_buffer[lane].m_segment;
            if (segment == 0) { throw new InvalidOperationException("An existing stop lane has no road segment."); }
            uint cursor = net.m_segments.m_buffer[segment].m_lanes;
            for (int index = 0; cursor != 0 && index < 256; index++, cursor = net.m_lanes.m_buffer[cursor].m_nextLane)
            {
                if (cursor == lane) { return new RouteStopSite(segment, net.m_segments.m_buffer[segment].m_buildIndex, true, index, stop.Position, false); }
            }
            throw new InvalidOperationException("An existing stop lane could not be identified.");
        }

        private static void RequirePlanning(PlanningPreviewResult planning, ExistingRouteSnapshot original)
        {
            if (planning == null || planning.Snapshot == null || planning.Snapshot.BuildIndexAtStart != planning.Snapshot.BuildIndexAtEnd ||
                planning.Snapshot.BuildIndexAtEnd != original.BuildIndex || planning.Snapshot.BusPrefabName != original.PrefabName) { throw new InvalidOperationException("Generate a fresh planning preview for the same bus prefab first."); }
        }

        private static void ValidateTargets(RouteEditPlan plan)
        {
            NetManager net = Singleton<NetManager>.instance;
            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[plan.Original.LineId].Info;
            foreach (RouteStopSite target in plan.Stops)
            {
                if (target.SegmentId == 0 || target.SegmentId >= net.m_segments.m_buffer.Length) { throw new InvalidOperationException("A target road disappeared."); }
                NetSegment segment = net.m_segments.m_buffer[target.SegmentId];
                if ((segment.m_flags & NetSegment.Flags.Created) == 0 || segment.m_buildIndex != target.SegmentBuildIndex ||
                    !StopPlacement.IsSegmentUsableForStops(target.SegmentId, info) || !StopPlacement.CanAnchorStop(target.Position, info)) { throw new InvalidOperationException("A target road or anchor no longer matches."); }
                bool originalSite = false;
                foreach (ExistingRouteStop old in plan.Original.Stops)
                {
                    if ((old.Position - target.Position).sqrMagnitude < 0.0001f) { originalSite = true; break; }
                }
                if (originalSite) { continue; }
                Vector3 position; bool fixedPlatform; int lane; bool forward; bool backward;
                if (!StopPlacement.TryGetStopPosition(target.SegmentId, target.Position, info, out position, out fixedPlatform, out lane, out forward, out backward) ||
                    lane != target.LaneIndex || fixedPlatform != target.FixedPlatform || (target.Forward ? !forward : !backward) || (position - target.Position).sqrMagnitude > 0.25f) { throw new InvalidOperationException("A target lane, direction or position changed."); }
            }
        }

        public static void OnAfterSimulationFrame()
        {
            if (s_checkLine == 0 || ++s_checkFrames < 60) { return; }
            try
            {
                TransportManager tm = Singleton<TransportManager>.instance;
                NetManager net = Singleton<NetManager>.instance;
                ushort line = s_checkLine;
                bool valid = (tm.m_lines.m_buffer[line].m_flags & TransportLine.Flags.Created) != 0 && tm.m_lines.m_buffer[line].Complete && tm.m_lines.m_buffer[line].CountStops(line) == s_checkStops;
                for (int i = 0; valid && i < s_checkStops; i++)
                {
                    ushort node = tm.m_lines.m_buffer[line].GetStop(i);
                    valid = node != 0 && StopPlacement.HasLaneConnection(node) && !net.m_nodes.m_buffer[node].m_problems.IsNotNone;
                    for (int j = 0; valid && j < 8; j++)
                    {
                        ushort segment = net.m_nodes.m_buffer[node].GetSegment(j);
                        if (segment != 0 && (net.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.PathFailed) != 0) { valid = false; }
                    }
                }
                if (!valid && s_checkFrames < 300) { return; }
                SetStatus(valid ? "Edited line " + line + " passed adjacent-stop checks only; verify the whole route and save/reload in game." : "Edited line " + line + " has connectivity/path warnings. Inspect it and restore your backup if needed.");
            }
            catch (Exception error) { SetStatus("Could not verify the edited line; inspect the city."); Log.Exception("Existing-route post-check", error); }
            s_checkLine = 0;
            Interlocked.Exchange(ref s_busy, 0);
        }

        private static void SetStatus(string text) { lock (Sync) { s_status = text; } }
    }
}
#endif
