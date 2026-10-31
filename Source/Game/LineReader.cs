// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using System;
using ColossalFramework;
using AutomatedPublicTransportVehicleManagement.Model;

namespace AutomatedPublicTransportVehicleManagement.Game
{
    // Called only from the simulation thread. No simulation state is written.
    internal static class LineReader
    {
        internal static bool TryCapture(ushort lineId, int session, long sequence, out LineObservation result)
        {
            result = null;
            if (!Singleton<TransportManager>.exists || !Singleton<VehicleManager>.exists) return false;
            TransportManager manager = Singleton<TransportManager>.instance;
            if (lineId == 0 || lineId >= manager.m_lines.m_buffer.Length) return false;
            if ((manager.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == 0) return false;
            TransportInfo info;
            // Info hides the prefab index. A removed prefab may throw before a null check.
            try { info = manager.m_lines.m_buffer[lineId].Info; }
            catch (Exception) { result = new LineObservation(new LineKey(session, lineId, sequence), null, null, ObservationQuality.InvalidPrefab); return true; }
            if (info == null) { result = new LineObservation(new LineKey(session, lineId, sequence), null, null, ObservationQuality.InvalidPrefab); return true; }
            // Deliberately exclude ambiguous and intercity prefabs instead of guessing by name.
            if (info.m_transportType != TransportInfo.TransportType.Bus || info.m_class == null || info.m_class.m_level != ItemClass.Level.Level1) return false;
            if (!manager.m_lines.m_buffer[lineId].Complete)
            {
                result = new LineObservation(new LineKey(session, lineId, sequence), null, null, ObservationQuality.Incomplete);
                return true;
            }
            VehicleManager vehicles = Singleton<VehicleManager>.instance;
            ushort vehicle = manager.m_lines.m_buffer[lineId].m_vehicles;
            int count = 0;
            // Bound traversal by the actual buffer length; cyclic/corrupt chains become unknown.
            while (vehicle != 0)
            {
                if (vehicle >= vehicles.m_vehicles.m_buffer.Length || count >= Math.Min(512, vehicles.m_vehicles.m_buffer.Length))
                {
                    result = new LineObservation(new LineKey(session, lineId, sequence), null, null, ObservationQuality.Unavailable);
                    return true;
                }
                count++;
                vehicle = vehicles.m_vehicles.m_buffer[vehicle].m_nextLineVehicle;
            }
            // A safe target-count accessor has not yet been established for this module.
            result = new LineObservation(new LineKey(session, lineId, sequence), null, count, ObservationQuality.Valid);
            return true;
        }
    }
}

