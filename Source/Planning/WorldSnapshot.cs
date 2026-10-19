using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// Everything the planner needs about the road network, copied out of the game
    /// once so that planning never reads a live buffer again.
    ///
    /// This is what makes "the engine does not touch game objects" true rather than
    /// merely asserted. The simulation thread rewrites m_segments and m_lanes while
    /// the game runs — it does so even when paused — so a planner that read them as
    /// it went would be racing those writes for however long it ran.
    ///
    /// Capture MUST be called on the simulation thread, through
    /// SimulationManager.AddAction. Everything after it can run anywhere.
    ///
    /// Cost, measured rather than estimated (see the A5-1 probe): about 36ms on a
    /// city of 3,759 segments, and bounded above by roughly 0.3s even if the whole
    /// map were built out, since the segment buffer is a compile-time 36,864. That
    /// is why stop positions are computed here rather than lazily: an earlier design
    /// deferred them on a cost estimate that turned out to be 20-45x too high, and
    /// deferring them would have meant reading game state from the phase that is
    /// supposed to have stopped doing that.
    /// </summary>
    internal sealed class WorldSnapshot
    {
        /// <summary>
        /// The simulation's build index when this was taken. Individual segments carry
        /// their own, which is what actually detects reuse; this is here to date the
        /// snapshot as a whole.
        /// </summary>
        public uint BuildIndex;

        public ushort[] SegmentIds;

        /// <summary>
        /// NetSegment.m_buildIndex per segment, for re-checking at the moment a line
        /// is accepted. Segment ids are recycled: an id noted here can, by then, name
        /// a different segment the player built in the meantime. The build index rises
        /// monotonically, so a reused id always carries a different one.
        /// </summary>
        public uint[] SegmentBuildIndex;

        /// <summary>NetSegment.m_averageLength — the length the game's own pathfinder uses.</summary>
        public float[] Length;

        /// <summary>
        /// The junctions a segment runs between, which is what makes two segments
        /// adjacent. Travelling forward means leaving at EndNode; backward means
        /// leaving at StartNode.
        /// </summary>
        public ushort[] StartNode;
        public ushort[] EndNode;

        /// <summary>
        /// Speed limit of the fastest lane a vehicle of this type may use here.
        ///
        /// The edge cost model is length / speed, which is what PathFind computes; see
        /// the R4 verification. PathFind uses the speed of the particular lane a path
        /// runs through, which is not known before the path exists, so this takes the
        /// best lane available and is therefore optimistic on a road whose lanes differ.
        /// If planned journey times ever disagree with the game's, look here first.
        /// </summary>
        public float[] SpeedLimit;

        /// <summary>
        /// Whether a vehicle may travel from the segment's start node to its end node,
        /// and the reverse. These are world directions: the lane's own direction is in
        /// prefab space and means nothing until the segment's Invert flag is applied.
        /// </summary>
        public bool[] ForwardOpen;
        public bool[] BackwardOpen;

        public Vector3[] MidPoint;

        /// <summary>Where a stop would sit, from StopPlacement. Vector3.zero when none is possible.</summary>
        public Vector3[] StopPosition;
        public bool[] FixedPlatform;

        /// <summary>Whether the path manager can anchor a stop at StopPosition.</summary>
        public bool[] CanAnchor;

        /// <summary>
        /// The fastest lane speed anywhere in the snapshot.
        ///
        /// This is not a statistic. The A* heuristic must never overestimate, and with
        /// an edge cost of length / speed the cheapest any distance can possibly be is
        /// that distance divided by the fastest speed in the network. Get this wrong
        /// and the search quietly stops returning optimal paths.
        /// </summary>
        public float MaxSpeedLimit;

        /// <summary>How long Capture took, in milliseconds.</summary>
        public double CaptureMillis;

        public int Count
        {
            get { return SegmentIds == null ? 0 : SegmentIds.Length; }
        }

        /// <summary>
        /// Takes a snapshot and writes its summary to the log, for checking against a
        /// live city. Queues itself onto the simulation thread, so it is safe to call
        /// from a button.
        /// </summary>
        public static void LogSnapshot()
        {
            if (!Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to snapshot.");
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
                    if (busInfo == null)
                    {
                        Log.Error("Cannot snapshot: no city bus prefab in this save.");
                        return;
                    }

                    WorldSnapshot snapshot = Capture(busInfo);
                    if (snapshot == null)
                    {
                        Log.Info("Level went away before the snapshot ran; skipped.");
                        return;
                    }

                    Log.Info(snapshot.Describe());
                }
                catch (System.Exception e)
                {
                    Log.Exception("Snapshot failed", e);
                }
            });
        }

        /// <summary>
        /// Copies the network. Call on the simulation thread.
        ///
        /// Returns null when the world is not there to be copied — the queued action
        /// that calls this runs a step or more after it was queued, and the player can
        /// leave in between.
        /// </summary>
        public static WorldSnapshot Capture(TransportInfo info)
        {
            if (info == null || !Singleton<NetManager>.exists || !Singleton<SimulationManager>.exists)
            {
                return null;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            NetManager net = Singleton<NetManager>.instance;
            int bufferLength = net.m_segments.m_buffer.Length;

            List<ushort> ids = new List<ushort>();
            List<uint> buildIndex = new List<uint>();
            List<float> length = new List<float>();
            List<ushort> startNode = new List<ushort>();
            List<ushort> endNode = new List<ushort>();
            List<float> speed = new List<float>();
            List<bool> forward = new List<bool>();
            List<bool> backward = new List<bool>();
            List<Vector3> midPoint = new List<Vector3>();
            List<Vector3> stopPosition = new List<Vector3>();
            List<bool> fixedPlatform = new List<bool>();
            List<bool> canAnchor = new List<bool>();

            float fastest = 0f;

            for (int i = 1; i < bufferLength; i++)
            {
                ushort segmentId = (ushort)i;

                if (!StopPlacement.IsSegmentUsableForStops(segmentId, info))
                {
                    continue;
                }

                NetInfo segmentInfo = net.m_segments.m_buffer[i].Info;
                if (segmentInfo == null || segmentInfo.m_lanes == null)
                {
                    continue;
                }

                bool invert = (net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;

                bool laneForward;
                bool laneBackward;
                float laneSpeed = FastestUsableLane(segmentInfo, info, invert, out laneForward, out laneBackward);

                // No lane this vehicle may drive in. IsSegmentUsableForStops works at
                // the segment level, on the union of the prefab's categories, so it can
                // pass a road whose individual lanes all exclude us.
                if (!laneForward && !laneBackward)
                {
                    continue;
                }

                Vector3 middle = net.m_segments.m_buffer[i].m_middlePosition;

                Vector3 stop;
                bool platform;
                if (!StopPlacement.TryGetStopPosition(segmentId, middle, info, out stop, out platform))
                {
                    continue;
                }

                ids.Add(segmentId);
                buildIndex.Add(net.m_segments.m_buffer[i].m_buildIndex);
                length.Add(net.m_segments.m_buffer[i].m_averageLength);
                startNode.Add(net.m_segments.m_buffer[i].m_startNode);
                endNode.Add(net.m_segments.m_buffer[i].m_endNode);
                speed.Add(laneSpeed);
                forward.Add(laneForward);
                backward.Add(laneBackward);
                midPoint.Add(middle);
                stopPosition.Add(stop);
                fixedPlatform.Add(platform);
                canAnchor.Add(StopPlacement.CanAnchorStop(stop, info));

                if (laneSpeed > fastest)
                {
                    fastest = laneSpeed;
                }
            }

            sw.Stop();

            WorldSnapshot snapshot = new WorldSnapshot();
            snapshot.BuildIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex;
            snapshot.SegmentIds = ids.ToArray();
            snapshot.SegmentBuildIndex = buildIndex.ToArray();
            snapshot.Length = length.ToArray();
            snapshot.StartNode = startNode.ToArray();
            snapshot.EndNode = endNode.ToArray();
            snapshot.SpeedLimit = speed.ToArray();
            snapshot.ForwardOpen = forward.ToArray();
            snapshot.BackwardOpen = backward.ToArray();
            snapshot.MidPoint = midPoint.ToArray();
            snapshot.StopPosition = stopPosition.ToArray();
            snapshot.FixedPlatform = fixedPlatform.ToArray();
            snapshot.CanAnchor = canAnchor.ToArray();

            // Never zero, so nothing downstream divides by it. The game applies the
            // same floor to individual lanes (PathFind: speedLimit <= 0 becomes 1).
            snapshot.MaxSpeedLimit = fastest > 0f ? fastest : 1f;
            snapshot.CaptureMillis = (double)sw.ElapsedTicks * 1000.0 / (double)System.Diagnostics.Stopwatch.Frequency;

            return snapshot;
        }

        /// <summary>
        /// The speed of the fastest lane this vehicle may use, and which world
        /// directions those lanes allow.
        ///
        /// Lane eligibility is asked with NetInfo.Lane.CheckType, the same test the
        /// game applies when it picks a lane, rather than a reimplementation of it.
        ///
        /// Direction is the part that is easy to get wrong. A lane's m_finalDirection
        /// is expressed in the prefab's own frame; whether that means start-to-end in
        /// the world depends on the segment's Invert flag. PathFind resolves it exactly
        /// this way. Skip the flag and every road the player happened to draw backwards
        /// becomes passable in the wrong direction — and those are common, and invisible
        /// on a map.
        /// </summary>
        private static float FastestUsableLane(NetInfo segmentInfo, TransportInfo info, bool invert,
                                               out bool forwardOpen, out bool backwardOpen)
        {
            forwardOpen = false;
            backwardOpen = false;
            float fastest = 0f;

            for (int i = 0; i < segmentInfo.m_lanes.Length; i++)
            {
                NetInfo.Lane lane = segmentInfo.m_lanes[i];
                if (lane == null)
                {
                    continue;
                }

                if (!lane.CheckType(NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle,
                                    info.m_vehicleType, info.vehicleCategory))
                {
                    continue;
                }

                NetInfo.Direction direction = invert
                    ? NetInfo.InvertDirection(lane.m_finalDirection)
                    : lane.m_finalDirection;

                if ((byte)(direction & NetInfo.Direction.Forward) != 0)
                {
                    forwardOpen = true;
                }

                if ((byte)(direction & NetInfo.Direction.Backward) != 0)
                {
                    backwardOpen = true;
                }

                float limit = lane.m_speedLimit;
                if (limit > fastest)
                {
                    fastest = limit;
                }
            }

            return fastest > 0f ? fastest : 1f;
        }

        /// <summary>
        /// A summary to check against the live city. The numbers here have to match
        /// what a survey of the same city reports; if they do not, the snapshot is
        /// wrong, and finding that out now is far cheaper than inferring it later from
        /// a route that comes out strangely.
        /// </summary>
        public string Describe()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("World snapshot");

            sb.Append("\n  captured ").Append(Count).Append(" segments in ")
              .Append(CaptureMillis.ToString("F2")).Append(" ms   buildIndex=").Append(BuildIndex);

            if (Count == 0)
            {
                sb.Append("\n  nothing to describe.");
                return sb.ToString();
            }

            float totalLength = 0f;
            int bothWays = 0;
            int oneWay = 0;
            int anchored = 0;
            int platforms = 0;
            float slowest = float.MaxValue;

            for (int i = 0; i < Count; i++)
            {
                totalLength += Length[i];

                if (ForwardOpen[i] && BackwardOpen[i]) { bothWays++; } else { oneWay++; }
                if (CanAnchor[i]) { anchored++; }
                if (FixedPlatform[i]) { platforms++; }
                if (SpeedLimit[i] < slowest) { slowest = SpeedLimit[i]; }
            }

            sb.Append("\n  totalLength=").Append(totalLength.ToString("F0")).Append(" m")
              .Append("  avg=").Append((totalLength / Count).ToString("F1")).Append(" m")
              .Append("\n  direction: bothWays=").Append(bothWays).Append("  oneWay=").Append(oneWay)
              .Append("\n  speed: slowest=").Append(slowest.ToString("F2"))
              .Append("  fastest=").Append(MaxSpeedLimit.ToString("F2"))
              .Append("   (heuristic lower bound = 1/").Append(MaxSpeedLimit.ToString("F2"))
              .Append(" = ").Append((1f / MaxSpeedLimit).ToString("F4")).Append(")")
              .Append("\n  stops: canAnchor=").Append(anchored).Append("/").Append(Count)
              .Append("  fixedPlatform=").Append(platforms);

            // One-way roads are the reason the graph is directed at all. Seeing none in
            // a city that has them would mean the Invert handling is not working.
            if (oneWay == 0)
            {
                sb.Append("\n  NOTE: every segment is two-way. Correct only if this city has")
                  .Append("\n        no one-way roads at all; otherwise the Invert handling is wrong.");
            }

            if (anchored != Count)
            {
                sb.Append("\n  NOTE: ").Append(Count - anchored)
                  .Append(" segment(s) have a stop position the path manager cannot anchor.")
                  .Append("\n        Those are unusable and the graph should leave them out.");
            }

            return sb.ToString();
        }
    }
}
