using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace AIImprove
{
    // Signed: OpenAI-Codex (2026-10-25).
    // DEVELOPMENT ONLY. Observes sanitation vehicle unspawn/release/re-spawn lifecycle without
    // changing vehicle state. This file is compiled only with -p:DevTools=true.
    internal static class SanitationLifecycleProbe
    {
        private const string HarmonyId = "spacesquare.aiimprove.dev.sanitation-lifecycle";
        private const int MaxPendingVehicles = 64;
        private const int MaxLogEvents = 128;
        private const uint StillUnspawnedAfterFrames = 256;

        private static readonly object PendingLock = new object();
        private static readonly Dictionary<ushort, PendingUnspawn> Pending =
            new Dictionary<ushort, PendingUnspawn>();

        private static bool installed;
        private static int pendingCount;
        private static int loggedEvents;
        private static int unspawnEvents;
        private static int resumedVehicles;
        private static int releasedVehicles;
        private static int stillUnspawnedVehicles;
        private static int droppedPendingVehicles;
        private static bool logLimitReported;
        private static bool pendingLimitReported;

        public struct UnspawnSnapshot
        {
            public bool IsSanitationVehicle;
            public TransferManager.TransferReason Material;
            public Vehicle.Flags FlagsBefore;
            public ushort TargetBefore;
            public bool HadAssignmentBefore;
            public ushort AssignmentBefore;
        }

        private struct PendingUnspawn
        {
            public TransferManager.TransferReason Material;
            public uint Frame;
            public bool ReportedStillUnspawned;
        }

        public static void Install()
        {
            if (installed)
            {
                return;
            }

            ResetCounters();
            Harmony harmony = new Harmony(HarmonyId);
            Type[] vehicleStepParameters =
            {
                typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vector3)
            };

            int patched = 0;
            patched += TryPatch(
                harmony, typeof(Vehicle), "Unspawn", new[] { typeof(ushort) },
                nameof(UnspawnPrefix), nameof(UnspawnPostfix));
            patched += TryPatch(
                harmony, typeof(VehicleAI), "ReleaseVehicle",
                new[] { typeof(ushort), typeof(Vehicle).MakeByRefType() },
                nameof(ReleasePrefix), null);
            patched += TryPatch(
                harmony, typeof(GarbageTruckAI), "SimulationStep", vehicleStepParameters,
                null, nameof(GarbageSimulationStepPostfix));
            patched += TryPatch(
                harmony, typeof(HearseAI), "SimulationStep", vehicleStepParameters,
                null, nameof(HearseSimulationStepPostfix));

            installed = patched > 0;
            if (installed)
            {
                Debug.Log("[AIImprove] DEV sanitation lifecycle probe installed (" + patched + "/4 hooks).");
            }
            else
            {
                Debug.LogWarning("[AIImprove] DEV sanitation lifecycle probe could not install any hooks.");
            }
        }

        public static void Uninstall()
        {
            if (!installed)
            {
                return;
            }

            int unresolved;
            lock (PendingLock)
            {
                unresolved = Pending.Count;
            }

            if (Log.VerboseEnabled)
            {
                Log.Verbose(
                    "[AIImprove] DEV sanitation lifecycle FINAL: " + unspawnEvents + " unspawn events, " +
                    resumedVehicles + " later spawned, " + stillUnspawnedVehicles +
                    " still created/unspawned after " + StillUnspawnedAfterFrames + " simulation frames, " +
                    releasedVehicles + " full releases, " + unresolved + " unresolved pending IDs, " +
                    droppedPendingVehicles + " pending records dropped at capacity.");
            }

            try
            {
                new Harmony(HarmonyId).UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AIImprove] DEV sanitation lifecycle unpatch failed: " + ex.Message);
            }

            lock (PendingLock)
            {
                Pending.Clear();
                pendingCount = 0;
            }

            installed = false;
        }

        private static int TryPatch(
            Harmony harmony, Type targetType, string targetName, Type[] parameters,
            string prefixName, string postfixName)
        {
            try
            {
                MethodInfo original = AccessTools.Method(targetType, targetName, parameters);
                if (original == null)
                {
                    Debug.LogWarning(
                        "[AIImprove] DEV sanitation lifecycle target not found: " +
                        targetType.Name + "." + targetName + ".");
                    return 0;
                }

                MethodInfo prefix = prefixName == null ? null : GetPatchMethod(prefixName);
                MethodInfo postfix = postfixName == null ? null : GetPatchMethod(postfixName);
                if ((prefixName != null && prefix == null) ||
                    (postfixName != null && postfix == null))
                {
                    Debug.LogWarning(
                        "[AIImprove] DEV sanitation lifecycle patch method missing for " +
                        targetType.Name + "." + targetName + ".");
                    return 0;
                }

                harmony.Patch(
                    original,
                    prefix: prefix == null ? null : new HarmonyMethod(prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(postfix));
                return 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[AIImprove] DEV sanitation lifecycle patch failed for " +
                    targetType.Name + "." + targetName + ": " + ex.Message);
                return 0;
            }
        }

        private static MethodInfo GetPatchMethod(string name)
        {
            return typeof(SanitationLifecycleProbe).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static);
        }

        public static void UnspawnPrefix(
            ref Vehicle __instance, ushort vehicleID, out UnspawnSnapshot __state)
        {
            __state = new UnspawnSnapshot();
            if (!Log.VerboseEnabled)
            {
                return;
            }

            TransferManager.TransferReason material;
            if (!TryGetMaterial(__instance.Info, out material))
            {
                return;
            }

            ushort assignedBuilding;
            __state.IsSanitationVehicle = true;
            __state.Material = material;
            __state.FlagsBefore = __instance.m_flags;
            __state.TargetBefore = __instance.m_targetBuilding;
            __state.HadAssignmentBefore = SanitationIdleSeekTracker.TryGetAssignedBuilding(
                material, vehicleID, out assignedBuilding);
            __state.AssignmentBefore = assignedBuilding;
        }

        public static void UnspawnPostfix(
            ref Vehicle __instance, ushort vehicleID, UnspawnSnapshot __state)
        {
            if (!__state.IsSanitationVehicle || !Log.VerboseEnabled)
            {
                return;
            }

            ushort assignedBuilding;
            bool hasAssignment = SanitationIdleSeekTracker.TryGetAssignedBuilding(
                __state.Material, vehicleID, out assignedBuilding);
            Vehicle.Flags flagsAfter = __instance.m_flags;
            ushort targetAfter = __instance.m_targetBuilding;
            bool created = (flagsAfter & Vehicle.Flags.Created) != 0;
            bool spawned = (flagsAfter & Vehicle.Flags.Spawned) != 0;
            bool hasRelevantState = __state.HadAssignmentBefore || hasAssignment ||
                                    __state.TargetBefore != 0 || targetAfter != 0;

            if (hasRelevantState)
            {
                Interlocked.Increment(ref unspawnEvents);
                Emit(
                    "[AIImprove] DEV sanitation lifecycle UNSPAWN " + __state.Material +
                    " vehicle " + vehicleID + "; flags " + __state.FlagsBefore + " -> " + flagsAfter +
                    "; target " + __state.TargetBefore + " -> " + targetAfter +
                    "; tracked building " + FormatAssignment(__state.HadAssignmentBefore, __state.AssignmentBefore) +
                    " -> " + FormatAssignment(hasAssignment, assignedBuilding) + ".");
            }

            if (created && !spawned && hasRelevantState)
            {
                AddPending(vehicleID, __state.Material);
            }
        }

        public static void ReleasePrefix(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            TransferManager.TransferReason material;
            if (!TryGetMaterial(__instance, out material))
            {
                return;
            }

            ushort assignedBuilding;
            bool hasAssignment = SanitationIdleSeekTracker.TryGetAssignedBuilding(
                material, vehicleID, out assignedBuilding);
            bool wasPending = RemovePending(vehicleID, material);
            if (!Log.VerboseEnabled || (!hasAssignment && !wasPending))
            {
                return;
            }

            Interlocked.Increment(ref releasedVehicles);
            Emit(
                "[AIImprove] DEV sanitation lifecycle FULL RELEASE " + material + " vehicle " + vehicleID +
                "; flags " + data.m_flags + "; target " + data.m_targetBuilding +
                "; tracked building " + FormatAssignment(hasAssignment, assignedBuilding) + ".");
        }

        public static void GarbageSimulationStepPostfix(ushort vehicleID, ref Vehicle data)
        {
            ObserveSimulationStep(TransferManager.TransferReason.Garbage, vehicleID, ref data);
        }

        public static void HearseSimulationStepPostfix(ushort vehicleID, ref Vehicle data)
        {
            ObserveSimulationStep(TransferManager.TransferReason.Dead, vehicleID, ref data);
        }

        private static void ObserveSimulationStep(
            TransferManager.TransferReason material, ushort vehicleID, ref Vehicle data)
        {
            if (!Log.VerboseEnabled || Interlocked.CompareExchange(ref pendingCount, 0, 0) == 0)
            {
                return;
            }

            PendingUnspawn pending;
            lock (PendingLock)
            {
                if (!Pending.TryGetValue(vehicleID, out pending) || pending.Material != material)
                {
                    return;
                }
            }

            Vehicle.Flags flags = data.m_flags;
            ushort assignedBuilding;
            bool hasAssignment = SanitationIdleSeekTracker.TryGetAssignedBuilding(
                material, vehicleID, out assignedBuilding);
            uint frame = CurrentFrame();

            if ((flags & Vehicle.Flags.Created) == 0)
            {
                if (RemovePending(vehicleID, material))
                {
                    Emit(
                        "[AIImprove] DEV sanitation lifecycle NO LONGER CREATED " + material +
                        " vehicle " + vehicleID + "; tracked building " +
                        FormatAssignment(hasAssignment, assignedBuilding) + ".");
                }

                return;
            }

            if ((flags & Vehicle.Flags.Spawned) != 0)
            {
                if (RemovePending(vehicleID, material))
                {
                    Interlocked.Increment(ref resumedVehicles);
                    Emit(
                        "[AIImprove] DEV sanitation lifecycle RESPAWNED " + material + " vehicle " +
                        vehicleID + "; target " + data.m_targetBuilding + "; tracked building " +
                        FormatAssignment(hasAssignment, assignedBuilding) + ".");
                }

                return;
            }

            if (!pending.ReportedStillUnspawned &&
                unchecked(frame - pending.Frame) >= StillUnspawnedAfterFrames)
            {
                pending.ReportedStillUnspawned = true;
                lock (PendingLock)
                {
                    PendingUnspawn current;
                    if (Pending.TryGetValue(vehicleID, out current) && current.Material == material)
                    {
                        Pending[vehicleID] = pending;
                    }
                }

                Interlocked.Increment(ref stillUnspawnedVehicles);
                Emit(
                    "[AIImprove] DEV sanitation lifecycle STILL CREATED/UNSPAWNED " + material +
                    " vehicle " + vehicleID + " after " + StillUnspawnedAfterFrames +
                    " simulation frames; target " + data.m_targetBuilding + "; tracked building " +
                    FormatAssignment(hasAssignment, assignedBuilding) + ".");
            }
        }

        private static void AddPending(ushort vehicleID, TransferManager.TransferReason material)
        {
            lock (PendingLock)
            {
                if (Pending.ContainsKey(vehicleID))
                {
                    Pending[vehicleID] = CreatePending(material);
                    return;
                }

                if (Pending.Count >= MaxPendingVehicles)
                {
                    Interlocked.Increment(ref droppedPendingVehicles);
                    if (!pendingLimitReported)
                    {
                        pendingLimitReported = true;
                        Debug.LogWarning(
                            "[AIImprove] DEV sanitation lifecycle pending-record limit reached; " +
                            "further unique unspawn IDs will not be tracked.");
                    }

                    return;
                }

                Pending.Add(vehicleID, CreatePending(material));
                pendingCount = Pending.Count;
            }
        }

        private static PendingUnspawn CreatePending(TransferManager.TransferReason material)
        {
            return new PendingUnspawn
            {
                Material = material,
                Frame = CurrentFrame(),
                ReportedStillUnspawned = false,
            };
        }

        private static bool RemovePending(ushort vehicleID, TransferManager.TransferReason material)
        {
            lock (PendingLock)
            {
                PendingUnspawn pending;
                if (!Pending.TryGetValue(vehicleID, out pending) || pending.Material != material)
                {
                    return false;
                }

                Pending.Remove(vehicleID);
                pendingCount = Pending.Count;
                return true;
            }
        }

        private static uint CurrentFrame()
        {
            SimulationManager simulation = Singleton<SimulationManager>.instance;
            return simulation == null ? 0u : simulation.m_currentFrameIndex;
        }

        private static bool TryGetMaterial(VehicleInfo info, out TransferManager.TransferReason material)
        {
            return TryGetMaterial(info == null ? null : info.m_vehicleAI, out material);
        }

        private static bool TryGetMaterial(VehicleAI vehicleAI, out TransferManager.TransferReason material)
        {
            if (vehicleAI is GarbageTruckAI)
            {
                material = TransferManager.TransferReason.Garbage;
                return true;
            }

            if (vehicleAI is HearseAI)
            {
                material = TransferManager.TransferReason.Dead;
                return true;
            }

            material = default(TransferManager.TransferReason);
            return false;
        }

        private static string FormatAssignment(bool hasAssignment, ushort buildingID)
        {
            return hasAssignment ? "building " + buildingID : "none";
        }

        private static void Emit(string message)
        {
            if (!Log.VerboseEnabled)
            {
                return;
            }

            int eventNumber = Interlocked.Increment(ref loggedEvents);
            if (eventNumber > MaxLogEvents)
            {
                if (!logLimitReported)
                {
                    logLimitReported = true;
                    Debug.LogWarning("[AIImprove] DEV sanitation lifecycle log limit reached; suppressing further events.");
                }

                return;
            }

            Log.Verbose(message);
        }

        private static void ResetCounters()
        {
            lock (PendingLock)
            {
                Pending.Clear();
                pendingCount = 0;
            }

            loggedEvents = 0;
            unspawnEvents = 0;
            resumedVehicles = 0;
            releasedVehicles = 0;
            stillUnspawnedVehicles = 0;
            droppedPendingVehicles = 0;
            logLimitReported = false;
            pendingLimitReported = false;
        }
    }
}
