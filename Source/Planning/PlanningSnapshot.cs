using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// One copied, stop-capable network segment. Value fields make the record immutable
    /// and safe to copy to the pure-data planning worker.
    /// </summary>
    internal struct SegmentSnapshotData
    {
        public readonly ushort Id;
        public readonly uint BuildIndex;
        public readonly float Length;
        public readonly ushort StartNode;
        public readonly ushort EndNode;
        public readonly float SpeedLimit;
        public readonly bool ForwardOpen;
        public readonly bool BackwardOpen;
        public readonly Vector3 MidPoint;
        public readonly Vector3 StopPosition;
        public readonly bool FixedPlatform;
        public readonly bool CanAnchor;

        public SegmentSnapshotData(ushort id, uint buildIndex, float length,
                                   ushort startNode, ushort endNode, float speedLimit,
                                   bool forwardOpen, bool backwardOpen, Vector3 midPoint,
                                   Vector3 stopPosition, bool fixedPlatform, bool canAnchor)
        {
            Id = id;
            BuildIndex = buildIndex;
            Length = length;
            StartNode = startNode;
            EndNode = endNode;
            SpeedLimit = speedLimit;
            ForwardOpen = forwardOpen;
            BackwardOpen = backwardOpen;
            MidPoint = midPoint;
            StopPosition = stopPosition;
            FixedPlatform = fixedPlatform;
            CanAnchor = canAnchor;
        }
    }

    /// <summary>A copied building's capacity-based demand, in people (citizen units x 5).</summary>
    internal struct BuildingDemandData
    {
        public readonly ushort BuildingId;
        public readonly Vector3 Position;
        public readonly int HomePeople;
        public readonly int WorkPeople;
        public readonly int VisitPeople;

        public BuildingDemandData(ushort buildingId, Vector3 position,
                                  int homePeople, int workPeople, int visitPeople)
        {
            BuildingId = buildingId;
            Position = position;
            HomePeople = homePeople;
            WorkPeople = workPeople;
            VisitPeople = visitPeople;
        }
    }

    /// <summary>The three live structural counters used to cancel an inconsistent capture.</summary>
    internal struct StructureSignature : IEquatable<StructureSignature>
    {
        public readonly int SegmentCount;
        public readonly int NodeCount;
        public readonly int BuildingCount;

        public StructureSignature(int segmentCount, int nodeCount, int buildingCount)
        {
            SegmentCount = segmentCount;
            NodeCount = nodeCount;
            BuildingCount = buildingCount;
        }

        public bool Equals(StructureSignature other)
        {
            return SegmentCount == other.SegmentCount &&
                   NodeCount == other.NodeCount &&
                   BuildingCount == other.BuildingCount;
        }

        public override bool Equals(object obj)
        {
            return obj is StructureSignature && Equals((StructureSignature)obj);
        }

        public override int GetHashCode()
        {
            return SegmentCount ^ (NodeCount << 11) ^ (BuildingCount << 22);
        }

        public override string ToString()
        {
            return "segments=" + SegmentCount + ", nodes=" + NodeCount +
                   ", buildings=" + BuildingCount;
        }
    }

    /// <summary>
    /// Immutable snapshot passed to pure-data computation. It owns copies of every
    /// array and only exposes read-only collections; Copy returns an independent value.
    /// </summary>
    internal sealed class PlanningSnapshot
    {
        private readonly SegmentSnapshotData[] m_segments;
        private readonly BuildingDemandData[] m_buildings;
        private readonly ReadOnlyCollection<SegmentSnapshotData> m_segmentView;
        private readonly ReadOnlyCollection<BuildingDemandData> m_buildingView;

        public ReadOnlyCollection<SegmentSnapshotData> Segments { get { return m_segmentView; } }
        public ReadOnlyCollection<BuildingDemandData> Buildings { get { return m_buildingView; } }
        public int CreatedSegmentCount { get; private set; }
        public int CreatedBuildingCount { get; private set; }
        public int ExistingStopCount { get; private set; }
        public int RetryCount { get; private set; }
        public string RetryDiagnostic { get; private set; }
        public int SliceCount { get; private set; }
        public int OverBudgetSliceCount { get; private set; }
        public uint BuildIndexAtStart { get; private set; }
        public uint BuildIndexAtEnd { get; private set; }
        public StructureSignature SignatureAtStart { get; private set; }
        public StructureSignature SignatureAtEnd { get; private set; }
        public double ElapsedMillis { get; private set; }
        public double SliceWorkMillis { get; private set; }
        public double MaxSliceMillis { get; private set; }

        public PlanningSnapshot(SegmentSnapshotData[] segments,
                                BuildingDemandData[] buildings,
                                int createdSegmentCount, int createdBuildingCount,
                                int existingStopCount, int retryCount, string retryDiagnostic, int sliceCount,
                                int overBudgetSliceCount, uint buildIndexAtStart,
                                uint buildIndexAtEnd, StructureSignature signatureAtStart,
                                StructureSignature signatureAtEnd, double elapsedMillis,
                                double sliceWorkMillis, double maxSliceMillis)
            : this(segments, buildings, createdSegmentCount, createdBuildingCount,
                   existingStopCount, retryCount, retryDiagnostic, sliceCount,
                   overBudgetSliceCount, buildIndexAtStart, buildIndexAtEnd,
                   signatureAtStart, signatureAtEnd, elapsedMillis, sliceWorkMillis,
                   maxSliceMillis, false)
        {
        }

        private PlanningSnapshot(SegmentSnapshotData[] segments,
                                 BuildingDemandData[] buildings,
                                 int createdSegmentCount, int createdBuildingCount,
                                 int existingStopCount, int retryCount, string retryDiagnostic, int sliceCount,
                                 int overBudgetSliceCount, uint buildIndexAtStart,
                                 uint buildIndexAtEnd, StructureSignature signatureAtStart,
                                 StructureSignature signatureAtEnd, double elapsedMillis,
                                 double sliceWorkMillis, double maxSliceMillis,
                                 bool takeOwnership)
        {
            m_segments = segments == null ? new SegmentSnapshotData[0] : takeOwnership ? segments :
                        (SegmentSnapshotData[])segments.Clone();
            m_buildings = buildings == null ? new BuildingDemandData[0] : takeOwnership ? buildings :
                          (BuildingDemandData[])buildings.Clone();
            m_segmentView = Array.AsReadOnly(m_segments);
            m_buildingView = Array.AsReadOnly(m_buildings);
            CreatedSegmentCount = createdSegmentCount;
            CreatedBuildingCount = createdBuildingCount;
            ExistingStopCount = existingStopCount;
            RetryCount = retryCount;
            RetryDiagnostic = retryDiagnostic ?? string.Empty;
            SliceCount = sliceCount;
            OverBudgetSliceCount = overBudgetSliceCount;
            BuildIndexAtStart = buildIndexAtStart;
            BuildIndexAtEnd = buildIndexAtEnd;
            SignatureAtStart = signatureAtStart;
            SignatureAtEnd = signatureAtEnd;
            ElapsedMillis = elapsedMillis;
            SliceWorkMillis = sliceWorkMillis;
            MaxSliceMillis = maxSliceMillis;
        }

        public static PlanningSnapshot TakeCapturedArrays(SegmentSnapshotData[] segments,
                                                          BuildingDemandData[] buildings,
                                                          int createdSegmentCount,
                                                          int createdBuildingCount,
                                                          int existingStopCount,
                                                          int retryCount,
                                                          string retryDiagnostic,
                                                          int sliceCount,
                                                          int overBudgetSliceCount,
                                                          uint buildIndexAtStart,
                                                          uint buildIndexAtEnd,
                                                          StructureSignature signatureAtStart,
                                                          StructureSignature signatureAtEnd,
                                                          double elapsedMillis,
                                                          double sliceWorkMillis,
                                                          double maxSliceMillis)
        {
            return new PlanningSnapshot(segments, buildings, createdSegmentCount,
                createdBuildingCount, existingStopCount, retryCount, retryDiagnostic,
                sliceCount, overBudgetSliceCount, buildIndexAtStart, buildIndexAtEnd,
                signatureAtStart, signatureAtEnd, elapsedMillis, sliceWorkMillis,
                maxSliceMillis, true);
        }

        public PlanningSnapshot Copy()
        {
            return new PlanningSnapshot(m_segments, m_buildings, CreatedSegmentCount,
                CreatedBuildingCount, ExistingStopCount, RetryCount, RetryDiagnostic, SliceCount,
                OverBudgetSliceCount, BuildIndexAtStart, BuildIndexAtEnd,
                SignatureAtStart, SignatureAtEnd, ElapsedMillis, SliceWorkMillis,
                MaxSliceMillis);
        }

        /// <summary>
        /// Adapts the immutable snapshot to the existing graph builder. The mutable
        /// adapter remains private to the worker and is never published.
        /// </summary>
        public WorldSnapshot ToWorldSnapshot()
        {
            WorldSnapshot snapshot = new WorldSnapshot();
            int count = m_segments.Length;
            snapshot.BuildIndex = BuildIndexAtStart;
            snapshot.SegmentIds = new ushort[count];
            snapshot.SegmentBuildIndex = new uint[count];
            snapshot.Length = new float[count];
            snapshot.StartNode = new ushort[count];
            snapshot.EndNode = new ushort[count];
            snapshot.SpeedLimit = new float[count];
            snapshot.ForwardOpen = new bool[count];
            snapshot.BackwardOpen = new bool[count];
            snapshot.MidPoint = new Vector3[count];
            snapshot.StopPosition = new Vector3[count];
            snapshot.FixedPlatform = new bool[count];
            snapshot.CanAnchor = new bool[count];

            float fastest = 0f;
            for (int i = 0; i < count; i++)
            {
                SegmentSnapshotData segment = m_segments[i];
                snapshot.SegmentIds[i] = segment.Id;
                snapshot.SegmentBuildIndex[i] = segment.BuildIndex;
                snapshot.Length[i] = segment.Length;
                snapshot.StartNode[i] = segment.StartNode;
                snapshot.EndNode[i] = segment.EndNode;
                snapshot.SpeedLimit[i] = segment.SpeedLimit;
                snapshot.ForwardOpen[i] = segment.ForwardOpen;
                snapshot.BackwardOpen[i] = segment.BackwardOpen;
                snapshot.MidPoint[i] = segment.MidPoint;
                snapshot.StopPosition[i] = segment.StopPosition;
                snapshot.FixedPlatform[i] = segment.FixedPlatform;
                snapshot.CanAnchor[i] = segment.CanAnchor;
                if (segment.SpeedLimit > fastest)
                {
                    fastest = segment.SpeedLimit;
                }
            }

            snapshot.MaxSpeedLimit = fastest > 0f ? fastest : 1f;
            snapshot.CaptureMillis = ElapsedMillis;
            return snapshot;
        }
    }
}
