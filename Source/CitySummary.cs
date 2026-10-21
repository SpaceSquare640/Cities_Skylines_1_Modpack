using System;
using System.Globalization;
using ColossalFramework;
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
                        DistrictManager districts = Singleton<DistrictManager>.instance;
                        ZoneManager zones = Singleton<ZoneManager>.instance;
                        if (districts == null || districts.m_districts.m_buffer == null || zones == null)
                        {
                            Debug.LogWarning("[AI Gaming] City snapshot unavailable: no loaded city was found.");
                            return;
                        }

                        uint population = districts.m_districts.m_buffer[0].m_populationData.m_finalCount;
                        long fundsCents = EconomyManager.playerMoney;
                        DateTime gameDate = simulation.m_currentGameTime;
                        int residentialDemand = zones.m_residentialDemand;
                        int commercialDemand = zones.m_commercialDemand;
                        int workplaceDemand = zones.m_workplaceDemand;
                        string snapshot = string.Format(
                            CultureInfo.InvariantCulture,
                            "[AI Gaming] City snapshot | schemaVersion=1 | gameDate={0:yyyy-MM-dd} | population={1} | fundsCents={2} | residentialDemand={3} | commercialDemand={4} | workplaceDemand={5}",
                            gameDate,
                            population,
                            fundsCents,
                            residentialDemand,
                            commercialDemand,
                            workplaceDemand);

                        Debug.Log(snapshot);
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

        private static void LogFailure(Exception exception)
        {
            try
            {
                Debug.LogError("[AI Gaming] City snapshot failed: " + exception.GetType().Name + ": " + exception.Message);
            }
            catch
            {
                // A logging failure must not escape into the game.
            }
        }
    }
}
