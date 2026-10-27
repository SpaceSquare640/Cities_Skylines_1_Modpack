using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AIGaming.Companion;

internal sealed class OllamaLocalProvider : IDisposable
{
    private readonly HttpClient _client;

    internal OllamaLocalProvider()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = ConnectLoopbackAsync
        };
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    internal async Task<CompanionResult> ExecuteAsync(CompanionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            string? modelError = await ValidateInstalledLocalModelAsync(request.ModelName, cancellationToken).ConfigureAwait(false);
            if (modelError != null)
            {
                return Error(request.ModelName, modelError);
            }

            string prompt = BuildPrompt(request);
            var payload = new
            {
                model = request.ModelName,
                messages = new[] { new { role = "user", content = prompt } },
                stream = false
            };

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload);
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = new ByteArrayContent(json)
            };
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using HttpResponseMessage response = await _client.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Error(request.ModelName, "model_not_found");
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Error(request.ModelName, "local_authentication_failed");
            }
            if (!response.IsSuccessStatusCode)
            {
                return Error(request.ModelName, "ollama_error");
            }

            byte[] responseBody = await BoundedOllamaResponseReader.ReadAsync(
                response.Content,
                BoundedOllamaResponseReader.MaximumResponseBytes,
                cancellationToken).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("message", out JsonElement messageElement) ||
                !messageElement.TryGetProperty("content", out JsonElement contentElement) ||
                contentElement.ValueKind != JsonValueKind.String)
            {
                return Error(request.ModelName, "invalid_response");
            }

            return new CompanionResult
            {
                Status = "ok",
                ModelName = request.ModelName,
                Content = contentElement.GetString() ?? string.Empty
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Error(request.ModelName, "cancelled");
        }
        catch (OperationCanceledException)
        {
            return Error(request.ModelName, "timeout");
        }
        catch (HttpRequestException exception) when (exception.InnerException is SocketException)
        {
            return Error(request.ModelName, "ollama_unavailable");
        }
        catch (SocketException)
        {
            return Error(request.ModelName, "ollama_unavailable");
        }
        catch (InvalidDataException)
        {
            return Error(request.ModelName, "invalid_response");
        }
        catch (JsonException)
        {
            return Error(request.ModelName, "invalid_response");
        }
        catch
        {
            return Error(request.ModelName, "ollama_error");
        }
    }

    public void Dispose() => _client.Dispose();

    private static async ValueTask<Stream> ConnectLoopbackAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(context.DnsEndPoint.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            context.DnsEndPoint.Port != 11434)
        {
            throw new HttpRequestException("The local provider endpoint is not allowed.");
        }

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(IPAddress.Loopback, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private async Task<string?> ValidateInstalledLocalModelAsync(string modelName, CancellationToken cancellationToken)
    {
        if (IsCloudModelName(modelName))
        {
            return "local_model_required";
        }

        using HttpResponseMessage response = await _client.GetAsync(
            "api/tags",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return "local_authentication_failed";
        }
        if (!response.IsSuccessStatusCode)
        {
            return "ollama_error";
        }

        byte[] responseBody = await BoundedOllamaResponseReader.ReadAsync(
            response.Content,
            BoundedOllamaResponseReader.MaximumResponseBytes,
            cancellationToken).ConfigureAwait(false);
        using JsonDocument document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("models", out JsonElement models) || models.ValueKind != JsonValueKind.Array)
        {
            return "invalid_response";
        }

        foreach (JsonElement model in models.EnumerateArray())
        {
            if (!TryGetLocalModelName(model, out string name, out long size))
            {
                continue;
            }
            if (size > 0 && ModelsMatch(modelName, name))
            {
                return null;
            }
        }

        return "model_not_found";
    }

    private static bool TryGetLocalModelName(JsonElement model, out string name, out long size)
    {
        name = string.Empty;
        size = 0;
        if (model.ValueKind != JsonValueKind.Object ||
            !model.TryGetProperty("size", out JsonElement sizeElement) ||
            sizeElement.ValueKind != JsonValueKind.Number || !sizeElement.TryGetInt64(out size) ||
            size < 0)
        {
            return false;
        }

        if (model.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String)
        {
            name = nameElement.GetString() ?? string.Empty;
        }
        else if (model.TryGetProperty("model", out JsonElement modelElement) && modelElement.ValueKind == JsonValueKind.String)
        {
            name = modelElement.GetString() ?? string.Empty;
        }
        return name.Length > 0;
    }

    private static bool ModelsMatch(string requested, string available)
    {
        return string.Equals(NormalizeLatestTag(requested), NormalizeLatestTag(available), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLatestTag(string value)
    {
        const string latestSuffix = ":latest";
        return value.EndsWith(latestSuffix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(0, value.Length - latestSuffix.Length)
            : value;
    }

    private static bool IsCloudModelName(string modelName)
    {
        return modelName.EndsWith(":cloud", StringComparison.OrdinalIgnoreCase) ||
            modelName.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPrompt(CompanionRequest request)
    {
        var text = new StringBuilder();
        text.AppendLine("You are an assistant for Cities: Skylines. Give advice only. Do not claim to operate the game.");
        text.AppendLine("Use only the city fields below and the player's question.");
        text.AppendLine("City snapshot schemaVersion=1:");
        Append(text, "gameDate", request.GameDate);
        Append(text, "population", request.Population?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "fundsCents", request.FundsCents?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "residentialDemand", request.ResidentialDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "commercialDemand", request.CommercialDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "workplaceDemand", request.WorkplaceDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.AppendLine("Player question:");
        text.Append(request.Question);
        return text.ToString();
    }

    private static void Append(StringBuilder text, string key, string? value)
    {
        if (value != null)
        {
            text.Append(key);
            text.Append('=');
            text.AppendLine(value);
        }
    }

    private static CompanionResult Error(string modelName, string errorCode) => new()
    {
        Status = "error",
        ModelName = modelName,
        ErrorCode = errorCode
    };
}
// Implementation update signed by OpenAI-Codex.
