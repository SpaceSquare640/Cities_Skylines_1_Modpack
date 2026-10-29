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

            byte[] responseBody;
            try
            {
                responseBody = await BoundedOllamaResponseReader.ReadAsync(
                    response.Content,
                    request.MaximumResponseBytes,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException) when (request.MaximumResponseBytes > 0)
            {
                return Error(request.ModelName, "response_limit_exceeded");
            }
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
        text.AppendLine("Use only the city fields, game-reported installed and available DLC identifiers below, and the player's question.");
        text.AppendLine("DLC identifiers describe content available to the player; do not assume a DLC is active in this save.");
        text.AppendLine("City snapshot schemaVersion=3:");
        Append(text, "gameDate", request.GameDate);
        Append(text, "population", request.Population?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "fundsCents", request.FundsCents?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "residentialDemand", request.ResidentialDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "commercialDemand", request.CommercialDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(text, "workplaceDemand", request.WorkplaceDemand?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (request.Utilities is not null)
        {
            text.AppendLine("Utilities (raw game district values; physical units and scales are not verified; do not invent units):");
            Append(text, "electricityCapacity", request.Utilities.ElectricityCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "electricityConsumption", request.Utilities.ElectricityConsumption.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "waterCapacity", request.Utilities.WaterCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "waterConsumption", request.Utilities.WaterConsumption.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "sewageCapacity", request.Utilities.SewageCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "sewageAccumulation", request.Utilities.SewageAccumulation.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "garbageAmount", request.Utilities.GarbageAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "garbageCapacity", request.Utilities.GarbageCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "garbageAccumulation", request.Utilities.GarbageAccumulation.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "incinerationCapacity", request.Utilities.IncinerationCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (request.Zoning is not null)
        {
            text.AppendLine("Built buildings and area by land use (not vacant zoned cells; area unit is game-defined and unverified):");
            Append(text, "residentialBuildingCount", request.Zoning.ResidentialBuildingCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "residentialBuildingArea", request.Zoning.ResidentialBuildingArea.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "commercialBuildingCount", request.Zoning.CommercialBuildingCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "commercialBuildingArea", request.Zoning.CommercialBuildingArea.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "industrialBuildingCount", request.Zoning.IndustrialBuildingCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "industrialBuildingArea", request.Zoning.IndustrialBuildingArea.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "officeBuildingCount", request.Zoning.OfficeBuildingCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "officeBuildingArea", request.Zoning.OfficeBuildingArea.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (request.TrafficTransit is not null)
        {
            text.AppendLine("Traffic and transit (game-reported statistics; raw values, unspecified scale/window; trafficFlow is not a congestion percentage):");
            Append(text, "trafficFlow", request.TrafficTransit.TrafficFlow.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "averagePassengers", request.TrafficTransit.AveragePassengers.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (request.ServiceStatus is not null)
        {
            text.AppendLine("Service status (fire coverage is mean m_fireCoverage byte 0-255 over created network segments; if fireCoverageSegmentCount is zero, the mean is unavailable; police and healthcare are citywide workload/capacity, not geographic coverage):");
            Append(text, "policeCriminalAmount", request.ServiceStatus.PoliceCriminalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "policeCriminalCapacity", request.ServiceStatus.PoliceCriminalCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "policeExtraCriminals", request.ServiceStatus.PoliceExtraCriminals.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "fireCoverageMeanByte", request.ServiceStatus.FireCoverageMeanByte.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "fireCoverageSegmentCount", request.ServiceStatus.FireCoverageSegmentCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "healthSickCount", request.ServiceStatus.HealthSickCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "healthCapacity", request.ServiceStatus.HealthCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(text, "healthCareProductivity", request.ServiceStatus.HealthCareProductivity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        text.AppendLine("installedAvailableDlcs (game API enum identifiers; installed and available):");
        if (request.InstalledAvailableDlcs.Length == 0)
        {
            text.AppendLine("(none detected)");
        }
        else
        {
            foreach (string identifier in request.InstalledAvailableDlcs)
            {
                text.Append("- ");
                text.AppendLine(identifier);
            }
        }
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
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
