using System.Collections.Generic;
using HarmonyLib;

namespace AIImprove
{
    // Feasibility-only transpilers. They pass the original instruction sequence through
    // unchanged so Harmony/Mono patch application can be tested without changing game behavior.
    internal static class TmpeCalculateAdvancedAiCostFactorsSpike
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions;
        }
    }

    internal static class TmpeProcessItemMainSpike
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions;
        }
    }
}
