using System;
using System.Globalization;

namespace AIGaming
{
    internal static class DiagnosticLogPolicy
    {
        internal const string Marker = "# AI Gaming diagnostic log v1";

        internal static bool ShouldLog(string edition, bool warning)
        {
            return warning || string.Equals(edition, "development", StringComparison.Ordinal);
        }

        internal static TimeSpan GetCleanupDelay(long lastTicks, DateTime nowUtc)
        {
            if (lastTicks <= 0 || lastTicks > nowUtc.Ticks) return TimeSpan.Zero;
            long remaining = TimeSpan.FromHours(48).Ticks - (nowUtc.Ticks - lastTicks);
            return remaining > 0 ? TimeSpan.FromTicks(remaining) : TimeSpan.Zero;
        }

        internal static string GetProviderLabel(string providerId)
        {
            if (providerId == "com.spacesquare640.ai-improve") return "AI Improve";
            if (providerId == "com.spacesquare640.automated-public-transport-planning") return "Transport";
            return "unknown";
        }

        internal static bool ShouldDelete(string name, string marker, DateTime modifiedUtc, DateTime nowUtc, bool active)
        {
            const string prefix = "ai-gaming-";
            const string suffix = ".log";
            if (active || !string.Equals(marker, Marker, StringComparison.Ordinal) || name == null ||
                name.Length != prefix.Length + 10 + suffix.Length ||
                !name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal)) return false;
            DateTime date;
            return DateTime.TryParseExact(name.Substring(prefix.Length, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date) && modifiedUtc < nowUtc.AddHours(-48);
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
