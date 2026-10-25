using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AIGaming
{
    internal sealed class LocalAiRequest
    {
        internal string ModelName { get; set; }
        internal string Question { get; set; }
        internal CitySnapshot Snapshot { get; set; }
        internal bool IncludeGameDate { get; set; }
        internal bool IncludePopulation { get; set; }
        internal bool IncludeFundsCents { get; set; }
        internal bool IncludeResidentialDemand { get; set; }
        internal bool IncludeCommercialDemand { get; set; }
        internal bool IncludeWorkplaceDemand { get; set; }
    }

    internal sealed class LocalAiResult
    {
        internal bool Success { get; set; }
        internal string ModelName { get; set; }
        internal string Content { get; set; }
        internal string ErrorCode { get; set; }
    }

    internal sealed class CompanionClient
    {
        private const int RequestTimeoutMilliseconds = 660000;
        private readonly object _sync = new object();
        private HttpWebRequest _activeRequest;
        private CompanionSession _activeSession;
        private bool _cancelled;

        internal LocalAiResult Send(LocalAiRequest request)
        {
            CompanionSession session = null;
            string errorCode;
            try
            {
                if (!CompanionLauncher.TryStart(out session, out errorCode))
                {
                    return Error(request.ModelName, errorCode);
                }
            }
            catch
            {
                return Error(request.ModelName, "companion_start_failed");
            }

            lock (_sync)
            {
                if (_cancelled)
                {
                    session.Dispose();
                    return Error(request.ModelName, "cancelled");
                }
                _activeSession = session;
            }

            try
            {
                byte[] body = SerializeRequest(request);
                ulong sequence = session.NextSequence();
                string bodyHash = ToHex(ComputeSha256(body));
                string proof = ToHex(ComputeHmac(
                    session.SessionToken,
                    BuildRequestCanonical(CompanionBuildInfo.Edition, sequence, bodyHash)));

                var webRequest = (HttpWebRequest)WebRequest.Create(
                    "http://127.0.0.1:" + session.Port.ToString(CultureInfo.InvariantCulture) + "/v1/ollama/local/chat");
                webRequest.Method = "POST";
                webRequest.ContentType = "text/plain; charset=utf-8";
                webRequest.ContentLength = body.Length;
                webRequest.KeepAlive = false;
                webRequest.AllowAutoRedirect = false;
                webRequest.Proxy = null;
                webRequest.Timeout = RequestTimeoutMilliseconds;
                webRequest.ReadWriteTimeout = RequestTimeoutMilliseconds;
                webRequest.ServicePoint.Expect100Continue = false;
                webRequest.Headers["X-AIGaming-Protocol"] = CompanionBuildInfo.ProtocolVersion;
                webRequest.Headers["X-AIGaming-Edition"] = CompanionBuildInfo.Edition;
                webRequest.Headers["X-AIGaming-Sequence"] = sequence.ToString(CultureInfo.InvariantCulture);
                webRequest.Headers["X-AIGaming-Body-SHA256"] = bodyHash;
                webRequest.Headers["X-AIGaming-Proof"] = proof;

                lock (_sync)
                {
                    if (_cancelled) return Error(request.ModelName, "cancelled");
                    _activeRequest = webRequest;
                }

                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    requestStream.Write(body, 0, body.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Error(request.ModelName, "companion_rejected_request");
                    }

                    string responseProtocol = response.Headers["X-AIGaming-Protocol"];
                    string responseEdition = response.Headers["X-AIGaming-Edition"];
                    string responseSequence = response.Headers["X-AIGaming-Sequence"];
                    string responseHash = response.Headers["X-AIGaming-Body-SHA256"];
                    string responseProof = response.Headers["X-AIGaming-Proof"];
                    ulong responseSequenceNumber;
                    byte[] parsedHash;
                    byte[] parsedProof;
                    if (responseProtocol != CompanionBuildInfo.ProtocolVersion ||
                        responseEdition != CompanionBuildInfo.Edition ||
                        !ulong.TryParse(responseSequence, NumberStyles.None, CultureInfo.InvariantCulture, out responseSequenceNumber) ||
                        responseSequenceNumber != sequence ||
                        !TryParseHex(responseHash, out parsedHash) ||
                        !TryParseHex(responseProof, out parsedProof))
                    {
                        return Error(request.ModelName, "companion_identity_mismatch");
                    }

                    string responseText;
                    using (var reader = new StreamReader(response.GetResponseStream(), new UTF8Encoding(false, true)))
                    {
                        responseText = reader.ReadToEnd();
                    }
                    byte[] responseBody = new UTF8Encoding(false, true).GetBytes(responseText);
                    if (!FixedTimeEquals(parsedHash, ComputeSha256(responseBody)))
                    {
                        return Error(request.ModelName, "companion_response_invalid");
                    }

                    string responseCanonical = BuildResponseCanonical(
                        CompanionBuildInfo.Edition,
                        sequence,
                        (int)response.StatusCode,
                        responseHash);
                    if (!FixedTimeEquals(parsedProof, ComputeHmac(session.SessionToken, responseCanonical)))
                    {
                        return Error(request.ModelName, "companion_response_invalid");
                    }

                    return ParseResult(responseText, request.ModelName);
                }
            }
            catch (WebException exception)
            {
                if (IsCancelled()) return Error(request.ModelName, "cancelled");
                return Error(request.ModelName, IsTimeout(exception) ? "timeout" : "companion_unavailable");
            }
            catch (IOException)
            {
                return Error(request.ModelName, IsCancelled() ? "cancelled" : "companion_unavailable");
            }
            catch
            {
                return Error(request.ModelName, IsCancelled() ? "cancelled" : "companion_request_failed");
            }
            finally
            {
                lock (_sync)
                {
                    _activeRequest = null;
                    _activeSession = null;
                }
                session.Dispose();
            }
        }

        internal void Cancel()
        {
            HttpWebRequest request;
            CompanionSession session;
            lock (_sync)
            {
                _cancelled = true;
                request = _activeRequest;
                session = _activeSession;
            }

            if (request != null)
            {
                try { request.Abort(); }
                catch { }
            }
            if (session != null)
            {
                session.Dispose();
            }
        }

        private bool IsCancelled()
        {
            lock (_sync) return _cancelled;
        }

        private static bool IsTimeout(WebException exception)
        {
            if (exception.Status == WebExceptionStatus.Timeout)
            {
                return true;
            }

            HttpWebResponse response = exception.Response as HttpWebResponse;
            if (response == null)
            {
                return false;
            }

            using (response)
            {
                return response.StatusCode == HttpStatusCode.GatewayTimeout;
            }
        }

        private static byte[] SerializeRequest(LocalAiRequest request)
        {
            var lines = new List<string>
            {
                "schemaVersion=1",
                "model=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ModelName ?? string.Empty)),
                "question=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Question ?? string.Empty))
            };

            if (request.Snapshot == null) throw new InvalidOperationException();
            if (request.IncludeGameDate) lines.Add("gameDate=" + request.Snapshot.GameDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (request.IncludePopulation) lines.Add("population=" + request.Snapshot.Population.ToString(CultureInfo.InvariantCulture));
            if (request.IncludeFundsCents) lines.Add("fundsCents=" + request.Snapshot.FundsCents.ToString(CultureInfo.InvariantCulture));
            if (request.IncludeResidentialDemand) lines.Add("residentialDemand=" + request.Snapshot.ResidentialDemand.ToString(CultureInfo.InvariantCulture));
            if (request.IncludeCommercialDemand) lines.Add("commercialDemand=" + request.Snapshot.CommercialDemand.ToString(CultureInfo.InvariantCulture));
            if (request.IncludeWorkplaceDemand) lines.Add("workplaceDemand=" + request.Snapshot.WorkplaceDemand.ToString(CultureInfo.InvariantCulture));

            return new UTF8Encoding(false, true).GetBytes(string.Join("\n", lines.ToArray()));
        }

        private static LocalAiResult ParseResult(string text, string expectedModel)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int separator = lines[i].IndexOf('=');
                if (separator <= 0) return Error(expectedModel, "companion_response_invalid");
                string key = lines[i].Substring(0, separator);
                if (values.ContainsKey(key)) return Error(expectedModel, "companion_response_invalid");
                values.Add(key, lines[i].Substring(separator + 1));
            }

            if (values.Count != 4 ||
                !values.TryGetValue("status", out string status64) ||
                !values.TryGetValue("model", out string model64) ||
                !values.TryGetValue("content", out string content64) ||
                !values.TryGetValue("errorCode", out string error64))
            {
                return Error(expectedModel, "companion_response_invalid");
            }

            string status;
            string model;
            string content;
            string errorCode;
            if (!TryDecode(status64, out status) || !TryDecode(model64, out model) ||
                !TryDecode(content64, out content) || !TryDecode(error64, out errorCode) ||
                !string.Equals(model, expectedModel, StringComparison.Ordinal))
            {
                return Error(expectedModel, "companion_response_invalid");
            }

            if (status == "ok" && string.IsNullOrEmpty(errorCode))
            {
                return new LocalAiResult { Success = true, ModelName = model, Content = content, ErrorCode = string.Empty };
            }
            if (status == "error" && IsKnownErrorCode(errorCode))
            {
                return Error(model, errorCode);
            }
            return Error(expectedModel, "companion_response_invalid");
        }

        private static bool IsKnownErrorCode(string value)
        {
            return value == "model_not_found" || value == "local_model_required" || value == "local_authentication_failed" ||
                value == "ollama_error" || value == "ollama_unavailable" || value == "invalid_response" ||
                value == "cancelled" || value == "timeout";
        }

        private static bool TryDecode(string value, out string decoded)
        {
            decoded = string.Empty;
            try { decoded = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(value)); return true; }
            catch { return false; }
        }

        private static string BuildRequestCanonical(string edition, ulong sequence, string bodyHash)
        {
            return string.Join("\n", new[]
            {
                CompanionBuildInfo.ProtocolVersion,
                edition,
                sequence.ToString(CultureInfo.InvariantCulture),
                "POST",
                "/v1/ollama/local/chat",
                bodyHash.ToLowerInvariant()
            });
        }

        private static string BuildResponseCanonical(string edition, ulong sequence, int status, string bodyHash)
        {
            return string.Join("\n", new[]
            {
                CompanionBuildInfo.ProtocolVersion,
                edition,
                sequence.ToString(CultureInfo.InvariantCulture),
                status.ToString(CultureInfo.InvariantCulture),
                "POST",
                "/v1/ollama/local/chat",
                bodyHash.ToLowerInvariant()
            });
        }

        private static byte[] ComputeHmac(byte[] secret, string value)
        {
            using (HMACSHA256 hmac = new HMACSHA256(secret)) return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        }

        private static byte[] ComputeSha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create()) return sha256.ComputeHash(value);
        }

        private static string ToHex(byte[] value)
        {
            var text = new StringBuilder(value.Length * 2);
            for (int i = 0; i < value.Length; i++) text.Append(value[i].ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        private static bool TryParseHex(string text, out byte[] value)
        {
            value = new byte[0];
            if (string.IsNullOrEmpty(text) || text.Length != 64) return false;
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = HexValue(text[i * 2]);
                int low = HexValue(text[i * 2 + 1]);
                if (high < 0 || low < 0) return false;
                bytes[i] = (byte)((high << 4) | low);
            }
            value = bytes;
            return true;
        }

        private static int HexValue(char value)
        {
            if (value >= '0' && value <= '9') return value - '0';
            if (value >= 'a' && value <= 'f') return value - 'a' + 10;
            if (value >= 'A' && value <= 'F') return value - 'A' + 10;
            return -1;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static LocalAiResult Error(string model, string code)
        {
            return new LocalAiResult { Success = false, ModelName = model ?? string.Empty, Content = string.Empty, ErrorCode = code };
        }
    }
}
// Implementation update signed by OpenAI-Codex.
