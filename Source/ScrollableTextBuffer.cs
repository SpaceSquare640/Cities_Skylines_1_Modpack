using System;

namespace AIGaming
{
    internal sealed class ScrollableTextBuffer
    {
        private readonly string _text;

        internal ScrollableTextBuffer(string text)
        {
            _text = text ?? string.Empty;
        }

        internal int Length { get { return _text.Length; } }

        internal string ReadWindow(int start, int visibleCharacters, out int next)
        {
            if (visibleCharacters <= 0) throw new ArgumentOutOfRangeException("visibleCharacters");
            start = Math.Max(0, Math.Min(start, _text.Length));
            if (start > 0 && start < _text.Length && char.IsLowSurrogate(_text[start]) && char.IsHighSurrogate(_text[start - 1])) start--;
            int count = Math.Min(visibleCharacters, _text.Length - start);
            int end = start + count;
            if (end > start && end < _text.Length && char.IsHighSurrogate(_text[end - 1]) && char.IsLowSurrogate(_text[end])) end++;
            next = end;
            return _text.Substring(start, end - start);
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
