using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AIGaming.Companion;

internal sealed class LoopbackHost : IDisposable
{
    private readonly byte[] _secret;
    private readonly string _edition;
    private readonly OllamaLocalProvider _provider;
    private readonly TcpListener _listener;
    private ulong _lastSequence;

    internal LoopbackHost(byte[] secret, string edition, OllamaLocalProvider provider)
    {
        _secret = secret;
        _edition = edition;
        _provider = provider;
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    internal int Port { get; private set; }

    internal void Start()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            using (client)
            {
                await HandleClientAsync(client, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal void Stop()
    {
        try { _listener.Stop(); }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        CryptographicOperations.ZeroMemory(_secret);
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken sessionToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(10));
        NetworkStream stream = client.GetStream();

        try
        {
            string? requestLine = await ReadLineAsync(stream, deadline.Token).ConfigureAwait(false);
            if (requestLine == null)
            {
                await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                return;
            }

            string[] requestParts = requestLine.Split(' ');
            if (requestParts.Length != 3 || requestParts[0] != "POST" || requestParts[1] != SessionProtocol.Route || requestParts[2] != "HTTP/1.1")
            {
                await WriteErrorAsync(stream, 404, deadline.Token).ConfigureAwait(false);
                return;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                string? line = await ReadLineAsync(stream, deadline.Token).ConfigureAwait(false);
                if (line == null)
                {
                    await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                    return;
                }
                if (line.Length == 0) break;

                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                    return;
                }
                string name = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                if (string.Equals(name, "Origin", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteErrorAsync(stream, 403, deadline.Token).ConfigureAwait(false);
                    return;
                }
                if (headers.ContainsKey(name))
                {
                    await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                    return;
                }
                headers.Add(name, value);
            }

            string expectedHost = "127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture);
            if (!headers.TryGetValue("Host", out string? host) || !string.Equals(host, expectedHost, StringComparison.OrdinalIgnoreCase) ||
                headers.ContainsKey("Transfer-Encoding") ||
                !headers.TryGetValue("Content-Length", out string? lengthText) ||
                !long.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out long contentLength) || contentLength < 0)
            {
                await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                return;
            }

            if (!TryGetHeader(headers, SessionProtocol.ProtocolHeader, out string protocol) ||
                !string.Equals(protocol, CompanionBuildInfo.ProtocolVersion, StringComparison.Ordinal) ||
                !TryGetHeader(headers, SessionProtocol.EditionHeader, out string edition) ||
                !string.Equals(edition, _edition, StringComparison.Ordinal) ||
                !TryGetHeader(headers, SessionProtocol.SequenceHeader, out string sequenceText) ||
                !ulong.TryParse(sequenceText, NumberStyles.None, CultureInfo.InvariantCulture, out ulong sequence) ||
                sequence == 0 || _lastSequence == ulong.MaxValue || sequence != _lastSequence + 1 ||
                !TryGetHeader(headers, SessionProtocol.BodyHashHeader, out string bodyHashText) ||
                !SessionProtocol.TryParseHex(bodyHashText, out byte[] bodyHash) ||
                !TryGetHeader(headers, SessionProtocol.ProofHeader, out string proofText) ||
                !SessionProtocol.TryParseHex(proofText, out byte[] proof))
            {
                await WriteErrorAsync(stream, 401, deadline.Token).ConfigureAwait(false);
                return;
            }

            string requestCanonical = SessionProtocol.BuildRequestCanonical(_edition, sequence, bodyHashText);
            byte[] expectedProof = SessionProtocol.ComputeHmac(_secret, requestCanonical);
            if (!SessionProtocol.FixedTimeEquals(expectedProof, proof))
            {
                await WriteErrorAsync(stream, 401, deadline.Token).ConfigureAwait(false);
                return;
            }

            _lastSequence = sequence;
            byte[] body = await ReadBodyAsync(stream, contentLength, deadline.Token).ConfigureAwait(false);
            byte[] actualBodyHash = SHA256.HashData(body);
            if (!SessionProtocol.FixedTimeEquals(bodyHash, actualBodyHash))
            {
                await WriteErrorAsync(stream, 401, deadline.Token).ConfigureAwait(false);
                return;
            }

            if (!SessionProtocol.TryParseRequest(body, out CompanionRequest request))
            {
                await WriteErrorAsync(stream, 400, deadline.Token).ConfigureAwait(false);
                return;
            }

            CompanionResult result = await _provider.ExecuteAsync(request, deadline.Token).ConfigureAwait(false);
            byte[] responseBody = SessionProtocol.SerializeResult(result);
            string responseHash = SessionProtocol.ToHex(SHA256.HashData(responseBody));
            string responseProof = SessionProtocol.ToHex(SessionProtocol.ComputeHmac(
                _secret,
                SessionProtocol.BuildResponseCanonical(_edition, sequence, 200, responseHash)));
            await WriteSuccessAsync(stream, sequence, responseHash, responseProof, responseBody, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!sessionToken.IsCancellationRequested)
            {
                try { await WriteErrorAsync(stream, 504, CancellationToken.None).ConfigureAwait(false); }
                catch { }
            }
        }
        catch
        {
            try { await WriteErrorAsync(stream, 500, CancellationToken.None).ConfigureAwait(false); }
            catch { }
        }
    }

    private static bool TryGetHeader(Dictionary<string, string> headers, string name, out string value)
    {
        if (headers.TryGetValue(name, out string? found))
        {
            value = found;
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        byte[] buffer = new byte[1];
        while (true)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0) return bytes.Count == 0 ? null : Encoding.ASCII.GetString(bytes.ToArray());
            if (buffer[0] == '\n')
            {
                if (bytes.Count > 0 && bytes[^1] == '\r') bytes.RemoveAt(bytes.Count - 1);
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
            bytes.Add(buffer[0]);
        }
    }

    private static async Task<byte[]> ReadBodyAsync(Stream stream, long contentLength, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        byte[] buffer = new byte[81920];
        long remaining = contentLength;
        while (remaining > 0)
        {
            int requested = (int)Math.Min(buffer.Length, remaining);
            int read = await stream.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException();
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            remaining -= read;
        }
        return output.ToArray();
    }

    private async Task WriteSuccessAsync(
        Stream stream,
        ulong sequence,
        string bodyHash,
        string proof,
        byte[] body,
        CancellationToken cancellationToken)
    {
        string headers = "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture) + "\r\n" +
            "Connection: close\r\n" +
            SessionProtocol.ProtocolHeader + ": " + CompanionBuildInfo.ProtocolVersion + "\r\n" +
            SessionProtocol.EditionHeader + ": " + _edition + "\r\n" +
            SessionProtocol.SequenceHeader + ": " + sequence.ToString(CultureInfo.InvariantCulture) + "\r\n" +
            SessionProtocol.BodyHashHeader + ": " + bodyHash + "\r\n" +
            SessionProtocol.ProofHeader + ": " + proof + "\r\n\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(Stream stream, int statusCode, CancellationToken cancellationToken)
    {
        string reason = statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Gateway Timeout"
        };
        byte[] headerBytes = Encoding.ASCII.GetBytes(
            "HTTP/1.1 " + statusCode.ToString(CultureInfo.InvariantCulture) + " " + reason + "\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
// Implementation update signed by OpenAI-Codex.
