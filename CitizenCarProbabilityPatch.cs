using UnityEngine;
using ColossalFramework;

namespace AIImprove
{
    // First step of "市民 AI" improvements (2026-08-13, per user request): make the walk/drive/
    // public-transit split respond to real-time congestion instead of being a fixed policy-driven
    // probability roll. Full multimodal-route intelligence (e.g. picking the least-congested
    // transit line for a trip) lives deep inside PathFind's own cost function - the same
    // multi-ref-struct method this project already established (months ago, see
    // Cities_Skylines_1_AI_Improve_Document/01) that Harmony/Mono cannot patch. This patch instead
    // targets the one safe, well-scoped lever available before that wall: ResidentAI's own
    // GetCarProbability(ushort, ref CitizenInstance, Citizen.AgeGroup) - a private method with a
    // single ref-struct parameter (CitizenInstance), called once per trip decision, well before
    // any pathfinding happens.
    //
    // Mechanism: Postfix reads the vanilla-computed probability (0-100, from age/policy/wealth
    // factors) and scales it down when the road network near the citizen's destination is already
    // heavily congested - nudging more citizens toward walking or public transit on trips that
    // would otherwise pile more cars onto an already-jammed area, without touching pathfinding
    // itself or removing anyone's ability to still choose to drive.
    internal static class CitizenCarProbabilityPatch
    {
        // If a citizen's parked car is this far from home, assume TM:PE Parking AI (or another
        // parking mod) left it at a previous destination. Tune after in-game testing.
        private const float HomeParkingDistanceThresholdMeters = 150f;
        private const float HomeParkingDistanceThresholdSquared =
            HomeParkingDistanceThresholdMeters * HomeParkingDistanceThresholdMeters;

        // Below this NetSegment.m_trafficDensity (0-100 scale) reading, vanilla's probability is
        // left untouched entirely - only meaningfully congested destinations are affected.
        // "每個功能中的調整設定及數據可以拆開以及詳細調整" (2026-08-15): now a slider.
        private static float DensityThreshold => ModSettings.CitizenCarDensityThreshold.value;

        // At density == 100, vanilla's car probability is cut by up to this fraction. Interim
        // value pending live-test calibration, same as every other threshold in this project.
        private static float MaxReductionFraction => ModSettings.CitizenCarMaxReductionPercent.value / 100f;

        // Root cause of a second stutter report (2026-08-13), same shape as the one
        // RerouteRateLimiter already fixed for vehicle reroutes: GetNearbyRoadDensity is a real
        // PathManager.FindPathPosition call, and this Postfix ran completely unthrottled once per
        // citizen trip decision - a busy city can start many trips in the same simulation frame.
        // Denied citizens simply keep vanilla's unmodified probability for this one decision -
        // there's no cooldown state to preserve here (unlike vehicle reroutes), so no special
        // handling is needed beyond just skipping the density check for this call.
        private static readonly PerFrameBudget Budget = new PerFrameBudget(5);

        // Updated without logging in the per-trip patch; one summary is emitted when the level unloads.
        private static int congestionReductionCount;
        private static int remoteParkedCarPreservationCount;
        private static bool hasActiveLevel;

        public static void ResetForNewLevel(SimulationManager.UpdateMode mode)
        {
            System.Threading.Interlocked.Exchange(ref congestionReductionCount, 0);
            System.Threading.Interlocked.Exchange(ref remoteParkedCarPreservationCount, 0);
            hasActiveLevel = true;
        }

        public static void ReportAndResetForLevel()
        {
            if (!hasActiveLevel)
            {
                return;
            }

            hasActiveLevel = false;
            int reductions = System.Threading.Interlocked.Exchange(ref congestionReductionCount, 0);
            int preserved = System.Threading.Interlocked.Exchange(ref remoteParkedCarPreservationCount, 0);
            Debug.Log(
                "[AIImprove] Citizen car probability FINAL: congestion reductions=" + reductions +
                ", preserved for parked car away from home=" + preserved + ".");
        }

        public static void Postfix(ref CitizenInstance citizenData, ref int __result)
        {
            if (!ModSettings.CitizenCarProbabilityEnabled.value)
            {
                return;
            }

            int originalProbability = __result;
            if (originalProbability <= 0)
            {
                return;
            }

            if (HasParkedVehicleAwayFromHome(ref citizenData))
            {
                System.Threading.Interlocked.Increment(ref remoteParkedCarPreservationCount);
                return;
            }

            float densityThreshold = DensityThreshold;
            if (densityThreshold >= 100f)
            {
                return;
            }

            if (!Budget.TryConsume())
            {
                return;
            }

            float density = CitizenCongestionQuery.GetNearbyRoadDensity(citizenData.m_targetPos);
            if (density < densityThreshold)
            {
                return;
            }

            float t = Mathf.Clamp01((density - densityThreshold) / (100f - densityThreshold));
            float reduction = t * MaxReductionFraction;
            int adjustedProbability = Mathf.Max(0, Mathf.RoundToInt(originalProbability * (1f - reduction)));
            if (adjustedProbability < originalProbability)
            {
                __result = adjustedProbability;
                System.Threading.Interlocked.Increment(ref congestionReductionCount);
            }
        }

        private static bool HasParkedVehicleAwayFromHome(ref CitizenInstance citizenData)
        {
            uint citizenId = citizenData.m_citizen;
            if (citizenId == 0)
            {
                return false;
            }

            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            if (citizenManager == null || vehicleManager == null || buildingManager == null)
            {
                return false;
            }

            Citizen[] citizens = citizenManager.m_citizens.m_buffer;
            if (citizenId >= (uint)citizens.Length)
            {
                return false;
            }

            Citizen citizen = citizens[(int)citizenId];
            ushort parkedVehicleId = citizen.m_parkedVehicle;
            ushort homeBuildingId = citizen.m_homeBuilding;
            if (parkedVehicleId == 0 || homeBuildingId == 0)
            {
                return false;
            }

            VehicleParked[] parkedVehicles = vehicleManager.m_parkedVehicles.m_buffer;
            Building[] buildings = buildingManager.m_buildings.m_buffer;
            if (parkedVehicleId >= parkedVehicles.Length || homeBuildingId >= buildings.Length)
            {
                return false;
            }

            VehicleParked parkedVehicle = parkedVehicles[parkedVehicleId];
            if ((((VehicleParked.Flags)parkedVehicle.m_flags) & VehicleParked.Flags.Created) == 0 ||
                parkedVehicle.m_ownerCitizen != citizenId)
            {
                return false;
            }

            Building homeBuilding = buildings[homeBuildingId];
            if ((homeBuilding.m_flags & Building.Flags.Created) == 0)
            {
                return false;
            }

            Vector3 vehiclePosition = parkedVehicle.m_position;
            Vector3 homePosition = homeBuilding.m_position;
            float dx = vehiclePosition.x - homePosition.x;
            float dy = vehiclePosition.y - homePosition.y;
            float dz = vehiclePosition.z - homePosition.z;
            float distanceSquared = dx * dx + dy * dy + dz * dz;
            return distanceSquared > HomeParkingDistanceThresholdSquared;
        }
    }
}
