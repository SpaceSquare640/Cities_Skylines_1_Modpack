using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace AIImprove
{
    // Applies the TM:PE emergency congestion adjustment at the TrafficCostFactor multiplier.
    // All TM:PE field discovery happens during patch setup; the injected instructions read the
    // cached fields directly and only count emergency adjustments through an allocation-free call.
    internal static class TmpeEmergencyCongestionPatch
    {
        private const BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const float EmergencyCongestionRetention = 0.25f;

        // Written by startup/settings UI and read directly by the injected pathfinding IL.
        // Keep this as a field so the transpiler never calls SavedBool.value on the hot path.
        public static volatile bool EmergencyCostAdjustmentEnabled;

        private static FieldInfo globalConfigurationField;
        private static FieldInfo advancedVehicleAiField;
        private static FieldInfo trafficCostFactorField;
        private static FieldInfo queueItemField;
        private static FieldInfo vehicleTypeField;
        private static FieldInfo emergencyCostAdjustmentEnabledField;
        private static MethodInfo recordAdjustmentMethod;
        private static int emergencyFlagValue;
        private static bool queueItemIsValueType;
        private static bool transpilerApplied;
        private static string failureReason;
        // Bounded patch-time diagnostics, including later Harmony rebuilds.
        // Signature: OpenAI-Codex_Responsible: AI_Improve
        private static int rejectionWarningLogged;
        private static long emergencyAdjustmentApplications;
        private static bool patchApplied;

        public static void SetAdjustmentEnabled(bool enabled)
        {
            EmergencyCostAdjustmentEnabled = enabled;
        }

        public static bool TryConfigure(Type customPathFindType, out string reason)
        {
            ResetConfiguration();

            if (customPathFindType == null)
            {
                reason = "CustomPathFind type was null.";
                failureReason = reason;
                return false;
            }

            try
            {
                globalConfigurationField = customPathFindType.GetField("globalConf_", InstanceFieldFlags);
                if (globalConfigurationField == null || globalConfigurationField.IsStatic)
                {
                    return Reject("CustomPathFind.globalConf_ field was not found.", out reason);
                }

                advancedVehicleAiField = globalConfigurationField.FieldType.GetField("AdvancedVehicleAI", InstanceFieldFlags);
                if (advancedVehicleAiField == null || advancedVehicleAiField.IsStatic)
                {
                    return Reject("GlobalConfig.AdvancedVehicleAI field was not found.", out reason);
                }

                trafficCostFactorField = advancedVehicleAiField.FieldType.GetField("TrafficCostFactor", InstanceFieldFlags);
                if (trafficCostFactorField == null || trafficCostFactorField.IsStatic ||
                    trafficCostFactorField.FieldType != typeof(float))
                {
                    return Reject("AdvancedVehicleAI.TrafficCostFactor float field was not found.", out reason);
                }

                queueItemField = customPathFindType.GetField("queueItem_", InstanceFieldFlags);
                if (queueItemField == null || queueItemField.IsStatic)
                {
                    return Reject("CustomPathFind.queueItem_ field was not found.", out reason);
                }

                queueItemIsValueType = queueItemField.FieldType.IsValueType;
                vehicleTypeField = queueItemField.FieldType.GetField("vehicleType", InstanceFieldFlags);
                if (vehicleTypeField == null || vehicleTypeField.IsStatic || !vehicleTypeField.FieldType.IsEnum ||
                    Enum.GetUnderlyingType(vehicleTypeField.FieldType) != typeof(int))
                {
                    return Reject("queueItem_.vehicleType was not an Int32-backed enum field.", out reason);
                }

                FieldInfo emergencyValue = vehicleTypeField.FieldType.GetField(
                    "Emergency",
                    BindingFlags.Public | BindingFlags.Static);
                if (emergencyValue == null || !emergencyValue.IsLiteral)
                {
                    return Reject("ExtVehicleType.Emergency enum value was not found.", out reason);
                }

                emergencyFlagValue = Convert.ToInt32(emergencyValue.GetRawConstantValue());
                if (emergencyFlagValue == 0)
                {
                    return Reject("ExtVehicleType.Emergency resolved to zero.", out reason);
                }

                emergencyCostAdjustmentEnabledField = typeof(TmpeEmergencyCongestionPatch).GetField(
                    nameof(EmergencyCostAdjustmentEnabled),
                    BindingFlags.Public | BindingFlags.Static);
                if (emergencyCostAdjustmentEnabledField == null ||
                    !emergencyCostAdjustmentEnabledField.IsStatic ||
                    emergencyCostAdjustmentEnabledField.FieldType != typeof(bool))
                {
                    return Reject("EmergencyCostAdjustmentEnabled static bool cache was not found.", out reason);
                }

                recordAdjustmentMethod = typeof(TmpeEmergencyCongestionPatch).GetMethod(
                    nameof(RecordAdjustmentApplied),
                    BindingFlags.Public | BindingFlags.Static);
                if (recordAdjustmentMethod == null)
                {
                    return Reject("Adjustment counter method was not found.", out reason);
                }

                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                return Reject("TM:PE fields could not be resolved: " + ex.Message, out reason);
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            transpilerApplied = false;
            failureReason = null;

            var code = new List<CodeInstruction>(instructions);
            if (generator == null || trafficCostFactorField == null || globalConfigurationField == null ||
                advancedVehicleAiField == null || queueItemField == null || vehicleTypeField == null ||
                emergencyCostAdjustmentEnabledField == null || recordAdjustmentMethod == null)
            {
                return RejectTranspiler(code, "Transpiler configuration was incomplete.");
            }

            var anchors = new List<int>();
            for (int i = 3; i + 5 < code.Count; i++)
            {
                if (code[i - 3].opcode == OpCodes.Ldarg_0 &&
                    IsFieldLoad(code[i - 2], globalConfigurationField) &&
                    IsFieldLoad(code[i - 1], advancedVehicleAiField) &&
                    IsFieldLoad(code[i], trafficCostFactorField) &&
                    IsLocalLoad(code[i + 1]) &&
                    code[i + 2].opcode == OpCodes.Mul &&
                    code[i + 3].opcode == OpCodes.Add &&
                    code[i + 4].opcode == OpCodes.Mul &&
                    code[i + 5].opcode == OpCodes.Stind_R4)
                {
                    anchors.Add(i);
                }
            }

            if (anchors.Count != 1)
            {
                return RejectTranspiler(
                    code,
                    "TrafficCostFactor multiplication anchor matched " + anchors.Count + " times; expected exactly one.");
            }

            int insertionIndex = anchors[0] + 1;
            if (code[insertionIndex].blocks.Count != 0)
            {
                return RejectTranspiler(code, "TrafficCostFactor anchor crosses an exception block boundary.");
            }

            Label skipAdjustment = generator.DefineLabel();
            var firstInjectedInstruction = new CodeInstruction(OpCodes.Ldarg_0);
            if (code[insertionIndex].labels.Count != 0)
            {
                firstInjectedInstruction.labels.AddRange(code[insertionIndex].labels);
                code[insertionIndex].labels.Clear();
            }
            code[insertionIndex].labels.Add(skipAdjustment);

            var injected = new List<CodeInstruction>
            {
                firstInjectedInstruction,
                new CodeInstruction(queueItemIsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, queueItemField),
                new CodeInstruction(OpCodes.Ldfld, vehicleTypeField),
                new CodeInstruction(OpCodes.Ldc_I4, emergencyFlagValue),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Brfalse, skipAdjustment),
                new CodeInstruction(OpCodes.Volatile),
                new CodeInstruction(OpCodes.Ldsfld, emergencyCostAdjustmentEnabledField),
                new CodeInstruction(OpCodes.Brfalse, skipAdjustment),
                new CodeInstruction(OpCodes.Ldc_R4, EmergencyCongestionRetention),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, recordAdjustmentMethod),
            };

            code.InsertRange(insertionIndex, injected);
            transpilerApplied = true;
            return code;
        }

        public static bool WasApplied
        {
            get { return transpilerApplied; }
        }

        public static string FailureReason
        {
            get { return failureReason ?? "Transpiler did not apply its IL change."; }
        }

        public static void MarkPatchApplied()
        {
            Interlocked.Exchange(ref emergencyAdjustmentApplications, 0L);
            patchApplied = true;
        }

        // Called only by the emergency branch injected during transpilation. This hot-path method
        // performs one atomic increment and does not allocate, reflect, or log.
        public static void RecordAdjustmentApplied()
        {
            Interlocked.Increment(ref emergencyAdjustmentApplications);
        }

        public static void LogFinalCount()
        {
            if (!patchApplied)
            {
                return;
            }

            long applications = Interlocked.Read(ref emergencyAdjustmentApplications);
            UnityEngine.Debug.Log(
                "[AIImprove] TMPE emergency congestion cost adjustment FINAL: " + applications + " applications.");
            patchApplied = false;
        }

        private static bool IsFieldLoad(CodeInstruction instruction, FieldInfo expectedField)
        {
            FieldInfo actualField = instruction.operand as FieldInfo;
            return instruction.opcode == OpCodes.Ldfld && actualField != null && actualField.Equals(expectedField);
        }

        private static bool IsLocalLoad(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Ldloc ||
                   instruction.opcode == OpCodes.Ldloc_S ||
                   instruction.opcode == OpCodes.Ldloc_0 ||
                   instruction.opcode == OpCodes.Ldloc_1 ||
                   instruction.opcode == OpCodes.Ldloc_2 ||
                   instruction.opcode == OpCodes.Ldloc_3;
        }

        private static IEnumerable<CodeInstruction> RejectTranspiler(List<CodeInstruction> original, string reason)
        {
            transpilerApplied = false;
            failureReason = reason;
            if (Interlocked.Exchange(ref rejectionWarningLogged, 1) == 0)
            {
                UnityEngine.Debug.LogWarning(
                    "[AIImprove] TMPE emergency congestion transpiler rejected; original IL retained: " + reason +
                    " Further rejection warnings are suppressed until configuration resets.");
            }
            return original;
        }

        private static bool Reject(string message, out string reason)
        {
            ResetConfiguration();
            reason = message;
            failureReason = message;
            return false;
        }

        private static void ResetConfiguration()
        {
            Interlocked.Exchange(ref rejectionWarningLogged, 0);
            globalConfigurationField = null;
            advancedVehicleAiField = null;
            trafficCostFactorField = null;
            queueItemField = null;
            vehicleTypeField = null;
            emergencyCostAdjustmentEnabledField = null;
            recordAdjustmentMethod = null;
            emergencyFlagValue = 0;
            queueItemIsValueType = false;
            transpilerApplied = false;
            failureReason = null;
        }
    }
}
