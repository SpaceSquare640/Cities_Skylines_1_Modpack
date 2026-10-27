using System.Net.Http;

namespace AIGaming.Companion;

internal static class Program
{
    private const int Limit = 8;

    private static async Task Main()
    {
        await ReadsBodyBelowLimitAsync();
        await AcceptsBodyAtLimitAsync();
        await RejectsAdvertisedBodyAboveLimitAsync();
        await RejectsUnknownLengthBodyAfterOneExtraByteAsync();
        Console.WriteLine("PASS: 4 bounded response body checks.");
    }

    private static async Task ReadsBodyBelowLimitAsync()
    {
        byte[] expected = "abc"u8.ToArray();
        using var content = new ByteArrayContent(expected);
        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        AssertBytes(expected, actual, "body below limit");
    }

    private static async Task AcceptsBodyAtLimitAsync()
    {
        byte[] expected = Enumerable.Range(0, Limit).Select(value => (byte)value).ToArray();
        using var content = new ByteArrayContent(expected);
        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        AssertBytes(expected, actual, "body at limit");
    }

    private static async Task RejectsAdvertisedBodyAboveLimitAsync()
    {
        using var content = new ByteArrayContent(new byte[Limit + 1]);
        await AssertRejectedAsync(content, "advertised body above limit");
    }

    private static async Task RejectsUnknownLengthBodyAfterOneExtraByteAsync()
    {
        var stream = new NonSeekableReadStream(new byte[Limit + 20]);
        using var content = new StreamContent(stream);
        if (content.Headers.ContentLength != null)
        {
            throw new InvalidOperationException("The test stream must not advertise a content length.");
        }

        await AssertRejectedAsync(content, "unknown-length body above limit");
        if (stream.BytesRead != Limit + 1)
        {
            throw new InvalidOperationException($"Expected {Limit + 1} bytes read, got {stream.BytesRead}.");
        }
    }

    private static async Task AssertRejectedAsync(HttpContent content, string testName)
    {
        try
        {
            _ = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {testName} to be rejected.");
    }

    private static void AssertBytes(byte[] expected, byte[] actual, string testName)
    {
        if (!expected.AsSpan().SequenceEqual(actual))
        {
            throw new InvalidOperationException($"Unexpected bytes for {testName}.");
        }
    }
}

internal sealed class NonSeekableReadStream : Stream
{
    private readonly byte[] _bytes;
    private int _position;

    internal NonSeekableReadStream(byte[] bytes) => _bytes = bytes;

    internal int BytesRead { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesToRead = Math.Min(count, _bytes.Length - _position);
        Array.Copy(_bytes, _position, buffer, offset, bytesToRead);
        _position += bytesToRead;
        BytesRead += bytesToRead;
        return bytesToRead;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int bytesToRead = Math.Min(buffer.Length, _bytes.Length - _position);
        _bytes.AsMemory(_position, bytesToRead).CopyTo(buffer);
        _position += bytesToRead;
        BytesRead += bytesToRead;
        return ValueTask.FromResult(bytesToRead);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

// Implementation update signed by OpenAI-Codex.
