using System;
using System.Collections.Generic;
using System.Globalization;
using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using UnityEngine;

namespace AIGaming
{
    internal static class CitySummary
    {
        internal static void QueueWrite()
        {
            try
            {
                SimulationManager simulation = Singleton<SimulationManager>.instance;
                if (simulation == null)
                {
                    Debug.LogWarning("[AI Gaming] City summary unavailable: the simulation is not ready.");
                    return;
                }

                simulation.AddAction(delegate
                {
                    try
                    {
                        Debug.Log(FormatLogLine(Capture(simulation, null)));
                    }
                    catch (Exception exception)
                    {
                        LogFailure(exception);
                    }
                });
            }
            catch (Exception exception)
            {
                LogFailure(exception);
            }
        }

        internal static void QueueCapture(CityCaptureOptions options, Action<CitySnapshot> completed, Action<string> failed)
        {
            try
            {
                SimulationManager simulation = Singleton<SimulationManager>.instance;
                if (simulation == null)
                {
                    DispatchToUi(delegate { if (failed != null) failed("simulation_unavailable"); });
                    return;
                }

                simulation.AddAction(delegate
                {
                    CitySnapshot snapshot;
                    try
                    {
                        snapshot = Capture(simulation, options);
                    }
                    catch (Exception exception)
                    {
                        LogFailure(exception);
                        DispatchToUi(delegate { if (failed != null) failed("snapshot_unavailable"); });
                        return;
                    }

                    DispatchToUi(delegate
                    {
                        try
                        {
                            snapshot.InstalledAvailableDlcIdentifiers = CaptureInstalledAvailableDlcIdentifiers();
                        }
                        catch (Exception exception)
                        {
                            LogFailure(exception);
                            if (failed != null) failed("dlc_detection_unavailable");
                            return;
                        }

                        if (completed != null) completed(snapshot);
                    });
                });
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                DispatchToUi(delegate { if (failed != null) failed("snapshot_unavailable"); });
            }
        }

        private static CitySnapshot Capture(SimulationManager simulation, CityCaptureOptions options)
        {
            DistrictManager districts = Singleton<DistrictManager>.instance;
            ZoneManager zones = Singleton<ZoneManager>.instance;
            if (districts == null || districts.m_districts.m_buffer == null || zones == null)
            {
                throw new InvalidOperationException("No loaded city is available.");
            }

            District district = districts.m_districts.m_buffer[0];
            var snapshot = new CitySnapshot
            {
                GameDate = simulation.m_currentGameTime,
                Population = district.m_populationData.m_finalCount,
                FundsCents = EconomyManager.playerMoney,
                ResidentialDemand = zones.m_residentialDemand,
                CommercialDemand = zones.m_commercialDemand,
                WorkplaceDemand = zones.m_workplaceDemand
            };

            if (options != null && options.IncludeUtilities)
            {
                snapshot.Utilities = new UtilitiesSnapshot
                {
                    ElectricityCapacity = district.GetElectricityCapacity(),
                    ElectricityConsumption = district.GetElectricityConsumption(),
                    WaterCapacity = district.GetWaterCapacity(),
                    WaterConsumption = district.GetWaterConsumption(),
                    SewageCapacity = district.GetSewageCapacity(),
                    SewageAccumulation = district.GetSewageAccumulation(),
                    GarbageAmount = district.GetGarbageAmount(),
                    GarbageCapacity = district.GetGarbageCapacity(),
                    GarbageAccumulation = district.GetGarbageAccumulation(),
                    IncinerationCapacity = district.GetIncinerationCapacity()
                };
            }

            if (options != null && options.IncludeZoning)
            {
                snapshot.Zoning = new ZoningSnapshot
                {
                    ResidentialBuildingCount = district.m_residentialData.m_finalBuildingCount,
                    ResidentialBuildingArea = district.m_residentialData.m_finalBuildingArea,
                    CommercialBuildingCount = district.m_commercialData.m_finalBuildingCount,
                    CommercialBuildingArea = district.m_commercialData.m_finalBuildingArea,
                    IndustrialBuildingCount = district.m_industrialData.m_finalBuildingCount,
                    IndustrialBuildingArea = district.m_industrialData.m_finalBuildingArea,
                    OfficeBuildingCount = district.m_officeData.m_finalBuildingCount,
                    OfficeBuildingArea = district.m_officeData.m_finalBuildingArea
                };
            }

            if (options != null && options.IncludeTrafficTransit)
            {
                StatisticsManager statistics = Singleton<StatisticsManager>.instance;
                if (statistics == null) throw new InvalidOperationException("City statistics are unavailable.");
                StatisticBase trafficFlow = statistics.Get(StatisticType.TrafficFlow);
                StatisticBase averagePassengers = statistics.Get(StatisticType.AveragePassengers);
                if (trafficFlow == null || averagePassengers == null)
                {
                    throw new InvalidOperationException("Traffic or transport statistics are unavailable.");
                }
                snapshot.TrafficTransit = new TrafficTransitSnapshot
                {
                    TrafficFlow = trafficFlow.GetLatestInt32(),
                    AveragePassengers = averagePassengers.GetLatestInt32()
                };
            }

            if (options != null && options.IncludeServiceStatus)
            {
                snapshot.ServiceStatus = CaptureServiceStatus(district);
            }

            return snapshot;
        }

        private static ServiceStatusSnapshot CaptureServiceStatus(District district)
        {
            NetManager network = Singleton<NetManager>.instance;
            if (network == null || network.m_segments.m_buffer == null)
            {
                throw new InvalidOperationException("Network segment data is unavailable.");
            }

            NetSegment[] segments = network.m_segments.m_buffer;
            long fireCoverageTotal = 0;
            int coveredSegmentCount = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                NetSegment segment = segments[i];
                if ((segment.m_flags & NetSegment.Flags.Created) == 0) continue;
                fireCoverageTotal += segment.m_fireCoverage;
                coveredSegmentCount++;
            }

            return new ServiceStatusSnapshot
            {
                PoliceCriminalAmount = district.GetCriminalAmount(),
                PoliceCriminalCapacity = district.GetCriminalCapacity(),
                PoliceExtraCriminals = district.GetExtraCriminals(),
                FireCoverageMeanByte = coveredSegmentCount == 0 ? 0f : (float)fireCoverageTotal / coveredSegmentCount,
                FireCoverageSegmentCount = coveredSegmentCount,
                HealthSickCount = district.GetSickCount(),
                HealthCapacity = district.GetHealCapacity(),
                HealthCareProductivity = district.GetHealthCareProductivity()
            };
        }

        private static string[] CaptureInstalledAvailableDlcIdentifiers()
        {
            if (!PlatformService.IsDLCStateReady())
            {
                throw new InvalidOperationException("The game DLC state is not ready.");
            }

            var availableDlcs = new HashSet<SteamHelper.DLC>();
            foreach (SteamHelper.DLC dlc in SteamHelper.DLCs(
                unchecked((SteamHelper.ExpansionBitMask)(-1)),
                unchecked((SteamHelper.ModderPackBitMask)(-1)),
                unchecked((SteamHelper.RadioBitMask)(-1))))
            {
                if (dlc == SteamHelper.DLC.None || dlc == SteamHelper.DLC.NotAllowed) continue;
                if (SteamHelper.IsDLCOwned(dlc) && SteamHelper.IsDLCAvailable(dlc))
                {
                    availableDlcs.Add(dlc);
                }
            }

            var identifiers = new List<string>();
            foreach (SteamHelper.DLC dlc in availableDlcs)
            {
                identifiers.Add(dlc.ToString());
            }
            identifiers.Sort(StringComparer.Ordinal);
            return identifiers.ToArray();
        }

        private static string FormatLogLine(CitySnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[AI Gaming] City snapshot | schemaVersion=1 | gameDate={0:yyyy-MM-dd} | population={1} | fundsCents={2} | residentialDemand={3} | commercialDemand={4} | workplaceDemand={5}",
                snapshot.GameDate,
                snapshot.Population,
                snapshot.FundsCents,
                snapshot.ResidentialDemand,
                snapshot.CommercialDemand,
                snapshot.WorkplaceDemand);
        }

        private static void DispatchToUi(Action action)
        {
            try
            {
                Dispatcher dispatcher = ThreadHelper.dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Dispatch(action);
                }
            }
            catch (Exception exception)
            {
                LogFailure(exception);
            }
        }

        private static void LogFailure(Exception exception)
        {
            try
            {
                Debug.LogError("[AI Gaming] City snapshot failed: exceptionType=" + exception.GetType().Name + ".");
            }
            catch
            {
                // A logging failure must not escape into the game.
            }
        }
    }

    internal sealed class CitySnapshot
    {
        internal DateTime GameDate { get; set; }
        internal uint Population { get; set; }
        internal long FundsCents { get; set; }
        internal int ResidentialDemand { get; set; }
        internal int CommercialDemand { get; set; }
        internal int WorkplaceDemand { get; set; }
        internal string[] InstalledAvailableDlcIdentifiers { get; set; }
        internal UtilitiesSnapshot Utilities { get; set; }
        internal ZoningSnapshot Zoning { get; set; }
        internal TrafficTransitSnapshot TrafficTransit { get; set; }
        internal ServiceStatusSnapshot ServiceStatus { get; set; }
    }

    internal sealed class CityCaptureOptions
    {
        internal bool IncludeUtilities { get; set; }
        internal bool IncludeZoning { get; set; }
        internal bool IncludeTrafficTransit { get; set; }
        internal bool IncludeServiceStatus { get; set; }
    }

    internal sealed class UtilitiesSnapshot
    {
        internal int ElectricityCapacity { get; set; }
        internal int ElectricityConsumption { get; set; }
        internal int WaterCapacity { get; set; }
        internal int WaterConsumption { get; set; }
        internal int SewageCapacity { get; set; }
        internal int SewageAccumulation { get; set; }
        internal int GarbageAmount { get; set; }
        internal int GarbageCapacity { get; set; }
        internal int GarbageAccumulation { get; set; }
        internal int IncinerationCapacity { get; set; }
    }

    internal sealed class ZoningSnapshot
    {
        internal ushort ResidentialBuildingCount { get; set; }
        internal uint ResidentialBuildingArea { get; set; }
        internal ushort CommercialBuildingCount { get; set; }
        internal uint CommercialBuildingArea { get; set; }
        internal ushort IndustrialBuildingCount { get; set; }
        internal uint IndustrialBuildingArea { get; set; }
        internal ushort OfficeBuildingCount { get; set; }
        internal uint OfficeBuildingArea { get; set; }
    }

    internal sealed class TrafficTransitSnapshot
    {
        internal int TrafficFlow { get; set; }
        internal int AveragePassengers { get; set; }
    }

    internal sealed class ServiceStatusSnapshot
    {
        internal int PoliceCriminalAmount { get; set; }
        internal int PoliceCriminalCapacity { get; set; }
        internal int PoliceExtraCriminals { get; set; }
        internal float FireCoverageMeanByte { get; set; }
        internal int FireCoverageSegmentCount { get; set; }
        internal int HealthSickCount { get; set; }
        internal int HealthCapacity { get; set; }
        internal int HealthCareProductivity { get; set; }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
