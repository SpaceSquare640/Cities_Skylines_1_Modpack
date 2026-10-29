using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AIGaming.Companion;

internal sealed class CompanionBootstrap
{
    internal string ProtocolVersion { get; init; } = string.Empty;
    internal string Edition { get; init; } = string.Empty;
    internal string ModVersion { get; init; } = string.Empty;
    internal string CompanionVersion { get; init; } = string.Empty;
    internal string ExpectedSha256 { get; init; } = string.Empty;
    internal int ParentProcessId { get; init; }
    internal string ParentImagePath { get; init; } = string.Empty;
}

internal sealed class CompanionRequest
{
    internal string ModelName { get; init; } = string.Empty;
    internal string Question { get; init; } = string.Empty;
    internal int MaximumResponseBytes { get; init; }
    internal string? GameDate { get; init; }
    internal uint? Population { get; init; }
    internal long? FundsCents { get; init; }
    internal int? ResidentialDemand { get; init; }
    internal int? CommercialDemand { get; init; }
    internal int? WorkplaceDemand { get; init; }
    internal string[] InstalledAvailableDlcs { get; init; } = Array.Empty<string>();
    internal UtilitiesData? Utilities { get; init; }
    internal ZoningData? Zoning { get; init; }
    internal TrafficTransitData? TrafficTransit { get; init; }
    internal ServiceStatusData? ServiceStatus { get; init; }
}

internal sealed class UtilitiesData
{
    internal int ElectricityCapacity { get; init; }
    internal int ElectricityConsumption { get; init; }
    internal int WaterCapacity { get; init; }
    internal int WaterConsumption { get; init; }
    internal int SewageCapacity { get; init; }
    internal int SewageAccumulation { get; init; }
    internal int GarbageAmount { get; init; }
    internal int GarbageCapacity { get; init; }
    internal int GarbageAccumulation { get; init; }
    internal int IncinerationCapacity { get; init; }
}

internal sealed class ZoningData
{
    internal ushort ResidentialBuildingCount { get; init; }
    internal uint ResidentialBuildingArea { get; init; }
    internal ushort CommercialBuildingCount { get; init; }
    internal uint CommercialBuildingArea { get; init; }
    internal ushort IndustrialBuildingCount { get; init; }
    internal uint IndustrialBuildingArea { get; init; }
    internal ushort OfficeBuildingCount { get; init; }
    internal uint OfficeBuildingArea { get; init; }
}

internal sealed class TrafficTransitData
{
    internal int TrafficFlow { get; init; }
    internal int AveragePassengers { get; init; }
}

internal sealed class ServiceStatusData
{
    internal int PoliceCriminalAmount { get; init; }
    internal int PoliceCriminalCapacity { get; init; }
    internal int PoliceExtraCriminals { get; init; }
    internal float FireCoverageMeanByte { get; init; }
    internal int FireCoverageSegmentCount { get; init; }
    internal int HealthSickCount { get; init; }
    internal int HealthCapacity { get; init; }
    internal int HealthCareProductivity { get; init; }
}

internal sealed class CompanionResult
{
    internal string Status { get; init; } = "error";
    internal string ModelName { get; init; } = string.Empty;
    internal string Content { get; init; } = string.Empty;
    internal string ErrorCode { get; init; } = string.Empty;
}

internal static class SessionProtocol
{
    internal const string Route = "/v1/ollama/local/chat";
    internal const string ProtocolHeader = "X-AIGaming-Protocol";
    internal const string EditionHeader = "X-AIGaming-Edition";
    internal const string SequenceHeader = "X-AIGaming-Sequence";
    internal const string BodyHashHeader = "X-AIGaming-Body-SHA256";
    internal const string ProofHeader = "X-AIGaming-Proof";

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly string[] AllowedRequestKeys =
    {
        "schemaVersion",
        "model",
        "question",
        "maxResponseBytes",
        "gameDate",
        "population",
        "fundsCents",
        "residentialDemand",
        "commercialDemand",
        "workplaceDemand",
        "installedAvailableDlcs",
        "electricityCapacity",
        "electricityConsumption",
        "waterCapacity",
        "waterConsumption",
        "sewageCapacity",
        "sewageAccumulation",
        "garbageAmount",
        "garbageCapacity",
        "garbageAccumulation",
        "incinerationCapacity",
        "residentialBuildingCount",
        "residentialBuildingArea",
        "commercialBuildingCount",
        "commercialBuildingArea",
        "industrialBuildingCount",
        "industrialBuildingArea",
        "officeBuildingCount",
        "officeBuildingArea",
        "trafficFlow",
        "averagePassengers",
        "policeCriminalAmount",
        "policeCriminalCapacity",
        "policeExtraCriminals",
        "fireCoverageMeanByte",
        "fireCoverageSegmentCount",
        "healthSickCount",
        "healthCapacity",
        "healthCareProductivity"
    };

    internal static bool TryParseBootstrap(string text, out CompanionBootstrap bootstrap)
    {
        bootstrap = new CompanionBootstrap();
        if (!TryDecodeEnvelope(text, "BOOT:", out string payload) ||
            !TryParseLines(payload, out Dictionary<string, string> values) ||
            !HasOnlyKeys(values, "protocol", "edition", "modVersion", "companionVersion", "expectedSha256", "parentProcessId", "parentImage"))
        {
            return false;
        }

        if (!values.TryGetValue("parentProcessId", out string? parentIdText) ||
            !int.TryParse(parentIdText, NumberStyles.None, CultureInfo.InvariantCulture, out int parentId) || parentId <= 0 ||
            !values.TryGetValue("parentImage", out string? parentImageBase64) ||
            !TryDecode(parentImageBase64, out string? parentImage))
        {
            return false;
        }

        bootstrap = new CompanionBootstrap
        {
            ProtocolVersion = Get(values, "protocol"),
            Edition = Get(values, "edition"),
            ModVersion = Get(values, "modVersion"),
            CompanionVersion = Get(values, "companionVersion"),
            ExpectedSha256 = Get(values, "expectedSha256"),
            ParentProcessId = parentId,
            ParentImagePath = parentImage
        };

        return bootstrap.ProtocolVersion.Length > 0 && bootstrap.Edition.Length > 0 &&
            bootstrap.ModVersion.Length > 0 && bootstrap.CompanionVersion.Length > 0 &&
            bootstrap.ExpectedSha256.Length == 64 && bootstrap.ParentImagePath.Length > 0;
    }

    internal static string SerializeReady(int port, int processId, int parentProcessId, string sha256, byte[] secret)
    {
        string payload = string.Join("\n", new[]
        {
            "protocol=" + CompanionBuildInfo.ProtocolVersion,
            "edition=" + CompanionBuildInfo.Edition,
            "companionVersion=" + CompanionBuildInfo.Version,
            "sha256=" + sha256,
            "port=" + port.ToString(CultureInfo.InvariantCulture),
            "processId=" + processId.ToString(CultureInfo.InvariantCulture),
            "parentProcessId=" + parentProcessId.ToString(CultureInfo.InvariantCulture),
            "sessionToken=" + Convert.ToBase64String(secret)
        });
        return "READY:" + Convert.ToBase64String(StrictUtf8.GetBytes(payload));
    }

    internal static bool TryParseReady(string text, out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryDecodeEnvelope(text, "READY:", out string payload) || !TryParseLines(payload, out values) ||
            !HasOnlyKeys(values, "protocol", "edition", "companionVersion", "sha256", "port", "processId", "parentProcessId", "sessionToken"))
        {
            return false;
        }
        return true;
    }

    internal static bool TryParseRequest(byte[] body, out CompanionRequest request)
    {
        request = new CompanionRequest();
        string text;
        try
        {
            text = StrictUtf8.GetString(body);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        if (!TryParseLines(text, out Dictionary<string, string> values) || !HasOnlyKeys(values, AllowedRequestKeys))
        {
            return false;
        }

        if (Get(values, "schemaVersion") != "4" ||
            !values.TryGetValue("model", out string? modelBase64) || !TryDecode(modelBase64, out string? model) || string.IsNullOrWhiteSpace(model) ||
            !values.TryGetValue("question", out string? questionBase64) || !TryDecode(questionBase64, out string? question) || string.IsNullOrWhiteSpace(question) ||
            !values.TryGetValue("maxResponseBytes", out string? maximumResponseBytesText) ||
            !int.TryParse(maximumResponseBytesText, NumberStyles.None, CultureInfo.InvariantCulture, out int maximumResponseBytes) ||
            maximumResponseBytes < 0)
        {
            return false;
        }

        if (!values.TryGetValue("installedAvailableDlcs", out string? availableDlcsBase64) ||
            !TryDecode(availableDlcsBase64, out string? availableDlcsText) ||
            !TryParseAvailableDlcIdentifiers(availableDlcsText, out string[] availableDlcs))
        {
            return false;
        }

        string? gameDate = null;
        uint? population = null;
        long? fundsCents = null;
        int? residentialDemand = null;
        int? commercialDemand = null;
        int? workplaceDemand = null;

        if (values.TryGetValue("gameDate", out string? dateValue))
        {
            if (!DateTime.TryParseExact(dateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
            gameDate = dateValue;
        }
        if (values.TryGetValue("population", out string? populationValue))
        {
            if (!uint.TryParse(populationValue, NumberStyles.None, CultureInfo.InvariantCulture, out uint parsed)) return false;
            population = parsed;
        }
        if (values.TryGetValue("fundsCents", out string? fundsValue))
        {
            if (!long.TryParse(fundsValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)) return false;
            fundsCents = parsed;
        }
        if (values.TryGetValue("residentialDemand", out string? residentialValue))
        {
            if (!int.TryParse(residentialValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return false;
            residentialDemand = parsed;
        }
        if (values.TryGetValue("commercialDemand", out string? commercialValue))
        {
            if (!int.TryParse(commercialValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return false;
            commercialDemand = parsed;
        }
        if (values.TryGetValue("workplaceDemand", out string? workplaceValue))
        {
            if (!int.TryParse(workplaceValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return false;
            workplaceDemand = parsed;
        }

        if (!TryParseUtilities(values, out UtilitiesData? utilities) ||
            !TryParseZoning(values, out ZoningData? zoning) ||
            !TryParseTrafficTransit(values, out TrafficTransitData? trafficTransit) ||
            !TryParseServiceStatus(values, out ServiceStatusData? serviceStatus))
        {
            return false;
        }

        request = new CompanionRequest
        {
            ModelName = model,
            Question = question,
            MaximumResponseBytes = maximumResponseBytes,
            GameDate = gameDate,
            Population = population,
            FundsCents = fundsCents,
            ResidentialDemand = residentialDemand,
            CommercialDemand = commercialDemand,
            WorkplaceDemand = workplaceDemand,
            InstalledAvailableDlcs = availableDlcs,
            Utilities = utilities,
            Zoning = zoning,
            TrafficTransit = trafficTransit,
            ServiceStatus = serviceStatus
        };
        return true;
    }

    private static bool TryParseUtilities(Dictionary<string, string> values, out UtilitiesData? data)
    {
        data = null;
        string[] keys =
        {
            "electricityCapacity", "electricityConsumption", "waterCapacity", "waterConsumption",
            "sewageCapacity", "sewageAccumulation", "garbageAmount", "garbageCapacity",
            "garbageAccumulation", "incinerationCapacity"
        };
        if (!HasAny(values, keys)) return true;
        if (!TryGetInt32(values, "electricityCapacity", out int electricityCapacity) ||
            !TryGetInt32(values, "electricityConsumption", out int electricityConsumption) ||
            !TryGetInt32(values, "waterCapacity", out int waterCapacity) ||
            !TryGetInt32(values, "waterConsumption", out int waterConsumption) ||
            !TryGetInt32(values, "sewageCapacity", out int sewageCapacity) ||
            !TryGetInt32(values, "sewageAccumulation", out int sewageAccumulation) ||
            !TryGetInt32(values, "garbageAmount", out int garbageAmount) ||
            !TryGetInt32(values, "garbageCapacity", out int garbageCapacity) ||
            !TryGetInt32(values, "garbageAccumulation", out int garbageAccumulation) ||
            !TryGetInt32(values, "incinerationCapacity", out int incinerationCapacity))
        {
            return false;
        }
        data = new UtilitiesData
        {
            ElectricityCapacity = electricityCapacity,
            ElectricityConsumption = electricityConsumption,
            WaterCapacity = waterCapacity,
            WaterConsumption = waterConsumption,
            SewageCapacity = sewageCapacity,
            SewageAccumulation = sewageAccumulation,
            GarbageAmount = garbageAmount,
            GarbageCapacity = garbageCapacity,
            GarbageAccumulation = garbageAccumulation,
            IncinerationCapacity = incinerationCapacity
        };
        return true;
    }

    private static bool TryParseZoning(Dictionary<string, string> values, out ZoningData? data)
    {
        data = null;
        string[] keys =
        {
            "residentialBuildingCount", "residentialBuildingArea", "commercialBuildingCount", "commercialBuildingArea",
            "industrialBuildingCount", "industrialBuildingArea", "officeBuildingCount", "officeBuildingArea"
        };
        if (!HasAny(values, keys)) return true;
        if (!TryGetUInt16(values, "residentialBuildingCount", out ushort residentialCount) ||
            !TryGetUInt32(values, "residentialBuildingArea", out uint residentialArea) ||
            !TryGetUInt16(values, "commercialBuildingCount", out ushort commercialCount) ||
            !TryGetUInt32(values, "commercialBuildingArea", out uint commercialArea) ||
            !TryGetUInt16(values, "industrialBuildingCount", out ushort industrialCount) ||
            !TryGetUInt32(values, "industrialBuildingArea", out uint industrialArea) ||
            !TryGetUInt16(values, "officeBuildingCount", out ushort officeCount) ||
            !TryGetUInt32(values, "officeBuildingArea", out uint officeArea))
        {
            return false;
        }
        data = new ZoningData
        {
            ResidentialBuildingCount = residentialCount,
            ResidentialBuildingArea = residentialArea,
            CommercialBuildingCount = commercialCount,
            CommercialBuildingArea = commercialArea,
            IndustrialBuildingCount = industrialCount,
            IndustrialBuildingArea = industrialArea,
            OfficeBuildingCount = officeCount,
            OfficeBuildingArea = officeArea
        };
        return true;
    }

    private static bool TryParseTrafficTransit(Dictionary<string, string> values, out TrafficTransitData? data)
    {
        data = null;
        string[] keys = { "trafficFlow", "averagePassengers" };
        if (!HasAny(values, keys)) return true;
        if (!TryGetInt32(values, "trafficFlow", out int trafficFlow) ||
            !TryGetInt32(values, "averagePassengers", out int averagePassengers))
        {
            return false;
        }
        data = new TrafficTransitData { TrafficFlow = trafficFlow, AveragePassengers = averagePassengers };
        return true;
    }

    private static bool TryParseServiceStatus(Dictionary<string, string> values, out ServiceStatusData? data)
    {
        data = null;
        string[] keys =
        {
            "policeCriminalAmount", "policeCriminalCapacity", "policeExtraCriminals", "fireCoverageMeanByte",
            "fireCoverageSegmentCount", "healthSickCount", "healthCapacity", "healthCareProductivity"
        };
        if (!HasAny(values, keys)) return true;
        if (!TryGetInt32(values, "policeCriminalAmount", out int policeAmount) ||
            !TryGetInt32(values, "policeCriminalCapacity", out int policeCapacity) ||
            !TryGetInt32(values, "policeExtraCriminals", out int extraCriminals) ||
            !values.TryGetValue("fireCoverageMeanByte", out string? fireMeanText) ||
            !float.TryParse(fireMeanText, NumberStyles.Float, CultureInfo.InvariantCulture, out float fireMean) ||
            float.IsNaN(fireMean) || float.IsInfinity(fireMean) || fireMean < 0f || fireMean > 255f ||
            !TryGetInt32(values, "fireCoverageSegmentCount", out int fireSegmentCount) || fireSegmentCount < 0 ||
            !TryGetInt32(values, "healthSickCount", out int sickCount) ||
            !TryGetInt32(values, "healthCapacity", out int healthCapacity) ||
            !TryGetInt32(values, "healthCareProductivity", out int healthProductivity))
        {
            return false;
        }
        data = new ServiceStatusData
        {
            PoliceCriminalAmount = policeAmount,
            PoliceCriminalCapacity = policeCapacity,
            PoliceExtraCriminals = extraCriminals,
            FireCoverageMeanByte = fireMean,
            FireCoverageSegmentCount = fireSegmentCount,
            HealthSickCount = sickCount,
            HealthCapacity = healthCapacity,
            HealthCareProductivity = healthProductivity
        };
        return true;
    }

    private static bool HasAny(Dictionary<string, string> values, string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (values.ContainsKey(keys[i])) return true;
        }
        return false;
    }

    private static bool TryGetInt32(Dictionary<string, string> values, string key, out int value)
    {
        value = 0;
        return values.TryGetValue(key, out string? text) &&
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetUInt16(Dictionary<string, string> values, string key, out ushort value)
    {
        value = 0;
        return values.TryGetValue(key, out string? text) &&
            ushort.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetUInt32(Dictionary<string, string> values, string key, out uint value)
    {
        value = 0;
        return values.TryGetValue(key, out string? text) &&
            uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseAvailableDlcIdentifiers(string value, out string[] identifiers)
    {
        identifiers = Array.Empty<string>();
        if (value.Length == 0) return true;

        string[] parsed = value.Split('\n');
        string? previous = null;
        foreach (string identifier in parsed)
        {
            if (!IsValidDlcIdentifier(identifier) ||
                (previous != null && string.CompareOrdinal(previous, identifier) >= 0))
            {
                return false;
            }
            previous = identifier;
        }

        identifiers = parsed;
        return true;
    }

    private static bool IsValidDlcIdentifier(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0])) return false;
        for (int i = 1; i < value.Length; i++)
        {
            char character = value[i];
            if (!IsAsciiLetter(character) && !(character >= '0' && character <= '9') && character != '_') return false;
        }
        return true;
    }

    private static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    internal static byte[] SerializeResult(CompanionResult result)
    {
        string text = string.Join("\n", new[]
        {
            "status=" + Encode(result.Status),
            "model=" + Encode(result.ModelName),
            "content=" + Encode(result.Content),
            "errorCode=" + Encode(result.ErrorCode)
        });
        return StrictUtf8.GetBytes(text);
    }

    internal static string BuildRequestCanonical(string edition, ulong sequence, string bodyHash)
    {
        return string.Join("\n", new[]
        {
            CompanionBuildInfo.ProtocolVersion,
            edition,
            sequence.ToString(CultureInfo.InvariantCulture),
            "POST",
            Route,
            bodyHash.ToLowerInvariant()
        });
    }

    internal static string BuildResponseCanonical(string edition, ulong sequence, int statusCode, string bodyHash)
    {
        return string.Join("\n", new[]
        {
            CompanionBuildInfo.ProtocolVersion,
            edition,
            sequence.ToString(CultureInfo.InvariantCulture),
            statusCode.ToString(CultureInfo.InvariantCulture),
            "POST",
            Route,
            bodyHash.ToLowerInvariant()
        });
    }

    internal static byte[] ComputeHmac(byte[] secret, string canonical)
    {
        using var hmac = new HMACSHA256(secret);
        return hmac.ComputeHash(StrictUtf8.GetBytes(canonical));
    }

    internal static byte[] ComputeSha256(byte[] value)
    {
        return SHA256.HashData(value);
    }

    internal static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }

    internal static bool TryParseHex(string text, out byte[] value)
    {
        value = Array.Empty<byte>();
        if (text.Length != 64) return false;
        try
        {
            value = Convert.FromHexString(text);
            return value.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        int difference = 0;
        for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
        return difference == 0;
    }

    private static string Encode(string value) => Convert.ToBase64String(StrictUtf8.GetBytes(value));

    private static bool TryDecode(string value, out string decoded)
    {
        decoded = string.Empty;
        try
        {
            decoded = StrictUtf8.GetString(Convert.FromBase64String(value));
            return true;
        }
        catch (FormatException) { return false; }
        catch (DecoderFallbackException) { return false; }
    }

    private static bool TryParseLines(string text, out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>(StringComparer.Ordinal);
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            int separator = line.IndexOf('=');
            if (separator <= 0) return false;
            string key = line.Substring(0, separator);
            string value = line.Substring(separator + 1);
            if (values.ContainsKey(key)) return false;
            values.Add(key, value);
        }
        return true;
    }

    private static bool TryDecodeEnvelope(string text, string prefix, out string payload)
    {
        payload = string.Empty;
        if (!text.StartsWith(prefix, StringComparison.Ordinal)) return false;
        try
        {
            payload = StrictUtf8.GetString(Convert.FromBase64String(text.Substring(prefix.Length)));
            return true;
        }
        catch (FormatException) { return false; }
        catch (DecoderFallbackException) { return false; }
    }

    private static bool HasOnlyKeys(Dictionary<string, string> values, params string[] allowed)
    {
        foreach (string key in values.Keys)
        {
            bool found = false;
            for (int i = 0; i < allowed.Length; i++)
            {
                if (string.Equals(key, allowed[i], StringComparison.Ordinal)) { found = true; break; }
            }
            if (!found) return false;
        }
        return true;
    }

    private static string Get(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) ? value : string.Empty;
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
