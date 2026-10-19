using System.Collections.Generic;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// The network a route is searched over: one node per segment per direction it can
    /// be driven in, and an edge wherever a vehicle can carry on from one into the next.
    ///
    /// Directed, and that is the whole point. A one-way street is passable one way and
    /// not the other, so an undirected graph would happily route a bus the wrong way up
    /// one — a route that looks perfectly sensible drawn on a map and that no vehicle
    /// can follow. Half the work of the snapshot this is built from goes into getting
    /// those directions into world terms in the first place.
    ///
    /// Built entirely from a WorldSnapshot. Nothing here reads game state, so it can run
    /// on any thread and be abandoned at any point.
    ///
    /// Known simplification: any two segments meeting at a junction are treated as
    /// connected. The game also checks lane connections, and mods like TMPE add turn
    /// restrictions on top. A planned route can therefore include a turn the game will
    /// not actually make, which is one reason a route is confirmed with the path
    /// manager before it is offered rather than trusted because the graph allowed it.
    /// </summary>
    internal sealed class StopGraph
    {
        /// <summary>Index into the snapshot's arrays, per node.</summary>
        public int[] SegmentIndex;

        /// <summary>Whether this node drives from the segment's start node toward its end node.</summary>
        public bool[] Forward;

        /// <summary>
        /// Cost of driving the length of this node's segment: length divided by speed,
        /// which is the shape PathFind uses. Speed is never zero — the snapshot floors
        /// it the same way the game does.
        /// </summary>
        public float[] TraversalCost;

        /// <summary>Segment midpoint, for the search's distance heuristic.</summary>
        public Vector3[] Position;

        /// <summary>Node indices reachable directly from each node.</summary>
        public int[][] Adjacency;

        public double BuildMillis;

        public int Count
        {
            get { return SegmentIndex == null ? 0 : SegmentIndex.Length; }
        }

        /// <summary>
        /// The lowest cost any unit of distance can possibly have, which an A* heuristic
        /// must not exceed. Carried through from the snapshot so the search does not
        /// have to go looking for it.
        /// </summary>
        public float MinCostPerMetre;

        public static StopGraph Build(WorldSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Count == 0)
            {
                return null;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            int segments = snapshot.Count;

            List<int> segmentIndex = new List<int>(segments * 2);
            List<bool> forward = new List<bool>(segments * 2);

            // Junction id -> the nodes that ENTER the network at that junction. Built
            // first so adjacency is a lookup per node rather than a scan over all of
            // them; the quadratic version is affordable at this size but stops being so
            // the moment a bigger map turns up.
            Dictionary<ushort, List<int>> entriesAt = new Dictionary<ushort, List<int>>();

            for (int i = 0; i < segments; i++)
            {
                if (!snapshot.CanAnchor[i])
                {
                    continue;
                }

                if (snapshot.ForwardOpen[i])
                {
                    AddNode(segmentIndex, forward, entriesAt, i, true, snapshot.StartNode[i]);
                }

                if (snapshot.BackwardOpen[i])
                {
                    AddNode(segmentIndex, forward, entriesAt, i, false, snapshot.EndNode[i]);
                }
            }

            int count = segmentIndex.Count;

            StopGraph graph = new StopGraph();
            graph.SegmentIndex = segmentIndex.ToArray();
            graph.Forward = forward.ToArray();
            graph.TraversalCost = new float[count];
            graph.Position = new Vector3[count];
            graph.Adjacency = new int[count][];
            graph.MinCostPerMetre = 1f / snapshot.MaxSpeedLimit;

            int[] empty = new int[0];

            for (int n = 0; n < count; n++)
            {
                int seg = graph.SegmentIndex[n];

                graph.TraversalCost[n] = snapshot.Length[seg] / snapshot.SpeedLimit[seg];
                graph.Position[n] = snapshot.MidPoint[seg];

                // Leaving forward means leaving at the end node, and the other way round.
                ushort exit = graph.Forward[n] ? snapshot.EndNode[seg] : snapshot.StartNode[seg];

                List<int> candidates;
                if (!entriesAt.TryGetValue(exit, out candidates))
                {
                    graph.Adjacency[n] = empty;
                    continue;
                }

                List<int> neighbours = new List<int>(candidates.Count);
                for (int c = 0; c < candidates.Count; c++)
                {
                    // Carrying straight on into the same segment travelling the other way
                    // is a U-turn in the middle of a route. Buses do not, and allowing it
                    // lets the search produce shapes no line would ever be drawn in.
                    if (graph.SegmentIndex[candidates[c]] == seg)
                    {
                        continue;
                    }

                    neighbours.Add(candidates[c]);
                }

                graph.Adjacency[n] = neighbours.Count == 0 ? empty : neighbours.ToArray();
            }

            sw.Stop();
            graph.BuildMillis = (double)sw.ElapsedTicks * 1000.0 / (double)System.Diagnostics.Stopwatch.Frequency;

            return graph;
        }

        private static void AddNode(List<int> segmentIndex, List<bool> forward,
                                    Dictionary<ushort, List<int>> entriesAt,
                                    int segment, bool isForward, ushort entryJunction)
        {
            int index = segmentIndex.Count;
            segmentIndex.Add(segment);
            forward.Add(isForward);

            List<int> list;
            if (!entriesAt.TryGetValue(entryJunction, out list))
            {
                list = new List<int>(4);
                entriesAt[entryJunction] = list;
            }

            list.Add(index);
        }

        /// <summary>
        /// A summary, plus the two checks most likely to catch a graph that was built
        /// wrongly but still looks like a graph.
        /// </summary>
        public string Describe(WorldSnapshot snapshot)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Stop graph");

            sb.Append("\n  built ").Append(Count).Append(" nodes in ")
              .Append(BuildMillis.ToString("F2")).Append(" ms");

            if (Count == 0)
            {
                sb.Append("\n  nothing to describe.");
                return sb.ToString();
            }

            int edges = 0;
            int isolated = 0;
            int maxDegree = 0;
            float costSum = 0f;

            for (int n = 0; n < Count; n++)
            {
                int degree = Adjacency[n].Length;
                edges += degree;
                costSum += TraversalCost[n];

                if (degree == 0) { isolated++; }
                if (degree > maxDegree) { maxDegree = degree; }
            }

            sb.Append("\n  edges=").Append(edges)
              .Append("  avgDegree=").Append(((float)edges / Count).ToString("F2"))
              .Append("  maxDegree=").Append(maxDegree)
              .Append("  isolated=").Append(isolated)
              .Append("\n  traversal cost: total=").Append(costSum.ToString("F1"))
              .Append("  avg=").Append((costSum / Count).ToString("F3"))
              .Append("  minCostPerMetre=").Append(MinCostPerMetre.ToString("F4"));

            // Reachability. A direction bug does not usually produce an empty graph — it
            // produces one that is quietly in pieces, where a search simply fails to find
            // routes that obviously exist. Walking it from one node says which.
            int reached = CountReachableFrom(LargestDegreeNode());
            float fraction = (float)reached / Count;

            sb.Append("\n  reachable from the busiest node: ").Append(reached)
              .Append("/").Append(Count)
              .Append(" (").Append((fraction * 100f).ToString("F1")).Append("%)");

            if (fraction < 0.5f)
            {
                sb.Append("\n  WARNING: most of the graph cannot be reached from one node.")
                  .Append("\n           A road network is normally almost entirely connected, so")
                  .Append("\n           this points at the direction handling rather than the city.");
            }

            if (snapshot != null)
            {
                // Every two-way segment should have produced two nodes and every one-way
                // exactly one. If this does not add up, nodes were dropped somewhere
                // between the snapshot and here.
                int expected = 0;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    if (!snapshot.CanAnchor[i]) { continue; }
                    if (snapshot.ForwardOpen[i]) { expected++; }
                    if (snapshot.BackwardOpen[i]) { expected++; }
                }

                sb.Append("\n  node count check: expected ").Append(expected)
                  .Append(", got ").Append(Count)
                  .Append(expected == Count ? "  OK" : "  MISMATCH");
            }

            return sb.ToString();
        }

        private int LargestDegreeNode()
        {
            int best = 0;
            int bestDegree = -1;

            for (int n = 0; n < Count; n++)
            {
                if (Adjacency[n].Length > bestDegree)
                {
                    bestDegree = Adjacency[n].Length;
                    best = n;
                }
            }

            return best;
        }

        private int CountReachableFrom(int start)
        {
            bool[] seen = new bool[Count];
            Queue<int> queue = new Queue<int>();

            seen[start] = true;
            queue.Enqueue(start);
            int reached = 1;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int[] neighbours = Adjacency[current];

                for (int i = 0; i < neighbours.Length; i++)
                {
                    int next = neighbours[i];
                    if (seen[next])
                    {
                        continue;
                    }

                    seen[next] = true;
                    reached++;
                    queue.Enqueue(next);
                }
            }

            return reached;
        }

        /// <summary>
        /// Snapshots the network, builds the graph, and writes both summaries to the
        /// log. Queues itself onto the simulation thread for the snapshot; the graph
        /// itself needs no game state, but there is no reason to hand it back and forth.
        /// </summary>
        public static void LogGraph()
        {
            if (!ColossalFramework.Singleton<SimulationManager>.exists)
            {
                Log.Warning("No city is loaded; nothing to build a graph from.");
                return;
            }

            ColossalFramework.Singleton<SimulationManager>.instance.AddAction(delegate
            {
                try
                {
                    TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
                    if (busInfo == null)
                    {
                        Log.Error("Cannot build a graph: no city bus prefab in this save.");
                        return;
                    }

                    WorldSnapshot snapshot = WorldSnapshot.Capture(busInfo);
                    if (snapshot == null)
                    {
                        Log.Info("Level went away before the graph ran; skipped.");
                        return;
                    }

                    StopGraph graph = Build(snapshot);
                    if (graph == null)
                    {
                        Log.Warning("Snapshot produced no usable segments; no graph built.");
                        return;
                    }

                    Log.Info(snapshot.Describe() + "\n\n" + graph.Describe(snapshot));
                }
                catch (System.Exception e)
                {
                    Log.Exception("Graph build failed", e);
                }
            });
        }
    }
}
