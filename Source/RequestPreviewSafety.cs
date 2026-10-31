using System;

namespace AIGaming
{
    internal static class RequestPreviewSafety
    {
        internal static bool CanConfirm(bool enabled, bool rendered, object displayedRequest, object preparedRequest)
        {
            return enabled && rendered && preparedRequest != null && ReferenceEquals(displayedRequest, preparedRequest);
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
