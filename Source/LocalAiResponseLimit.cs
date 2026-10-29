using System.Globalization;

namespace AIGaming
{
    internal static class LocalAiResponseLimit
    {
        internal const string PreferenceKey = "AIGaming.LocalAiResponseLimitMiB";
        internal const int DefaultMiB = 0;
        internal const int BytesPerMiB = 1024 * 1024;
        internal const int MaximumMiB = int.MaxValue / BytesPerMiB;

        internal static bool TryParseMiB(string text, out int mebibytes, out int maximumResponseBytes)
        {
            mebibytes = DefaultMiB;
            maximumResponseBytes = 0;
            string value = text == null ? string.Empty : text.Trim();
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) ||
                parsed < DefaultMiB || parsed > MaximumMiB)
            {
                return false;
            }

            mebibytes = parsed;
            maximumResponseBytes = parsed == DefaultMiB ? 0 : checked(parsed * BytesPerMiB);
            return true;
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
