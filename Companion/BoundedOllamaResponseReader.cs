using System.Net.Http;

namespace AIGaming.Companion;

internal static class BoundedOllamaResponseReader
{
    internal const int MaximumResponseBytes = 1024 * 1024;
    private const int BufferSize = 8192;

    internal static Task<byte[]> ReadUnboundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        return content.ReadAsByteArrayAsync(cancellationToken);
    }

    internal static async Task<byte[]> ReadAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (maximumBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        long? declaredLength = content.Headers.ContentLength;
        if (declaredLength.HasValue && declaredLength.Value > maximumBytes)
        {
            throw new InvalidDataException("Response body exceeds the maximum size.");
        }

        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream(Math.Min(maximumBytes, BufferSize));
        byte[] buffer = new byte[BufferSize];

        while (true)
        {
            int remainingBytes = maximumBytes - (int)output.Length;
            int readLimit = remainingBytes < BufferSize ? remainingBytes + 1 : BufferSize;
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readLimit), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                return output.ToArray();
            }
            if (bytesRead > remainingBytes)
            {
                throw new InvalidDataException("Response body exceeds the maximum size.");
            }

            output.Write(buffer, 0, bytesRead);
        }
    }
}

// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
