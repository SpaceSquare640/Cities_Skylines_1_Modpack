namespace AIGaming
{
    internal sealed class RequestCaptureGeneration
    {
        private long _captureGeneration;
        private long _responseGeneration;

        internal long Begin()
        {
            unchecked { return ++_captureGeneration; }
        }

        internal void Invalidate()
        {
            unchecked { _captureGeneration++; }
        }

        internal bool IsCurrent(long generation)
        {
            return generation == _captureGeneration;
        }

        internal long BeginResponse()
        {
            unchecked { return ++_responseGeneration; }
        }

        internal void InvalidateResponse()
        {
            unchecked { _responseGeneration++; }
        }

        internal bool IsResponseCurrent(long generation)
        {
            return generation == _responseGeneration;
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
