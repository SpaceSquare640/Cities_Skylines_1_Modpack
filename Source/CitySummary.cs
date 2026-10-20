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
                        CitizenManager citizens = Singleton<CitizenManager>.instance;
                        if (citizens == null)
                        {
                            Debug.LogWarning("[AI Gaming] City summary unavailable: no loaded city was found.");
                            return;
                        }

                        int population = citizens.m_citizenCount;
                        long funds = EconomyManager.playerMoney;
                        DateTime gameDate = simulation.m_currentGameTime;
                        string summary = string.Format(
                            CultureInfo.InvariantCulture,
                            "[AI Gaming] City summary | Date: {0:yyyy-MM-dd} | Population: {1:N0} | Funds: {2:N0}",
                            gameDate,
                            population,
                            funds);

                        Debug.Log(summary);
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
                Debug.LogError("[AI Gaming] City summary failed: " + exception.GetType().Name + ": " + exception.Message);
            }
            catch
            {
                // A logging failure must not escape into the game.
            }
        }
    }
}
