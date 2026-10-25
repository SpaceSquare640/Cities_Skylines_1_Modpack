using System;
using System.Globalization;
using ColossalFramework;
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
                        Debug.Log(FormatLogLine(Capture(simulation)));
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

        internal static void QueueCapture(Action<CitySnapshot> completed, Action<string> failed)
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
                        snapshot = Capture(simulation);
                    }
                    catch (Exception exception)
                    {
                        LogFailure(exception);
                        DispatchToUi(delegate { if (failed != null) failed("snapshot_unavailable"); });
                        return;
                    }

                    DispatchToUi(delegate { if (completed != null) completed(snapshot); });
                });
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                DispatchToUi(delegate { if (failed != null) failed("snapshot_unavailable"); });
            }
        }

        private static CitySnapshot Capture(SimulationManager simulation)
        {
            DistrictManager districts = Singleton<DistrictManager>.instance;
            ZoneManager zones = Singleton<ZoneManager>.instance;
            if (districts == null || districts.m_districts.m_buffer == null || zones == null)
            {
                throw new InvalidOperationException("No loaded city is available.");
            }

            return new CitySnapshot
            {
                GameDate = simulation.m_currentGameTime,
                Population = districts.m_districts.m_buffer[0].m_populationData.m_finalCount,
                FundsCents = EconomyManager.playerMoney,
                ResidentialDemand = zones.m_residentialDemand,
                CommercialDemand = zones.m_commercialDemand,
                WorkplaceDemand = zones.m_workplaceDemand
            };
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
    }
}
// Implementation update signed by OpenAI-Codex.
